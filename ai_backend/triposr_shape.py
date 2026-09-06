from __future__ import annotations

import sys
from pathlib import Path

from model_manager import component_path, TOOLS_ROOT

_MODEL = None
_DEVICE = None


def _code_dir() -> Path:
    code_dir = TOOLS_ROOT / "TripoSR"
    if not code_dir.exists():
        raise RuntimeError("TripoSR source code is not installed")
    code = str(code_dir)
    if code not in sys.path:
        sys.path.insert(0, code)
    return code_dir


def _load():
    global _MODEL, _DEVICE
    if _MODEL is not None:
        return _MODEL, _DEVICE
    model_dir = component_path("triposr")
    if model_dir is None:
        raise RuntimeError("TripoSR model weights are not installed")
    _code_dir()
    try:
        import torch
        from tsr.system import TSR
    except Exception as exc:
        raise RuntimeError("TripoSR Python dependencies are unavailable. Re-run the AI backend setup or reinstall TripoSR.") from exc
    _DEVICE = "cuda:0" if torch.cuda.is_available() else "cpu"
    _MODEL = TSR.from_pretrained(str(model_dir), config_name="config.yaml", weight_name="model.ckpt")
    _MODEL.renderer.set_chunk_size(8192)
    _MODEL.to(_DEVICE)
    return _MODEL, _DEVICE


def generate_shape(image_path: str, output_path: str, foreground_ratio: float = .85, mc_resolution: int = 256) -> str:
    # The downloaded TripoSR source tree is not a site-package. Put it on sys.path before
    # importing tsr.utils; the previous ordering imported preprocessing before _load() had
    # registered the source directory and failed even when TripoSR was correctly installed.
    _code_dir()
    try:
        from PIL import Image
        from tsr.utils import remove_background, resize_foreground
        import rembg
    except Exception as exc:
        raise RuntimeError("TripoSR image preprocessing dependencies are unavailable") from exc

    model, device = _load()
    image = Image.open(image_path).convert("RGBA")
    try:
        image = remove_background(image, rembg.new_session())
    except Exception:
        pass
    image = resize_foreground(image, foreground_ratio)

    import torch
    with torch.no_grad():
        scene_codes = model([image], device=device)
        meshes = model.extract_mesh(
            scene_codes,
            True,
            resolution=int(max(128, min(512, mc_resolution))),
        )

    if not meshes:
        raise RuntimeError("TripoSR produced no mesh")

    # Current upstream TSR.extract_mesh returns one trimesh.Trimesh per scene. Older forks
    # have returned a nested list, so accept either result shape without assuming nesting.
    mesh = meshes[0]
    if isinstance(mesh, (list, tuple)):
        if not mesh:
            raise RuntimeError("TripoSR produced an empty mesh result")
        mesh = mesh[0]
    if mesh is None or not hasattr(mesh, "export"):
        raise RuntimeError(f"TripoSR returned an unsupported mesh result type: {type(mesh).__name__}")

    out = Path(output_path).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    mesh.export(str(out))
    if not out.exists() or out.stat().st_size == 0:
        raise RuntimeError("TripoSR finished without writing a mesh")
    return str(out)


def release_model() -> None:
    global _MODEL, _DEVICE
    _MODEL = None
    _DEVICE = None
    try:
        import torch
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
    except Exception:
        pass
