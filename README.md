# Miniscuplter v1.0.7

Miniscuplter is a Windows desktop application for **AI-assisted 3D model creation, kitbashing, posing, sculpting, local detail refinement, model validation/repair and final STL export**.

v1.0.7 is a storage-safety patch on top of v1.0.6. It keeps the verified/resumable self-update path, but removes the remaining assumptions that Windows TEMP and the system drive have plenty of free space.

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

## v1.0.7 updater/storage fixes

- Application-update downloads choose a safe cache location based on available free space. The configured `DataRoot` is preferred, then a sibling cache on the installation drive, with Windows TEMP only as a final fallback when it has enough room.
- Existing `.partial` application downloads are reused from the best available cache location instead of forcing a fresh download.
- The staged updater executable is launched from the update cache rather than copied into `%TEMP%`.
- The updater itself is now a Windows GUI executable, so a normal successful update no longer opens an empty console window.
- Update extraction/rollback storage is created beside the installation instead of under Windows TEMP.
- The updater calculates the ZIP's expanded size and checks free space before changing installed files.
- The old managed application is moved into same-volume rollback storage rather than copied. Persistent AI/runtime data is parked with directory moves as before, so multi-GB model/runtime data is not duplicated.
- The extracted new managed tree is moved into place on the same volume rather than copied again, minimizing peak temporary storage.
- If installation validation fails, the managed tree and parked runtime/data are restored transactionally.

The v1.0.6 integration fixes remain in place: Model/Print compatibility, guarded command-palette rig generation, corrected 3D detail apply, verified public-release self-update, SHA-256/package-version checks, and preserved AI/runtime/model data.

## Release / self-update path

A finished version branch produces:

```text
Miniscuplter-Setup-1.0.7.exe
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

CI/static validation is required before v1.0.7 is considered code-green. Actual CUDA inference for every optional model still requires runtime testing on representative hardware; upstream Windows support for some specialist models is explicitly experimental.
