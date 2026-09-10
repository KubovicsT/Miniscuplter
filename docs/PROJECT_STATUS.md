# Miniscuplter Project Status

> Fast-moving project dashboard. Update at the end of every meaningful development session. Do not use this file as a substitute for inspecting the actual branch, release and CI state.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 starting point:** exact v1.0.19 released commit above.
- **Latest application/code commit in this run:** `8d394f289e02682bf27a133ed3456c7dc2852df9`.
- **v1.0.19 release validation:** passed editor/launcher/updater C# builds, Python compilation/core tests, job-progress tests, real geometry regressions, Stage-B Core tests, release audit, verified Godot 4.7.2 Windows export, release hash verification and silent installer smoke-install.
- **Release pipeline run:** `34469538260`.

Documentation commits may advance v1.0.20 HEAD past the application commit. Resolve exact HEAD and CI from Git at the start of each run.

## Current development phase

The project remains in the transition between **Stage B (replacement foundation/migration harness)** and **Stage C (prove one reliable end-to-end thin slice)**. Stage A safety work is substantially implemented. Stage B exists materially in code but remains mixed with the legacy editor. Stage C now has a tested Core-level accepted-baseline → revision-bound generation → candidate/apply seam, but the normal editor/backend workflow has not yet been migrated onto it and target-machine acceptance is still missing.

## Overall completion estimate

**56% toward the defined finished product**.

This remains unchanged because the current work creates and verifies important reliability foundations but does not yet prove the actual end-to-end workflow or the user-observed viewport/storage defects on the GTX 1080 target machine. Completion is acceptance-based, not commit-count-based.

| Workstream | Weight | Estimated completion | Basis |
|---|---:|---:|---|
| Application foundation / state / persistence | 15% | 50% | Stable IDs, project models/history/store/migration and now a tested Stage-C generation bridge exist; legacy `Main.V*.cs` still remains authoritative in large areas. |
| 2D workflow | 12% | 72% | Generation/import, center canvas, regional edit, Enhance, preview and multi-source references exist; the production UI still needs to bind accepted baseline to the new Core seam. |
| 3D generation | 15% | 48% | Multiple providers/routing plus readiness-aware preflight and successful-inference qualification recording now exist; target-machine inference qualification and production thin-slice integration remain incomplete. |
| 3D editing / sculpt / kitbash | 15% | 45% | Significant legacy functionality exists, but state/history/performance integration with the replacement core is incomplete. |
| Rig & Pose | 10% | 40% | Rigging/IK/pose features exist historically; independent rest-state and new-core integration need substantial work. |
| Cleanup / validation / export | 8% | 65% | Repair/remesh/thickness/validated export exist with real geometry regressions; new project-history integration and broader workflow qualification remain. |
| AI runtime / provider reliability | 10% | 65% | Repair, CUDA checks, resumable installs, routing/progress, readiness preflight and persisted success qualification exist; real inference-qualified default-provider evidence is still missing. |
| UI / UX | 8% | 58% | Four-stage workflow, responsive layout, center 2D canvas, settings and newer viewport tooling exist; legacy composition and runtime visual defects remain a risk. |
| Launcher / updater / release reliability | 4% | 92% | Verified resumable self-update, transaction/rollback safeguards and installer smoke testing are mature; destructive fault-injection coverage can still improve. |
| Testing / hardware qualification | 3% | 50% | C#, Python, Core, geometry, audit, package and installer checks are strong; real CUDA/provider/GUI acceptance matrix is incomplete. |

Weighted result remains approximately **56%**.

## Current v1.0.20 work

### MS-022 — provider qualification

`ai_backend/provider_readiness.py` provides separate downloaded, installed, importable, device-tested and inference-tested states with persisted timestamp, runtime/provider revision, device/failure details and optional benchmark data.

Generation-time preflight covers the main single-mesh Stage-C candidates: TripoSR, Hunyuan3D 2mini, Hunyuan3D 2.1 Shape, Stable Fast 3D and SPAR3D. The probes test provider imports and CUDA visibility without loading model weights. Auto 3D routing skips providers that fail readiness preflight; explicit provider selection fails early and preserves no-silent-substitution semantics. Health/routing status remains non-blocking and never launches those subprocess probes.

This run completed the successful-inference seam: when a real `3d-generate` job reaches the existing verified-completion point, `job_progress.complete()` records inference qualification for the **final provider actually used**, including Auto fallback, elapsed time and current hardware context. Failed/cancelled/input-specific jobs do not mark a provider inference-tested or globally broken. Qualification persistence is best-effort and occurs outside the job-progress lock so a state-write problem cannot convert a verified inference into a failed job.

Regression coverage verifies that only completed 3D jobs reach the qualification recorder, the final fallback provider is retained, and qualification-persistence failure cannot fail an already completed generation job.

### MS-018 / MS-020 — accepted baseline → generated candidate Core seam

`Core/StageCGeneration.cs` now introduces a strongly typed Stage-C bridge with stable generation job identity and immutable binding to:

- project identity;
- project revision number at submission;
- accepted input `ImageRevision` identity;
- reserved output `ObjectId`.

Accepted 2D baseline selection is a normal `ProjectSession` transaction. A completed generated mesh is registered as an immutable `MeshRevision` plus a review candidate rather than silently becoming active project state. If the accepted baseline changed while inference ran, the result is preserved as `Conflict`; it never overwrites newer work. A current result remains `Ready` until explicit apply. Apply creates the generated project object and candidate status in one transaction, so undo/redo restores both together. Save/reload preserves the accepted baseline, ready/applied/conflict state and generated mesh provenance.

To avoid a breaking project-schema bump while this vertical slice is still being proven, the bridge stores only its small baseline/candidate descriptors behind a strongly typed Core API in the existing schema-7 metadata dictionary; actual image/mesh data remain ordinary durable Core revisions. This is a migration bridge, not widget-owned state, and can later become first-class manifest collections without changing the Stage-C calling contract.

## Validation state

- Readiness work through `8cd00173b78577fed040cce5d71e05738cf404be` previously passed build, Python/core logic, geometry/release-audit, packaging and Stage-B Core CI.
- The new Stage-C Core bridge at `8d394f289e02682bf27a133ed3456c7dc2852df9` passed the dedicated `core-foundation` restore/build/regression run, including stale-baseline conflict, explicit apply, undo/redo and save/reload tests.
- On the broader Windows build for `8d394f289e02682bf27a133ed3456c7dc2852df9`, Python compilation, dependency resolution, core logic tests, job-progress tests, real geometry regressions and release audit passed; editor/launcher/updater/Core C# build also passed. Portable packaging/installer compilation was still finishing when this status was written.
- No real CUDA inference was performed by CI, so no provider is being falsely marked inference-tested from CI alone.
- No v1.0.20 release has been made.

## Released in v1.0.19, awaiting real-machine acceptance

### MS-009 — 3D viewport/grid/model/gizmo

v1.0.19 contains the native viewport pipeline with explicit viewport/world/camera ownership, starter mesh, grid, selection/gizmo refresh and render-frame diagnostics. Status remains **FIXED - NEEDS USER VERIFICATION** until tested on the actual machine.

### MS-013 — storage containment

v1.0.18 introduced authoritative app data-root handling and v1.0.19 adds backend storage/canonical containment hardening/tests. Status remains **FIXED - NEEDS USER VERIFICATION** until representative real jobs prove important working artifacts stay off C:\ AppData/TEMP when a configured data root exists.

## Current highest-priority issues

1. **MS-009 — 3D viewport/grid/gizmo reliability.** v1.0.19 released; target-machine verification required.
2. **MS-018 — Stage-C end-to-end thin slice.** Primary product-level acceptance gap; Core handoff semantics now exist but production UI/backend integration remains.
3. **MS-013 — storage containment.** v1.0.19 released; representative target-machine verification required.
4. **MS-020 — authoritative Job Broker/stale-result handling.** Core revision-bound candidate semantics now exist; transport/resource ownership/recovery remain incomplete.
5. **MS-022 — provider qualification/self-tests.** Readiness + successful-inference recording are implemented; GTX 1080 qualification/default-provider evidence remains incomplete.
6. **MS-019 — legacy application-state architecture.** Stage-B replacement exists but is not authoritative across the editor.

## Immediate engineering priority

1. Finish/reconcile the latest `8d394f...` broader build/packaging CI; fix any regression if it appears.
2. Integrate the existing editor's **Accept 2D Baseline** path with `StageCGeneration.AcceptBaseline()` and a real `ProjectSession`.
3. When submitting 3D generation, create a `GenerationJobBinding` and carry project/object/input-revision identity through the request/job context rather than relying only on file paths.
4. Import the verified generated STL into a durable Core `MeshRevision`, call `StageCGeneration.RegisterResult()`, and make the UI review/apply the candidate explicitly. Never insert a stale result directly into the viewport/project as authoritative state.
5. Persist/save after candidate registration/apply through `ProjectStore`; then extend the thin-slice test around the actual legacy UI compatibility bridge.
6. Collect real GTX 1080 inference qualification evidence for one lightweight/default 3D route.

If the user reports v1.0.19 still renders a blank viewport or leaks working files to C:, that regression becomes immediate priority.

## Next milestone acceptance criteria

On the target Windows machine:

- a 2D image can be generated/imported and edited;
- an accepted baseline persists as a project revision;
- one supported lightweight 3D provider passes its readiness/self-test and generates a mesh;
- the resulting mesh is visibly present with grid, camera, selection and gizmo;
- stale generation results cannot overwrite a newer accepted baseline/object state;
- cancellation followed by a new job works reliably;
- save/reload preserves the result and candidate lineage;
- basic cleanup works;
- export produces a validated STL;
- required working files remain inside the configured Miniscuplter data root;
- peak RAM/VRAM and elapsed time are recorded for the reference machine.

## User input currently required

No product/design decision blocks autonomous work.

For acceptance evidence, test released **v1.0.19** on the GTX 1080 machine: viewport/grid/model/gizmo plus representative data-output paths. If blank, retain viewport diagnostics/render-probe text; if storage escapes, retain the unexpected path(s).
