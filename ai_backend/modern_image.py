from __future__ import annotations

import gc
from pathlib import Path
from typing import Any

from model_manager import component_path

_PIPELINES: dict[str, Any] = {}


def _torch():
    import torch
    return torch


def _load(component: str, purpose: str):
    key = f"{component}:{purpose}"
    if key in _PIPELINES: return _PIPELINES[key]
    path = component_path(component)
    if path is None: raise RuntimeError(f"{component} is not installed")
    torch = _torch()
    from diffusers import DiffusionPipeline
    dtype = torch.bfloat16 if torch.cuda.is_available() and torch.cuda.is_bf16_supported() else torch.float16
    kwargs: dict[str, Any] = {"torch_dtype": dtype, "local_files_only": True}
    pipe = DiffusionPipeline.from_pretrained(str(path), **kwargs)
    if torch.cuda.is_available():
        try: pipe.enable_model_cpu_offload()
        except Exception: pipe.to("cuda")
    _PIPELINES[key] = pipe
    return pipe


def _save(image, output_path: str) -> str:
    p = Path(output_path).resolve(); p.parent.mkdir(parents=True, exist_ok=True); image.save(p)
    if not p.exists() or p.stat().st_size == 0: raise RuntimeError("Image model produced no output")
    return str(p)


def generate(component: str, prompt: str, output_path: str) -> str:
    pipe = _load(component, "generate")
    # Turbo models prefer few steps; Qwen benefits from a fuller schedule.
    steps = 9 if component == "z-image-turbo" else 30
    guidance = 0.0 if component == "z-image-turbo" else 4.0
    result = pipe(prompt=prompt, num_inference_steps=steps, guidance_scale=guidance)
    return _save(result.images[0], output_path)


def edit(component: str, image_path: str, mask_path: str | None, prompt: str, output_path: str) -> str:
    from PIL import Image
    pipe = _load(component, "edit")
    image = Image.open(image_path).convert("RGB")
    kwargs: dict[str, Any] = {"image": image, "prompt": prompt, "num_inference_steps": 30}
    # Qwen Image Edit accepts the source image directly. Mask support differs between model
    # revisions; preserve Miniscuplter's selection by compositing the edited result afterwards.
    result = pipe(**kwargs).images[0].convert("RGB").resize(image.size)
    if mask_path and Path(mask_path).exists():
        mask = Image.open(mask_path).convert("L").resize(image.size)
        result = Image.composite(result, image, mask)
    return _save(result, output_path)


def release_models() -> None:
    _PIPELINES.clear(); gc.collect()
    try:
        torch = _torch()
        if torch.cuda.is_available(): torch.cuda.empty_cache()
    except Exception: pass
