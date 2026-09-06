# Miniscuplter v1.0.10

Miniscuplter is a Windows desktop application for **AI-assisted 3D model creation, kitbashing, posing, sculpting, local detail refinement, model validation/repair and final STL export**.

v1.0.10 packages the responsive-layout fixes validated after v1.0.9: the 3D viewport now follows window/split resizing, and the finished tool tabs scroll vertically so long AI job feedback cannot push later controls such as the 2D preview out of reach.

## Product boundary

Miniscuplter creates the finished 3D model. It is not a slicer: support generation, slicing, printer profiles and printer toolpaths remain outside the application.

## AI provider tiers

| Role | Lower hardware | Mid tier | High/workstation tier |
|---|---|---|---|
| Concept | SDXL | Z-Image Turbo / FLUX.2 Klein | Qwen-Image-2512 |
| Image edit/detail | SDXL | FLUX.2 Klein | Qwen-Image-Edit |
| Fast image → 3D | TripoSR | Stable Fast 3D | SPAR3D |
| Quality image → 3D | Hunyuan3D 2mini | Hunyuan3D 2.1 | TRELLIS.2 4B |
| Structured parts | PartCrafter | PartCrafter | PartPacker |
| Smart Select | CLIPSeg + rig/metadata/geometry | same hybrid selector | same hybrid selector |

The launcher/backend component status exposes role, tier, VRAM guidance, native-platform support and hardware-fit metadata. Auto routing considers detected VRAM and installed providers. Explicit user selection always wins and unsupported/uninstalled providers fail clearly rather than silently changing models.

### Windows compatibility boundary

Stable Fast 3D and SPAR3D have upstream experimental Windows support and are installed into isolated per-provider Python environments to protect Miniscuplter's shared AI runtime. PartPacker likewise uses an isolated runtime. TRELLIS.2 upstream officially targets Linux with >=24 GB NVIDIA VRAM; Miniscuplter manages its source/capability and invokes a configured native-Linux/WSL2 runtime through `MINISCULPTER_TRELLIS2_COMMAND` instead of pretending it is a native Windows provider.

## Recommended GTX 1080 / 8 GB starting set

```text
SDXL
Hunyuan3D 2mini
Stable Fast 3D
TripoSR
CLIPSeg
```

SPAR3D low-VRAM mode is available as an additional experiment. Hunyuan3D 2.1 remains useful with offload. Qwen, TRELLIS.2 and PartPacker are intended primarily for larger GPUs.

## Current v1.x UX/runtime improvements

- **v1.0.8** — visible concept-generation status/cancel/error UI, self-healing runtime repair, explicit PyTorch/CUDA validation, and model-install TEMP/pip-cache relocation into AIData.
- **v1.0.9** — VRAM-first SDXL execution modes with an 85% soft allocator ceiling, visible 3D floor grid, Settings for model/quality/GPU routing, large zoomable 2D preview, Wikimedia Commons reference search, and cancellable 2D→3D job feedback.
- **v1.0.10** — responsive editor layout: tool tabs scroll vertically, the right panel uses a proportional bounded width, and the actual Godot `SubViewport` tracks its host during window/splitter resizing.

The v1.0.7 updater/storage fixes remain in place: storage-aware application update cache selection, same-volume move-based rollback/staging, free-space preflight, hidden updater console, preserved AI models/runtime caches/projects, and resumable application downloads.

## Release / self-update path

A finished release produces:

```text
Miniscuplter-Setup-1.0.10.exe
Miniscuplter-win-x64.zip
Miniscuplter-win-x64.zip.sha256
```

On launcher startup, application-update checks are enabled by default. The launcher queries the repository's stable GitHub Releases, selects the highest semantic version, and offers an update when it is newer than the installed launcher. It never installs code silently: the user approves the update first.

Automatic update requires all of the following:

- an asset named exactly `Miniscuplter-win-x64.zip`;
- an exact release asset byte size;
- a SHA-256 from GitHub's release-asset digest or the published `.sha256` sidecar;
- a package-internal `release.json` whose version matches the release being installed.

Application updates preserve existing AI model data, interrupted model stages, a configured `DataRoot`, the Python `.venv`, runtime caches, legacy backend model data, projects, parts library, exports, user data, launcher settings, and a separately installed runtime.

## Documentation

- `docs/ARCHITECTURE.md` — application architecture
- `docs/AI_MODELS.md` — provider/model architecture and hardware tiers
- `docs/BUILD_AND_RELEASE.md` — CI, GitHub Release publishing and self-update packaging
- `docs/RUNTIME_TESTING.md` — runtime validation protocol
- `docs/RELEASE_HISTORY.md` — milestone/branch history

## Validation boundary

CI/static validation plus the full Windows Godot export and installer smoke test are required before v1.0.10 is published. Actual CUDA inference for every optional model still requires runtime testing on representative hardware; upstream Windows support for some specialist models is explicitly experimental.
