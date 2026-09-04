from __future__ import annotations

import sys
import tempfile
from pathlib import Path

from PIL import Image

from model_manager import component_path, hardware_info, TOOLS_ROOT
from quality_runtime import get_config

_PIPE = None


def _load_pipeline():
    global _PIPE
    if _PIPE is not None:
        return _PIPE

    weights = component_path("hunyuan21-shape")
    code_root = TOOLS_ROOT / "Hunyuan3D-2.1"
    if weights is None or not code_root.exists():
        raise RuntimeError("Hunyuan3D 2.1 Shape is not installed. Install it from Miniscuplter Launcher or the AI Models panel.")

    if str(code_root) not in sys.path: sys.path.insert(0, str(code_root))
    if str(code_root / "hy3dshape") not in sys.path: sys.path.insert(0, str(code_root / "hy3dshape"))
    try:
        from hy3dshape.pipelines import Hunyuan3DDiTFlowMatchingPipeline
    except Exception as exc:
        raise RuntimeError(
            "The Hunyuan3D Python dependencies are not ready. Use Miniscuplter Launcher -> Repair AI Runtime, then reinstall/update Hunyuan if necessary."
        ) from exc

    model_dir = weights / "hunyuan3d-dit-v2-1"
    try:
        _PIPE = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(weights), subfolder="hunyuan3d-dit-v2-1")
    except TypeError:
        _PIPE = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(model_dir))

    try:
        import torch
        hw = hardware_info()
        if torch.cuda.is_available():
            if hw.get("vram_mb", 0) <= 8192 and hasattr(_PIPE, "enable_model_cpu_offload"):
                _PIPE.enable_model_cpu_offload()
            elif hasattr(_PIPE, "to"):
                _PIPE.to("cuda")
    except Exception:
        pass
    return _PIPE


def _prepare_image(image_path: str) -> tuple[str, bool]:
    cfg = get_config(); max_px = int(cfg["max_input_px"])
    src = Path(image_path).resolve()
    if not src.is_file(): raise FileNotFoundError(f"Input image does not exist: {src}")
    with Image.open(src) as opened:
        image = opened.convert("RGB")
        if max(image.size) <= max_px:
            return str(src), False
        image.thumbnail((max_px, max_px), Image.Resampling.LANCZOS)
        tmp = tempfile.NamedTemporaryFile(prefix="miniscuplter_hunyuan_", suffix=".png", delete=False)
        tmp.close()
        image.save(tmp.name)
        return tmp.name, True


def generate_shape(image_path: str, output_path: str, prompt: str = "", quality: str = "standard") -> str:
    pipe = _load_pipeline()
    prepared, temporary = _prepare_image(image_path)
    try:
        kwargs = {"image": prepared}
        steps = int(get_config()["shape_steps"])
        try:
            result = pipe(**kwargs, num_inference_steps=steps)
        except TypeError:
            result = pipe(**kwargs)

        mesh = result[0] if isinstance(result, (list, tuple)) else result
        out = Path(output_path).resolve()
        out.parent.mkdir(parents=True, exist_ok=True)

        if hasattr(mesh, "export"):
            mesh.export(str(out))
        elif hasattr(mesh, "save"):
            mesh.save(str(out))
        else:
            raise RuntimeError(f"Hunyuan returned an unsupported mesh object: {type(mesh)!r}")

        if not out.exists() or out.stat().st_size == 0:
            raise RuntimeError("Hunyuan generation completed but no usable STL was produced.")
        return str(out)
    finally:
        if temporary:
            try: Path(prepared).unlink(missing_ok=True)
            except Exception: pass


def release_model() -> None:
    global _PIPE
    _PIPE = None
    try:
        import torch
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
    except Exception:
        pass
