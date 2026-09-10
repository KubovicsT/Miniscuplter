# Miniscuplter Project Status

> Fast-moving project dashboard. Update at the end of every meaningful development session. Do not use this file as a substitute for inspecting the actual branch, release and CI state.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 starting point:** exact v1.0.19 released commit above.
- **Latest application/code commit from the current MS-022 run:** `8cd00173b78577fed040cce5d71e05738cf404be`.
- **v1.0.19 release validation:** passed editor/launcher/updater C# builds, Python compilation/core tests, job-progress tests, real geometry regressions, Stage-B Core tests, release audit, verified Godot 4.7.2 Windows export, release hash verification and silent installer smoke-install.
- **Release pipeline run:** `34469538260`.

Documentation commits may advance v1.0.20 HEAD past the application commit. Resolve exact HEAD and CI from Git at the start of each run.

## Current development phase

The project remains in the transition between **Stage B (replacement foundation/migration harness)** and **Stage C (prove one reliable end-to-end thin slice)**. Stage A safety work is substantially implemented. Stage B exists materially in code but remains mixed with the legacy editor. Stage C is not yet acceptance-proven on the real target machine.

## Overall completion estimate

**56% toward the defined finished product**.

This remains unchanged because the current work improves reliability infrastructure but does not yet prove the Stage-C thin slice or the user-observed viewport/storage defects on the GTX 1080 target machine. Completion is acceptance-based, not commit-count-based.

| Workstream | Weight | Estimated completion | Basis |
|---|---:|---:|---|
| Application foundation / state / persistence | 15% | 50% | Stable IDs, project models/history/store and migration work exist; legacy `Main.V*.cs` still remains authoritative in large areas. |
| 2D workflow | 12% | 72% | Generation/import, center canvas, regional edit, Enhance, preview and multi-source references exist; candidate/provenance and target-hardware acceptance still need consolidation. |
| 3D generation | 15% | 48% | Multiple providers and routing exist; v1.0.20 now adds readiness-aware 3D preflight, but target-machine inference qualification and thin-slice proof remain incomplete. |
| 3D editing / sculpt / kitbash | 15% | 45% | Significant legacy functionality exists, but state/history/performance integration with the replacement core is incomplete. |
| Rig & Pose | 10% | 40% | Rigging/IK/pose features exist historically; independent rest-state and new-core integration need substantial work. |
| Cleanup / validation / export | 8% | 65% | Repair/remesh/thickness/validated export exist with real geometry regressions; new project-history integration and broader workflow qualification remain. |
| AI runtime / provider reliability | 10% | 65% | Repair, CUDA checks, resumable installs, routing/progress and new provider readiness state/preflight exist; real inference-qualified default-provider evidence is still missing. |
| UI / UX | 8% | 58% | Four-stage workflow, responsive layout, center 2D canvas, settings and newer viewport tooling exist; legacy composition and runtime visual defects remain a risk. |
| Launcher / updater / release reliability | 4% | 92% | Verified resumable self-update, transaction/rollback safeguards and installer smoke testing are mature; destructive fault-injection coverage can still improve. |
| Testing / hardware qualification | 3% | 50% | C#, Python, Core, geometry, audit, package and installer checks are strong; real CUDA/provider/GUI acceptance matrix is incomplete. |

Weighted result remains approximately **56%**.

## Current v1.0.20 work — MS-022 provider readiness

`ai_backend/provider_readiness.py` now provides separate downloaded, installed, importable, device-tested and inference-tested states with persisted timestamp, runtime/provider revision, device/failure details and optional benchmark data.

Generation-time preflight exists for the main single-mesh Stage-C candidates: TripoSR, Hunyuan3D 2mini, Hunyuan3D 2.1 Shape, Stable Fast 3D and SPAR3D. The probes test provider imports and CUDA visibility without loading model weights. Auto 3D routing skips providers that fail readiness preflight; explicit provider selection fails early and preserves no-silent-substitution semantics.

Health/routing status uses cached/persisted readiness only. A first implementation would have allowed health polling to launch provider subprocess probes; self-review caught this and commit `b8b07f2399b9c5b48c977b826d3b3a00ece9e9d9` separated non-blocking status inspection from generation-time preflight.

Regression tests now cover readiness-aware fallback, explicit broken-provider failure and state/benchmark persistence. The first new CI run exposed defects in the mocked persistence fixture, not a demonstrated application defect; commits `ab60bd1e73ef0f074ce9d1806fe94247d6f0b7f1` and `8cd00173b78577fed040cce5d71e05738cf404be` corrected the fixture and triggered fresh validation.

At the time this status was written, CI for `8cd00173...` was still running. Do not claim the current branch fully green until the latest build/core-foundation runs are reconciled. No v1.0.20 release has been made.

## Released in v1.0.19, awaiting real-machine acceptance

### MS-009 — 3D viewport/grid/model/gizmo

v1.0.19 contains the native viewport pipeline with explicit viewport/world/camera ownership, starter mesh, grid, selection/gizmo refresh and render-frame diagnostics. Status remains **FIXED - NEEDS USER VERIFICATION** until tested on the actual machine.

### MS-013 — storage containment

v1.0.18 introduced authoritative app data-root handling and v1.0.19 adds backend storage/canonical containment hardening/tests. Status remains **FIXED - NEEDS USER VERIFICATION** until representative real jobs prove important working artifacts stay off C:\ AppData/TEMP when a configured data root exists.

## Current highest-priority issues

1. **MS-009 — 3D viewport/grid/gizmo reliability.** v1.0.19 released; target-machine verification required.
2. **MS-018 — Stage-C end-to-end thin slice.** Primary product-level acceptance gap.
3. **MS-013 — storage containment.** v1.0.19 released; representative target-machine verification required.
4. **MS-022 — provider qualification/self-tests.** In progress; readiness state/preflight now exists, but inference qualification/default benchmark evidence is incomplete.
5. **MS-020 — authoritative Job Broker/stale-result handling.** Current progress infrastructure is useful but not the final architecture.
6. **MS-019 — legacy application-state architecture.** Stage-B replacement exists but is not authoritative across the editor.

## Immediate engineering priority

1. Reconcile/fix the latest v1.0.20 CI for the readiness changes.
2. Wire a verified successful `/generate-3d` result to `record_inference_success()` for the final provider actually used, including elapsed/hardware benchmark context, without treating input-specific failures as provider-wide qualification failures.
3. Collect real GTX 1080 qualification evidence for one lightweight/default 3D route.
4. Continue MS-018/MS-020 by binding accepted 2D baseline → immutable revision-bound 3D job → candidate mesh revision → explicit transactional accept/apply, with stale-result conflict protection.
5. Keep legacy UI compatibility while migrating this one vertical slice onto the Stage-B core.

If the user reports v1.0.19 still renders a blank viewport or leaks working files to C:, that regression becomes immediate priority.

## Next milestone acceptance criteria

On the target Windows machine:

- a 2D image can be generated/imported and edited;
- an accepted baseline persists as a project revision;
- one supported lightweight 3D provider passes its readiness/self-test and generates a mesh;
- the resulting mesh is visibly present with grid, camera, selection and gizmo;
- cancellation followed by a new job works reliably;
- save/reload preserves the result;
- basic cleanup works;
- export produces a validated STL;
- required working files remain inside the configured Miniscuplter data root;
- peak RAM/VRAM and elapsed time are recorded for the reference machine.

## User input currently required

No product/design decision blocks autonomous work.

For acceptance evidence, test released **v1.0.19** on the GTX 1080 machine: viewport/grid/model/gizmo plus representative data-output paths. If blank, retain viewport diagnostics/render-probe text; if storage escapes, retain the unexpected path(s).
