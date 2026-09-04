from __future__ import annotations

import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
import sys
sys.path.insert(0, str(ROOT / "ai_backend"))

import model_manager
import model_router
import quality_runtime


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def test_quality_clamps() -> None:
    cfg = quality_runtime.normalize({
        "image_size": 99999,
        "image_steps": -1,
        "image_guidance": 999,
        "image_edit_strength": -2,
        "max_input_px": 1,
        "shape_steps": 999,
        "remesh_voxel_mm": 0,
        "repair_voxel_mm": 99,
        "max_voxel_cells": 999999999999,
        "thickness_samples": 1,
        "smart_select_views": 99,
        "smart_select_render_size": 1,
    })
    check(cfg["image_size"] == 1536, "image size upper clamp failed")
    check(cfg["image_steps"] == 4, "image steps lower clamp failed")
    check(cfg["image_guidance"] == 20.0, "guidance upper clamp failed")
    check(cfg["image_edit_strength"] == 0.05, "edit strength lower clamp failed")
    check(cfg["max_input_px"] == 512, "max input lower clamp failed")
    check(cfg["shape_steps"] == 100, "shape steps upper clamp failed")
    check(cfg["remesh_voxel_mm"] == 0.04, "remesh lower clamp failed")
    check(cfg["repair_voxel_mm"] == 5.0, "repair upper clamp failed")
    check(cfg["max_voxel_cells"] == 2_000_000_000, "voxel budget upper clamp failed")
    check(cfg["thickness_samples"] == 100, "thickness lower clamp failed")
    check(cfg["smart_select_views"] == 12, "view count upper clamp failed")
    check(cfg["smart_select_render_size"] == 128, "render size lower clamp failed")


def test_model_routing() -> None:
    original_installed = model_router.installed
    original_hw = model_router.hardware_info
    try:
        present = {"sdxl-base", "sd21", "triposr", "hunyuan21-shape", "partcrafter"}
        model_router.installed = lambda component_id: component_id in present
        model_router.hardware_info = lambda: {"vram_mb": 8192}
        check(model_router.choose_image_provider("generate").provider == "sdxl", "8 GB auto 2D route should prefer SDXL")
        check(model_router.choose_3d_provider("fast").provider == "triposr", "fast 3D route should prefer TripoSR")
        check(model_router.choose_3d_provider("quality").provider == "hunyuan", "quality 3D route should prefer Hunyuan")
        check(model_router.choose_3d_provider("structured").provider == "partcrafter", "structured route should prefer PartCrafter")

        present.add("flux2-klein-4b")
        model_router.hardware_info = lambda: {"vram_mb": 16384}
        check(model_router.choose_image_provider("detail").provider == "flux", "high-VRAM detail route should prefer FLUX when installed")

        present.remove("sdxl-base")
        present.remove("flux2-klein-4b")
        check(model_router.choose_image_provider("generate").provider == "sd21", "2D fallback should reach SD2.1")

        present.discard("hunyuan21-shape")
        check(model_router.choose_3d_provider("quality").provider == "triposr", "quality route should fall back to TripoSR")

        try:
            model_router.choose_image_provider("generate", "sdxl")
            raise AssertionError("explicit missing provider should fail")
        except RuntimeError:
            pass
    finally:
        model_router.installed = original_installed
        model_router.hardware_info = original_hw


def test_component_file_validation() -> None:
    original_tools = model_manager.TOOLS_ROOT
    try:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            model_manager.TOOLS_ROOT = root / "tools"
            model_manager.TOOLS_ROOT.mkdir()

            clip = root / "clip"
            clip.mkdir()
            check(not model_manager._component_files_valid("clipseg-smart-select", clip), "empty CLIPSeg directory must not count as installed")
            (clip / "config.json").write_text("{}", encoding="utf-8")
            (clip / "model.safetensors").write_bytes(b"x")
            check(model_manager._component_files_valid("clipseg-smart-select", clip), "complete CLIPSeg marker files should validate")

            trip = root / "trip"
            trip.mkdir()
            (trip / "config.yaml").write_text("x", encoding="utf-8")
            (trip / "model.ckpt").write_bytes(b"x")
            tool = model_manager.TOOLS_ROOT / "TripoSR"
            tool.mkdir(parents=True)
            (tool / "code.py").write_text("x", encoding="utf-8")
            check(model_manager._component_files_valid("triposr", trip), "TripoSR complete markers should validate")
            (trip / "model.ckpt").unlink()
            check(not model_manager._component_files_valid("triposr", trip), "missing TripoSR checkpoint must invalidate component")
    finally:
        model_manager.TOOLS_ROOT = original_tools


def test_uninstall_path_guard() -> None:
    try:
        model_manager._managed_path(ROOT)
        raise AssertionError("managed path guard accepted repository root outside AI data")
    except RuntimeError:
        pass


if __name__ == "__main__":
    test_quality_clamps()
    test_model_routing()
    test_component_file_validation()
    test_uninstall_path_guard()
    print("v1.0 core logic tests passed")
