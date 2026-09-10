# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above.
- **Latest application/code commit from this run:** `cfd0352223cada181ea75843f16b25b9b5ceb541`.
- **v1.0.19 release validation:** editor/launcher/updater C# builds, Python/core/job tests, real geometry regressions, Stage-B Core tests, release audit, Godot 4.7.2 Windows export, ZIP/hash verification and silent installer smoke-install passed. Release pipeline: `34469538260`.

The mandatory release/version reconciliation was clean: v1.0.19 remains immutable and published, while v1.0.20 is a distinct forward development branch. No history rewrite or replacement release branch was required.

## Current phase

The project is transitioning from **Stage B (replacement foundation/migration harness)** into **Stage C (one reliable end-to-end thin slice)**. This run moved the real editor's accepted-2D-baseline → 3D-generation seam onto the Stage-B project/revision model while preserving the existing UI and AI cancellation/resource path.

## Overall completion estimate

**56% toward the defined finished product.**

Do not increase this merely because code exists. The production bridge is meaningful progress, but real GTX 1080 provider/viewport/storage acceptance and the remainder of the thin slice are still unproven.

| Workstream | Weight | Estimated completion | Current basis |
|---|---:|---:|---|
| Application foundation / state / persistence | 15% | 50% | Stable IDs, immutable revisions, ProjectStore/history/migration and a production Stage-C compatibility bridge now exist; legacy state is still authoritative elsewhere. |
| 2D workflow | 12% | 72% | Generation/import/edit/reference flow exists; accepted Stage-C baseline is now copied into durable project-owned image revision storage. |
| 3D generation | 15% | 48% | Readiness-aware providers plus production revision-bound candidate generation exist; target-hardware qualification remains missing. |
| 3D editing / sculpt / kitbash | 15% | 45% | Significant legacy functionality exists; broad Core/history migration remains incomplete. |
| Rig & Pose | 10% | 40% | Historical feature set exists; modern rest-state/Core integration remains substantial work. |
| Cleanup / validation / export | 8% | 65% | Repair/remesh/thickness/export and geometry regressions exist; full Stage-C integration remains. |
| AI runtime / provider reliability | 10% | 65% | Repair, routing, progress, readiness preflight and persisted real-inference qualification exist; real default-provider evidence is still missing. |
| UI / UX | 8% | 58% | Four-stage UI and newer viewport/workflow tooling exist; real-machine viewport acceptance and legacy composition risks remain. |
| Launcher / updater / release reliability | 4% | 92% | Mature verified updater/release flow. |
| Testing / hardware qualification | 3% | 50% | Strong CI/regression coverage; real CUDA/GUI acceptance matrix remains incomplete. |

## v1.0.20 work completed so far

### MS-022 — provider readiness / qualification

`ai_backend/provider_readiness.py` distinguishes downloaded, installed, importable, device-tested and inference-tested states. Main single-mesh 3D routes receive lightweight import/CUDA preflight before expensive inference. Auto routing skips known-unready providers; explicit selection reports the failure instead of silently substituting another provider. Normal health polling does not launch slow provider probes.

A verified completed real 3D job records inference qualification for the provider that actually succeeded, including an Auto fallback, with elapsed time and hardware context. Failed/cancelled jobs do not qualify or globally blacklist a provider. CI never fabricates inference-tested status.

### MS-018 / MS-020 — Core Stage-C generation semantics

`Core/StageCGeneration.cs` provides stable generation identity bound to project ID, project revision, exact accepted input `ImageRevision`, and reserved output `ObjectId`. Generated meshes register as Ready review candidates. If the accepted baseline changes while inference runs, the result becomes Conflict and cannot silently replace newer work. Apply/discard are explicit, with Apply changing generated-object state and candidate state in one undoable transaction.

### MS-018 / MS-019 / MS-020 — production compatibility bridge

This run added and wired the real editor bridge:

- `Core/StageCAssetStore.cs` copies an accepted source image into project-owned `images/` storage using temp-write + flush + atomic rename, calculates SHA-256, and returns an immutable `ImageRevision`. The source path is no longer the durable baseline authority.
- `Scripts/Main.V1020StageCBridge.cs` replaces the legacy baseline acceptance callback and the old immediate-authority 3D-generation callback after historical UI composition has completed.
- Accepting the baseline now stores an `ImageRevision`, calls `StageCGeneration.AcceptBaseline()` on a real `ProjectSession`, saves through `ProjectStore`, and only then enables generation.
- 3D submission captures a `GenerationJobBinding` before inference. A verified returned STL is converted to `MeshData`, written as a durable Core `MeshRevision` using the binding's reserved output object ID, and passed to `StageCGeneration.RegisterResult()`.
- A Ready result is **not** inserted into the visible/authoritative project automatically. The UI now exposes **Apply 3D Candidate** and **Discard Candidate**. Conflict candidates remain preserved and cannot be applied.
- Explicit Apply uses the transactional Core command first, saves, then materializes the mesh into the legacy Godot scene with the stable Core object ID mapped onto the scene object.
- Pending Ready/Conflict candidate state and accepted baseline are restored from the `.msculpt2` compatibility project.
- `Scripts/Main.V1020StageCRestore.cs` additionally re-materializes already-applied Stage-C generated objects into the visible Godot scene after editor restart, closing a save/reload visibility gap found during self-review.
- `Core.Tests/StageCGenerationTests.cs` now uses the real durable image store and verifies that deleting the external source after acceptance does not destroy the project baseline; stale-result conflict, explicit apply, undo/redo, and project save/reload tests remain covered.

The compatibility project is currently `projects/stagec_working.msculpt2` under the authoritative `AppDataRoot`. This deliberately avoids replacing legacy `.msculpt` project files while the vertical slice is still being proven.

## Validation

For code commit `04fd660296e6d9657328de6b41b03c58fde334aa`, GitHub Actions build run `34490666005` passed editor/launcher/updater/Core C# build + Stage-B tests, Python compile/dependency resolution/core tests/job-progress tests, real geometry regressions, release audit, portable package/hash verification and installer-definition compilation.

The later applied-object reload fix through `cfd0352223cada181ea75843f16b25b9b5ceb541` triggered fresh branch validation; reconcile its final result from Git before further code or release decisions. No real CUDA inference or Godot target-machine GUI acceptance occurred in CI.

**No v1.0.20 release has been made.** The batch is not release-ready while stable identity is still not propagated through the actual HTTP/backend job context and target-machine Stage-C acceptance is missing.

## Released v1.0.19 items awaiting real-machine verification

- **MS-009 viewport/grid/model/gizmo:** FIXED - NEEDS USER VERIFICATION.
- **MS-013 storage containment:** FIXED - NEEDS USER VERIFICATION.

If either regresses on the user's machine, it becomes immediate priority.

## Current highest-priority issues

1. **MS-009** — released viewport fix still needs target-machine verification.
2. **MS-018** — full Stage-C end-to-end thin slice remains the primary acceptance gap; the production baseline→candidate seam is now integrated but not target-qualified.
3. **MS-013** — released storage containment still needs representative target-machine verification.
4. **MS-020** — stale-result candidate semantics are integrated, but the stable generation binding is not yet carried through the actual AI HTTP/backend job context and the final durable Job Broker remains incomplete.
5. **MS-022** — readiness/self-test/inference-recording exists; GTX 1080 qualification/default-provider evidence is missing.
6. **MS-019** — legacy `Main.V*.cs` remains authoritative outside migrated vertical slices.

## Immediate engineering priority

1. Reconcile CI for `cfd0352223cada181ea75843f16b25b9b5ceb541`; fix any regression.
2. Extend the existing AIClient/backend 3D request without bypassing its job gate/cancellation ownership so `GenerationJobId`, `ProjectId`, input project revision, accepted `ImageRevisionId`, and reserved output `ObjectId` are carried through backend job context and validated/echoed before candidate registration.
3. Harden compatibility-session persistence on save failure so in-memory candidate/baseline state cannot remain ahead of durable state.
4. Continue the Stage-C flow through basic cleanup and exact export-scope/validated STL while retaining transactional Core identity.
5. Collect real GTX 1080 provider readiness/inference timing/RAM/VRAM evidence and verify v1.0.19 viewport/storage behavior.

## User input currently required

No product/design decision blocks autonomous development. Real-machine acceptance evidence remains useful when available, but it does not block independent engineering work.