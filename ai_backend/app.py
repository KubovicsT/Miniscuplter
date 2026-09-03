from __future__ import annotations

import base64
import os
import subprocess
from pathlib import Path
from typing import Optional

import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from model_manager import install_component, uninstall_component, status as component_status, component_path

app = FastAPI(title="Miniscuplter AI Backend", version="0.2.0")

SD_WEBUI_URL = os.getenv("MINISCULPTER_SD_URL", "").rstrip("/")
THREED_COMMAND = os.getenv("MINISCULPTER_3D_COMMAND", "")


class ConceptRequest(BaseModel):
    prompt: str
    output_path: str


class EditRequest(BaseModel):
    image_path: str
    mask_path: Optional[str] = None
    prompt: str
    output_path: str


class Generate3DRequest(BaseModel):
    image_path: str
    prompt: str = ""
    output_path: str


class ComponentRequest(BaseModel):
    id: str


@app.get("/health")
def health():
    local_image = component_path("sd21") is not None
    local_3d = component_path("hunyuan21-shape") is not None
    return {
        "ok": True,
        "version": "0.2.0",
        "image_provider": "local-sd21" if local_image else ("automatic1111" if SD_WEBUI_URL else "not-configured"),
        "three_d_provider": "hunyuan3d-2.1" if local_3d else ("command" if THREED_COMMAND else "not-configured"),
        "internet": True,
        "components": component_status(),
    }


@app.get("/components")
def components():
    return component_status()


@app.post("/components/install")
def install(req: ComponentRequest):
    try:
        return install_component(req.id)
    except Exception as exc:
        raise HTTPException(500, f"Component installation failed: {exc}") from exc


@app.post("/components/uninstall")
def uninstall(req: ComponentRequest):
    try:
        return uninstall_component(req.id)
    except Exception as exc:
        raise HTTPException(500, f"Component removal failed: {exc}") from exc


@app.post("/release-models")
def release_models():
    try:
        from local_image import release_models as release_image
        release_image()
    except Exception:
        pass
    try:
        from hunyuan_shape import release_model as release_3d
        release_3d()
    except Exception:
        pass
    return {"ok": True}


def _write_b64_image(data: str, output_path: str) -> str:
    if "," in data:
        data = data.split(",", 1)[1]
    p = Path(output_path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(base64.b64decode(data))
    return str(p)


def _a1111_concept(req: ConceptRequest) -> str:
    payload = {
        "prompt": req.prompt,
        "negative_prompt": "blurry, low detail, text, watermark",
        "steps": 24,
        "width": 512,
        "height": 512,
        "cfg_scale": 6.5,
        "sampler_name": "DPM++ 2M Karras",
    }
    r = requests.post(f"{SD_WEBUI_URL}/sdapi/v1/txt2img", json=payload, timeout=600)
    r.raise_for_status()
    images = r.json().get("images", [])
    if not images:
        raise RuntimeError("Image provider returned no images")
    return _write_b64_image(images[0], req.output_path)


def _a1111_edit(req: EditRequest) -> str:
    image_path = Path(req.image_path)
    if not image_path.exists():
        raise RuntimeError(f"Input image does not exist: {image_path}")
    payload = {
        "prompt": req.prompt,
        "negative_prompt": "blurry, low detail, text, watermark",
        "init_images": [base64.b64encode(image_path.read_bytes()).decode("ascii")],
        "denoising_strength": 0.55,
        "steps": 24,
        "cfg_scale": 6.5,
    }
    if req.mask_path and Path(req.mask_path).exists():
        payload["mask"] = base64.b64encode(Path(req.mask_path).read_bytes()).decode("ascii")
        payload["inpainting_fill"] = 1
        payload["inpaint_full_res"] = True
    r = requests.post(f"{SD_WEBUI_URL}/sdapi/v1/img2img", json=payload, timeout=600)
    r.raise_for_status()
    images = r.json().get("images", [])
    if not images:
        raise RuntimeError("Image provider returned no images")
    return _write_b64_image(images[0], req.output_path)


@app.post("/generate-concept")
def generate_concept(req: ConceptRequest):
    try:
        if component_path("sd21") is not None:
            from local_image import generate_concept as local_generate
            return {"path": local_generate(req.prompt, req.output_path), "provider": "local-sd21"}
        if SD_WEBUI_URL:
            return {"path": _a1111_concept(req), "provider": "automatic1111"}
        raise RuntimeError("No 2D generator is installed. Open AI Components in Miniscuplter and install Stable Diffusion 2.1.")
    except Exception as exc:
        raise HTTPException(502, f"2D image provider failed: {exc}") from exc


@app.post("/edit-image")
def edit_image(req: EditRequest):
    try:
        if component_path("sd21") is not None:
            from local_image import edit_image as local_edit
            return {"path": local_edit(req.image_path, req.mask_path, req.prompt, req.output_path), "provider": "local-sd21"}
        if SD_WEBUI_URL:
            return {"path": _a1111_edit(req), "provider": "automatic1111"}
        raise RuntimeError("No 2D generator is installed. Open AI Components in Miniscuplter and install Stable Diffusion 2.1.")
    except Exception as exc:
        raise HTTPException(502, f"2D image edit provider failed: {exc}") from exc


@app.post("/generate-3d")
def generate_3d(req: Generate3DRequest):
    image = str(Path(req.image_path).resolve())
    output = str(Path(req.output_path).resolve())
    Path(output).parent.mkdir(parents=True, exist_ok=True)
    try:
        if component_path("hunyuan21-shape") is not None:
            # Free the 2D model before loading Hunyuan on constrained GPUs.
            try:
                from local_image import release_models
                release_models()
            except Exception:
                pass
            from hunyuan_shape import generate_shape
            return {"path": generate_shape(image, output, req.prompt), "provider": "hunyuan3d-2.1"}

        if THREED_COMMAND:
            command = THREED_COMMAND.format(image=image, output=output, prompt=req.prompt.replace('"', '\\"'))
            completed = subprocess.run(command, shell=True, capture_output=True, text=True, timeout=3600)
            if completed.returncode != 0:
                raise RuntimeError(completed.stderr[-3000:])
            if not Path(output).exists():
                raise RuntimeError("3D provider completed but did not create the requested STL output.")
            return {"path": output, "provider": "command"}

        raise RuntimeError("No 3D generator is installed. Open AI Components in Miniscuplter and install Hunyuan3D 2.1 Shape.")
    except Exception as exc:
        raise HTTPException(502, f"3D provider failed: {exc}") from exc


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=7868, log_level="info")
