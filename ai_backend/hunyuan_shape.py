from __future__ import annotations

import os
import sys
from pathlib import Path

from model_manager import component_path, hardware_info, TOOLS_ROOT

_PIPE = None


def _load_pipeline():
    global _PIPE
    if _PIPE is not None:
        return _PIPE

    weights = component_path("hunyuan21-shape")
    code_root = TOOLS_ROOT / "Hunyuan3D-2.1"
    if weights is None or not code_root.exists():
        raise RuntimeError("Hunyuan3D 2.1 Shape is not installed. Use AI Components -> Install 3D AI.")

    sys.path.insert(0, str(code_root))
    sys.path.insert(0, str(code_root / "hy3dshape"))
    try:
        from hy3dshape.pipelines import Hunyuan3DDiTFlowMatchingPipeline
    except Exception as exc:
        raise RuntimeError(
            "The Hunyuan3D Python dependencies are not ready. Run setup_v02_ai.bat after installing the component."
        ) from exc

    model_dir = weights / "hunyuan3d-dit-v2-1"
    # Tencent's API accepts either the repository root/subfolder or a local model folder.
    try:
        _PIPE = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(weights), subfolder="hunyuan3d-dit-v2-1")
    except TypeError:
        _PIPE = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(model_dir))

    try:
        import torch
        hw = hardware_info()
        if torch.cuda.is_available():
            # Hunyuan's documented shape requirement is about 10 GB. On 8 GB cards
            # we prefer model CPU offload when the pipeline exposes it.
            if hw.get("vram_mb", 0) <= 8192 and hasattr(_PIPE, "enable_model_cpu_offload"):
                _PIPE.enable_model_cpu_offload()
            elif hasattr(_PIPE, "to"):
                _PIPE.to("cuda")
    except Exception:
        # Let generation itself provide the detailed provider error.
        pass
    return _PIPE


def generate_shape(image_path: str, output_path: str, prompt: str = "") -> str:
    pipe = _load_pipeline()
    kwargs = {"image": image_path}

    # Keep defaults conservative for a GTX 1080-class system. Different upstream
    # revisions expose slightly different keyword sets, so retry with the minimal API.
    try:
        result = pipe(**kwargs, num_inference_steps=30)
    except TypeError:
        result = pipe(**kwargs)

    mesh = result[0] if isinstance(result, (list, tuple)) else result
    out = Path(output_path)
    out.parent.mkdir(parents=True, exist_ok=True)

    if hasattr(mesh, "export"):
        mesh.export(str(out))
    elif hasattr(mesh, "save"):
        mesh.save(str(out))
    else:
        raise RuntimeError(f"Hunyuan returned an unsupported mesh object: {type(mesh)!r}")

    if not out.exists():
        raise RuntimeError("Hunyuan generation completed but no STL was produced.")
    return str(out)


def release_model() -> None:
    global _PIPE
    _PIPE = None
    try:
        import torch
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
    except Exception:
        pass
