# Miniscuplter Handoff

> Operational baton for the next development run. Inspect actual Git/release/CI state first; repository state wins over this document if they differ.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest application/code commit:** `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`
- **Overall completion estimate:** 57% acceptance-weighted.

Mandatory release/version reconciliation was clean this run: v1.0.19 remains the immutable published release and v1.0.20 remains a distinct forward-only development branch. Documentation commits advance branch HEAD after the application commit; resolve exact HEAD and latest release/CI before editing.

## What this run accomplished

### MS-018 / MS-019 — persisted Stage-C cleanup and explicit STL export scope

The exact previous HANDOFF task was to carry the already-applied Stage-C object through cleanup and final STL output without reverting to widget/STL authority. That path is now implemented.

New Core contract in `Core/StageCCleanup.cs`:

- `StageCCleanup.Begin()` binds the exact project, object and active input mesh revision.
- cleanup result application requires the same project/object, the exact input revision still active, and a new output revision directly parented to that input revision;
- stale cleanup output is rejected if the object advanced while work was running;
- cleanup Apply is one `ProjectSession` transaction adding the immutable child revision and advancing the same object's `ActiveMeshRevisionId`;
- `ResolveExportRevision()` fails closed unless the requested revision is exactly the current active revision of the requested object.

Production bridge in `Scripts/Main.V1020StageCCleanupExport.cs`:

- the final composed `Repair Selected` / `Repair selected model` controls are replaced with Stage-C-aware handlers;
- selected objects that map to the Stage-C session use project revision authority; unrelated legacy objects retain the existing safe legacy repair fallback;
- the exact immutable `.msh` revision is read from ProjectStore and STL is materialized only as temporary backend interchange input;
- repair output is converted back to project-native `MeshData`, written as a new immutable child `MeshRevision` with provenance, transactionally applied and saved through `V1020SaveSessionAsync()` before the Godot mesh is changed;
- final `Export STL` is also Stage-C-aware: it resolves the durable active object/revision, applies the durable project transform for output, writes a destination-side temporary STL, reopens it, validates finite/non-empty triangle geometry, then atomically replaces the destination;
- legacy selected objects retain `SafeV095ExportStl()` fallback.

`Scripts/ExtrasInstaller.cs` installs this bridge after the v1.0.20 generation/restore bridges so it patches the final composed controls rather than competing with earlier version installers.

### Self-review regression found and fixed

The first cleanup-lineage regression run exposed a genuine integration flaw: `StageCGeneration.ValidateCandidate()` required an Applied candidate's generated `MeshRevision` to remain the object's current active revision. Legitimate cleanup advances the object to a child revision, so the candidate metadata became unreadable even though its generated revision remained valid provenance.

- Failed Core run: `34499552249` at application commit `52feda4be0ef46f5c5e3dc4ab07261e3028f3f0f`.
- Exact failure: Applied Stage-C candidate was “not the active revision of its generated object” after cleanup.
- Fix: `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b` changes Applied-candidate validation to require the generated output revision to exist in the active object's parent lineage, with cycle protection, rather than requiring exact-current equality.
- Result: generation provenance survives cleanup/descendant edits while unrelated/malformed lineage remains rejected.

The failed attempt is intentionally retained in CI history and documented here; do not erase it.

### Regression coverage

`Core.Tests/StageCGenerationTests.cs` now proves:

- cleanup binds the exact active generated revision;
- cleanup produces a new child revision and preserves the original generated revision;
- cleanup advances the same object in exactly one project transaction;
- stale cleanup results cannot overwrite a newer active revision;
- export scope rejects a stale/non-active revision and resolves the exact active revision;
- generated candidate provenance remains Applied/readable after cleanup;
- cleanup lineage and active revision survive save/reload;
- previous failed-save recovery and cross-project recovery rejection tests remain intact.

Relevant application commits:

- `3f34cd078d435d532632612e9a52d79860c888d6` — Add transactional Stage C cleanup revision contract
- `35078ab977839353afa3b59461d065889e83d50d` — Bind Stage C cleanup and export to project revisions
- `55db2a7641bcbffa912f4b93aa131f1c15629816` — Install Stage C cleanup and export bridge
- `52feda4be0ef46f5c5e3dc4ab07261e3028f3f0f` — Test Stage C cleanup lineage and export scope
- `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b` — Preserve applied candidate provenance through cleanup revisions

No new durable product decision was needed; this implements MD-006/007/008/018 and the Stage-C plan rather than changing scope or compatibility.

## Validation state

For application HEAD `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`:

- `core-foundation` run `34499826871`: **PASS** including cleanup lineage/stale/export/save-reload regressions.
- broader Windows build run `34499826741`:
  - Python compile/dependency resolution: **PASS**;
  - core logic/execution tests: **PASS**;
  - real geometry regressions: **PASS**;
  - release audit: **PASS**;
  - editor/launcher/updater/Core C# restore/build: **PASS**;
  - portable package build/layout/SHA verification: **PASS**;
  - installer-definition compilation: **PASS** at the latest check; final workflow cleanup/conclusion should still be reconciled at the start of the next run.

The earlier Core run `34499552249` **FAILED** and directly exposed the Applied-candidate lineage defect fixed by `dfedbf533...`; this is expected historical evidence, not an unresolved current failure.

A real Godot Windows release export and installer smoke-install were not run because v1.0.20 is not yet release-ready. No real CUDA inference or target-machine GUI acceptance occurred. No v1.0.20 release was made.

## Current unresolved priorities

1. **MS-009 — viewport/grid/model/gizmo:** v1.0.19 fix is published but still needs target-PC verification; a newly reported blank viewport immediately outranks planned work.
2. **MS-018 — Stage-C thin slice:** code now reaches accepted baseline → identity-bound generation → explicit Apply → save/reload → cleanup child revision → explicitly scoped validated STL. It still needs real target-machine qualification and normal editing state must stop escaping Core authority.
3. **MS-013 — storage containment:** v1.0.19 hardening still needs representative target-machine verification.
4. **MS-019 — legacy `Main.V*.cs`:** normal transforms/sculpting on mapped Stage-C objects can still mutate Godot state without transactionally advancing durable project state. This is now the most important in-slice architecture gap because durable export intentionally trusts ProjectStore, not unsaved widget state.
5. **MS-020 — final Job Broker:** transport correlation and save-failure recovery are hardened for the Stage-C seam; durable queue/resource ownership/crash recovery remain incomplete.
6. **MS-022 — provider qualification:** readiness and success recording exist; GTX 1080 lightweight/default-provider inference evidence is still missing.

## Exact next task

Continue the **same Stage-C vertical slice**; do not broaden into unrelated feature work:

1. Reconcile actual release/branch/CI first, including final conclusion of build `34499826741` and any newer documentation-only CI. Stable should remain v1.0.19 unless a release was genuinely published.
2. Audit normal move/rotate/scale handlers and transform gizmo commit paths for mapped Stage-C objects. Make those edits update the corresponding `ProjectObject.Transform` through `ProjectSession` transactions, preserving stable `ObjectId` and active mesh revision. Keep legacy fallback for non-migrated objects.
3. Save at an appropriate durable boundary so save/reload and Stage-C export reproduce the visible transform. Do not make export read uncommitted widget transforms as a shortcut; durable project state remains authority.
4. Migrate the minimum sculpt/edit mutation needed for Stage-C acceptance: a committed mesh edit on a mapped Stage-C object must create a new immutable child `MeshRevision` with parent/provenance and transactionally advance the object rather than only replacing `MeshInstance3D.Mesh`. Ensure Applied candidate provenance remains valid through that descendant lineage.
5. Add deterministic Core/editor-adjacent regression coverage for transform persistence, descendant edit lineage, undo/redo and stale cleanup/export interaction.
6. If practical, extract the exact revision→validated-STL artifact step below Godot UI so it can be regression-tested directly; do not introduce STL as internal storage.
7. Preserve existing AIClient/backend cancellation/resource ownership; do not create another HTTP/job path.
8. When target hardware is available, run the complete Stage-C slice and collect v1.0.19 viewport/storage verification plus one lightweight/default 3D inference qualification on GTX 1080/16 GB.

The immediate product goal remains one reliable `2D → accepted baseline → qualified 3D → visible/editable persisted object → cleanup revision → validated STL` path, not feature accumulation.

## Release policy

Do not modify v1.0.19. Do not publish v1.0.20 yet. Cleanup/export is now integrated, but normal Stage-C editing-state authority and target-machine acceptance are still incomplete. Release only when intended v1.0.20 scope is coherent, no release-blocking regression remains, all automated gates pass, a real Godot Windows export succeeds, artifacts/hashes verify, installer smoke-install succeeds, canonical docs describe the release, and the increment is meaningfully testable. After publication verify GitHub latest-release state and create/use v1.0.21 before further application development.

## User input

No product/design decision currently blocks autonomous engineering. Real-machine verification is useful when available, but independent engineering can continue without it.
