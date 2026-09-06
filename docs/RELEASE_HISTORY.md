# Miniscuplter Release History and Branch Map

Each release branch is preserved as a historical snapshot. Documentation-only corrections may be backported to old release branches; feature code is not silently backported.

| Branch | Milestone | Primary addition |
|---|---|---|
| `main` | v0.1 | Core editor, STL IO, basic sculpting, provider adapters |
| `v0.2` | Managed local AI | SD2.1/Hunyuan component management and hardware detection |
| `v0.3` | User image input | Artwork/reference image → AI workflow |
| `v0.4` | Voxel/remesh | Volumetric geometry context and reconstruction |
| `v0.5` | AI patch workflow | Masks, candidates, quality controls, learned ETA |
| `v0.5.5` | Stabilization | Completion/integration pass across v0.1–v0.5 |
| `v0.6` | Rigging/posing | Quick Rig, skeleton editing, posing, IK |
| `v0.7` | Parts/kitbashing | Parts library, sockets and attachment workflow |
| `v0.8` | Advanced sculpting | Expanded brush set, symmetry, masks, remesh |
| `v0.9` | Final model | Validation, repair/finalization, thickness heatmap |
| `v0.9.5` | Safety/integration | Transactional projects, guards, validated export |
| `v0.9.6` | Smart Select | Command palette and semantic selection |
| `v0.9.7` | Quality presets | Central Low/Medium/High/Ultra/custom runtime settings |
| `v0.9.8` | Multi-model AI | SDXL/FLUX/TripoSR/Hunyuan/PartCrafter routing and detail refinement |
| `v0.9.9` | Productization | Launcher, model/app updates, installer and configurable storage |
| `v1.0` | Release candidate | Full code audit, failure-path hardening, reproducible installer artifact |
| `v1.0.5` | Local-AI expansion | Hardware-aware model matrix, verified/resumable model installs, Xet, process-lifetime hardening |
| `v1.0.6` | Update/integration hardening | Cross-version editor fixes plus verified, resumable, data-preserving self-update and GitHub Release publishing |
| `v1.0.7` | Update storage hardening | Storage-aware update cache, no TEMP-only staging, low-space preflight, move-based rollback, hidden updater console |
| `v1.0.8` | AI runtime diagnostics | Self-healing runtime repair, visible AI job feedback, CUDA validation, model-install TEMP/cache relocation |
| `v1.0.9` | AI/editor UX | VRAM-first SDXL modes, visible grid, Settings, large 2D preview, reference-search repair, 2D→3D feedback |
| `v1.0.10` | Responsive editor layout | Resizable 3D viewport and scrollable tool panels so dynamic AI feedback cannot push controls out of view |
| `v1.0.11` | Workflow consolidation | Four task tabs, explicit 2D baseline approval, sculpting in 3D workflow, launch grid hardening |
| `v1.0.12` | Takeover safety bridge | Updater transaction journal/launch validation, guarded autosave/export, real geometry fixes/tests, TripoSR adapter repair |

## v1.0.6 integration fixes

The v1.0.6 audit closed several seams discovered while reading the full v1.0.5 codebase:

- the release-facing **Model** tab again uses the stable internal `Print` compatibility name before additive version installers run;
- `/rig quick|universal` uses the same guarded/validated rig path as the rig UI buttons;
- final 3D-detail application calls the canonical path-based voxel-remesh API correctly;
- Windows file/product metadata, launcher/updater assemblies, backend and installer report one release version consistently;
- launcher application updates are public-release based, SHA-256 verified, resumable and transactional while preserving AI/model/runtime data.

## v1.0.7 update-storage fixes

v1.0.7 removed most system-drive/TEMP pressure from the normal self-update path:

- update downloads choose among DataRoot, the installation drive and TEMP based on reusable partial data and available free space;
- the staged updater executable lives with the update cache rather than `%TEMP%`;
- the updater is `WinExe`, so normal updates no longer open an empty console window;
- extraction and rollback staging happen beside the installation and are preflighted against the release's expanded ZIP size;
- the old managed app and the new extracted app are moved on the same volume instead of copied, while AI/runtime data remains parked and preserved.

## v1.0.10 responsive-layout fixes

- the 3D SubViewport follows the actual viewport host size whenever the window/splitter changes;
- the right tool panel uses a proportional bounded width instead of the old fixed 890 px split offset;
- completed main tabs are wrapped in vertical scroll containers, so long AI status/detail text cannot make later controls unreachable;
- prompt and embedded 2D-preview minimum heights remain useful without forcing the rest of the AI panel out of frame.

## v1.0.11 workflow consolidation

v1.0.11 replaced the visible milestone/version stack with the product workflow **2D → 3D → Rig & Pose → Cleanup & Export**, while preserving advanced legacy controls under Settings. It also introduced explicit accepted 2D baseline state for the current session and rebuilt a brighter grid during editor launch.

This improved the product surface but did not replace the underlying historical `Main.V*.cs` application architecture. The later takeover audit therefore treats v1.0.11 as the behavioral/migration baseline rather than the desired final internal design.

## v1.0.12 takeover safety bridge

The takeover audit recommended a major architectural refactor with selective subsystem rewrites, but delivery and current-user data safety must be improved before that replacement is distributed. v1.0.12 is that first bridge:

- updater-owned paths are explicit; unknown install-root content is not automatically treated as disposable application data;
- an on-disk transaction journal records update phases and enables recovery of interrupted rollback/preserved-runtime state;
- partial backup failure is distinguished from a completed backup so restored/unmoved old files are not deleted by the caller;
- rollback data is retained until the new launcher successfully starts and writes an update-health acknowledgement;
- the launcher closes the application ZIP writer before SHA verification/rename on Windows;
- final export entry points use the validated STL writer and recovery autosave uses the guarded transactional project writer under the configured project root;
- trimesh finite-coordinate validation uses the actual vertex array rather than a nonexistent `Trimesh.is_finite` member;
- topology analysis welds coincident STL vertices in an analysis copy, preventing false open-edge reports from STL per-face vertex duplication;
- CI runs real closed-STL, voxel-remesh and thickness fixtures, with `rtree` included in the runtime dependency contract;
- the TripoSR adapter fixes source import order and handles current upstream mesh-return semantics;
- v1 release CI/tag gates now apply consistently across future `v1.x` minor versions.

The next planned phase is the new foundation: stable UUID object identity, full-state commands/undo, indexed project assets/migration, declarative workflow UI and a real local job broker. Those are intentionally not emulated by adding more permanent compatibility layers to the legacy `Main` class.

## Non-release branches

Development branches such as `*-work`, `*-dev`, `*-temp`, `v0.2-ai`, `v0.2-build` and `v0.4-impl` are historical implementation branches. They are not release targets and should not be used for runtime testing unless investigating old development history.

## Product scope

Miniscuplter creates and finalizes the 3D model. It does not slice, generate print supports, manage printer profiles, optimize exposure settings, or produce printer toolpaths. STL remains the primary final format, while richer interchange formats may be added when they preserve useful scene/rig/material information.

## Current testing target

Use **v1.0.12** for the current stable safety-bridge runtime validation once published. Older release branches remain immutable historical baselines and migration fixtures.
