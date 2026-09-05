from __future__ import annotations

from dataclasses import dataclass, asdict
from typing import Any
import sys


@dataclass(frozen=True)
class Capability:
    id: str
    provider: str
    name: str
    roles: tuple[str, ...]
    tier: str
    min_vram_mb: int
    recommended_vram_mb: int
    estimated_gb: float
    platforms: tuple[str, ...] = ("win32", "linux", "darwin")
    low_vram: bool = False
    experimental_windows: bool = False
    notes: str = ""


CAPABILITIES: tuple[Capability, ...] = (
    Capability("sd21", "sd21", "Stable Diffusion 2.1", ("concept", "edit", "detail"), "low", 4096, 6144, 5.5),
    Capability("sdxl-base", "sdxl", "Stable Diffusion XL", ("concept", "edit", "detail"), "low", 6144, 8192, 7.0, low_vram=True),
    Capability("z-image-turbo", "zimage", "Z-Image Turbo", ("concept",), "medium", 8192, 16384, 33.0, low_vram=True),
    Capability("flux2-klein-4b", "flux", "FLUX.2 Klein 4B", ("concept", "edit", "detail"), "medium", 10000, 13000, 13.0, low_vram=True),
    Capability("qwen-image-2512", "qwen", "Qwen-Image-2512", ("concept",), "high", 16000, 24000, 58.0, low_vram=True),
    Capability("qwen-image-edit", "qwen-edit", "Qwen-Image-Edit", ("edit", "detail"), "high", 16000, 24000, 55.0, low_vram=True),

    Capability("triposr", "triposr", "TripoSR", ("fast3d", "quality3d", "detail3d"), "low", 6000, 8000, 2.5),
    Capability("sf3d", "sf3d", "Stable Fast 3D", ("fast3d", "quality3d"), "low", 6000, 8000, 8.0, experimental_windows=True),
    Capability("spar3d", "spar3d", "SPAR3D", ("fast3d", "quality3d"), "medium", 7000, 12000, 10.0, low_vram=True, experimental_windows=True),
    Capability("hunyuan2mini", "hunyuan-mini", "Hunyuan3D 2mini", ("quality3d", "detail3d"), "low", 6000, 8000, 5.0, low_vram=True),
    Capability("hunyuan21-shape", "hunyuan", "Hunyuan3D 2.1 Shape", ("quality3d", "detail3d"), "medium", 8000, 12000, 10.0, low_vram=True),
    Capability("trellis2", "trellis2", "TRELLIS.2 4B", ("quality3d",), "workstation", 24000, 24000, 40.0, platforms=("linux",), notes="Official upstream runtime is Linux-only; Windows launcher may use WSL2 when configured."),

    Capability("partcrafter", "partcrafter", "PartCrafter", ("parts",), "medium", 8000, 12000, 12.0),
    Capability("partpacker", "partpacker", "PartPacker", ("parts",), "high", 10000, 24000, 12.0, experimental_windows=True),
    Capability("clipseg-smart-select", "clipseg", "CLIPSeg Smart Select", ("select",), "low", 0, 2048, 0.7),
)

BY_ID = {c.id: c for c in CAPABILITIES}


def recommendations(vram_mb: int, platform: str | None = None) -> list[dict[str, Any]]:
    platform = platform or sys.platform
    out: list[dict[str, Any]] = []
    for c in CAPABILITIES:
        native = platform in c.platforms
        # TRELLIS.2 can be driven from Windows through a user-enabled WSL2 runtime, but is not
        # labelled native because upstream only supports Linux.
        wsl_possible = platform == "win32" and c.id == "trellis2"
        if not native and not wsl_possible:
            fit = "unsupported-os"
        elif vram_mb <= 0:
            fit = "cpu/unknown" if c.min_vram_mb == 0 else "not-recommended"
        elif vram_mb >= c.recommended_vram_mb:
            fit = "recommended"
        elif vram_mb >= c.min_vram_mb:
            fit = "possible"
        elif c.low_vram and vram_mb >= max(4096, c.min_vram_mb - 2048):
            fit = "possible-with-offload"
        else:
            fit = "not-recommended"
        row = asdict(c); row["hardware_fit"] = fit; row["native_platform"] = native; row["wsl_possible"] = wsl_possible
        out.append(row)
    return out


def role_options(role: str, vram_mb: int, platform: str | None = None) -> list[dict[str, Any]]:
    return [x for x in recommendations(vram_mb, platform) if role in x["roles"]]
