from __future__ import annotations

from pathlib import Path
from typing import Optional

from PIL import Image, ImageFilter

from model_manager import component_path, hardware_info

_PIPE = None
_IMG_PIPE = None


def _torch_and_model():
    try:
        import torch
        from diffusers import StableDiffusionPipeline, StableDiffusionImg2ImgPipeline, EulerDiscreteScheduler
    except Exception as exc:
        raise RuntimeError(
            "Local 2D AI dependencies are not installed. Run setup_ai_backend.bat and install the SD 2.1 component from Miniscuplter."
        ) from exc
    model = component_path("sd21")
    if model is None:
        raise RuntimeError("Stable Diffusion 2.1 is not installed. Use AI Components -> Install 2D AI.")
    return torch, StableDiffusionPipeline, StableDiffusionImg2ImgPipeline, EulerDiscreteScheduler, model


def _configure(pipe, torch):
    hw = hardware_info()
    if torch.cuda.is_available():
        pipe.enable_attention_slicing()
        # GTX 1080 / 8 GB and similar cards are treated conservatively.
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


def generate_concept(prompt: str, output_path: str) -> str:
    pipe = _text_pipe()
    result = pipe(
        prompt=prompt,
        negative_prompt="blurry, low detail, text, watermark, cropped, malformed anatomy",
        num_inference_steps=24,
        guidance_scale=7.0,
        width=512,
        height=512,
    ).images[0]
    out = Path(output_path)
    out.parent.mkdir(parents=True, exist_ok=True)
    result.save(out)
    return str(out)


def edit_image(image_path: str, mask_path: Optional[str], prompt: str, output_path: str) -> str:
    pipe = _img_pipe()
    original = Image.open(image_path).convert("RGB")
    # SD 2.1 base is a 512 model; preserve the original resolution after generation.
    work = original.resize((512, 512), Image.Resampling.LANCZOS)
    result = pipe(
        prompt=prompt,
        negative_prompt="blurry, low detail, text, watermark, malformed anatomy",
        image=work,
        strength=0.58,
        guidance_scale=7.0,
        num_inference_steps=24,
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
