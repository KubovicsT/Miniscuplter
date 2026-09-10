# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above.
- **Latest application/code commit:** `a9f7e0989d5486bdee27f054edb58713aa00cc41`.
- **v1.0.19 release validation:** editor/launcher/updater C# builds, Python/core/job tests, real geometry regressions, Stage-B Core tests, release audit, Godot 4.7.2 Windows export, ZIP/hash verification and silent installer smoke-install passed. Release pipeline: `34469538260`.

Mandatory release/version reconciliation is clean: v1.0.19 remains immutable and published; v1.0.20 is a distinct forward-only development branch.

## Current phase

The project remains between **Stage B (replacement foundation/migration harness)** and **Stage C (one reliable end-to-end thin slice)**. The production accepted-2D-baseline → generated-3D-candidate seam now uses stable project/revision identity from editor through AI transport/backend job tracking and back into candidate registration.

## Overall completion estimate

**56% toward the defined finished product.**

This stays unchanged because the transport safety work removes a serious stale/replay risk but does not yet add target-machine acceptance for the full thin slice.

| Workstream | Weight | Estimated completion | Current basis |
|---|---:|---:|---|
| Application foundation / state / persistence | 15% | 50% | Stable IDs, immutable revisions, ProjectStore/history/migration and production Stage-C compatibility bridge exist; legacy state is still authoritative elsewhere. |
| 2D workflow | 12% | 72% | Generation/import/edit/reference flow exists; accepted baseline is durably project-owned. |
| 3D generation | 15% | 50% | Revision-bound generation identity is now carried through AIClient/backend and verified before candidate registration; real hardware qualification remains missing. |
| 3D editing / sculpt / kitbash | 15% | 45% | Significant legacy functionality exists; broad Core/history migration remains incomplete. |
| Rig & Pose | 10% | 40% | Historical feature set exists; modern rest-state/Core integration remains substantial work. |
| Cleanup / validation / export | 8% | 65% | Repair/remesh/thickness/export and geometry regressions exist; full Stage-C integration remains. |
| AI runtime / provider reliability | 10% | 65% | Repair, routing, progress, readiness preflight and persisted real-inference qualification exist; real default-provider evidence is missing. |
| UI / UX | 8% | 58% | Four-stage UI and newer viewport/workflow tooling exist; real-machine viewport acceptance and legacy composition risks remain. |
| Launcher / updater / release reliability | 4% | 92% | Mature verified updater/release flow. |
| Testing / hardware qualification | 3% | 50% | Strong CI/regression coverage; real CUDA/GUI acceptance matrix remains incomplete. |

## v1.0.20 Stage-C state

### Durable baseline and candidate model

- Accepted 2D source is copied into project-owned immutable `ImageRevision` storage.
- `StageCGeneration.BeginImageToMesh()` binds generation to `GenerationJobId`, `ProjectId`, input project revision, accepted `ImageRevisionId`, and reserved output `ObjectId`.
- Verified generated STL is converted to a durable immutable `MeshRevision` and registered as a Ready or Conflict review candidate.
- A stale baseline result remains Conflict and cannot silently overwrite newer work.
- Apply/Discard are explicit; Apply is transactional and applied objects restore into the visible Godot scene after restart.

### MS-020 transport identity — implemented for the Stage-C 3D seam

This run extended the existing transport rather than creating a parallel HTTP path:

- `AIClient.Generate3DStageCAsync()` sends the complete `GenerationJobBinding` identity through the existing serialized request gate/cancellation/backend-reset path.
- The Core `GenerationJobId` is also the backend transport job ID for this Stage-C request.
- `ai_backend.Generate3DRequest` accepts the complete context only as an all-or-nothing set; partial context or a transport/job-ID mismatch is rejected.
- `job_progress.begin()` stores a deep-copied context snapshot with the job.
- Successful `/generate-3d` responses echo the context.
- AIClient rejects a missing or mismatched echoed context before the editor can create/register a Core mesh candidate.
- The editor no longer needs a secondary progress lookup to infer the final provider; the verified response returns the actual provider directly.
- Legacy non-Stage-C 3D requests remain compatible with empty context.

Relevant commits:

- `ebfa4fce7691e71cbfaffc16832e171dd83bd839` — carry Stage-C context in backend job progress.
- `1429d9bc4c45db2cbca0a9b99b3bf2bb6f93d674` — propagate/validate Stage-C identity through `/generate-3d`.
- `ba99de7e59303c33805565df5159d2fa83a89bc1` — typed AIClient Stage-C request/response and mismatch rejection.
- `4885f80d825213d66af957397a03045a80432328` — bind production editor generation to the typed transport identity.
- `a9f7e0989d5486bdee27f054edb58713aa00cc41` — regression test job-context retention/snapshot isolation.

## Validation

Initial v1.0.20 branch state was green before editing. Fresh build run `34493356971` for `a9f7e0989d5486bdee27f054edb58713aa00cc41` has already passed:

- Python compilation and dependency resolution;
- core logic tests;
- job-progress tests including Stage-C context retention;
- real geometry regression tests;
- release audit;
- editor / launcher / updater / Core restore and C# build.

Portable package build/layout/SHA verification also passed; installer-definition packaging was still finishing when this status entry was written. Reconcile the final run conclusion before a release decision. The `core-foundation` workflow was also triggered for the same HEAD.

No real CUDA inference or target-machine GUI acceptance occurred in CI. **No v1.0.20 release has been made.**

## Released v1.0.19 items awaiting real-machine verification

- **MS-009 viewport/grid/model/gizmo:** FIXED - NEEDS USER VERIFICATION.
- **MS-013 storage containment:** FIXED - NEEDS USER VERIFICATION.

If either regresses on the target machine, it becomes immediate priority.

## Current highest-priority issues

1. **MS-009** — released viewport fix still needs target-machine verification.
2. **MS-018** — full Stage-C thin slice remains the primary acceptance gap; the baseline→candidate seam is integrated and identity-bound, but cleanup/export and target qualification remain.
3. **MS-013** — released storage containment still needs representative target-machine verification.
4. **MS-020** — Stage-C request/result identity is now carried through the production transport; durable queue/resource ownership/crash recovery and save-failure consistency remain incomplete.
5. **MS-022** — readiness/self-test/inference-recording exists; GTX 1080 qualification/default-provider evidence is missing.
6. **MS-019** — legacy `Main.V*.cs` remains authoritative outside migrated vertical slices.

## Immediate engineering priority

1. Reconcile final CI for `a9f7e0989d5486bdee27f054edb58713aa00cc41`; fix any regression.
2. Harden `V1020SaveSessionAsync()` so a failed ProjectStore save reloads/rolls back to the last durable state rather than leaving the in-memory compatibility session ahead of disk.
3. Add a deterministic regression for the transport mismatch contract if practical without requiring GPU inference.
4. Continue the same persisted Stage-C object through basic cleanup and exact export-scope/validated STL integration.
5. Collect real GTX 1080 provider readiness/inference timing/RAM/VRAM evidence and verify v1.0.19 viewport/storage behavior.

## User input currently required

No product/design decision blocks autonomous development. Real-machine acceptance evidence remains useful when available, but independent engineering can continue without it.
