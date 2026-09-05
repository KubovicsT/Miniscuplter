# Miniscuplter v1.0.6

Miniscuplter is a Windows desktop application for **AI-assisted 3D model creation, kitbashing, posing, sculpting, local detail refinement, model validation/repair and final STL export**.

v1.0.6 is a reliability/update release on top of the v1.0.5 hardware-aware local-AI stack. It fixes cross-version editor integration seams found during the v1.0.5 audit and turns the launcher self-updater into a verified, resumable, data-preserving update path.

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

## v1.0.6 reliability fixes

- Restores the stable internal `Print` panel compatibility name while continuing to display the release-facing tab as **Model**, so older additive geometry/validation controls install again.
- Routes command-palette `/rig` generation through the same v0.9.5 validation/rollback guard used by the UI buttons.
- Fixes final 3D-detail apply so the source/patch STL paths are passed to the canonical voxel-remesh API instead of passing an in-memory mesh to a path-based function.
- Corrects Windows, launcher, updater, backend and installer version metadata to v1.0.6.
- Keeps the v1.0.5 AI runtime fingerprint compatible when runtime dependencies/setup did not actually change, avoiding a cosmetic multi-GB runtime reinstall.

## Release / self-update path

A finished version branch produces:

```text
Miniscuplter-Setup-1.0.6.exe
Miniscuplter-win-x64.zip
Miniscuplter-win-x64.zip.sha256
```

On launcher startup, application-update checks are enabled by default. The launcher queries the repository's stable GitHub Releases, selects the highest semantic version, and offers an update when it is newer than the installed launcher. It never installs code silently: the user approves the update first.

Automatic update requires all of the following:

- an asset named exactly `Miniscuplter-win-x64.zip`;
- an exact release asset byte size;
- a SHA-256 from GitHub's release-asset digest or the published `.sha256` sidecar;
- a package-internal `release.json` whose version matches the release being installed.

The ZIP download is stored under the configured AI data root in a persistent `update-cache`, supports HTTP range resume, and is SHA-256 verified before the staged updater starts. The staged updater verifies the SHA-256 and package version again independently before changing installed files.

Application updates preserve existing AI model data, interrupted model stages, a configured `DataRoot`, the Python `.venv`, runtime caches, legacy backend model data, projects, parts library, exports, user data, launcher settings, and a separately installed runtime. Expensive nested runtime directories are parked with same-volume directory moves rather than copied into the update backup. Managed application files are replaced transactionally and the previous tree is restored if validation/copy fails.

## Documentation

- `docs/ARCHITECTURE.md` — application architecture
- `docs/AI_MODELS.md` — provider/model architecture and hardware tiers
- `docs/BUILD_AND_RELEASE.md` — CI, GitHub Release publishing and self-update packaging
- `docs/RUNTIME_TESTING.md` — runtime validation protocol
- `docs/RELEASE_HISTORY.md` — milestone/branch history

## Validation boundary

CI/static validation is required before v1.0.6 is considered code-green. Actual CUDA inference for every optional model still requires runtime testing on representative hardware; upstream Windows support for some specialist models is explicitly experimental.
