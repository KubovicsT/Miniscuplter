from __future__ import annotations

import threading
from copy import deepcopy

_DEFAULT = {
    "image_size": 512,
    "image_steps": 24,
    "image_guidance": 7.0,
    "image_edit_strength": 0.58,
    "max_input_px": 2048,
    "shape_steps": 30,
    "remesh_voxel_mm": 0.28,
    "repair_voxel_mm": 0.30,
    "max_voxel_cells": 100_000_000,
    "thickness_samples": 12_000,
    "smart_select_views": 6,
    "smart_select_render_size": 352,
}

_LOCK = threading.RLock()
_CONFIG = deepcopy(_DEFAULT)


def _clamp(value, lo, hi):
    return max(lo, min(hi, value))


def normalize(data: dict | None) -> dict:
    src = data or {}
    return {
        "image_size": int(_clamp(int(src.get("image_size", _DEFAULT["image_size"])), 256, 1536)),
        "image_steps": int(_clamp(int(src.get("image_steps", _DEFAULT["image_steps"])), 4, 100)),
        "image_guidance": float(_clamp(float(src.get("image_guidance", _DEFAULT["image_guidance"])), 1.0, 20.0)),
        "image_edit_strength": float(_clamp(float(src.get("image_edit_strength", _DEFAULT["image_edit_strength"])), 0.05, 0.95)),
        "max_input_px": int(_clamp(int(src.get("max_input_px", _DEFAULT["max_input_px"])), 512, 8192)),
        "shape_steps": int(_clamp(int(src.get("shape_steps", _DEFAULT["shape_steps"])), 8, 100)),
        "remesh_voxel_mm": float(_clamp(float(src.get("remesh_voxel_mm", _DEFAULT["remesh_voxel_mm"])), 0.04, 5.0)),
        "repair_voxel_mm": float(_clamp(float(src.get("repair_voxel_mm", _DEFAULT["repair_voxel_mm"])), 0.04, 5.0)),
        "max_voxel_cells": int(_clamp(int(src.get("max_voxel_cells", _DEFAULT["max_voxel_cells"])), 1_000_000, 2_000_000_000)),
        "thickness_samples": int(_clamp(int(src.get("thickness_samples", _DEFAULT["thickness_samples"])), 100, 100_000)),
        "smart_select_views": int(_clamp(int(src.get("smart_select_views", _DEFAULT["smart_select_views"])), 2, 12)),
        "smart_select_render_size": int(_clamp(int(src.get("smart_select_render_size", _DEFAULT["smart_select_render_size"])), 128, 1024)),
    }


def set_config(data: dict) -> dict:
    global _CONFIG
    cfg = normalize(data)
    with _LOCK:
        _CONFIG = cfg
        return deepcopy(_CONFIG)


def get_config() -> dict:
    with _LOCK:
        return deepcopy(_CONFIG)


def reset_config() -> dict:
    return set_config(_DEFAULT)
