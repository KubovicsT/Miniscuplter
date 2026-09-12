# Miniscuplter Technical Roadmap

> Authoritative end-to-end sequencing. HANDOFF owns immediate execution state.

Latest stable: `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2`
Forward branch: `v1.0.28` (bootstrap-only until identity + exact-head CI are green)
Acceptance-weighted completion: **approximately 64%**

## Whole-system direction
Preserve the selective-refactor architecture: Core owns durable project/object/revision/history state; Godot owns presentation/input; Python owns inference/geometry; launcher/updater owns runtime setup and delivery safety. Continue retiring duplicate authority through bounded production seams, not another broad rewrite.

## P0 acceptance path
Released-build reference-machine evidence remains the highest-value input and preempts fallback engineering:
1. update/reopen;
2. Repair AI Runtime health;
3. Generate 3D through provider resolution/inference;
4. viewport resize invariance;
5. compact workspace UX;
6. Apply → save/reopen → transform/sculpt → cleanup → exact STL export;
7. storage containment and cancellation/recovery.

CI cannot resolve renderer/GPU/runtime acceptance claims.

## v1.0.27 release decision
The completed v1.0.27 tranche is coherent and release-sized: mapped ground-placement Core authority; stable-ID viewport picking; revision-bound sculpt commits; durable protected-selection binding/invalidation; and dependent-state undo/history restoration. Exact candidate `7d40d06cb4084403db3193ec77ab77b71baa24e2` passed exact-SHA C#/Core, Python/runtime, geometry, release-audit, real Godot Windows export, installer smoke-install and immutable publication gates. v1.0.27 is published and immutable.

## v1.0.28 sequencing
### A — forward-version bootstrap
Complete 1.0.28 identity on every audited surface and restore exact-head green CI before product work. Current known stale surfaces are `ai_backend/app.py` and `tools/release_audit.py`.

### B — durable local job envelope (MS-020)
Introduce one canonical persisted generation-job record with stable job ID, immutable project/object/revision input context, stage/state and contained artifacts. Recover truthful terminal/incomplete state after backend restart. Keep scope local and provider-neutral.

### C — heavyweight runtime ownership
Establish one authoritative gate for heavyweight GPU inference versus conflicting runtime install/repair/remove operations. Do not duplicate provider routing or create a second scheduler.

### D — truthful cancellation/recovery
For one production generation path, cancellation is acknowledged only after owned work is actually stopped; persist terminal state and prove a subsequent job starts cleanly after restart/recovery.

A→B→C→D may auto-proceed when acceptance conditions are met and no P0 evidence preempts. Coordinator review is required after D.

## Priority rationale
Stage-D object/revision/selection/history seams have now reached a coherent checkpoint. With no new target-machine evidence available, the next bounded architectural risk worth reducing is MS-020 job lifetime/resource ownership because it directly affects cancellation, crash recovery, stale work and reference-machine reliability. This is not permission for provider expansion or a generalized distributed scheduler.

## Explicit non-priorities
Do not expand provider families, broad Rig/Pose/kitbash, scene-tree redesign, full sculpt rewrite, unrelated UI breadth, or speculative cloud services. Preserve local-first operation and contained storage.

## User dependency
No product decision is required. Real GTX 1080 / 16 GB testing of released v1.0.27 is the highest-value input and may immediately reorder v1.0.28 work.
