# Miniscuplter v0.2

> **Isolation:** v0.1 remains on the `main` branch. This version lives on the separate `v0.2` branch so both builds can be tested independently.

Miniscuplter is an experimental single-application workflow for AI-assisted miniature sculpting and STL preparation.

## What v0.2 adds

v0.2 keeps every v0.1 editor feature and adds a managed local-AI layer:

- Hardware detection through NVIDIA `nvidia-smi`
- GTX 1080 / 8 GB-class GPUs automatically receive the `low-vram` profile
- In-app **AI Components** panel
- Install/remove/status controls for local AI components
- Official-provider model downloads rather than committing model weights into this repository
- Built-in local Stable Diffusion 2.1 concept generation
- Built-in local 2D viewport-edit generation
- Built-in Hunyuan3D 2.1 Shape adapter for approved-image -> 3D generation
- 2D models are unloaded before Hunyuan loads on constrained GPUs
- Manual Automatic1111/Forge and custom 3D command adapters remain as fallbacks
- v0.2-specific CI checks both C# compilation and Python backend syntax

## Existing editor workflow inherited from v0.1

- Desktop 3D viewport with orbit, pan, zoom and framing
- Multiple mesh objects for non-destructive kitbashing
- STL import and binary STL export
- Sculpt brushes: Draw, Smooth, Inflate, Grab, Crease and Flatten
- Undo/redo for sculpt operations
- Object duplicate/delete, move, rotate, scale and transform baking
- Project save/load
- View capture for AI editing
- Rectangular selected-region mask creation
- Capture -> 2D AI edit -> approved image -> 3D part workflow
- In-app 2D result preview
- AI-generated parts are added as separate scene objects first
- Internet reference search from the AI panel
- Basic print-prep mesh statistics and build-plane placement

## AI model sources

The installer/model manager downloads weights only when you choose to install them.

### 2D AI

Default model:

`stabilityai/stable-diffusion-2-1-base`

Source: Hugging Face / Stability AI model repository.

Used for:

- concept images
- viewport image refinement
- selected-region design changes

### 3D AI

Default model:

`tencent/Hunyuan3D-2.1`

Code source:

`https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1`

Only the **shape** model is targeted. The texture/PBR model is intentionally not installed by default because Miniscuplter's primary output is geometry/STL.

Tencent documents roughly 10 GB VRAM for Hunyuan3D 2.1 shape generation. An 8 GB GTX 1080 is therefore treated as a low-VRAM/offload system and should be expected to run substantially more slowly than the reference hardware.

## Source test setup

Requirements:

- Windows 10/11 64-bit
- Godot Engine 4.7.2 .NET edition
- .NET 8 SDK
- Python 3.10 x64
- Git
- NVIDIA driver for local CUDA generation

Steps:

1. Clone the repository.
2. Checkout the v0.2 branch:

   `git checkout v0.2`

3. Run `setup_ai_backend.bat` once.
4. Open `project.godot` with Godot 4.7.2 .NET.
5. Press F5.
6. Open the AI panel and locate **AI Components — v0.2**.
7. Verify that your GPU and VRAM are detected correctly.
8. Install the 2D AI component first.
9. Test concept generation and selected-region editing.
10. Install the 3D AI component and test approved-image -> 3D generation.

Model downloads are stored under `ai_backend/data/` and are intentionally excluded from source control.

## Important v0.2 testing caveat

Hunyuan3D 2.1 has a large official dependency stack and its reference environment is significantly more powerful than a GTX 1080. v0.2 deliberately keeps its integration behind an adapter instead of merging Tencent's entire environment into the Miniscuplter backend. The first hardware test is expected to tell us which shape-only dependencies and memory settings are actually required on this machine.

That is specifically why v0.1 is being kept intact during this phase.

## Recommended first test order

1. Application startup
2. Viewport navigation
3. STL import/export
4. Sculpt brushes
5. Save/load
6. Internet reference search
7. AI backend status
8. GPU/VRAM detection
9. Install 2D AI
10. Generate concept
11. Capture model view
12. Select AI edit region
13. Generate 2D edit
14. Approve/regenerate result
15. Install Hunyuan3D Shape
16. Generate 3D part
17. Position/sculpt the returned part

When reporting an issue, include the exact action, expected behavior, actual behavior, and any Godot console or Python backend error text.

## Branches

- `main` — frozen v0.1 test version
- `v0.2` — managed local-AI test version
