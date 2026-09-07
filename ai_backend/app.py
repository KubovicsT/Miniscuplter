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
from job_progress import begin as begin_job, bind as bind_job, report as report_job, complete as complete_job, fail as fail_job, current as current_job

APP_VERSION = "1.0.17"
app = FastAPI(title="Miniscuplter AI Backend", version=APP_VERSION)
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
    return {
        "ok": True,
        "version": APP_VERSION,
        "routing": routing_status(),
        "geometry_provider": "trimesh-voxel + model-analysis + transactional-detail-union",
        "rig_provider": "adaptive-quick + optional-universal-command",
        "smart_select_provider": "local-clipseg-or-geometry",
        "components": component_status(),
    }


@app.get("/routing")
def routing():
    return routing_status()


@app.get("/components")
def components():
    return component_status()


@app.get("/job-progress/current")
def job_progress_current():
    return current_job()


@app.post("/components/install")
def install(req: ComponentRequest):
    try:
        release_all_models()
        return install_component(req.id)
    except Exception as e:
        raise HTTPException(500, f"Component installation failed: {e}") from e


@app.post("/components/uninstall")
def uninstall(req: ComponentRequest):
    try:
        release_all_models()
        return uninstall_component(req.id)
    except Exception as e:
        raise HTTPException(500, f"Component removal failed: {e}") from e


@app.post("/release-models")
def release_models():
    release_all_models()
    return {"ok": True}


def _write_b64_image(data, out):
    if "," in data:
        data = data.split(",", 1)[1]
    p = Path(out).resolve()
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(base64.b64decode(data))
    return str(p)


def _image_generate(provider, req):
    if provider == "sdxl":
        return __import__("sdxl_image", fromlist=["generate_concept"]).generate_concept(req.prompt, req.output_path)
    if provider == "flux":
        return __import__("flux_klein", fromlist=["generate_concept"]).generate_concept(req.prompt, req.output_path)
    if provider == "sd21":
        return __import__("local_image", fromlist=["generate_concept"]).generate_concept(req.prompt, req.output_path, req.quality)
    if provider in {"zimage", "qwen"}:
        cid = {"zimage": "z-image-turbo", "qwen": "qwen-image-2512"}[provider]
        return __import__("modern_image", fromlist=["generate"]).generate(cid, req.prompt, req.output_path)
    raise RuntimeError(f"Unsupported image provider: {provider}")


def _image_edit(provider, req):
    if provider == "sdxl":
        return __import__("sdxl_image", fromlist=["edit_image"]).edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, detail=req.detail)
    if provider == "flux":
        return __import__("flux_klein", fromlist=["edit_image"]).edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, detail=req.detail)
    if provider == "sd21":
        return __import__("local_image", fromlist=["edit_image"]).edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, req.quality)
    if provider == "qwen-edit":
        return __import__("modern_image", fromlist=["edit"]).edit("qwen-image-edit", req.image_path, req.mask_path, req.prompt, req.output_path)
    raise RuntimeError(f"Unsupported image edit provider: {provider}")


@app.post("/generate-concept")
def generate_concept(req: ConceptRequest):
    begin_job("2d-generate")
    try:
        report_job("resolving_provider", "Choosing the installed local image model for this hardware and route.", 5)
        d = choose_image_provider("generate", req.provider)
        report_job("preparing_runtime", "Freeing previously loaded models before the selected provider is prepared.", 10, d.provider)
        release_all_models()
        report_job("loading_model", f"Preparing {d.provider} model weights and runtime.", 18, d.provider)
        path = _image_generate(d.provider, req)
        report_job("validating_output", "Image inference returned; verifying the saved result.", 97, d.provider)
        if not Path(path).is_file() or Path(path).stat().st_size <= 0:
            raise RuntimeError("Image provider returned no usable output file")
        complete_job("Concept image saved and verified.", d.provider)
        return {"path": path, "provider": d.provider, "routing_reason": d.reason, "quality": req.quality}
    except Exception as e:
        fail_job(f"2D image provider failed: {e}")
        raise HTTPException(502, f"2D image provider failed: {e}") from e
    finally:
        bind_job(None)


@app.post("/edit-image")
def edit_image(req: EditRequest):
    begin_job("2d-edit")
    try:
        report_job("resolving_provider", "Choosing the local image-edit model.", 5)
        d = choose_image_provider("detail" if req.detail else "edit", req.provider)
        report_job("preparing_runtime", "Freeing previously loaded models and preparing image-edit memory.", 10, d.provider)
        release_all_models()
        report_job("loading_model", f"Preparing {d.provider} for image editing.", 18, d.provider)
        path = _image_edit(d.provider, req)
        report_job("validating_output", "Edit inference returned; verifying the saved image.", 97, d.provider)
        if not Path(path).is_file() or Path(path).stat().st_size <= 0:
            raise RuntimeError("Image-edit provider returned no usable output file")
        complete_job("Edited image saved and verified.", d.provider)
        return {"path": path, "provider": d.provider, "routing_reason": d.reason, "quality": req.quality}
    except Exception as e:
        fail_job(f"2D image edit provider failed: {e}")
        raise HTTPException(502, f"2D image edit provider failed: {e}") from e
    finally:
        bind_job(None)


def _generate_shape(provider, req, image, output):
    if provider == "hunyuan":
        return __import__("hunyuan_shape", fromlist=["generate_shape"]).generate_shape(image, output, req.prompt, req.quality)
    if provider == "triposr":
        return __import__("triposr_shape", fromlist=["generate_shape"]).generate_shape(image, output, mc_resolution=192 if req.role in {"fast", "rough", "draft"} else 320)
    s = __import__("specialist_3d_v105", fromlist=["x"])
    if provider == "sf3d":
        return s.generate_sf3d(image, output)
    if provider == "spar3d":
        return s.generate_spar3d(image, output, low_vram=int(__import__("model_manager").hardware_info().get("vram_mb", 0)) < 10000)
    if provider == "hunyuan-mini":
        return s.generate_hunyuan_mini(image, output)
    if provider == "trellis2":
        return s.generate_trellis2(image, output)
    raise RuntimeError(f"Provider {provider} is not a single-mesh generator")


@app.post("/generate-3d")
def generate_3d(req: Generate3DRequest):
    begin_job("3d-generate")
    image = str(Path(req.image_path).resolve())
    output = str(Path(req.output_path).resolve())
    Path(output).parent.mkdir(parents=True, exist_ok=True)
    provider = None
    try:
        report_job("resolving_provider", "Choosing the local 3D reconstruction provider.", 5)
        d = choose_3d_provider(req.role, req.provider)
        provider = d.provider
        report_job("preparing_runtime", "Releasing other models and reserving resources for 3D reconstruction.", 10, provider)
        release_all_models()
        try:
            report_job("loading_model", f"Preparing {provider} model weights and runtime.", 18, provider)
            path = _generate_shape(provider, req, image, output)
            reason = d.reason
            fallback_from = None
        except Exception as primary:
            if (req.provider or "auto").lower() != "auto" or not d.fallback or d.fallback == d.provider:
                raise
            release_all_models()
            provider = d.fallback
            report_job("loading_model", f"Primary provider failed ({primary}). Preparing automatic fallback {provider}.", 20, provider)
            try:
                path = _generate_shape(provider, req, image, output)
                reason = f"{d.reason}; {d.provider} failed ({primary}); automatic fallback to {provider}"
                fallback_from = d.provider
            except Exception as fallback:
                raise RuntimeError(f"Auto 3D route failed. Primary {d.provider}: {primary}. Fallback {provider}: {fallback}") from fallback

        report_job("validating_output", "3D provider returned; verifying the generated mesh file.", 96, provider)
        if not Path(path).is_file() or Path(path).stat().st_size <= 0:
            raise RuntimeError("3D provider returned no usable mesh file")
        report_job("cleanup", "Releasing 3D model resources before returning control to the editor.", 99, provider)
        release_all_models()
        complete_job("Generated mesh saved and verified.", provider)
        result = {"path": path, "provider": provider, "routing_reason": reason, "role": req.role, "quality": req.quality}
        if fallback_from:
            result["fallback_from"] = fallback_from
        return result
    except Exception as e:
        fail_job(f"3D provider failed: {e}")
        release_all_models()
        raise HTTPException(502, f"3D provider failed: {e}") from e
    finally:
        bind_job(None)


@app.post("/generate-parts")
def generate_parts(req: GeneratePartsRequest):
    try:
        d = choose_3d_provider("structured", req.provider)
        release_all_models()
        if d.provider == "partcrafter":
            r = __import__("partcrafter_shape", fromlist=["generate_parts"]).generate_parts(req.image_path, req.output_dir, req.num_parts, req.tag)
        elif d.provider == "partpacker":
            r = __import__("specialist_3d_v105", fromlist=["generate_partpacker"]).generate_partpacker(req.image_path, req.output_dir, req.tag)
        else:
            raise RuntimeError("Selected provider does not generate structured parts")
        r["routing_reason"] = d.reason
        return r
    except Exception as e:
        raise HTTPException(502, f"Structured 3D generation failed: {e}") from e
    finally:
        release_all_models()


@app.post("/detail-2d")
def detail_2d_route(req: Detail2DRequest):
    begin_job("2d-detail")
    try:
        report_job("resolving_provider", "Choosing the local detail/edit provider for the selected region.", 5)
        report_job("preparing_inputs", "Preparing the selected mask and surrounding image context.", 12)
        result = detail_2d(req.image_path, req.mask_path, req.prompt, req.output_path, req.image_provider)
        provider = str(result.get("provider") or "")
        report_job("validating_output", "Regional enhancement returned; verifying the saved image.", 97, provider)
        path = str(result.get("path") or "")
        if not path or not Path(path).is_file() or Path(path).stat().st_size <= 0:
            raise RuntimeError("Detail provider returned no usable output image")
        complete_job("Context-aware regional result saved and verified.", provider)
        return result
    except Exception as e:
        fail_job(f"2D detail refinement failed: {e}")
        raise HTTPException(502, f"2D detail refinement failed: {e}") from e
    finally:
        bind_job(None)


@app.post("/detail-3d")
def detail_3d_route(req: Detail3DRequest):
    try:
        return detail_3d(req.source_mesh, req.image_path, req.mask_path, req.prompt, req.bounds_min, req.bounds_max, req.output_patch, req.output_image, req.output_crop, req.image_provider, req.three_d_provider)
    except Exception as e:
        raise HTTPException(502, f"3D detail refinement failed: {e}") from e
    finally:
        release_all_models()


@app.post("/detail-apply")
def detail_apply_route(req: DetailApplyRequest):
    try:
        return apply_detail(req.source_mesh, req.patch_mesh, req.output_path, req.voxel_size)
    except Exception as e:
        raise HTTPException(502, f"Detail apply failed: {e}") from e


@app.post("/semantic-select")
def semantic_select_route(req: SemanticSelectRequest):
    try:
        return semantic_select(req.input_path, req.query)
    except Exception as e:
        raise HTTPException(502, f"Smart Select failed: {e}") from e


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=7868, log_level="info")
