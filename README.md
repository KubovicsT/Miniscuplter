# Miniscuplter v1.0

Miniscuplter is a Windows desktop application for AI-assisted 3D model creation, kitbashing, posing, sculpting, local detail refinement, model validation, repair, and STL export.

The v1.0 product goal is deliberately narrower than a slicer: Miniscuplter produces the finished 3D model. It does **not** generate print supports, slice models, manage printer profiles, optimize exposure settings, or export printer toolpaths. A finished STL may be used for 3D printing, rendering, archival, another modeling package, or any other geometry workflow.

## v1.0 workflow

Typical workflow:

1. Start from an imported STL, starter mesh, saved `.msculpt` project, user image, AI concept, or reusable part.
2. Build the scene non-destructively with separate objects, sockets, attachments, and reusable library parts.
3. Optionally create or edit a rig and pose the model.
4. Sculpt with the advanced brush set and masks.
5. Use Smart Select to identify semantic regions and route selected areas to local AI refinement.
6. Inspect structural model integrity and optional thickness information.
7. Repair or finalize/union geometry only when required.
8. Export the finished model as a validated STL.

A valid single mesh does not have to be voxel-remeshed before export. Avoiding unnecessary final remeshing preserves the source mesh's original detail.

## Major systems

### Core editor

- Persistent 3D viewport and multi-object scene
- STL import/export
- Object transform, duplicate, delete, framing, and transform baking
- Undo/redo for destructive mesh operations
- `.msculpt` project save/load with transactional validation and recovery safeguards
- Configurable project, reusable model-library, and STL-export locations

### Sculpting

Brushes include Draw, Smooth, Inflate, Grab, Crease, Flatten, Pinch, Scrape, Clay, and SnakeHook, with falloff, symmetry, masks, cursor feedback, and optional voxel remeshing.

### Rigging and posing

- Quick rig generation
- Optional universal external rig provider
- Editable skeleton visualization
- Pose preview/reset/apply
- Approximate CPU skinning
- IK support
- Rig-aware project persistence and topology invalidation

### Parts, sockets, and kitbashing

- Reusable parts library
- Categories and attachment sockets
- Mount points, normals, roll, offsets, rotation, and scale
- Portable library metadata in saved projects
- User-configurable library location

### Smart Select and command palette

Press **Space** to open the command palette. Smart Select can combine metadata/rig evidence, local CLIPSeg multi-view semantic segmentation, and geometry fallback selection.

Important commands include:

- `/s <region>`, `/s+ <region>`, `/s- <region>`
- `/grow`, `/shrink`, `/smooth`, `/invert`, `/clear`
- `/hide`, `/show`, `/isolate`, `/frame`
- `/remesh [0.04-5 mm]`
- `/analyze`, `/thickness [target]`
- `/rig quick`, `/rig universal`
- `/pose preview|reset|apply`
- `/edit <prompt>`
- `/detail2d <prompt>`, `/detail3d <prompt>`
- `/detail apply`, `/detail discard`
- `/ai routes`

### Multi-model local AI

The AI router uses specialist models rather than assuming one model should do every job. Models are installed independently and loaded sequentially so they do not all remain resident in VRAM.

Supported managed components:

- Stable Diffusion 2.1 — legacy 2D fallback
- Stable Diffusion XL Base 1.0 — primary modern 2D generation/editing route for 8 GB-class hardware
- FLUX.2 Klein 4B — optional heavier 2D specialist
- TripoSR — fast/rough image-to-3D route
- Hunyuan3D 2.1 Shape — quality whole-object and selected-detail 3D route
- PartCrafter — structured multi-part generation
- CLIPSeg — semantic Smart Select

The recommended stack for an 8 GB NVIDIA GPU is SDXL + TripoSR + Hunyuan3D Shape + CLIPSeg. The launcher recommendation is advisory; users may install or select any supported specialist.

### Local detail refinement

2D detail refinement crops the selected region with context, spends the configured model resolution on that crop, and composites it back through the selection mask.

3D detail refinement creates an isolated selected-region reference, reconstructs a detail patch, aligns it to the selected 3D bounds, and imports it as a non-destructive preview. Applying the patch uses a transactional watertight volumetric union; the source mesh is not replaced until the operation succeeds, and Undo remains available.

### Quality presets

Built-in Low, Medium, High, and Ultra presets plus unlimited custom presets configure:

- 2D image resolution, inference steps, guidance, and edit strength
- maximum input-image dimension
- Hunyuan shape steps
- sculpt/remesh voxel pitch
- repair/finalization voxel pitch
- voxel safety budget
- thickness sample budget
- Smart Select view count and render resolution

Hardware detection recommends a starting preset, but never prevents the user from selecting another preset. Explicit user choice persists.

### Model validation and finalization

Structural validation reports:

- watertightness
- winding consistency
- open edges
- non-manifold edges
- connected shells
- degenerate faces
- bounds, surface area, and volume when closed

Feature-size and self-intersection results are explicitly advisory heuristics. Optional thickness analysis is application-agnostic and is not a hard printability rule.

Repair/finalization uses filled voxel reconstruction and can soften details smaller than the selected pitch. It is therefore opt-in rather than mandatory.

## Launcher and installation management

`Miniscuplter.Launcher.exe` is the normal application entry point.

The launcher provides:

- native CPU/RAM/GPU/VRAM hardware detection
- hardware-based starting recommendation
- installed/missing AI model status
- explicit model installation and removal
- upstream model revision checks
- warnings when model updates are available
- user-approved model updates only; model updates are never automatic
- application update checks through GitHub Releases
- user-approved staged application updates
- AI-runtime repair
- application launch

The Windows installer lets the user select the installation location. The chosen folder contains the application, AI runtime, and AI model store and must remain writable by the normal Windows user so the launcher can manage models and updates.

Default install location:

`%LOCALAPPDATA%\Programs\Miniscuplter`

Projects, the reusable model library, and STL exports have independent locations configured inside Miniscuplter and can live on other drives.

## Release/update layout

Typical installed layout:

```text
Miniscuplter/
├── Miniscuplter.Launcher.exe
├── Miniscuplter.Updater.exe
├── launcher.settings.json
├── App/
│   ├── Miniscuplter.exe
│   └── ai_backend/
│       └── .venv/
└── AIData/
    ├── models/
    ├── tools/
    └── components.json
```

Application updates preserve the managed AI data and user data. Model updates are tracked independently by their Hugging Face revision and, where relevant, companion Git inference-code revision.

## Source/build requirements

Development/runtime components currently target:

- Windows 10/11 x64
- Godot 4.7.2 .NET
- .NET 8
- Python 3.10 x64 for the local AI backend
- Git for AI components that use companion source repositories
- NVIDIA CUDA GPU recommended for local AI inference

`build_release.ps1` publishes the self-contained launcher and updater, copies the backend source, optionally exports the Godot Windows application, and creates `Miniscuplter-win-x64.zip`. The Inno Setup definition under `installer/` builds the Windows installer.

## v1.0 validation boundary

The v1.0 branch is intended as the first code-stabilized release candidate. CI verifies C# compilation, Python syntax, release invariants, update-package layout, and installer compilation. GPU inference quality, driver/CUDA behavior, real model download/install behavior, UI interaction, and performance still require runtime testing on target hardware before a public release is declared production-ready.
