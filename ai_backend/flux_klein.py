from __future__ import annotations

from pathlib import Path
from typing import Optional

from PIL import Image, ImageFilter

from model_manager import component_path, hardware_info
from quality_runtime import get_config
from job_progress import report

_PIPE = None


def _load():
    global _PIPE
    if _PIPE is not None:
        report("loading_model", "FLUX.2 Klein pipeline is already loaded in this worker.", 30, "flux")
        return _PIPE
    report("loading_model", "Loading FLUX.2 Klein 4B weights from local storage.", 20, "flux")
    try:
        import torch
        from diffusers import Flux2KleinPipeline
    except Exception as exc:
        raise RuntimeError("FLUX.2 Klein requires a Diffusers build containing Flux2KleinPipeline. Re-run setup_ai_backend.bat.") from exc
    model = component_path("flux2-klein-4b")
    if model is None: raise RuntimeError("FLUX.2 Klein 4B is not installed")
    dtype = torch.float16 if torch.cuda.is_available() else torch.float32
    _PIPE = Flux2KleinPipeline.from_pretrained(str(model), torch_dtype=dtype, local_files_only=True)
    if torch.cuda.is_available():
        try: _PIPE.enable_model_cpu_offload()
        except Exception:
            try: _PIPE.enable_sequential_cpu_offload()
            except Exception: _PIPE.to("cuda")
    else: _PIPE.to("cpu")
    try: _PIPE.enable_attention_slicing()
    except Exception: pass
    _PIPE.set_progress_bar_config(disable=True)
    report("loading_model", "FLUX.2 Klein weights loaded and offload policy configured.", 34, "flux")
    return _PIPE


def _size() -> int:
    raw = int(get_config().get("image_size", 1024)); return max(512, min(1536, (raw//64)*64))


def generate_concept(prompt: str, output_path: str) -> str:
    cfg=get_config(); pipe=_load(); size=_size()
    steps=max(4,min(12,int(cfg.get("image_steps",8))))
    report("preparing_inputs", f"Preparing {size}×{size} FLUX canvas and prompt conditioning.", 38, "flux")
    report("running_inference", f"FLUX.2 Klein denoising is running for {steps} distilled steps.", 45, "flux")
    image=pipe(prompt=prompt,height=size,width=size,guidance_scale=1.0,num_inference_steps=steps).images[0]
    report("decoding_output", "FLUX inference finished; decoding image output.", 90, "flux")
    out=Path(output_path); out.parent.mkdir(parents=True,exist_ok=True)
    report("saving_result", f"Saving generated PNG to {out.name}.", 94, "flux")
    image.save(out); return str(out)


def edit_image(image_path: str, mask_path: Optional[str], prompt: str, output_path: str, *, detail: bool=False) -> str:
    pipe=_load(); cfg=get_config()
    report("preparing_inputs", "Loading source image, mask and surrounding edit context.", 38, "flux")
    source=Image.open(image_path).convert("RGB")
    mask=Image.open(mask_path).convert("L") if mask_path and Path(mask_path).exists() else None
    box=None
    if detail and mask is not None and mask.getbbox():
        l,t,r,b=mask.getbbox(); pad=max(24,int(max(r-l,b-t)*.35)); box=(max(0,l-pad),max(0,t-pad),min(source.width,r+pad),min(source.height,b+pad))
    work=source.crop(box) if box else source
    size=_size(); work_resized=work.resize((size,size),Image.Resampling.LANCZOS)
    steps=max(4,min(12,int(cfg.get("image_steps",8))))
    report("running_inference", f"FLUX.2 Klein image-to-image inference is running for {steps} distilled steps.", 45, "flux")
    generated=pipe(image=work_resized,prompt=prompt,height=size,width=size,guidance_scale=1.0,num_inference_steps=steps).images[0]
    report("decoding_output", "FLUX edit inference finished; compositing the selected region.", 88, "flux")
    generated=generated.resize(work.size,Image.Resampling.LANCZOS)
    if box:
        result=source.copy(); lm=mask.crop(box).filter(ImageFilter.GaussianBlur(radius=max(2,int(min(work.size)*.012))))
        result.paste(Image.composite(generated,work,lm),box[:2])
    elif mask is not None:
        m=mask.resize(source.size,Image.Resampling.BILINEAR).filter(ImageFilter.GaussianBlur(radius=4))
        result=Image.composite(generated.resize(source.size,Image.Resampling.LANCZOS),source,m)
    else: result=generated.resize(source.size,Image.Resampling.LANCZOS)
    out=Path(output_path); out.parent.mkdir(parents=True,exist_ok=True)
    report("saving_result", f"Saving edited PNG to {out.name}.", 94, "flux")
    result.save(out); return str(out)


def release_models() -> None:
    global _PIPE
    _PIPE=None
    try:
        import torch
        if torch.cuda.is_available(): torch.cuda.empty_cache()
    except Exception: pass
