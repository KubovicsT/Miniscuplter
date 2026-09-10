# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above.
- **Latest application/code commit:** `fe9079e718ad055c0ef5b43c7205e505ce54e217`.
- **v1.0.19 release validation:** editor/launcher/updater C# builds, Python/core/job tests, real geometry regressions, Stage-B Core tests, release audit, Godot 4.7.2 Windows export, ZIP/hash verification and silent installer smoke-install passed. Release pipeline: `34469538260`.

Mandatory release/version reconciliation is clean: v1.0.19 remains immutable and published; v1.0.20 is a distinct forward-only development branch.

## Current phase

The project remains between **Stage B (replacement foundation/migration harness)** and **Stage C (one reliable end-to-end thin slice)**. The production accepted-2D-baseline → generated-3D-candidate seam now uses stable project/revision identity end-to-end and failed project saves no longer leave the in-memory Stage-C session falsely ahead of durable storage.

## Overall completion estimate

**56% toward the defined finished product.**

This remains unchanged: save-failure rollback closes an important persistence hazard but the full cleanup/export thin slice and target-machine acceptance are still incomplete.

| Workstream | Weight | Estimated completion | Current basis |
|---|---:|---:|---|
| Application foundation / state / persistence | 15% | 52% | Stable IDs, immutable revisions, ProjectStore/history/migration, production Stage-C bridge, and fail-closed save recovery now exist; legacy authority remains elsewhere. |
| 2D workflow | 12% | 72% | Generation/import/edit/reference flow exists; accepted baseline is durably project-owned. |
| 3D generation | 15% | 50% | Revision-bound generation identity is carried through AIClient/backend and verified before candidate registration; real hardware qualification remains missing. |
| 3D editing / sculpt / kitbash | 15% | 45% | Significant legacy functionality exists; broad Core/history migration remains incomplete. |
| Rig & Pose | 10% | 40% | Historical feature set exists; modern rest-state/Core integration remains substantial work. |
| Cleanup / validation / export | 8% | 65% | Repair/remesh/thickness/export and geometry regressions exist; full Stage-C integration remains. |
| AI runtime / provider reliability | 10% | 65% | Repair, routing, progress, readiness preflight and persisted real-inference qualification exist; real default-provider evidence is missing. |
| UI / UX | 8% | 58% | Four-stage UI and newer viewport/workflow tooling exist; real-machine viewport acceptance and legacy composition risks remain. |
| Launcher / updater / release reliability | 4% | 92% | Mature verified updater/release flow. |
| Testing / hardware qualification | 3% | 52% | Strong CI/regression coverage now includes deterministic failed-save rollback; real CUDA/GUI acceptance remains incomplete. |

Weighted completion still rounds to approximately **56%**.

## v1.0.20 Stage-C state

### Durable baseline and candidate model

- Accepted 2D source is copied into project-owned immutable `ImageRevision` storage.
- `StageCGeneration.BeginImageToMesh()` binds generation to `GenerationJobId`, `ProjectId`, input project revision, accepted `ImageRevisionId`, and reserved output `ObjectId`.
- Verified generated STL is converted to a durable immutable `MeshRevision` and registered as a Ready or Conflict review candidate.
- A stale baseline result remains Conflict and cannot silently overwrite newer work.
- Apply/Discard are explicit; Apply is transactional and applied objects restore into the visible Godot scene after restart.

### MS-020 transport identity

The migrated Stage-C 3D seam uses the existing AIClient/backend request gate and cancellation ownership. Complete generation identity is sent, stored in backend progress, echoed, and checked fail-closed before candidate registration. Legacy non-Stage-C requests remain compatible.

### MS-020 save-failure consistency — hardened this run

`ProjectSession.SaveRecoveringAsync()` now provides one reusable persistence boundary:

- successful save marks exactly the persisted revision as saved;
- failed save reloads the last durable/recoverable `ProjectStore` state and calls `ReplaceFromLoad()` before surfacing the error;
- undo/redo history is cleared when rolling back to durable state, so pre-failure transactions cannot be replayed against a replaced state;
- recovery is rejected if it returns a different `ProjectId`;
- if both save and recovery fail, an aggregate error is surfaced and the unresolved in-memory state is not falsely marked durable.

`Main.V1020StageCBridge.V1020SaveSessionAsync()` now uses that Core helper. Existing ordering already keeps baseline/candidate UI fields and visible mesh insertion after successful persistence, so a failed baseline/apply/discard/candidate save does not present the attempted state as committed. Immutable asset files created before a manifest save may remain orphaned; they are not authoritative project state.

Relevant commits:

- `80b1c68fac12bfa6d9bb6c53f99c74fe3ddb4ba3` — add reusable `ProjectSession` save recovery.
- `4afdbd75ce6ae72a76fd9569eae40235918d7dae` — wire Stage-C editor saves through durable recovery.
- `fe9079e718ad055c0ef5b43c7205e505ce54e217` — deterministic rollback and foreign-project recovery regression tests.

## Validation

Previous application commit `a9f7e0989d5486bdee27f054edb58713aa00cc41` was already fully green.

Current application commit `fe9079e718ad055c0ef5b43c7205e505ce54e217` is now fully green:

- `core-foundation` run `34497651489`: **SUCCESS**, including deterministic save-failure rollback and foreign-project rejection tests.
- broader Windows build run `34497651608`: **SUCCESS**, covering editor/launcher/updater/Core C# restore/build, Python compilation and dependency resolution, core logic tests, job-progress tests, real geometry regressions, release audit, portable package layout/SHA verification, and installer-definition compilation.

A real Godot Windows release export was not run because v1.0.20 is not yet release-ready. No real CUDA inference or target-machine GUI acceptance occurred in CI. **No v1.0.20 release has been made.**

## Released v1.0.19 items awaiting real-machine verification

- **MS-009 viewport/grid/model/gizmo:** FIXED - NEEDS USER VERIFICATION.
- **MS-013 storage containment:** FIXED - NEEDS USER VERIFICATION.

If either regresses on the target machine, it becomes immediate priority.

## Current highest-priority issues

1. **MS-009** — released viewport fix still needs target-machine verification.
2. **MS-018** — full Stage-C thin slice remains the primary acceptance gap; baseline→candidate is integrated and persistence-safe, but cleanup/export and target qualification remain.
3. **MS-013** — released storage containment still needs representative target-machine verification.
4. **MS-020** — request/result identity and save-failure consistency are hardened for the migrated Stage-C seam; durable queue/resource ownership/crash recovery remain incomplete.
5. **MS-022** — readiness/self-test/inference-recording exists; GTX 1080 qualification/default-provider evidence is missing.
6. **MS-019** — legacy `Main.V*.cs` remains authoritative outside migrated vertical slices.

## Immediate engineering priority

1. Continue the same persisted Stage-C object through the minimum cleanup path. Cleanup must create a **new immutable `MeshRevision`** and transactionally advance the object's active revision; never overwrite the generated revision.
2. Bind Cleanup & Export to an explicit project object/revision scope and validated STL output. STL remains interchange/output, never project authority.
3. Add a deterministic transport-response mismatch regression at the lowest practical layer if it can be done without introducing another client path or GPU dependency.
4. Collect real GTX 1080 provider readiness/inference timing/RAM/VRAM evidence and verify v1.0.19 viewport/storage behavior when target hardware is available.

## User input currently required

No product/design decision blocks autonomous development. Real-machine acceptance evidence remains useful when available, but independent engineering can continue without it.
