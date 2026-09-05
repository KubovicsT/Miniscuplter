from __future__ import annotations

from pathlib import Path
from typing import Optional

from PIL import Image, ImageFilter

from model_manager import component_path, hardware_info
from quality_runtime import get_config

_TXT = None
_IMG = None


def _deps():
    try:
        import torch
        from diffusers import StableDiffusionXLPipeline, StableDiffusionXLImg2ImgPipeline, EulerDiscreteScheduler
    except Exception as exc:
        raise RuntimeError("SDXL dependencies are unavailable. Re-run setup_ai_backend.bat.") from exc
    model = component_path("sdxl-base")
    if model is None: raise RuntimeError("SDXL Base is not installed")
    return torch, StableDiffusionXLPipeline, StableDiffusionXLImg2ImgPipeline, EulerDiscreteScheduler, model


def _configure(pipe, torch):
    hw = hardware_info(); vram = int(hw.get("vram_mb", 0) or 0)
    pipe.enable_attention_slicing()
    try: pipe.enable_vae_slicing()
    except Exception: pass
    try: pipe.enable_vae_tiling()
    except Exception: pass
    if torch.cuda.is_available():
        if vram <= 10240:
            try: pipe.enable_sequential_cpu_offload()
            except Exception:
                try: pipe.enable_model_cpu_offload()
                except Exception: pipe.to("cuda")
        else: pipe.to("cuda")
    else: pipe.to("cpu")
    pipe.set_progress_bar_config(disable=True)
    return pipe


def _text_pipe():
    global _TXT
    if _TXT is not None: return _TXT
    torch, Txt, _, Scheduler, model = _deps()
    dtype = torch.float16 if torch.cuda.is_available() else torch.float32
    scheduler = Scheduler.from_pretrained(str(model), subfolder="scheduler", local_files_only=True)
    _TXT = Txt.from_pretrained(str(model), scheduler=scheduler, torch_dtype=dtype,
                               variant="fp16", use_safetensors=True, local_files_only=True)
    return _configure(_TXT, torch)


def _img_pipe():
    global _IMG
    if _IMG is not None: return _IMG
    torch, _, Img, Scheduler, model = _deps()
    dtype = torch.float16 if torch.cuda.is_available() else torch.float32
    scheduler = Scheduler.from_pretrained(str(model), subfolder="scheduler", local_files_only=True)
    _IMG = Img.from_pretrained(str(model), scheduler=scheduler, torch_dtype=dtype,
                               variant="fp16", use_safetensors=True, local_files_only=True)
    return _configure(_IMG, torch)


def _size() -> int:
    cfg = get_config(); raw = int(cfg.get("image_size", 1024))
    return max(512, min(1536, (raw // 64) * 64))


def generate_concept(prompt: str, output_path: str) -> str:
    cfg = get_config(); size = _size(); pipe = _text_pipe()
    image = pipe(prompt=prompt,
                 negative_prompt="blurry, low detail, text, watermark, cropped, malformed anatomy",
                 width=size, height=size,
                 num_inference_steps=int(cfg["image_steps"]),
                 guidance_scale=float(cfg["image_guidance"])).images[0]
    out = Path(output_path); out.parent.mkdir(parents=True, exist_ok=True); image.save(out); return str(out)


def edit_image(image_path: str, mask_path: Optional[str], prompt: str, output_path: str, *, detail: bool = False) -> str:
    cfg = get_config(); pipe = _img_pipe(); source = Image.open(image_path).convert("RGB")
    size = _size(); mask = Image.open(mask_path).convert("L") if mask_path and Path(mask_path).exists() else None
    box = None
    if detail and mask is not None:
        bbox = mask.getbbox()
        if bbox:
            l,t,r,b = bbox; pad = max(24, int(max(r-l,b-t) * .35)); box = (max(0,l-pad), max(0,t-pad), min(source.width,r+pad), min(source.height,b+pad))
    work_src = source.crop(box) if box else source; work = work_src.resize((size,size), Image.Resampling.LANCZOS)
    strength = float(cfg["image_edit_strength"])
    if detail: strength = min(.72, max(.28, strength * .82))
    generated = pipe(prompt=prompt, negative_prompt="blurry, low detail, text, watermark, malformed anatomy",
                     image=work, strength=strength, guidance_scale=float(cfg["image_guidance"]),
                     num_inference_steps=int(cfg["image_steps"])).images[0]
    generated = generated.resize(work_src.size, Image.Resampling.LANCZOS)
    if box:
        result = source.copy(); local_mask = mask.crop(box).filter(ImageFilter.GaussianBlur(radius=max(2, int(min(work_src.size)*.012))))
        result.paste(Image.composite(generated, work_src, local_mask), box[:2])
    elif mask is not None:
        resized_mask = mask.resize(source.size, Image.Resampling.BILINEAR).filter(ImageFilter.GaussianBlur(radius=4))
        result = Image.composite(generated.resize(source.size, Image.Resampling.LANCZOS), source, resized_mask)
    else: result = generated.resize(source.size, Image.Resampling.LANCZOS)
    out=Path(output_path); out.parent.mkdir(parents=True, exist_ok=True); result.save(out); return str(out)


def release_models() -> None:
    global _TXT, _IMG
    _TXT=None; _IMG=None
    try:
        import torch
        if torch.cuda.is_available(): torch.cuda.empty_cache()
    except Exception: pass
