# Miniscuplter v0.2 — Managed Local AI

This preserved branch adds the first managed local-AI layer on top of the v0.1 editor. It is a historical milestone, not the current release candidate; current development is on `v1.0`.

## Added in v0.2

- NVIDIA GPU/VRAM detection and low-VRAM profile selection
- in-app AI component status/install/remove controls
- Stable Diffusion 2.1 local concept and image-edit route
- Hunyuan3D 2.1 Shape adapter for approved-image → 3D
- deliberate unloading/switching of heavy models on constrained GPUs
- Automatic1111/Forge and custom-command fallbacks retained
- Python backend syntax validation in CI

## Inherited editor capabilities

The v0.1 viewport, STL import/export, object transforms, basic sculpting, project save/load, viewport capture, regional masks and reference search remain available.

## Intended hardware context

The architecture was designed with an 8 GB NVIDIA GPU in mind. Hunyuan3D Shape exceeds that VRAM class at official defaults, so this branch should be understood as the first managed/offload experiment rather than a proven production runtime.

## Source testing

Requirements: Windows 10/11 x64, Godot 4.7.2 .NET, .NET 8, Python 3.10 x64, Git and an NVIDIA driver for CUDA inference.

```bash
git checkout v0.2
```

Run `setup_ai_backend.bat`, open `project.godot` in Godot 4.7.2 .NET, then test the 2D route before installing/testing Hunyuan.

## Historical status

Frozen. Later branches add image-input workflows, voxel/remesh context, AI patching, rigging, kitbashing, advanced sculpting, model validation, Smart Select, quality presets, multi-model routing, launcher/update management and release hardening. See `v1.0` for the complete documentation set.
