from __future__ import annotations

from pathlib import Path
from typing import Optional

from PIL import Image, ImageFilter

from model_manager import component_path, hardware_info
from quality_runtime import get_config

_PIPE = None
_IMG_PIPE = None

QUALITY = {
    "preview": {"steps": 12, "size": 384, "guidance": 6.5, "strength": 0.52},
    "standard": {"steps": 24, "size": 512, "guidance": 7.0, "strength": 0.58},
    "high": {"steps": 36, "size": 640, "guidance": 7.5, "strength": 0.62},
}


def _q(name: str):
    """Return active v1.0 runtime settings while preserving preview as a cheap candidate pass.

    The central preset remains authoritative for normal/high operations. A request explicitly marked
    preview is capped at the historical preview workload so four-candidate generation does not turn
    into four Ultra-quality jobs. It never raises a low custom preset above the user's selected values.
    """
    runtime = get_config()
    result = {
        "steps": int(runtime["image_steps"]),
        "size": int(runtime["image_size"]),
        "guidance": float(runtime["image_guidance"]),
        "strength": float(runtime["image_edit_strength"]),
    }
    if (str(name or "").lower() == "preview"):
        preview = QUALITY["preview"]
        result["steps"] = min(result["steps"], int(preview["steps"]))
        result["size"] = min(result["size"], int(preview["size"]))
        result["guidance"] = min(result["guidance"], float(preview["guidance"]))
        result["strength"] = min(result["strength"], float(preview["strength"]))
    return result


def _limit_input(image: Image.Image) -> Image.Image:
    max_px = int(get_config()["max_input_px"])
    if max(image.size) <= max_px:
        return image
    work = image.copy()
    work.thumbnail((max_px, max_px), Image.Resampling.LANCZOS)
    return work


def _torch_and_model():
    try:
        import torch
        from diffusers import StableDiffusionPipeline, StableDiffusionImg2ImgPipeline, EulerDiscreteScheduler
    except Exception as exc:
        raise RuntimeError(
            "Local 2D AI dependencies are not installed. Use Miniscuplter Launcher -> Repair AI Runtime, then install Stable Diffusion 2.1 if you want the legacy fallback."
        ) from exc
    model = component_path("sd21")
    if model is None:
        raise RuntimeError("Stable Diffusion 2.1 is not installed. Install it from Miniscuplter Launcher or the AI Models panel.")
    return torch, StableDiffusionPipeline, StableDiffusionImg2ImgPipeline, EulerDiscreteScheduler, model


def _configure(pipe, torch):
    hw = hardware_info()
    if torch.cuda.is_available():
        pipe.enable_attention_slicing()
        if hw.get("vram_mb", 0) <= 8192:
            try:
                pipe.enable_model_cpu_offload()
            except Exception:
                pipe.to("cuda")
        else:
            pipe.to("cuda")
    else:
        pipe.to("cpu")
    pipe.set_progress_bar_config(disable=True)
    return pipe


def _text_pipe():
    global _PIPE
    if _PIPE is not None:
        return _PIPE
    torch, Txt, _, Scheduler, model = _torch_and_model()
    dtype = torch.float16 if torch.cuda.is_available() else torch.float32
    scheduler = Scheduler.from_pretrained(str(model), subfolder="scheduler")
    _PIPE = Txt.from_pretrained(str(model), scheduler=scheduler, torch_dtype=dtype, safety_checker=None)
    return _configure(_PIPE, torch)


def _img_pipe():
    global _IMG_PIPE
    if _IMG_PIPE is not None:
        return _IMG_PIPE
    torch, _, Img, Scheduler, model = _torch_and_model()
    dtype = torch.float16 if torch.cuda.is_available() else torch.float32
    scheduler = Scheduler.from_pretrained(str(model), subfolder="scheduler")
    _IMG_PIPE = Img.from_pretrained(str(model), scheduler=scheduler, torch_dtype=dtype, safety_checker=None)
    return _configure(_IMG_PIPE, torch)


def generate_concept(prompt: str, output_path: str, quality: str = "standard") -> str:
    cfg = _q(quality)
    pipe = _text_pipe()
    result = pipe(
        prompt=prompt,
        negative_prompt="blurry, low detail, text, watermark, cropped, malformed anatomy",
        num_inference_steps=cfg["steps"],
        guidance_scale=cfg["guidance"],
        width=cfg["size"],
        height=cfg["size"],
    ).images[0]
    out = Path(output_path)
    out.parent.mkdir(parents=True, exist_ok=True)
    result.save(out)
    return str(out)


def edit_image(image_path: str, mask_path: Optional[str], prompt: str, output_path: str, quality: str = "standard") -> str:
    cfg = _q(quality)
    pipe = _img_pipe()
    original = _limit_input(Image.open(image_path).convert("RGB"))
    work = original.resize((cfg["size"], cfg["size"]), Image.Resampling.LANCZOS)
    result = pipe(
        prompt=prompt,
        negative_prompt="blurry, low detail, text, watermark, malformed anatomy",
        image=work,
        strength=cfg["strength"],
        guidance_scale=cfg["guidance"],
        num_inference_steps=cfg["steps"],
    ).images[0]

    if mask_path and Path(mask_path).exists():
        mask = Image.open(mask_path).convert("L").resize(original.size, Image.Resampling.BILINEAR)
        mask = mask.filter(ImageFilter.GaussianBlur(radius=4))
        generated = result.resize(original.size, Image.Resampling.LANCZOS)
        final = Image.composite(generated, original, mask)
    else:
        final = result.resize(original.size, Image.Resampling.LANCZOS)

    out = Path(output_path)
    out.parent.mkdir(parents=True, exist_ok=True)
    final.save(out)
    return str(out)


def release_models() -> None:
    global _PIPE, _IMG_PIPE
    _PIPE = None
    _IMG_PIPE = None
    try:
        import torch
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
    except Exception:
        pass
