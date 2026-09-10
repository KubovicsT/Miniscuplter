# Miniscuplter Project Status

> Fast-moving project dashboard. Update at the end of every meaningful development session. Do not use this file as a substitute for inspecting the actual branch, release and CI state.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 starting point:** exact v1.0.19 released commit above.
- **v1.0.19 release validation:** passed editor/launcher/updater C# builds, Python compilation/core tests, job-progress tests, real geometry regressions, Stage-B Core tests, release audit, verified Godot 4.7.2 Windows export, release hash verification and silent installer smoke-install.
- **Release pipeline run:** `34469538260`.

The first publication helper attempt failed only because the temporary helper workflow referenced the wrong Stage-B test project filename (`Core.Tests/Core.Tests.csproj`). The actual project is `Core.Tests/Miniscuplter.Core.Tests.csproj`. No application defect was found by that failure. The helper was corrected, the full pipeline passed, v1.0.19 was published, and the helper branch was reset to the released application commit.

## Current development phase

The project remains in the transition between **Stage B (replacement foundation/migration harness)** and **Stage C (prove one reliable end-to-end thin slice)** from the accepted Astra refactor plan.

Stage A safety work is substantially implemented. Stage B exists materially in code but remains mixed with the legacy editor. Stage C is not yet considered acceptance-proven on the real target machine.

## Overall completion estimate

**56% toward the defined finished product**.

This remains unchanged after publishing v1.0.19 because publication/CI does not prove the user-observed viewport and storage defects are resolved on the GTX 1080 target machine. Completion is acceptance-based, not a count of commits or controls.

| Workstream | Weight | Estimated completion | Basis |
|---|---:|---:|---|
| Application foundation / state / persistence | 15% | 50% | Stable IDs, project models/history/store and migration work exist; legacy `Main.V*.cs` still remains authoritative in large areas. |
| 2D workflow | 12% | 72% | Generation/import, center canvas, regional edit, Enhance, preview and multi-source references exist; candidate/provenance and target-hardware acceptance still need consolidation. |
| 3D generation | 15% | 48% | Multiple providers and routing exist; provider qualification and reliable target-machine thin-slice proof remain incomplete. |
| 3D editing / sculpt / kitbash | 15% | 45% | Significant legacy functionality exists, but state/history/performance integration with the replacement core is incomplete. |
| Rig & Pose | 10% | 40% | Rigging/IK/pose features exist historically; independent rest-state and new-core integration need substantial work. |
| Cleanup / validation / export | 8% | 65% | Repair/remesh/thickness/validated export exist with real geometry regressions; new project-history integration and broader workflow qualification remain. |
| AI runtime / provider reliability | 10% | 65% | Repair, CUDA checks, resumable installs, routing and progress infrastructure are substantial; optional providers are not all inference-qualified on Windows/reference hardware. |
| UI / UX | 8% | 58% | Four-stage workflow, responsive layout, center 2D canvas, settings and newer viewport tooling exist; legacy composition and runtime visual defects remain a risk. |
| Launcher / updater / release reliability | 4% | 92% | Verified resumable self-update, transaction/rollback safeguards and installer smoke testing are mature; destructive fault-injection coverage can still improve. |
| Testing / hardware qualification | 3% | 50% | C#, Python, core, geometry, audit, package and installer checks are strong; real CUDA/provider/GUI acceptance matrix is incomplete. |

Weighted result: approximately **56%**.

Do not increase this number simply because code was written. Increase workstream completion when acceptance criteria become demonstrably true.

## Working / comparatively mature areas

- Windows launcher/updater and GitHub Release delivery.
- Data-preserving application update path with integrity checking and rollback safeguards.
- AI runtime setup/repair, CUDA validation and resumable model installation.
- 2D concept generation and user-image input.
- Center/large 2D image workflow and regional selection.
- Prompt-driven AI edit and context-aware regional Enhance implementation.
- Openverse + Wikimedia reference discovery and adoption into the 2D workflow.
- Explicit 2D baseline acceptance concept.
- Multiple image-to-3D provider adapters and hardware-aware routing.
- Sculpting, transforms, selection, parts/attachments and historical kitbash tools.
- Rigging/IK/pose feature set in the legacy implementation.
- Geometry validation, voxel/remesh, thickness analysis and guarded STL export.
- Real geometry regression tests for important topology/remesh/thickness behavior.
- Stage-B `Core` work for stable IDs, project models, history, indexed project storage and legacy migration.
- Structured backend job-progress infrastructure.
- Editable Low/Medium/High/Ultra/custom quality settings in the newer Settings surface.

## Released in v1.0.19, awaiting real-machine acceptance

### 3D viewport — MS-009

v1.0.19 contains `Main.V1019ViewportPipeline.cs`, which explicitly configures the native `SubViewportContainer`, world/camera ownership, starter mesh, materials, grid, selection/gizmo and a rendered-frame diagnostic probe. This is now distributed to users, but historical failures were machine/UI-render specific. Status remains **FIXED - NEEDS USER VERIFICATION** until tested on the actual machine.

### Storage containment — MS-013

v1.0.18 introduced `AppDataRoot`; v1.0.19 adds backend storage canonicalization/containment and additional Windows path tests. This is now distributed, but representative real jobs must confirm no important working artifacts still escape to C:\ AppData/TEMP. Status is **FIXED - NEEDS USER VERIFICATION**.

## Partially working / not yet acceptance-proven

### AI job feedback

v1.0.17/v1.0.18 added job progress for concept generation, image edit and 3D operations, including stages, elapsed time/cancellation and backend polling. More provider-specific real step reporting is still desirable. Never convert activity heartbeats into fake inference percentages.

### Quality presets

The newer Settings quality panel exposes preset parameters and custom preset create/clone/rename/save/delete operations. It still needs real UX verification and eventual migration into the cleaner declarative UI.

### Image-to-3D / provider readiness

Several providers exist, but the project does not yet have a small, fully qualified default provider set with repeatable target-machine benchmarks and self-tests. Provider presence/install state must not be confused with inference readiness.

### New project foundation

`Core` contains meaningful Stage-B work, but the normal editor still relies heavily on legacy scene/widget state. The replacement project model is not yet the sole production source of truth.

## Known missing or incomplete product work

- Make the new project/domain state authoritative across the application.
- Full-state transactional undo/redo for transform, sculpt, AI Apply, topology and dependent metadata.
- Revision-bound selections/masks and explicit transfer/invalidation rules.
- Authoritative local Job Broker with durable job IDs, resource ownership, isolated workers, stale-result conflict handling and recovery.
- Provider qualification registry with separate downloaded/installed/importable/device-tested/inference-tested states.
- Reliable target-hardware Stage-C thin slice.
- Candidate comparison and model experiment branches integrated with the new core.
- Spatial acceleration / better sculpt/picking performance.
- Protected-region semantics through topology and AI replacement.
- Independent rest mesh and fully reversible modern Rig & Pose model.
- Better landmark-assisted rig correction.
- Named user checkpoints/history and before/after comparison.
- Guided first-model/example workflow.
- Multi-view/turnaround consistency assistance later, after the core flow is reliable.
- Richer GLB interchange where justified.
- Retirement of obsolete `Main.V*.cs` layers only after migration/acceptance coverage exists.

## Current highest-priority issues

1. **MS-009 — 3D viewport/grid/gizmo reliability.** v1.0.19 is now released; needs target-machine verification.
2. **MS-018 — Stage-C end-to-end thin slice is not yet qualified.** This remains the primary product-level acceptance gap.
3. **MS-013 — storage containment.** v1.0.19 is released; needs representative real-machine path verification.
4. **MS-022 — provider qualification/self-tests.** This is the highest-value independent engineering task while v1.0.19 awaits user verification.
5. **MS-020 — authoritative Job Broker/stale-result handling.** Current progress infrastructure is useful but not the final architecture.
6. **MS-019 — legacy application-state architecture.** Stage-B replacement exists but is not yet authoritative.

## Immediate engineering priority for v1.0.20

While v1.0.19 awaits real-machine viewport/storage verification, advance Stage C without depending on that feedback:

1. Build a provider self-test/readiness contract that distinguishes downloaded, installed, importable, device-tested and inference-tested states.
2. Start with the intended lightweight/default 3D route rather than broad optional-provider expansion.
3. Bind accepted 2D baseline → 3D job → resulting candidate/import to stable Project/Object/Revision identity.
4. Prevent a stale AI result from silently overwriting a newer revision; make acceptance/apply transactional.
5. Keep legacy UI compatibility while moving this one vertical slice onto the Stage-B core.

If the user reports v1.0.19 still renders a blank viewport or leaks working files to C:, that regression becomes the immediate priority.

## Next milestone acceptance criteria

The next major milestone is achieved when, on the target Windows machine:

- a 2D image can be generated/imported and edited;
- an accepted baseline persists as a project revision;
- one supported lightweight 3D provider passes its self-test and generates a mesh;
- the resulting mesh is visibly present in the 3D viewport with grid, camera, selection and gizmo;
- cancellation followed by a new job works reliably;
- save/reload preserves the result;
- basic cleanup works;
- export produces a validated STL;
- required working files remain inside the configured Miniscuplter data root;
- peak RAM/VRAM and elapsed time are recorded for the reference machine.

## User input currently required

No product/design decision blocks autonomous work.

For acceptance evidence, the user should update through the launcher to **v1.0.19** and test the 3D viewport/grid/model/gizmo plus representative data-output paths on the real GTX 1080 machine. Any failure should include the v1.0.19 viewport diagnostics/render-probe text and the unexpected path(s).