from __future__ import annotations

import base64
import json
import os
import shlex
import subprocess
from pathlib import Path
from typing import Optional

import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

app = FastAPI(title="Miniscuplter AI Backend", version="0.1.0")

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


@app.get("/health")
def health():
    return {
        "ok": True,
        "image_provider": "automatic1111" if SD_WEBUI_URL else "not-configured",
        "three_d_provider": "command" if THREED_COMMAND else "not-configured",
        "internet": True,
    }


def _write_b64_image(data: str, output_path: str) -> str:
    if "," in data:
        data = data.split(",", 1)[1]
    p = Path(output_path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(base64.b64decode(data))
    return str(p)


def _require_sd():
    if not SD_WEBUI_URL:
        raise HTTPException(
            503,
            "No 2D generator configured. Set MINISCULPTER_SD_URL to an Automatic1111/Forge-compatible API URL, e.g. http://127.0.0.1:7860."
        )


@app.post("/generate-concept")
def generate_concept(req: ConceptRequest):
    _require_sd()
    payload = {
        "prompt": req.prompt,
        "negative_prompt": "blurry, low detail, text, watermark",
        "steps": 24,
        "width": 768,
        "height": 768,
        "cfg_scale": 6.5,
        "sampler_name": "DPM++ 2M Karras",
    }
    try:
        r = requests.post(f"{SD_WEBUI_URL}/sdapi/v1/txt2img", json=payload, timeout=600)
        r.raise_for_status()
        images = r.json().get("images", [])
        if not images:
            raise RuntimeError("Image provider returned no images")
        return {"path": _write_b64_image(images[0], req.output_path)}
    except Exception as exc:
        raise HTTPException(502, f"2D image provider failed: {exc}") from exc


@app.post("/edit-image")
def edit_image(req: EditRequest):
    _require_sd()
    image_path = Path(req.image_path)
    if not image_path.exists():
        raise HTTPException(400, f"Input image does not exist: {image_path}")
    init = base64.b64encode(image_path.read_bytes()).decode("ascii")
    payload = {
        "prompt": req.prompt,
        "negative_prompt": "blurry, low detail, text, watermark",
        "init_images": [init],
        "denoising_strength": 0.55,
        "steps": 24,
        "cfg_scale": 6.5,
    }
    if req.mask_path:
        mp = Path(req.mask_path)
        if mp.exists():
            payload["mask"] = base64.b64encode(mp.read_bytes()).decode("ascii")
            payload["inpainting_fill"] = 1
            payload["inpaint_full_res"] = True
    try:
        r = requests.post(f"{SD_WEBUI_URL}/sdapi/v1/img2img", json=payload, timeout=600)
        r.raise_for_status()
        images = r.json().get("images", [])
        if not images:
            raise RuntimeError("Image provider returned no images")
        return {"path": _write_b64_image(images[0], req.output_path)}
    except Exception as exc:
        raise HTTPException(502, f"2D image edit provider failed: {exc}") from exc


@app.post("/generate-3d")
def generate_3d(req: Generate3DRequest):
    if not THREED_COMMAND:
        raise HTTPException(
            503,
            "No 3D generator configured. Set MINISCULPTER_3D_COMMAND to a command template containing {image}, {output}, and optionally {prompt}."
        )
    image = str(Path(req.image_path).resolve())
    output = str(Path(req.output_path).resolve())
    Path(output).parent.mkdir(parents=True, exist_ok=True)
    command = THREED_COMMAND.format(image=image, output=output, prompt=req.prompt.replace('"', '\\"'))
    try:
        completed = subprocess.run(command, shell=True, capture_output=True, text=True, timeout=3600)
    except Exception as exc:
        raise HTTPException(502, f"3D provider could not be launched: {exc}") from exc
    if completed.returncode != 0:
        raise HTTPException(502, f"3D provider failed: {completed.stderr[-3000:]}")
    if not Path(output).exists():
        raise HTTPException(502, "3D provider completed but did not create the requested STL output.")
    return {"path": output}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=7868, log_level="info")
