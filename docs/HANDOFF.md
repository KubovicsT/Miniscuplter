# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest validated application/code commit:** `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`
- **Overall completion:** 57% acceptance-weighted
- **Coordinator roadmap:** `docs/TECHNICAL_ROADMAP.md`
- **Coordinator history:** `docs/COORDINATOR_LOG.md`

Release/version reconciliation is clean: v1.0.19 remains immutable and published; v1.0.20 is the distinct forward development branch.

## Completed Stage-C trajectory

The migrated vertical slice now has:

- project-owned immutable accepted `ImageRevision` baseline;
- end-to-end generation identity (`GenerationJobId`, `ProjectId`, input project revision, accepted `ImageRevisionId`, reserved `ObjectId`);
- fail-closed stale/mismatched result handling;
- durable generated `MeshRevision` candidate with explicit Apply/Discard;
- visible restore after restart;
- recovery-safe project saves;
- revision-bound cleanup that creates a new immutable child mesh revision rather than overwriting the source;
- explicitly scoped Stage-C STL export from the durable active object/revision;
- destination-side temporary write, reopen/finite/non-empty validation, then atomic replacement;
- generated candidate provenance preserved through descendant cleanup lineage.

Recent application commits for cleanup/export:

- `3f34cd078d435d532632612e9a52d79860c888d6` — transactional Stage-C cleanup revision contract
- `35078ab977839353afa3b59461d065889e83d50d` — bind Stage-C cleanup/export to project revisions
- `55db2a7641bcbffa912f4b93aa131f1c15629816` — install Stage-C cleanup/export bridge
- `52feda4be0ef46f5c5e3dc4ab07261e3028f3f0f` — cleanup lineage/export-scope regressions
- `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b` — preserve applied candidate provenance through cleanup descendants

The first cleanup-lineage regression at `52feda4...` exposed a real defect: Applied candidate validation incorrectly required the generated revision to remain the exact current active revision. `dfedbf533...` corrected this to require the generated revision to remain in the active object's ancestry, with cycle protection. Preserve this failed-then-fixed history.

## Validation

For application commit `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`:

- `core-foundation` run `34499826871`: **SUCCESS**
- broader Windows build run `34499826741`: **SUCCESS**
- C# editor/launcher/updater/Core build: **PASS**
- Python compile/dependency resolution: **PASS**
- Core/execution tests: **PASS**
- real geometry regressions: **PASS**
- release audit: **PASS**
- portable package/layout/SHA checks: **PASS**
- installer-definition compilation: **PASS**

No real CUDA inference or target-machine GUI/storage acceptance has yet been performed for the current Stage-C path.

## Coordinator direction

The selective-refactor direction remains correct. Do not broaden into an all-at-once rewrite.

The current highest architectural risk is **dual authority during normal Stage-C editing**: Godot can visibly mutate a mapped object while durable Core/ProjectStore state may remain unchanged. Since save/reload/export intentionally trust Core state, this can make the visible model differ from the durable/exported model.

`docs/TECHNICAL_ROADMAP.md` is authoritative for the bounded v1.0.20 scope.

## Exact next task

Continue only the same Stage-C vertical slice:

1. Reconcile actual branch/release/CI state first. Stable should remain v1.0.19 unless a newer release was genuinely published.
2. Audit normal move/rotate/scale handlers and transform-gizmo commit paths for mapped Stage-C objects.
3. Route committed transforms through `ProjectSession` transactions updating the exact `ProjectObject.Transform`, preserving `ObjectId` and active `MeshRevisionId`.
4. Persist at a safe boundary so save/reload/export reproduce the visible transform. **Do not make export read uncommitted Godot/widget transform state as a shortcut.**
5. Add deterministic coverage for transform persistence, undo/redo, save/reload, and export interaction.
6. Then migrate **one bounded committed mesh-edit/sculpt path** for mapped Stage-C objects: create a new immutable child `MeshRevision`, preserve parent/provenance, and transactionally advance the same object. Do not migrate the entire sculpt subsystem in v1.0.20.
7. Add regression coverage for descendant edit lineage, candidate provenance, undo/redo, stale cleanup, and exact export scope.
8. If practical, extract the exact `MeshRevision + ProjectObject.Transform → validated STL artifact` step below Godot UI for deterministic testing, without introducing STL as project storage or a parallel export authority.
9. Preserve existing AIClient/backend cancellation/resource ownership. Do not create another HTTP/job mechanism.

## Current issue priority

1. **MS-018 — Critical:** complete/qualify the Stage-C thin slice.
2. **MS-019 — Immediate P0 within Stage-C only:** transform + one mesh-edit state authority.
3. **MS-009 — Critical verification risk:** if target-PC testing still shows blank viewport/grid/model/gizmo, it immediately becomes P0; do not add speculative fixes without new evidence.
4. **MS-013 — High verification risk:** verify storage containment on target PC before speculative additional hardening unless a concrete leak is found.
5. **MS-022 — High:** qualify one intended lightweight/default 3D route on GTX 1080; do not expand provider count first.
6. **MS-020 — High but sequenced behind Stage-C closure:** resume durable queue/resource ownership/crash-recovery work after the thin slice unless a concrete lifecycle defect blocks it.

## v1.0.20 release policy

Do not modify v1.0.19.

Do **not** release v1.0.20 merely because a scheduled run ends or CI is green. However, also do not hold v1.0.20 open for unrelated refactor work.

v1.0.20 becomes release-ready when the bounded roadmap scope is coherent:

- Stage-C transform state is Core-authoritative and durable;
- one bounded committed mesh-edit path uses immutable child revisions;
- exact revision/export behavior is trustworthy and regression-covered;
- no known release-blocking regression remains;
- automated gates pass;
- real Godot Windows export passes;
- artifacts/hashes verify;
- installer smoke-install passes;
- canonical docs describe the released increment.

**Target-machine acceptance is not a circular precondition to publishing the build needed for that test.** Once the bounded scope and release gates above pass, publish immutable v1.0.20, verify GitHub latest-release state, then move application development to v1.0.21. Use released v1.0.20 for GTX 1080 / 16 GB acceptance of MS-009, MS-013, MS-018, and MS-022. Any failures are fixed forward in v1.0.21; never mutate v1.0.20.

Stage-C itself is not considered accepted until that real-machine evidence exists.

## Explicit non-priorities before v1.0.20 release

Do not broaden into:

- full sculpt migration;
- full Job Broker durability/crash recovery;
- Rig & Pose migration;
- kitbash migration;
- provider proliferation;
- full UI rewrite;
- global removal of `Main.V*.cs`.

## User input

No product/design decision blocks autonomous engineering. User testing becomes important after v1.0.20 is published: full Stage-C flow, viewport/grid/model/gizmo, storage containment, and one intended 3D provider on GTX 1080 / 16 GB.
