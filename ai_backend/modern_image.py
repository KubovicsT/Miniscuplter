from __future__ import annotations

import gc
from pathlib import Path
from typing import Any

from model_manager import component_path, hardware_info
from performance_runtime import get_config as get_performance_config
from quality_runtime import get_config as get_quality_config

_PIPELINES: dict[str, Any] = {}
_PIPELINE_PROFILES: dict[str, dict[str, Any]] = {}


def _torch():
    import torch
    return torch


def _is_oom(exc: BaseException) -> bool:
    text = str(exc).lower()
    return "out of memory" in text or "cuda error" in text and "memory" in text


def _effective_mode(vram_mb: int, configured: str) -> str:
    mode = str(configured or "auto").strip().lower()
    if mode in {"fast", "balanced", "safe"}:
        return mode
    if vram_mb >= 24576:
        return "fast"
    if vram_mb >= 10000:
        return "balanced"
    return "safe"


def _offload_strategy(component: str, vram_mb: int, configured: str, *, force_safe: bool = False) -> str:
    """Return a strategy without importing torch/diffusers so CI can regression-test routing.

    Z-Image's transformer is larger than an 8GB GPU by itself. Model CPU offload moves an
    entire model component onto the GPU and therefore cannot be the low-VRAM strategy for it.
    Sequential offload moves submodules instead and is deliberately selected on 8-10GB cards.
    """
    if vram_mb <= 0:
        return "cpu"
    mode = "safe" if force_safe else _effective_mode(vram_mb, configured)

    if component == "z-image-turbo":
        if vram_mb <= 10240 or mode == "safe":
            return "sequential"
        if mode == "fast" and vram_mb >= 24576:
            return "cuda"
        return "model"

    if component == "flux2-klein-4b":
        if vram_mb < 10000 or mode == "safe":
            return "sequential"
        if mode == "fast" and vram_mb >= 16000:
            return "cuda"
        return "model"

    # Qwen image models are much larger again. Keep them offloaded unless the machine is a
    # workstation-class GPU; this does not promise that low-system-RAM PCs can run them.
    if component in {"qwen-image-2512", "qwen-image-edit"}:
        if vram_mb < 16000 or mode == "safe":
            return "sequential"
        if mode == "fast" and vram_mb >= 24576:
            return "cuda"
        return "model"

    return "model" if mode != "safe" else "sequential"


def _set_vram_ceiling(torch, target: float) -> None:
    try:
        torch.cuda.set_per_process_memory_fraction(float(max(.50, min(.95, target))), 0)
    except Exception:
        pass


def _configure(pipe, component: str, torch, *, force_safe: bool = False) -> dict[str, Any]:
    hw = hardware_info()
    vram_mb = int(hw.get("vram_mb", 0) or 0)
    perf = get_performance_config()
    configured = str(perf.get("mode", "auto"))
    target = float(perf.get("vram_target_fraction", .85))
    strategy = _offload_strategy(component, vram_mb, configured, force_safe=force_safe)

    try:
        pipe.enable_vae_slicing()
    except Exception:
        pass
    try:
        pipe.enable_vae_tiling()
    except Exception:
        pass

    if torch.cuda.is_available():
        _set_vram_ceiling(torch, target)
        try:
            torch.cuda.empty_cache()
        except Exception:
            pass

        if strategy == "cuda":
            pipe.to("cuda")
        elif strategy == "model":
            try:
                pipe.enable_model_cpu_offload()
            except Exception:
                pipe.enable_sequential_cpu_offload()
                strategy = "sequential"
        elif strategy == "sequential":
            # Do not call pipe.to("cuda") first. Sequential CPU offload relies on installing
            # submodule hooks while the pipeline is still on CPU/meta storage.
            pipe.enable_sequential_cpu_offload()
        else:
            pipe.to("cpu")
            strategy = "cpu"
    else:
        pipe.to("cpu")
        strategy = "cpu"

    try:
        pipe.set_progress_bar_config(disable=True)
    except Exception:
        pass

    return {
        "strategy": strategy,
        "vram_mb": vram_mb,
        "configured_mode": configured,
        "vram_target_fraction": target,
        "gpu": hw.get("gpu"),
    }


def _load(component: str, purpose: str, *, force_safe: bool = False):
    key = f"{component}:{purpose}:{'safe' if force_safe else 'normal'}"
    if key in _PIPELINES:
        return _PIPELINES[key]
    path = component_path(component)
    if path is None:
        raise RuntimeError(f"{component} is not installed")
    torch = _torch()
    from diffusers import DiffusionPipeline

    dtype = torch.bfloat16 if torch.cuda.is_available() and torch.cuda.is_bf16_supported() else torch.float16
    kwargs: dict[str, Any] = {
        "torch_dtype": dtype,
        "local_files_only": True,
        # Avoid a second full copy of these very large weights while the pipeline is loading.
        "low_cpu_mem_usage": True,
    }
    try:
        pipe = DiffusionPipeline.from_pretrained(str(path), **kwargs)
    except TypeError:
        # Compatibility fallback for a pipeline revision that does not expose this loader flag.
        kwargs.pop("low_cpu_mem_usage", None)
        pipe = DiffusionPipeline.from_pretrained(str(path), **kwargs)

    profile = _configure(pipe, component, torch, force_safe=force_safe)
    _PIPELINES[key] = pipe
    _PIPELINE_PROFILES[key] = profile
    return pipe


def _generation_size(component: str, vram_mb: int, *, retry: bool = False) -> int:
    cfg = get_quality_config()
    raw = int(cfg.get("image_size", 1024) or 1024)
    size = max(512, min(1536, (raw // 64) * 64))

    # Z-Image is a 6B-class transformer. On 8GB hardware a smaller canvas leaves room for
    # activations/VAE work while sequential offload handles the weights. This is a hardware
    # adaptation, not a step-count reduction; Turbo still uses its intended denoising schedule.
    if component == "z-image-turbo":
        if vram_mb <= 8192:
            size = min(size, 768)
        elif vram_mb <= 10240:
            size = min(size, 896)
    elif component == "flux2-klein-4b" and vram_mb <= 8192:
        size = min(size, 768)

    if retry:
        size = min(size, 640 if vram_mb <= 8192 else 768)
    return max(512, (size // 64) * 64)


def _save(image, output_path: str) -> str:
    p = Path(output_path).resolve()
    p.parent.mkdir(parents=True, exist_ok=True)
    image.save(p)
    if not p.exists() or p.stat().st_size == 0:
        raise RuntimeError("Image model produced no output")
    return str(p)


def _run_generate(component: str, prompt: str, *, force_safe: bool = False, retry: bool = False):
    torch = _torch()
    hw = hardware_info()
    vram_mb = int(hw.get("vram_mb", 0) or 0)
    pipe = _load(component, "generate", force_safe=force_safe)
    size = _generation_size(component, vram_mb, retry=retry)

    # Turbo models prefer few steps; Qwen benefits from a fuller schedule.
    steps = 9 if component == "z-image-turbo" else 30
    guidance = 0.0 if component == "z-image-turbo" else 4.0
    kwargs: dict[str, Any] = {
        "prompt": prompt,
        "num_inference_steps": steps,
        "guidance_scale": guidance,
        "height": size,
        "width": size,
    }
    if component == "z-image-turbo":
        # Prompt activations also matter on a 16GB-RAM machine. 256 tokens is ample for the
        # concept prompts Miniscuplter generates while materially reducing temporary memory.
        kwargs["max_sequence_length"] = 256
    result = pipe(**kwargs)
    return result.images[0], size


def generate(component: str, prompt: str, output_path: str) -> str:
    try:
        image, _ = _run_generate(component, prompt)
        return _save(image, output_path)
    except Exception as exc:
        if not _is_oom(exc):
            raise

        # CUDA OOM can leave cached allocations and offload hooks in a poor state. Release the
        # entire pipeline and retry exactly once with the strongest supported offload policy and
        # a smaller canvas. If that still cannot fit, return a useful hardware-specific error.
        release_models()
        try:
            image, retry_size = _run_generate(component, prompt, force_safe=True, retry=True)
            return _save(image, output_path)
        except Exception as retry_exc:
            if _is_oom(retry_exc):
                hw = hardware_info()
                vram_mb = int(hw.get("vram_mb", 0) or 0)
                raise RuntimeError(
                    f"{component} ran out of memory even in Miniscuplter low-VRAM mode "
                    f"({vram_mb / 1024:.1f} GB VRAM, sequential CPU offload, reduced canvas). "
                    "Close other GPU/RAM-heavy applications or use SDXL for this machine. "
                    f"Original CUDA error: {retry_exc}"
                ) from retry_exc
            raise


def edit(component: str, image_path: str, mask_path: str | None, prompt: str, output_path: str) -> str:
    from PIL import Image

    pipe = _load(component, "edit")
    image = Image.open(image_path).convert("RGB")
    kwargs: dict[str, Any] = {"image": image, "prompt": prompt, "num_inference_steps": 30}
    # Qwen Image Edit accepts the source image directly. Mask support differs between model
    # revisions; preserve Miniscuplter's selection by compositing the edited result afterwards.
    try:
        result = pipe(**kwargs).images[0].convert("RGB").resize(image.size)
    except Exception as exc:
        if not _is_oom(exc):
            raise
        release_models()
        pipe = _load(component, "edit", force_safe=True)
        result = pipe(**kwargs).images[0].convert("RGB").resize(image.size)
    if mask_path and Path(mask_path).exists():
        mask = Image.open(mask_path).convert("L").resize(image.size)
        result = Image.composite(result, image, mask)
    return _save(result, output_path)


def release_models() -> None:
    _PIPELINES.clear()
    _PIPELINE_PROFILES.clear()
    gc.collect()
    try:
        torch = _torch()
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
            try:
                torch.cuda.ipc_collect()
            except Exception:
                pass
    except Exception:
        pass
