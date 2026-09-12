# Miniscuplter Technical Roadmap

> Authoritative end-to-end technical sequencing. PROJECT_CHARTER / DECISIONS own product truth; HANDOFF owns the immediate Dev baton.

Last coordinator review: 2026-09-12
Latest stable: `v1.0.26` at `a41e0419ba40fd1118775e8f516b1a31145f18d8`
Writable development branch: `v1.0.27`
Acceptance-weighted completion: **approximately 64%**

## Whole-system direction

Preserve the selective-refactor architecture:
- Core owns durable project/object/revision/history state.
- Godot owns presentation, viewport/input and live interaction.
- Python owns inference and geometry execution.
- Launcher/updater owns runtime setup and delivery safety.

The main architectural risk remains duplicate/legacy authority at subsystem seams. Continue retiring those seams through bounded production paths; do not reopen a broad rewrite.

## Product critical path

Released `v1.0.26` reference-machine acceptance remains P0 and outranks fallback development work:
1. update/reopen behavior;
2. Repair AI Runtime health;
3. Generate 3D through provider resolution/inference;
4. viewport resize invariance;
5. compact workspace UX;
6. candidate Apply → save/reopen → edit/sculpt → cleanup → exact STL export;
7. storage containment and cancellation/recovery observations.

Any reproduced Critical issue from this sequence immediately preempts forward migration.

## v1.0.27 accepted foundation

The first three forward objectives are complete and validated:
- semantic/version bootstrap is coherent at 1.0.27;
- mapped ground placement now commits through durable Core transform authority with stale-state protection;
- mapped viewport picking now binds stable `ObjectId + MeshRevisionId` before presentation selection.

Exact engineering checkpoint `3d63204c368ca6b6564b7e1b74868f3636183668` passed the relevant Core/editor/runtime/release-audit/package validation. Current docs HEAD remains CI-green.

## Current v1.0.27 sequence

### D — revision-bound sculpt/edit seam
Move one real sculpt/edit path onto exact object + active mesh-revision identity. Stale identity must fail closed; successful edits advance immutable revision/history state transactionally and project committed Core state back into Godot.

### E — revision-dependent selection/protected-region invalidation
Bind one production revision-dependent selection or protected-region path to Core `SelectionBinding` semantics. Topology/revision changes must explicitly transfer or invalidate it; stale indices may never silently survive.

### F — transactional Stage-D edit-history closure
Prove one user-visible select → transform/sculpt → undo/redo → save/reopen sequence restores complete dependent state: stable object identity, active mesh revision, transform and revision-bound selection/dependency state.

D and E may auto-proceed when acceptance gates are met and no P0 evidence preempts them. F ends with Coordinator review before further Stage-D expansion.

## Release strategy

`v1.0.27` remains **ACCUMULATING**, not frozen. Ground-placement plus viewport-picking authority alone are too small a release chunk immediately after v1.0.26. Reassess release size/readiness after D/E/F or earlier if target-machine evidence materially changes scope.

Do not create a speculative v1.0.28 branch. Only a real Coordinator-owned release transition after exact-head preflight may freeze v1.0.27 and establish the next forward branch.

## Explicit non-priorities

Until v1.0.26 acceptance is consumed, do not broaden into:
- new provider families or generalized Job Broker work;
- broad Rig/Pose or kitbash work;
- scene-tree/resource-graph redesign;
- full sculpt-system rewrite;
- unrelated UI breadth.

## User dependency

No product decision is required. Highest-value input is real GTX 1080 / 16 GB reference-machine evidence from the ordered v1.0.26 acceptance flow above.
