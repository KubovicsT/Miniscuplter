from __future__ import annotations

import importlib.util
import sys
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable

from model_manager import component_path, hardware_info, TOOLS_ROOT

PROVIDER_COMPONENTS = {
    "triposr": "triposr",
    "sf3d": "sf3d",
    "spar3d": "spar3d",
    "hunyuan-mini": "hunyuan2mini",
    "hunyuan": "hunyuan21-shape",
    "trellis2": "trellis2",
    "partcrafter": "partcrafter",
    "partpacker": "partpacker",
}

@dataclass(frozen=True)
class Readiness:
    provider: str
    component: str
    ready: bool
    installed: bool
    cuda_available: bool
    vram_mb: int
    checks: tuple[str, ...]
    errors: tuple[str, ...]

    def to_dict(self):
        return asdict(self)


def _module_available(name: str, extra_path: Path | None = None) -> bool:
    if extra_path is None:
        return importlib.util.find_spec(name) is not None
    inserted = False
    p = str(extra_path)
    if p not in sys.path:
        sys.path.insert(0, p)
        inserted = True
    try:
        return importlib.util.find_spec(name) is not None
    except Exception:
        return False
    finally:
        if inserted:
            try:
                sys.path.remove(p)
            except ValueError:
                pass


def check_3d_provider(provider: str) -> Readiness:
    provider = (provider or "").strip().lower()
    component = PROVIDER_COMPONENTS.get(provider, provider)
    hw = hardware_info()
    cuda = bool(hw.get("cuda_available"))
    vram = int(hw.get("vram_mb", 0) or 0)
    checks: list[str] = []
    errors: list[str] = []

    path = component_path(component)
    installed = path is not None
    if installed:
        checks.append("component-files")
    else:
        errors.append(f"Required component '{component}' is not installed or is incomplete.")

    if provider in {"triposr", "sf3d", "spar3d", "hunyuan-mini", "hunyuan", "partcrafter"}:
        if _module_available("torch"):
            checks.append("python:torch")
        else:
            errors.append("Python dependency 'torch' is unavailable.")

    if provider == "triposr":
        tool = TOOLS_ROOT / "TripoSR"
        if tool.is_dir() and _module_available("tsr", tool):
            checks.append("python:tsr")
        else:
            errors.append("TripoSR source/import 'tsr' is unavailable.")
        for module in ("PIL", "rembg", "trimesh"):
            if _module_available(module):
                checks.append(f"python:{module}")
            else:
                errors.append(f"Python dependency '{module}' is unavailable.")

    if provider == "hunyuan":
        tool = TOOLS_ROOT / "Hunyuan3D-2.1"
        if tool.is_dir() and (_module_available("hy3dshape", tool) or _module_available("hy3dshape", tool / "hy3dshape")):
            checks.append("python:hy3dshape")
        else:
            errors.append("Hunyuan3D source/import 'hy3dshape' is unavailable.")

    # Local 3D GPU providers are only considered ready for the normal route when CUDA is visible.
    # This is deliberately an early qualification check, not a claim that full inference will fit VRAM.
    if provider in {"triposr", "sf3d", "spar3d", "hunyuan-mini", "hunyuan", "partcrafter", "partpacker"}:
        if cuda:
            checks.append("cuda-visible")
        else:
            errors.append("CUDA/NVIDIA GPU is not visible to the Miniscuplter runtime.")

    return Readiness(provider, component, installed and not errors, installed, cuda, vram, tuple(checks), tuple(errors))


def first_ready(providers: Iterable[str]) -> str | None:
    for provider in providers:
        if check_3d_provider(provider).ready:
            return provider
    return None
