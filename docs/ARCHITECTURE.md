# Miniscuplter v1.0 Architecture

## Product boundary

Miniscuplter is a Windows desktop model-creation application combining manual editing, kitbashing, posing, sculpting, local AI generation/refinement and model validation. Its responsibility ends at a finished model/STL. Slicing and printer-specific preparation are intentionally outside scope.

## High-level structure

```text
Miniscuplter.Launcher.exe
│
├── hardware/model/update management
└── starts App/Miniscuplter.exe
        │
        ├── Godot 4.7.2 .NET / C# editor
        │   ├── 3D viewport and scene objects
        │   ├── sculpting
        │   ├── rigging/posing
        │   ├── parts/sockets/library
        │   ├── project persistence/recovery
        │   ├── Smart Select UI/command palette
        │   └── model validation/final export UI
        │
        └── local Python backend
            ├── FastAPI bridge
            ├── image-model routing
            ├── 3D-model routing
            ├── semantic selection
            ├── geometry analysis/remesh/repair/thickness
            └── managed AI component store
```

## Installed storage layout

```text
<InstallRoot>/
├── Miniscuplter.Launcher.exe
├── Miniscuplter.Updater.exe
├── launcher.settings.json
├── setup_ai_backend.bat
├── App/
│   ├── Miniscuplter.exe
│   └── ai_backend/
│       └── .venv/
└── AIData/
    ├── models/
    ├── tools/
    ├── components.json
    └── quality_runtime.json
```

Projects, reusable model/parts library and STL exports are configured independently inside the app and do not have to live under the install root.

## Local AI routing

Model choice and quality are deliberately separate concepts. The central quality preset controls how expensive an operation is; the model router chooses which installed specialist should perform the requested role. Explicit user provider choice wins over automatic routing.

Heavy models are released before switching specialists to reduce VRAM pressure. The recommended 8 GB stack is SDXL + TripoSR + Hunyuan3D Shape + CLIPSeg; FLUX and PartCrafter are optional heavier routes.

## Geometry model

Scene editing stays non-destructive as long as practical. Separate objects, parts and AI patches remain editable until the user explicitly applies a destructive operation.

Validated single meshes may export directly. Filled voxel reconstruction is used for repair, remesh, selected-detail union and optional final scene bake, but is never mandatory because it can soften detail smaller than the chosen voxel pitch.

Model analysis treats watertightness, winding, open/non-manifold edges and degenerates as structural properties. Thickness and feature-size information are advisory rather than universal validity requirements.

## Persistence and safety

Later stabilization passes make project save/load and destructive geometry operations transactional where practical. Topology-changing actions invalidate dependent rig/selection data rather than silently retaining stale references. Final STL export uses the guarded validation path introduced in v0.9.5+.

## Launcher/updater safety

Model installation/update is staged before live replacement, validated before state commit and rolled back on swap failure. Model mutations are blocked while the editor is running.

Application updates are staged through a temporary updater executable. Program files are replaced transactionally while AIData/user data and the installed AI `.venv` are preserved. A runtime fingerprint detects when a preserved Python environment no longer matches the updated backend and requires Repair AI Runtime.

## External dependencies

- Godot 4.7.2 .NET for source/release export
- .NET 8 for the editor/launcher/updater
- Python 3.10 x64 for the local AI runtime
- Git for managed specialist code repositories
- NVIDIA CUDA recommended for practical local AI inference

Model weights are never stored in the Git repository; the launcher/model manager downloads them explicitly into AIData.
