# Miniscuplter Project Status

> Fast-moving project dashboard. Update at the end of every meaningful development session. Do not use this file as a substitute for inspecting the actual branch, release and CI state.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.18`
- **Stable release application commit:** `ce2d876fc145e615d63bd8d9fc809610f6038301`
- **v1.0.18 branch head after release-workflow restoration:** `67a76087e32144845b9a4ff06d4b9e6c7cb06c26`
- **Current development branch:** `v1.0.19`
- **Last code-bearing v1.0.19 HEAD before this documentation bootstrap:** `87601d0f343e9117c172097ad9a64cef574b1f0f`
- **v1.0.19 branch validation at that code HEAD:** latest observed `core-foundation` and build workflow runs passed.
- **Current branch HEAD:** always resolve from Git at the start of a run; documentation commits may advance it beyond the code-bearing SHA above.

`v1.0.19` already existed when the autonomous-project documentation was bootstrapped. It is 12 commits ahead of the v1.0.18 branch and includes substantive work in viewport rendering/interaction, storage containment, updater hardening, project-store/history foundations, backend storage and geometry/runtime contracts. Do not discard or recreate that work.

## Current development phase

The project is in the transition between **Stage B (replacement foundation/migration harness)** and **Stage C (prove one reliable end-to-end thin slice)** from the accepted Astra refactor plan.

Stage A safety work is substantially implemented. Stage B exists materially in code but remains mixed with the legacy editor. Stage C is not yet considered acceptance-proven on the real target machine.

## Overall completion estimate

**56% toward the defined finished product**.

This is an acceptance-based weighted estimate, not a count of implemented controls. Existing legacy features receive partial credit when they are not yet integrated with the replacement state/history architecture or not proven reliable on target hardware.

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

These are present and have meaningful implementation/test history. They may still contain bugs.

- Windows launcher/updater and GitHub Release delivery.
- Data-preserving application update path with integrity checking and rollback safeguards.
- AI runtime setup/repair, CUDA validation and resumable model installation.
- 2D concept generation and user-image input.
- Large/center 2D image workflow and regional image selection.
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

## Partially working / not yet acceptance-proven

### 3D viewport

v1.0.18 introduced a native viewport tool foundation; v1.0.19 contains a further `Main.V1019ViewportPipeline.cs` repair/rebind/render-probe implementation. It explicitly configures a native `SubViewportContainer`, world/camera, starter mesh, materials, grid, selection/gizmo and render diagnostics.

Historical user testing repeatedly found a blank viewport even when generated STL output existed. Until v1.0.19 is released and verified on the real machine, this remains an active high-severity issue rather than a resolved claim.

### Storage containment

v1.0.18 added a single `AppDataRoot` that defaults to `InstallRoot/AIData`, sets backend/cache/temp environment variables under that root and rejects path escape. v1.0.19 contains additional backend/storage and Windows canonicalization work.

Historical user testing showed artifacts under `C:\Users\...\AppData\Roaming\Godot`. This remains `FIXED - NEEDS USER VERIFICATION` / in-progress until target-machine testing confirms no important paths leak to C:.

### AI job feedback

v1.0.17/v1.0.18 added job progress for concept generation, image edit and 3D operations, including stages, elapsed time/cancellation and backend polling. More provider-specific real step reporting is still desirable. Never convert activity heartbeats into fake inference percentages.

### Quality presets

The newer Settings quality panel exposes preset parameters and custom preset create/clone/rename/save/delete operations. This needs continued UX verification and should eventually become part of the cleaner declarative UI rather than legacy additive composition.

### Image-to-3D

Several providers exist, but the project does not yet have a small, fully qualified default provider set with repeatable target-machine benchmarks and self-tests. Provider presence/install state must not be confused with inference readiness.

### New project foundation

`Core` contains meaningful Stage-B work, but the normal editor still relies heavily on legacy scene/widget state. The `.msculpt2`/replacement project model is not yet the sole production source of truth.

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

See `ISSUES.md` for full records. The most important current items are:

1. **MS-009 — 3D viewport/grid/gizmo reliability.** v1.0.19 contains a major repair pipeline; needs target-machine verification.
2. **MS-018 — Stage-C end-to-end thin slice is not yet qualified.** This is the key product-level acceptance gap.
3. **MS-013 — storage containment.** v1.0.18/v1.0.19 contain fixes; needs verification that significant working files no longer land on C:.
4. **MS-020 — authoritative job broker/stale-result handling.** Current progress infrastructure is useful but not the final architecture.
5. **MS-019 — legacy application-state architecture.** Stage-B replacement exists but is not yet authoritative.
6. **MS-022 — provider qualification/self-tests.** Needed before a trustworthy default bundle can be claimed.

## Immediate engineering priority

Unless a new user-observed regression has higher severity, the next autonomous work should advance **Stage C reliability while continuing safe Stage-B migration**.

Recommended sequence:

1. Reconcile and test the existing v1.0.19 viewport/storage/updater/project-store changes.
2. Strengthen automated viewport/scene-import diagnostics where CI can help, while leaving real rendering verification for the user's machine.
3. Build/extend provider self-test contracts so a chosen 3D provider fails before a long job when dependencies/device support are missing.
4. Move the accepted 2D baseline → 3D generation → candidate/import handoff onto stable project/revision identity.
5. Ensure stale results cannot overwrite a newer mesh/image revision.
6. Continue toward one qualified GTX 1080 thin slice before broadening optional provider scope.

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

No fundamental product/design decision is currently blocking autonomous engineering.

User input is still valuable for **real-machine verification** of issues that CI cannot prove. Such issues must stay `FIXED - NEEDS USER VERIFICATION` until the user tests the released build.
