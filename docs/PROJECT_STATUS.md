# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above.
- **Latest application/code commit:** `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`.
- **v1.0.19 release validation:** editor/launcher/updater C# builds, Python/core/job tests, real geometry regressions, Stage-B Core tests, release audit, Godot 4.7.2 Windows export, ZIP/hash verification and silent installer smoke-install passed. Release pipeline: `34469538260`.

Mandatory release/version reconciliation is clean: v1.0.19 remains immutable and published; v1.0.20 is a distinct forward-only development branch.

## Current phase

The project remains between **Stage B (replacement foundation/migration harness)** and **Stage C (one reliable end-to-end thin slice)**. The production Stage-C path now reaches accepted baseline → identity-bound generated candidate → explicit apply → durable save/reload → revision-bound cleanup → explicitly scoped validated STL export. Real target-machine qualification and some editing-state migration remain before the slice is accepted.

## Overall completion estimate

**57% toward the defined finished product.**

This increases by one point because cleanup/export is now materially integrated into the same durable object/revision model rather than existing only as disconnected legacy functionality. It does not receive full acceptance credit until the workflow is exercised on the target Windows/GTX1080 machine and normal Stage-C edits are fully represented in project state.

| Workstream | Weight | Estimated completion | Current basis |
|---|---:|---:|---|
| Application foundation / state / persistence | 15% | 55% | Stable IDs, immutable revisions, ProjectStore/history/migration, production Stage-C bridge, fail-closed saves, and cleanup lineage exist; legacy authority remains elsewhere. |
| 2D workflow | 12% | 72% | Generation/import/edit/reference flow exists; accepted baseline is durably project-owned. |
| 3D generation | 15% | 50% | Revision-bound generation identity is carried through AIClient/backend and verified before candidate registration; real hardware qualification remains missing. |
| 3D editing / sculpt / kitbash | 15% | 45% | Significant legacy functionality exists; normal transform/sculpt mutations on Stage-C objects still need Core/history authority. |
| Rig & Pose | 10% | 40% | Historical feature set exists; modern rest-state/Core integration remains substantial work. |
| Cleanup / validation / export | 8% | 76% | Stage-C repair now creates immutable child revisions and STL export resolves exact object/revision scope with write-then-reopen validation; full target-machine acceptance remains. |
| AI runtime / provider reliability | 10% | 65% | Repair, routing, progress, readiness preflight and persisted real-inference qualification exist; real default-provider evidence is missing. |
| UI / UX | 8% | 58% | Four-stage UI and newer viewport/workflow tooling exist; real-machine viewport acceptance and legacy composition risks remain. |
| Launcher / updater / release reliability | 4% | 92% | Mature verified updater/release flow. |
| Testing / hardware qualification | 3% | 55% | Core tests cover cleanup lineage/stale scope/save recovery; real CUDA/GUI acceptance remains incomplete. |

Weighted completion rounds to approximately **57%**.

## v1.0.20 Stage-C state

### Durable baseline, candidate and persistence model

- Accepted 2D source is copied into project-owned immutable `ImageRevision` storage.
- `StageCGeneration.BeginImageToMesh()` binds generation to `GenerationJobId`, `ProjectId`, input project revision, accepted `ImageRevisionId`, and reserved output `ObjectId`.
- Verified generated STL is converted to a durable immutable `MeshRevision` and registered as a Ready or Conflict review candidate.
- A stale baseline result remains Conflict and cannot silently overwrite newer work.
- Apply/Discard are explicit; Apply is transactional and applied objects restore into the visible Godot scene after restart.
- `ProjectSession.SaveRecoveringAsync()` rolls failed saves back to the last recoverable durable state rather than leaving memory falsely committed.

### Stage-C cleanup and export — integrated this run

`Core/StageCCleanup.cs` now defines the cleanup/export revision contract:

- cleanup binds the exact `ProjectId`, `ObjectId`, and current active `MeshRevisionId` before work;
- successful cleanup output must belong to the same object and directly descend from the exact bound input revision;
- a stale cleanup result is rejected if the active object revision advanced while work was running;
- applying cleanup is one project transaction that registers the new immutable revision and advances the same object's active revision;
- export resolution is fail-closed: the requested object/revision must be the object's exact active revision.

`Main.V1020StageCCleanupExport.cs` patches the composed release UI after legacy installers:

- **Repair Selected / Repair selected model** uses the Stage-C path when the selected Godot object maps to a durable Stage-C object; unrelated legacy objects retain the existing safe repair fallback.
- Stage-C repair reads the exact immutable `.msh` revision and materializes STL only as a backend interchange input.
- The repair result is converted back into project-native `MeshData`, stored as a new immutable child revision with repair provenance, then persisted through the recovery-safe save boundary before the visible Godot mesh is changed.
- **Export STL** for Stage-C objects resolves the durable active revision, applies the durable object transform for output, writes to a temporary destination-side STL, reopens and validates finite/non-empty triangle geometry, then atomically replaces the destination.
- STL remains export/interchange, never project authority.

During self-review, the first cleanup-lineage test exposed a real integration defect: `StageCGeneration` required an Applied candidate's generated revision to remain the object's current active revision. Legitimate cleanup therefore made candidate metadata unreadable after reload. That failed attempt is retained in CI history. Commit `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b` changed validation to require the generated revision to remain in the active revision's ancestry instead, preserving provenance through later edits while still rejecting unrelated lineage.

Relevant commits:

- `3f34cd078d435d532632612e9a52d79860c888d6` — transactional Stage-C cleanup revision contract.
- `35078ab977839353afa3b59461d065889e83d50d` — bind repair/export to Stage-C project revisions.
- `55db2a7641bcbffa912f4b93aa131f1c15629816` — install cleanup/export bridge after final UI composition.
- `52feda4be0ef46f5c5e3dc4ab07261e3028f3f0f` — cleanup lineage, stale-result and export-scope regressions.
- `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b` — preserve Applied candidate provenance through descendant cleanup revisions.

## Validation

- Earlier save-recovery application commit `fe9079e718ad055c0ef5b43c7205e505ce54e217` passed `core-foundation` run `34497651489` and broader Windows build `34497651608` completely.
- Cleanup test run `34499552249` for `52feda4...` **FAILED intentionally-usefully** at the new lineage scenario: an Applied candidate became invalid after cleanup advanced its object to a child revision. This exposed the defect described above; it was not suppressed.
- After the lineage fix, `core-foundation` run `34499826871` for `dfedbf533...` passed the Stage-B/Core suite including cleanup lineage, stale cleanup rejection, exact export scope and save/reload.
- Broader Windows build run `34499826741` for `dfedbf533...`: Python compilation/dependency resolution, core logic/execution tests, real geometry regressions and release audit passed; editor/launcher/updater/Core C# build also passed. Portable package/layout/SHA passed. Installer-definition compilation was still finishing at the latest reconciliation and must be checked by the next run before any release decision.

A real Godot Windows release export was not run because v1.0.20 is not yet release-ready. No real CUDA inference or target-machine GUI acceptance occurred in CI. **No v1.0.20 release has been made.**

## Released v1.0.19 items awaiting real-machine verification

- **MS-009 viewport/grid/model/gizmo:** FIXED - NEEDS USER VERIFICATION.
- **MS-013 storage containment:** FIXED - NEEDS USER VERIFICATION.

If either regresses on the target machine, it becomes immediate priority.

## Current highest-priority issues

1. **MS-009** — released viewport fix still needs target-machine verification.
2. **MS-018** — the Stage-C thin slice now reaches durable cleanup and scoped STL export in code/tests, but still needs target-machine qualification and authoritative normal-edit persistence.
3. **MS-013** — released storage containment still needs representative target-machine verification.
4. **MS-020** — request/result identity and save-failure consistency are hardened for the migrated Stage-C seam; durable queue/resource ownership/crash recovery remain incomplete.
5. **MS-022** — readiness/self-test/inference-recording exists; GTX 1080 qualification/default-provider evidence is missing.
6. **MS-019** — legacy `Main.V*.cs` remains authoritative outside migrated vertical slices, including normal transform/sculpt mutations.

## Immediate engineering priority

1. Reconcile final result of build `34499826741`; fix any packaging regression if present.
2. Continue the same Stage-C vertical slice by moving normal transform mutations for mapped Stage-C objects into `ProjectSession` transactions so export/save/reload cannot ignore visible moves/rotations/scales.
3. Migrate the minimum sculpt/edit mutation needed for Stage-C acceptance to a new immutable mesh revision with full-state undo, or explicitly constrain the Stage-C acceptance path until that authority exists.
4. Add deterministic export artifact validation at a lower layer if practical so exact revision → STL output can be tested without Godot UI interaction.
5. Collect real GTX 1080 provider readiness/inference timing/RAM/VRAM evidence and verify v1.0.19 viewport/storage behavior when target hardware is available.

## User input currently required

No product/design decision blocks autonomous development. Real-machine acceptance evidence remains useful when available, but independent engineering can continue without it.
