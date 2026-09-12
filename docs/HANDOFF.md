# Miniscuplter Handoff

> Immediate Dev execution state. TECHNICAL_ROADMAP owns whole-system sequencing.

Last updated: 2026-09-12

## Repository / release state
- Stable: `v1.0.26` at `a41e0419ba40fd1118775e8f516b1a31145f18d8` (published, immutable).
- Writable branch: `v1.0.27`.
- Current validated engineering checkpoint / HEAD: `4d924e948647022dce4bd2f7682140a005edebc5`.
- Exact-head Core and full build workflows are green.
- No active release request freezes `v1.0.27`.

## Completed authorized queue
- **D — revision-bound sculpt/edit:** production sculpt captures exact `ObjectId + MeshRevisionId`, rejects stale/changed selection state, commits a new immutable revision through Core history, saves through the project store, and restores presentation from Core on failure.
- **E — revision-dependent protected selection:** Smart Selection persists as Core `SelectionBinding` plus project-contained asset data; revision/topology changes invalidate stale vertex indices and valid bindings restore after project/history reconciliation.
- **F — transactional Stage-D history closure:** focused round-trip covers durable transform, mesh revision advancement, selection validity/invalidation, undo/redo, save/reopen and exact edited export scope. Protected selection restoration through undo history is regression-covered.

Validation checkpoint `4d924e948647022dce4bd2f7682140a005edebc5`: Core workflow **success**; full build workflow **success** (editor/Core, Python/runtime/backend lifecycle, geometry, release audit and packaging gates).

## COORDINATOR REVIEW REQUESTED
The authorized D/E/F queue is complete and F explicitly has `Auto-proceed: NO`. Do not invent further Stage-D scope. Coordinator should review accumulated v1.0.27 scope, target-machine evidence availability, next objective ordering and release chunk/readiness.

Safe work while waiting: read-only validation/review only unless new Critical v1.0.26 reference-machine evidence preempts this boundary.

## User evidence dependency
Released v1.0.26 reference-machine acceptance remains P0: update/reopen → Repair AI Runtime → Generate 3D → resize invariance → compact workspace UX → Apply/save-reopen/edit-sculpt/cleanup/exact-STL, including storage/cancellation observations.
