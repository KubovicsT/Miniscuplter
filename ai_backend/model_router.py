from __future__ import annotations

from dataclasses import dataclass, asdict
from typing import Callable, Any

from model_manager import component_path, hardware_info


@dataclass(frozen=True)
class RouteDecision:
    role: str
    provider: str
    reason: str
    fallback: str | None = None


def installed(component_id: str) -> bool:
    return component_path(component_id) is not None


def choose_image_provider(role: str = "generate", mode: str = "auto") -> RouteDecision:
    """Choose an installed 2D model without ever silently using an unavailable provider."""
    role = (role or "generate").lower()
    mode = (mode or "auto").lower()
    hw = hardware_info(); vram = int(hw.get("vram_mb", 0) or 0)

    if mode in {"sd21", "sdxl", "flux"}:
        cid = {"sd21":"sd21", "sdxl":"sdxl-base", "flux":"flux2-klein-4b"}[mode]
        if not installed(cid):
            raise RuntimeError(f"Requested image provider '{mode}' is not installed")
        return RouteDecision(role, mode, "explicit user/provider selection")

    # On 8 GB-class cards SDXL is the preferred quality/compatibility balance.
    # FLUX.2 Klein remains available as an explicit/heavy specialist because its
    # official target is around 13 GB VRAM and therefore requires aggressive offload here.
    if role in {"detail", "edit"} and installed("flux2-klein-4b") and vram >= 12288:
        return RouteDecision(role, "flux", "high-VRAM semantic editing specialist", "sdxl" if installed("sdxl-base") else "sd21")
    if installed("sdxl-base"):
        return RouteDecision(role, "sdxl", "best installed 2D quality compatible with the detected hardware", "sd21" if installed("sd21") else None)
    if installed("flux2-klein-4b"):
        return RouteDecision(role, "flux", "only modern installed 2D model; CPU offload may be required", "sd21" if installed("sd21") else None)
    if installed("sd21"):
        return RouteDecision(role, "sd21", "legacy local fallback")
    raise RuntimeError("No local image model is installed")


def choose_3d_provider(role: str = "quality", mode: str = "auto") -> RouteDecision:
    role = (role or "quality").lower(); mode = (mode or "auto").lower()
    if mode in {"triposr", "hunyuan", "partcrafter"}:
        cid = {"triposr":"triposr", "hunyuan":"hunyuan21-shape", "partcrafter":"partcrafter"}[mode]
        if not installed(cid):
            raise RuntimeError(f"Requested 3D provider '{mode}' is not installed")
        return RouteDecision(role, mode, "explicit user/provider selection")

    if role in {"fast", "draft", "rough"} and installed("triposr"):
        return RouteDecision(role, "triposr", "fast whole-object reconstruction", "hunyuan" if installed("hunyuan21-shape") else None)
    if role in {"parts", "structured"} and installed("partcrafter"):
        return RouteDecision(role, "partcrafter", "structured part-aware generation", "hunyuan" if installed("hunyuan21-shape") else None)
    if installed("hunyuan21-shape"):
        return RouteDecision(role, "hunyuan", "quality/detail image-to-shape route", "triposr" if installed("triposr") else None)
    if installed("triposr"):
        return RouteDecision(role, "triposr", "quality provider unavailable; using fast reconstruction")
    raise RuntimeError("No local 3D model is installed")


def routing_status() -> dict[str, Any]:
    result: dict[str, Any] = {"image": {}, "three_d": {}}
    for role in ("generate", "edit", "detail"):
        try: result["image"][role] = asdict(choose_image_provider(role))
        except Exception as exc: result["image"][role] = {"error": str(exc)}
    for role in ("fast", "quality", "detail", "structured"):
        try: result["three_d"][role] = asdict(choose_3d_provider(role))
        except Exception as exc: result["three_d"][role] = {"error": str(exc)}
    return result


def release_all_models() -> None:
    """Release every in-process specialist so only the current job owns scarce VRAM."""
    releases: list[Callable[[], Any]] = []
    for module_name, fn_name in (
        ("local_image", "release_models"),
        ("sdxl_image", "release_models"),
        ("flux_klein", "release_models"),
        ("hunyuan_shape", "release_model"),
        ("triposr_shape", "release_model"),
        ("semantic_select", "release_model"),
    ):
        try:
            module = __import__(module_name, fromlist=[fn_name]); fn = getattr(module, fn_name, None)
            if callable(fn): releases.append(fn)
        except Exception:
            pass
    for fn in releases:
        try: fn()
        except Exception: pass
