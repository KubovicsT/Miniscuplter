from __future__ import annotations

import base64
import os
import shlex
import subprocess
from pathlib import Path
from typing import Optional

import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from model_manager import install_component, uninstall_component, status as component_status, component_path
from geometry_api import router as geometry_router
from rig_api import router as rig_router
from semantic_select import semantic_select, SMART_SELECT_COMMAND, release_model as release_smart_select
from model_router import choose_image_provider, choose_3d_provider, routing_status, release_all_models
from detail_pipeline import detail_2d, detail_3d, apply_detail

app = FastAPI(title="Miniscuplter AI Backend", version="1.0.0")
app.include_router(geometry_router)
app.include_router(rig_router)

SD_WEBUI_URL = os.getenv("MINISCULPTER_SD_URL", "").rstrip("/")
THREED_COMMAND = os.getenv("MINISCULPTER_3D_COMMAND", "")


class ConceptRequest(BaseModel):
    prompt: str
    output_path: str
    quality: str = "standard"
    provider: str = "auto"


class EditRequest(BaseModel):
    image_path: str
    mask_path: Optional[str] = None
    prompt: str
    output_path: str
    quality: str = "standard"
    provider: str = "auto"
    detail: bool = False


class Generate3DRequest(BaseModel):
    image_path: str
    prompt: str = ""
    output_path: str
    quality: str = "standard"
    provider: str = "auto"
    role: str = "quality"


class GeneratePartsRequest(BaseModel):
    image_path: str
    output_dir: str
    num_parts: int = Field(default=4, ge=1, le=16)
    tag: str = "miniscuplter"
    provider: str = "auto"


class ComponentRequest(BaseModel):
    id: str


class SemanticSelectRequest(BaseModel):
    input_path: str
    query: str


class Detail2DRequest(BaseModel):
    image_path: str
    mask_path: str
    prompt: str
    output_path: str
    image_provider: str = "auto"


class Detail3DRequest(BaseModel):
    source_mesh: str
    image_path: str
    mask_path: str
    prompt: str
    bounds_min: list[float]
    bounds_max: list[float]
    output_patch: str
    output_image: str
    output_crop: str
    image_provider: str = "auto"
    three_d_provider: str = "auto"


class DetailApplyRequest(BaseModel):
    source_mesh: str
    patch_mesh: str
    output_path: str
    voxel_size: Optional[float] = None


@app.get("/health")
def health():
    local_select = component_path("clipseg-smart-select") is not None
    return {
        "ok": True,
        "version": "1.0.0",
        "routing": routing_status(),
        "geometry_provider": "trimesh-voxel + model-analysis + transactional-detail-union",
        "rig_provider": "adaptive-quick + optional-universal-command",
        "smart_select_provider": "external-ai-command" if SMART_SELECT_COMMAND else ("local-clipseg-multiview" if local_select else "geometry-semantic-fallback"),
        "internet": True,
        "components": component_status(),
    }


@app.get("/routing")
def routing():
    return routing_status()


@app.get("/components")
def components():
    return component_status()


@app.post("/components/install")
def install(req: ComponentRequest):
    try:
        release_all_models()
        return install_component(req.id)
    except Exception as exc:
        raise HTTPException(500, f"Component installation failed: {exc}") from exc


@app.post("/components/uninstall")
def uninstall(req: ComponentRequest):
    try:
        release_all_models()
        if req.id == "clipseg-smart-select": release_smart_select()
        return uninstall_component(req.id)
    except Exception as exc:
        raise HTTPException(500, f"Component removal failed: {exc}") from exc


@app.post("/release-models")
def release_models():
    release_all_models()
    return {"ok": True}


def _write_b64_image(data: str, output_path: str) -> str:
    if "," in data: data = data.split(",", 1)[1]
    p = Path(output_path).resolve(); p.parent.mkdir(parents=True, exist_ok=True); p.write_bytes(base64.b64decode(data))
    if p.stat().st_size == 0: raise RuntimeError("Image provider produced an empty output file")
    return str(p)


def _a1111_concept(req: ConceptRequest) -> str:
    from quality_runtime import get_config
    cfg = get_config(); size = int(cfg["image_size"])
    payload = {"prompt": req.prompt, "negative_prompt": "blurry, low detail, text, watermark", "steps": int(cfg["image_steps"]), "width": size, "height": size,
               "cfg_scale": float(cfg["image_guidance"]), "sampler_name": "DPM++ 2M Karras"}
    r = requests.post(f"{SD_WEBUI_URL}/sdapi/v1/txt2img", json=payload, timeout=900); r.raise_for_status(); images = r.json().get("images", [])
    if not images: raise RuntimeError("Image provider returned no images")
    return _write_b64_image(images[0], req.output_path)


def _a1111_edit(req: EditRequest) -> str:
    from quality_runtime import get_config
    image_path = Path(req.image_path).resolve()
    if not image_path.exists(): raise RuntimeError(f"Input image does not exist: {image_path}")
    cfg = get_config(); size = int(cfg["image_size"])
    payload = {"prompt": req.prompt, "negative_prompt": "blurry, low detail, text, watermark", "init_images": [base64.b64encode(image_path.read_bytes()).decode("ascii")],
               "denoising_strength": float(cfg["image_edit_strength"]), "steps": int(cfg["image_steps"]), "cfg_scale": float(cfg["image_guidance"]), "width": size, "height": size}
    if req.mask_path and Path(req.mask_path).exists():
        payload["mask"] = base64.b64encode(Path(req.mask_path).read_bytes()).decode("ascii"); payload["inpainting_fill"] = 1; payload["inpaint_full_res"] = True
    r = requests.post(f"{SD_WEBUI_URL}/sdapi/v1/img2img", json=payload, timeout=900); r.raise_for_status(); images = r.json().get("images", [])
    if not images: raise RuntimeError("Image provider returned no images")
    return _write_b64_image(images[0], req.output_path)


def _image_generate(provider: str, req: ConceptRequest) -> str:
    if provider == "sdxl":
        from sdxl_image import generate_concept; return generate_concept(req.prompt, req.output_path)
    if provider == "flux":
        from flux_klein import generate_concept; return generate_concept(req.prompt, req.output_path)
    if provider == "sd21":
        from local_image import generate_concept; return generate_concept(req.prompt, req.output_path, req.quality)
    raise RuntimeError(f"Unsupported image provider: {provider}")


def _image_edit(provider: str, req: EditRequest) -> str:
    if provider == "sdxl":
        from sdxl_image import edit_image; return edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, detail=req.detail)
    if provider == "flux":
        from flux_klein import edit_image; return edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, detail=req.detail)
    if provider == "sd21":
        from local_image import edit_image; return edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, req.quality)
    raise RuntimeError(f"Unsupported image provider: {provider}")


@app.post("/generate-concept")
def generate_concept(req: ConceptRequest):
    try:
        if req.provider == "automatic1111" and SD_WEBUI_URL: return {"path": _a1111_concept(req), "provider": "automatic1111"}
        decision = choose_image_provider("generate", req.provider); release_all_models()
        return {"path": _image_generate(decision.provider, req), "provider": decision.provider, "routing_reason": decision.reason, "quality": req.quality}
    except Exception as exc:
        if req.provider == "auto" and SD_WEBUI_URL:
            try: return {"path": _a1111_concept(req), "provider": "automatic1111", "routing_reason": "local model route failed; explicit external fallback"}
            except Exception: pass
        raise HTTPException(502, f"2D image provider failed: {exc}") from exc


@app.post("/edit-image")
def edit_image(req: EditRequest):
    try:
        if req.provider == "automatic1111" and SD_WEBUI_URL: return {"path": _a1111_edit(req), "provider": "automatic1111"}
        decision = choose_image_provider("detail" if req.detail else "edit", req.provider); release_all_models()
        return {"path": _image_edit(decision.provider, req), "provider": decision.provider, "routing_reason": decision.reason, "quality": req.quality}
    except Exception as exc:
        if req.provider == "auto" and SD_WEBUI_URL:
            try: return {"path": _a1111_edit(req), "provider": "automatic1111", "routing_reason": "local model route failed; external fallback"}
            except Exception: pass
        raise HTTPException(502, f"2D image edit provider failed: {exc}") from exc


def _generate_shape(provider: str, req: Generate3DRequest, image: str, output: str) -> str:
    if provider == "hunyuan":
        from hunyuan_shape import generate_shape; return generate_shape(image, output, req.prompt, req.quality)
    if provider == "triposr":
        resolution = 192 if req.role in {"fast", "rough", "draft"} else 320
        return __import__("triposr_shape", fromlist=["generate_shape"]).generate_shape(image, output, mc_resolution=resolution)
    raise RuntimeError(f"Provider {provider} is not a single-mesh generator")


def _shell_arg(value: str) -> str:
    """Quote values inserted into an explicitly user-configured shell command template."""
    if os.name == "nt":
        return subprocess.list2cmdline([value])
    return shlex.quote(value)


@app.post("/generate-3d")
def generate_3d(req: Generate3DRequest):
    image = str(Path(req.image_path).resolve()); output = str(Path(req.output_path).resolve()); Path(output).parent.mkdir(parents=True, exist_ok=True)
    try:
        decision = choose_3d_provider(req.role, req.provider); release_all_models()
        if decision.provider == "partcrafter": raise RuntimeError("PartCrafter returns multiple parts; use /generate-parts")
        path = _generate_shape(decision.provider, req, image, output)
        return {"path": path, "provider": decision.provider, "routing_reason": decision.reason, "role": req.role, "quality": req.quality}
    except Exception as exc:
        if req.provider == "auto" and THREED_COMMAND:
            try:
                command = THREED_COMMAND.format(image=_shell_arg(image), output=_shell_arg(output), prompt=_shell_arg(req.prompt))
                completed = subprocess.run(command, shell=True, capture_output=True, text=True, timeout=3600)
                if completed.returncode == 0 and Path(output).exists() and Path(output).stat().st_size > 0:
                    return {"path": output, "provider": "command", "role": req.role}
            except Exception:
                pass
        raise HTTPException(502, f"3D provider failed: {exc}") from exc
    finally:
        release_all_models()


@app.post("/generate-parts")
def generate_parts(req: GeneratePartsRequest):
    try:
        decision = choose_3d_provider("structured", req.provider); release_all_models()
        if decision.provider != "partcrafter": raise RuntimeError("PartCrafter is required for structured part generation")
        from partcrafter_shape import generate_parts as run
        result = run(req.image_path, req.output_dir, req.num_parts, req.tag); result["routing_reason"] = decision.reason; return result
    except Exception as exc:
        raise HTTPException(502, f"Structured 3D generation failed: {exc}") from exc
    finally:
        release_all_models()


@app.post("/detail-2d")
def detail_2d_route(req: Detail2DRequest):
    try: return detail_2d(req.image_path, req.mask_path, req.prompt, req.output_path, req.image_provider)
    except Exception as exc: raise HTTPException(502, f"2D detail refinement failed: {exc}") from exc


@app.post("/detail-3d")
def detail_3d_route(req: Detail3DRequest):
    try:
        if len(req.bounds_min) != 3 or len(req.bounds_max) != 3: raise ValueError("Selection bounds must contain exactly 3 coordinates")
        return detail_3d(req.source_mesh, req.image_path, req.mask_path, req.prompt, req.bounds_min, req.bounds_max, req.output_patch, req.output_image, req.output_crop, req.image_provider, req.three_d_provider)
    except Exception as exc: raise HTTPException(502, f"3D detail refinement failed: {exc}") from exc
    finally: release_all_models()


@app.post("/detail-apply")
def detail_apply_route(req: DetailApplyRequest):
    try: return apply_detail(req.source_mesh, req.patch_mesh, req.output_path, req.voxel_size)
    except MemoryError as exc: raise HTTPException(413, f"Detail apply memory guard stopped the job: {exc}") from exc
    except Exception as exc: raise HTTPException(502, f"Detail apply failed: {exc}") from exc


@app.post("/semantic-select")
def semantic_select_route(req: SemanticSelectRequest):
    try: return semantic_select(req.input_path, req.query)
    except Exception as exc: raise HTTPException(502, f"Smart Select failed: {exc}") from exc


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=7868, log_level="info")
