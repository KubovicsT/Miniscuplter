from __future__ import annotations

from pathlib import Path
from typing import Optional

import numpy as np
import trimesh
from PIL import Image, ImageFilter

from model_router import choose_image_provider, choose_3d_provider, release_all_models
from quality_runtime import get_config
from geometry_ops import voxel_remesh


def _masked_crop(image_path: str, mask_path: str, output_path: str, pad_ratio: float = .38) -> str:
    with Image.open(image_path) as opened: image = opened.convert("RGB")
    with Image.open(mask_path) as opened: mask = opened.convert("L").resize(image.size, Image.Resampling.BILINEAR)
    bbox = mask.getbbox()
    if not bbox: raise ValueError("Detail mask is empty")
    l, t, r, b = bbox; pad = max(24, int(max(r-l, b-t) * pad_ratio)); box = (max(0,l-pad), max(0,t-pad), min(image.width,r+pad), min(image.height,b+pad))
    crop = image.crop(box); local_mask = mask.crop(box).filter(ImageFilter.GaussianBlur(radius=max(2,int(min(crop.size)*.01))))
    isolated = Image.composite(crop, Image.new("RGB", crop.size, (245,245,245)), local_mask)
    side = max(isolated.size); canvas = Image.new("RGB", (side,side), (245,245,245)); canvas.paste(isolated, ((side-isolated.width)//2,(side-isolated.height)//2))
    out = Path(output_path).resolve(); out.parent.mkdir(parents=True, exist_ok=True); canvas.save(out); return str(out)


def _run_image_edit(provider: str, image: str, mask: str, prompt: str, output: str) -> str:
    if provider == "flux":
        from flux_klein import edit_image; return edit_image(image, mask, prompt, output, detail=True)
    if provider == "sdxl":
        from sdxl_image import edit_image; return edit_image(image, mask, prompt, output, detail=True)
    if provider == "sd21":
        from local_image import edit_image; return edit_image(image, mask, prompt, output, "high")
    raise RuntimeError(f"Unsupported detail image provider: {provider}")


def _run_3d(provider: str, image: str, prompt: str, output: str) -> str:
    if provider == "hunyuan":
        from hunyuan_shape import generate_shape; return generate_shape(image, output, prompt, "high")
    if provider == "triposr":
        from triposr_shape import generate_shape; return generate_shape(image, output, mc_resolution=320)
    raise RuntimeError(f"Provider {provider} does not support single-part detail reconstruction")


def _load_mesh(path: str) -> trimesh.Trimesh:
    mesh = trimesh.load_mesh(path, force="mesh", process=False)
    if isinstance(mesh, trimesh.Scene):
        if not mesh.geometry: raise RuntimeError("Mesh scene contains no geometry")
        mesh = trimesh.util.concatenate(tuple(mesh.geometry.values()))
    if mesh.is_empty or len(mesh.faces) == 0 or not np.isfinite(mesh.vertices).all(): raise RuntimeError("Mesh is empty or invalid")
    return mesh


def _fit_patch_to_bounds(path: str, bounds_min: list[float], bounds_max: list[float], padding: float = 1.04) -> dict:
    mesh = _load_mesh(path)
    lo = np.asarray(bounds_min, dtype=float); hi = np.asarray(bounds_max, dtype=float)
    if lo.shape != (3,) or hi.shape != (3,) or not np.isfinite(lo).all() or not np.isfinite(hi).all() or np.any(hi <= lo):
        raise ValueError("Selection bounds are invalid")
    target = np.maximum(hi-lo, 1e-3); target_center = (lo+hi)*.5
    ext = np.maximum(np.asarray(mesh.extents, dtype=float), 1e-6)
    # Uniform scaling must fit every dimension, not merely the largest dimension. The previous
    # max/max rule could place a long generated patch far outside a narrow Smart Selection.
    scale = float(np.min((target * float(padding)) / ext))
    if not np.isfinite(scale) or scale <= 0: raise RuntimeError("Could not calculate a finite detail-patch scale")
    mesh.apply_translation(-np.asarray(mesh.bounds).mean(axis=0)); mesh.apply_scale(scale); mesh.apply_translation(target_center)
    mesh.remove_unreferenced_vertices(); mesh.merge_vertices()
    out = Path(path).resolve(); mesh.export(out, file_type="stl")
    if not out.exists() or out.stat().st_size == 0: raise RuntimeError("Aligned detail patch was not written")
    return {"scale": scale, "target_center_mm": target_center.tolist(), "target_extents_mm": target.tolist(), "patch_extents_mm": mesh.extents.tolist(), "fit_method": "uniform all-axis bounding fit"}


def detail_2d(image_path: str, mask_path: str, prompt: str, output_path: str, image_provider: str="auto") -> dict:
    decision = choose_image_provider("detail", image_provider)
    release_all_models()
    try:
        result = _run_image_edit(decision.provider, image_path, mask_path, prompt, output_path)
        return {"path": result, "provider": decision.provider, "routing_reason": decision.reason}
    finally:
        release_all_models()


def detail_3d(source_mesh: str, image_path: str, mask_path: str, prompt: str, bounds_min: list[float], bounds_max: list[float],
              output_patch: str, output_image: str, output_crop: str, image_provider: str="auto", three_d_provider: str="auto") -> dict:
    image_decision = choose_image_provider("detail", image_provider)
    release_all_models()
    try:
        enhanced = _run_image_edit(image_decision.provider, image_path, mask_path, prompt, output_image)
    finally:
        release_all_models()
    crop = _masked_crop(enhanced, mask_path, output_crop)
    shape_decision = choose_3d_provider("detail", three_d_provider)
    if shape_decision.provider == "partcrafter": shape_decision = choose_3d_provider("quality", "hunyuan")
    try:
        patch = _run_3d(shape_decision.provider, crop, prompt, output_patch)
    finally:
        release_all_models()
    fit = _fit_patch_to_bounds(patch, bounds_min, bounds_max)
    return {"patch_path": patch, "enhanced_image": enhanced, "crop_image": crop,
            "image_provider": image_decision.provider, "three_d_provider": shape_decision.provider,
            "image_reason": image_decision.reason, "three_d_reason": shape_decision.reason, "fit": fit,
            "source_mesh": str(Path(source_mesh).resolve())}


def apply_detail(source_mesh: str, patch_mesh: str, output_path: str, voxel_size: Optional[float]=None) -> dict:
    cfg = get_config(); pitch = float(voxel_size if voxel_size is not None else cfg.get("repair_voxel_mm", .20))
    if not (0.04 <= pitch <= 5.0): raise ValueError("Detail-apply voxel pitch must be 0.04–5.0 mm")
    # Robust application deliberately uses a watertight volumetric union. This avoids an unsafe
    # arbitrary triangle-boundary splice. The source is never modified in place, so the editor
    # can preview/discard and keep Undo before replacing the live mesh with the validated result.
    result = voxel_remesh([source_mesh, patch_mesh], output_path, pitch)
    return {"path": result, "voxel_size": pitch, "method": "watertight volumetric union of original + aligned local detail patch"}
