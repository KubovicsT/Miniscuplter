# Miniscuplter v1.0.12

Miniscuplter is a Windows desktop application for **AI-assisted 3D model creation, kitbashing, posing, sculpting, local detail refinement, model validation/repair and final STL export**.

v1.0.12 is the first safety/foundation bridge following the repository-wide takeover audit. It keeps the current v1.0.11 creative workflow while hardening application delivery, guarded save/export entry points and real geometry correctness before the larger editor-core refactor begins.

## Product boundary

Miniscuplter creates the finished 3D model. It is not a slicer: support generation, slicing, printer profiles and printer toolpaths remain outside the application.

## Main workflow

1. **2D** — generate a concept or load your own image, edit it with AI if desired, review it in the large preview, then explicitly accept the current image as the 3D baseline.
2. **3D** — generate from the accepted baseline, sculpt, mask/refine areas, generate alternatives, transform objects and kitbash/attach parts.
3. **Rig & Pose** — create/refine rigs, edit joints, use IK and pose the model.
4. **Cleanup & Export** — inspect/repair/remesh when needed, analyze structure/thickness, finalize and export STL.

The target user does not need to be a 3D artist. The reference hardware class remains an ordinary Windows machine around a GTX 1080 8 GB GPU and 16 GB system RAM; local inference must be useful there rather than treating that hardware as an edge case.

## v1.0.12 safety bridge

- The updater now owns an explicit list of application paths instead of treating every unknown install-root folder as replaceable application content.
- Same-volume update work records a persistent transaction journal and can recover interrupted rollback/preservation state on the next updater run.
- Partial managed-tree backup failures no longer trigger deletion of unmoved/restored old application files.
- The old application backup is retained until the newly installed launcher actually starts and acknowledges a healthy visible startup. Failed startup rolls back to the previous application.
- The launcher update downloader disposes its ZIP writer before SHA-256 verification and rename, avoiding the Windows file-sharing hazard identified in the audit.
- The final user-facing export controls are rebound to the validated STL export path; the old recovery autosave is replaced by the guarded transactional project writer and stored under the configured project root.
- Geometry finite checks no longer call the nonexistent `Trimesh.is_finite` property.
- STL topology analysis now welds coincident per-face vertices in a separate analysis representation, so a normal closed STL is not falsely reported as having open edges merely because of STL vertex duplication.
- `rtree` is part of the runtime contract for ray/thickness queries, and CI now executes real box/STL analysis, remesh and thickness fixtures.
- The TripoSR adapter registers the downloaded source tree before importing `tsr.utils` and accepts the current upstream `extract_mesh` result shape rather than assuming a nested mesh list.
- v1 release CI/tag gates now cover `v1.x` generally instead of assuming every future release remains `v1.0.x`.

Because `requirements.txt` changed to include the spatial-index dependency, an installation upgraded from v1.0.11 may require **Repair AI Runtime** before using thickness/ray-based geometry tools. Existing model weights are not part of that repair and remain preserved.

## AI provider tiers

| Role | Lower hardware | Mid tier | High/workstation tier |
|---|---|---|---|
| Concept | SDXL | Z-Image Turbo / FLUX.2 Klein | Qwen-Image-2512 |
| Image edit/detail | SDXL | FLUX.2 Klein | Qwen-Image-Edit |
| Fast image → 3D | TripoSR | Stable Fast 3D | SPAR3D |
| Quality image → 3D | Hunyuan3D 2mini | Hunyuan3D 2.1 | TRELLIS.2 4B |
| Structured parts | PartCrafter | PartCrafter | PartPacker |
| Smart Select | CLIPSeg + rig/metadata/geometry | same hybrid selector | same hybrid selector |

Auto routing considers detected VRAM and installed providers. Explicit user selection wins and unsupported/uninstalled providers fail clearly rather than silently changing models.

### Windows compatibility boundary

Stable Fast 3D and SPAR3D have upstream experimental Windows support and are installed into isolated per-provider Python environments to protect Miniscuplter's shared AI runtime. PartPacker likewise uses an isolated runtime. TRELLIS.2 is treated as an external Linux/WSL2 runtime rather than pretending it is a native Windows provider.

## Recommended GTX 1080 / 8 GB starting set

```text
SDXL
Hunyuan3D 2mini
Stable Fast 3D
TripoSR
CLIPSeg
```

Provider presence is not the same as certified inference readiness. The takeover plan is to qualify a smaller supported default set with real Windows/GPU tests and keep less-proven providers experimental.

## Architecture direction

The current Godot/C# desktop and local Python boundary remain useful. The next development phase is a **major architectural refactor with selective subsystem rewrites**, not continued layering of `Main.V*.cs` patches. The planned replacement foundation includes stable object IDs, full-state transactional commands/undo, indexed binary project assets, durable 2D/3D/rig handoffs, a real local job broker with job IDs/cancellation/resource ownership, one provider registry and one storage service.

v1.0.12 intentionally makes delivery and current-user data safer before those internal structures are replaced.

## Release / self-update path

A finished release produces:

```text
Miniscuplter-Setup-1.0.12.exe
Miniscuplter-win-x64.zip
Miniscuplter-win-x64.zip.sha256
```

The launcher selects the highest stable semantic-versioned GitHub Release and offers it when newer than the installed version. Updates preserve configured AI/model data, interrupted model stages, Python environments/runtime caches, projects, parts libraries, exports, user data and launcher settings.

Published releases are treated as immutable. New application changes go to a new version branch and release.

## Documentation

- `docs/PROJECT_CONTEXT.md` — product intent and takeover context
- `docs/ARCHITECTURE.md` — current application architecture
- `docs/AI_MODELS.md` — provider/model architecture and hardware tiers
- `docs/BUILD_AND_RELEASE.md` — CI, GitHub Release publishing and self-update packaging
- `docs/RUNTIME_TESTING.md` — runtime validation protocol
- `docs/RELEASE_HISTORY.md` — milestone/branch history

## Validation boundary

CI/static validation plus the full Windows Godot export and installer smoke test are required before v1.0.12 is published. v1.0.12 additionally runs real geometry fixtures. Actual CUDA inference for every optional model and destructive updater fault injection on a real installed Windows fixture remain runtime/integration test requirements rather than claims inferred from compilation alone.
