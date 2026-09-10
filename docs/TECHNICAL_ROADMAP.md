# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. The Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, and version scope. `HANDOFF.md` remains the immediate Dev Cycle baton.

Last coordinator review: 2026-09-10
Current stable release: `v1.0.19`
Current development branch: `v1.0.20`
Latest validated application commit reviewed: `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`
Acceptance-weighted completion at review: **57%**

## 1. Current technical objective

Finish one trustworthy Stage-C vertical slice on the replacement Core architecture and ship it as a meaningful testable increment before broadening migration.

The target slice is:

`2D source → accepted immutable image baseline → revision-bound local 3D generation → explicit candidate Apply → visible project-owned object → save/reload → authoritative transform + one committed mesh edit → immutable cleanup revision → exact validated STL export`

The purpose is not to make every historical 3D feature Core-native before release. It is to prove that the new identity/revision/persistence model can own a complete practical path without falling back to widget/STL authority for the state that matters to that path.

## 2. Technical-direction assessment

**Direction: PRESERVE, with tighter milestone boundaries.**

The major-refactor-with-selective-rewrites strategy remains correct. Recent work is converging vertically rather than creating another parallel architecture:

- accepted images are project-owned immutable revisions;
- generation is bound to stable project/image/object/job identity and rejects stale/mismatched results;
- Apply/Discard and save recovery are transactional;
- cleanup creates immutable child mesh revisions instead of overwriting meshes;
- export resolves exact durable object/revision scope and treats STL only as interchange/output.

Do not replace this with a ground-up rewrite. Do not broaden Core migration horizontally yet. Continue using compatibility bridges only where they move this same vertical slice onto Core authority; they are transitional seams, not the final presentation architecture.

## 3. Current milestone — v1.0.20 Stage-C foundation slice

### Required before v1.0.20 release candidate

1. **Transform authority**
   - Move/rotate/scale for mapped Stage-C objects must update `ProjectObject.Transform` through `ProjectSession` transactions.
   - Save/reload and export must reproduce the visible transform from durable project state.
   - Undo/redo must restore the complete transform state.
   - Export must not read uncommitted Godot/widget transforms as a shortcut.

2. **One authoritative mesh-edit path**
   - Migrate the minimum practical sculpt/mesh mutation needed to prove editable Stage-C ownership.
   - A committed edit on a mapped Stage-C object must create a new immutable `MeshRevision`, parent it to the exact active input revision, transactionally advance the object's active revision, and preserve generation provenance through descendant lineage.
   - Full sculpt subsystem migration is explicitly NOT required for v1.0.20.

3. **Revision-to-export acceptance seam**
   - Preserve exact object/revision export scope.
   - Add the lowest-cost deterministic testable seam for `MeshRevision + ProjectObject.Transform → validated STL artifact` if practical without duplicating UI/backend paths.
   - Existing destination safety/validation must remain fail-closed.

4. **Automated release gates**
   - Exact application HEAD Core/Stage-C regressions green.
   - Broader Windows C#/Python/geometry/release-audit/package gates green.
   - Real Godot Windows export, artifact/hash verification, and installer smoke-install pass before publication.

### Explicitly NOT required before v1.0.20 publication

- complete migration of all sculpt tools;
- full durable Job Broker/queue/crash-recovery architecture;
- broad provider-framework expansion;
- Rig & Pose migration;
- kitbash/parts migration;
- removal of legacy `Main.V*.cs` globally;
- complete UI rewrite;
- target-machine verification of every historical `FIXED - NEEDS USER VERIFICATION` issue before a testable build exists.

## 4. Release / target-machine strategy

Target-machine acceptance is a real product gate, but it should not become a circular prerequisite that prevents shipping the build needed to obtain that evidence.

For **v1.0.20**:

- finish the bounded Stage-C state-authority scope above;
- pass all automated and Windows packaging/release gates;
- publish v1.0.20 as the immutable testable increment;
- immediately move further application development to `v1.0.21`;
- use released v1.0.20 on the GTX 1080 / 16 GB reference PC to collect acceptance evidence for MS-009, MS-013, MS-018, and MS-022.

If target-machine testing exposes a release-blocking defect, fix it forward in v1.0.21. Do not mutate v1.0.20.

This does not lower the definition of acceptance: Stage-C is not considered accepted until real-machine evidence exists. It only separates **release readiness** from **post-release target-machine qualification** where publication is needed to perform that qualification.

## 5. Ordered critical path

### P0 — Finish in-slice state authority (`MS-019` serving `MS-018`)

1. Map transform gizmo/move/rotate/scale commit paths to `ProjectSession` transactions for Stage-C objects.
2. Persist/reload/undo those transforms and ensure export uses only durable state.
3. Migrate one bounded committed sculpt/mesh edit to immutable child-revision semantics.
4. Add stale/undo/reload/export interaction regressions.

Rationale: cleanup/export now trust ProjectStore. Visible edits that escape Core would make the user's apparent model disagree with the saved/exported model; that is the largest remaining in-slice architectural correctness gap.

### P1 — Harden exact export artifact seam (`MS-018`)

Extract/test revision-to-STL artifact generation below UI when this can be done without adding another authority path. Preserve STL as output/interchange only.

### P2 — Cut v1.0.20 release candidate and publish when gates pass

Do not hold v1.0.20 merely to accumulate unrelated architecture work. Once P0/P1 are coherent and required release gates pass, publish.

### P3 — Reference-machine acceptance

On released v1.0.20 collect evidence for:

- `MS-009`: viewport/grid/model/gizmo visibility and interaction;
- `MS-013`: representative storage containment, especially caches/temp/intermediates;
- `MS-018`: full Stage-C user flow through save/reload/edit/cleanup/export;
- `MS-022`: at least one intended lightweight/default 3D route, with elapsed time and practical RAM/VRAM behavior;
- cancellation/recovery where practical in the same test session.

A real failure here immediately outranks planned post-release architecture work.

### P4 — Post-Stage-C structural work

Only after the v1.0.20 slice is released and acceptance evidence is available:

1. address any target-machine regressions first;
2. continue `MS-020` toward authoritative durable job ownership/queue/crash recovery;
3. continue `MS-019` outward from the proven slice into practical editing architecture;
4. use measured `MS-022` evidence to define default/supported/experimental provider tiers;
5. then advance Stage D practical editing priorities.

## 6. Issue priority

### Active critical-path priority

1. **MS-018 — Critical:** primary milestone. The production path is structurally close but not accepted until editing authority and target qualification are complete.
2. **MS-019 — High architectural risk, currently P0:** narrow focus on transform + one mesh edit for Stage-C. Do not broaden legacy migration yet.
3. **MS-009 — Critical verification risk:** if target testing still shows a blank viewport, it becomes immediate P0. Until new runtime evidence exists, do not spend autonomous cycles adding speculative viewport overlays/fixes.
4. **MS-013 — High verification risk:** same principle; verify on target machine before more speculative containment work unless CI/code review finds a concrete leak.
5. **MS-022 — High:** collect one real intended 3D-provider qualification on reference hardware; do not expand provider count first.
6. **MS-020 — High but temporarily behind Stage-C closure:** existing correlation/cancellation/save-safety work is sufficient for the thin slice. Resume durable broker work after the slice/release unless a concrete job-lifecycle bug blocks it.

### Lower-priority / deferred while Stage-C closes

- optional provider additions;
- full UI declarative rewrite;
- broad sculpt architecture migration;
- Rig & Pose modernization;
- kitbash expansion;
- node-graph/animation-suite/cloud features;
- cleanup feature breadth beyond the minimum reliable path.

## 7. Dependency notes

- Stage-C release readiness depends on in-slice editing authority, not on completion of the whole Stage-B refactor.
- Transform persistence must precede trustworthy export acceptance because export intentionally reads durable `ProjectObject.Transform`.
- One mesh-edit revision path must precede claiming the Stage-C object is meaningfully editable under the replacement state model.
- Target-machine provider qualification depends on having a published/installable build and installed runtime/model state; it should feed provider policy rather than be simulated by CI.
- Broader Job Broker work should reuse the proven project/revision identity semantics rather than invent another identity model.
- Legacy removal remains blocked on migration/acceptance coverage; compatibility fallbacks for non-migrated objects remain legitimate for now.

## 8. Architecture ownership boundaries

- **Core C# domain/state:** project identity, objects, immutable image/mesh revisions, provenance, transactions/history, persistence contracts, stale-result rules, cleanup/edit revision contracts.
- **Godot presentation/editor:** viewport, input tools, visual scene projection of Core state, dialogs, user interactions. For migrated Stage-C objects Godot must project authoritative Core state, not independently own durable model state.
- **Python backend:** inference and geometry execution, provider/runtime management, structured progress. It receives immutable/revision-bound job inputs and returns isolated artifacts; it does not become project-state authority.
- **Launcher/updater:** delivery/runtime/update ownership and data preservation; do not mix this with project editing state.
- **STL:** interchange/export artifact only.

## 9. Technical risks to watch

1. **Dual authority during migration:** Godot scene mutations can visually succeed while durable Core state remains unchanged. This is the current highest architectural risk.
2. **Compatibility bridge accumulation:** `Main.V1020...` bridges are acceptable only while shrinking widget authority in the active slice. Do not let them become permanent parallel business logic.
3. **False acceptance from CI:** rendering/CUDA/storage behavior still requires reference-machine evidence.
4. **Provider-framework drift:** readiness infrastructure must serve one reliable default route before supporting more adapters.
5. **Release starvation:** do not keep v1.0.20 open for unrelated refactor work once its bounded Stage-C increment is coherent.
6. **Release regression churn:** target-machine failures should be fixed forward in the next semantic version, never by altering a published build.

## 10. Next Coordinator-level objectives for Dev Cycle

Until superseded by a later Coordinator review, the Dev Cycle should:

1. finish authoritative Stage-C transform transactions/persistence/undo;
2. migrate exactly one bounded mesh-edit/sculpt commit path to immutable child revisions;
3. regression-test save/reload/undo/stale cleanup/export interactions;
4. harden the revision→validated-STL seam if it can be done without duplicate authority;
5. once those are coherent and release gates pass, publish v1.0.20 rather than broadening scope;
6. after publication, move code work to v1.0.21 and prioritize actual reference-machine findings over speculative refactor work.

## 11. User input

No product-level decision currently blocks development. The user will be needed for reference-machine acceptance once v1.0.20 is published, especially viewport/storage behavior and real 3D provider qualification.
