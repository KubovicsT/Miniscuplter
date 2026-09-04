# Miniscuplter v1.0

Miniscuplter is a Windows desktop application for **AI-assisted 3D model creation, kitbashing, posing, sculpting, local detail refinement, model validation/repair and final STL export**.

v1.0 is the current release-candidate/runtime-testing branch. Its code and lightweight packaging pipeline have passed CI; the branch also contains a reproducible GitHub Actions job that performs the real Godot Windows export and uploads an installable v1.0 artifact.

## Product boundary

Miniscuplter creates the finished 3D model. It is **not a slicer**: no support generation, printer profiles, exposure settings, slicing or printer toolpaths. STL is the primary final format, but the finished model may be used for printing, rendering, games, CAD utility, archival or another modeling package.

## Typical workflow

```text
Import / image / AI concept
        ↓
AI 3D or manual base
        ↓
Parts + sockets + kitbash
        ↓
Rig + pose
        ↓
Manual sculpt / Smart Select / AI detail
        ↓
Model validation / optional repair
        ↓
Finalize Model
        ↓
Final_Model.stl
```

A valid single mesh can export directly. Voxel union/remesh is opt-in because it can soften detail below the selected pitch.

## Major v1.0 systems

- multi-object Godot 4.7.2 .NET editor and STL IO
- advanced sculpting with masks, symmetry and voxel remesh
- Quick Rig, editable skeleton, posing, CPU skinning and IK
- reusable parts library, sockets and Hero-Forge-style attachment workflow
- Smart Select + Space-key command palette
- central Low/Medium/High/Ultra/custom quality presets
- multi-model local AI routing: SD2.1, SDXL, FLUX.2 Klein, TripoSR, Hunyuan3D 2.1 Shape, PartCrafter and CLIPSeg
- local 2D and selected-3D detail refinement with preview/apply/discard
- structural mesh analysis, optional thickness heatmap, repair and final scene bake
- transactional project save/load/recovery and guarded final STL export
- launcher with native hardware detection, explicit model install/remove/update and application update checks
- user-selectable install location plus independent Project / Model Library / STL Export locations
- transactional AI model updates and staged application updater with rollback protections

## Recommended 8 GB NVIDIA starting stack

```text
SDXL
TripoSR
Hunyuan3D 2.1 Shape
CLIPSeg
```

FLUX and PartCrafter are optional heavier routes and should be tested after the core stack is stable on 8 GB Pascal-class hardware.

## Installable GitHub Actions build

On pushes to `v1.0`, the workflow's **full-windows-release** job runs only after C#, Python/core tests and package validation are green. It downloads the pinned official Godot 4.7.2 .NET editor and matching mono templates, verifies their SHA-256 hashes, performs the real Windows export, builds the Inno Setup installer and uploads:

```text
Miniscuplter-Setup-1.0.0.exe
Miniscuplter-win-x64.zip
```

Use the installer artifact for primary runtime testing instead of running the project from Godot.

## Documentation

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — application, backend, geometry, persistence and updater architecture
- [`docs/AI_MODELS.md`](docs/AI_MODELS.md) — managed AI components, routing, VRAM behavior and update safety
- [`docs/BUILD_AND_RELEASE.md`](docs/BUILD_AND_RELEASE.md) — CI artifact pipeline, pinned Godot build and local release build
- [`docs/RUNTIME_TESTING.md`](docs/RUNTIME_TESTING.md) — ordered v1.0 runtime validation protocol
- [`docs/RELEASE_HISTORY.md`](docs/RELEASE_HISTORY.md) — version history and release/development branch map

Each historical release branch also has its own version-specific README. Documentation-only corrections may be applied to preserved branches; old feature code remains frozen.

## Source requirements

For source development:

- Windows 10/11 x64
- Godot 4.7.2 .NET
- .NET 8 SDK
- Python 3.10 x64 for local AI
- Git for specialist AI source repositories
- NVIDIA CUDA GPU recommended for practical AI inference

For normal installed use, Godot/.NET SDK development tooling is not required. The launcher/updater are self-contained; Python 3.10 x64 is currently required once to create/repair the local AI virtual environment.

## Current validation boundary

CI verifies the main C# app, launcher/updater, backend Python syntax, dependency-free core logic, release invariants, portable package construction, installer compilation and the full Godot Windows release artifact pipeline.

The remaining validation layer is real runtime testing on target hardware: actual model downloads, CUDA/driver compatibility, inference quality/performance, UI interaction and end-to-end modeling workflows. Use the runtime protocol above and report failures without working around them so the underlying v1.0 behavior can be fixed.
