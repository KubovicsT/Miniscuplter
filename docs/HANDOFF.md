# Miniscuplter Handoff

> Current execution baton. TECHNICAL_ROADMAP owns strategy.

## Release / branch state
- Stable: `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2` (published, immutable; Windows installer/ZIP/hash verified).
- Forward branch: `v1.0.28`, created from the exact v1.0.27 candidate.
- `v1.0.28` is **BOOTSTRAP-ONLY**. Ordinary product work must not start yet.
- Bootstrap checkpoint `3f6d791a1211f51ae10ca35b16435c53f725348d`: launcher, updater, editor assembly, installer, Windows export metadata, editor display and backend lifecycle expectation are 1.0.28. CI correctly fails semantic identity because **`ai_backend/app.py` and `tools/release_audit.py` still say 1.0.27**.
- Dev scheduler is disabled; Coordinator did not change it.

## CURRENT — A: complete v1.0.28 bootstrap
**Outcome:** change only the two remaining stale release-identity surfaces to 1.0.28, then obtain green exact-head Core/build CI.
**Constraints:** never mutate published v1.0.27; no product changes before bootstrap acceptance.
**Acceptance:** semantic identity, backend lifecycle, release audit, editor/Core, runtime, geometry and packaging gates green at exact HEAD.
**Auto-proceed:** YES when green, unless new reference-machine evidence preempts.

## NEXT — B: durable local job envelope (MS-020)
Persist one canonical generation-job record with stable ID, immutable project/object/revision input context, stage/state and contained artifacts; recover truthful terminal/incomplete state after backend restart. No provider expansion.
**Auto-proceed:** YES.

## NEXT — C: heavyweight runtime ownership gate
Serialize heavyweight GPU inference against conflicting runtime install/repair/remove work through one authoritative local ownership gate.
**Auto-proceed:** YES.

## NEXT — D: truthful cancellation/recovery
For one production generation path, acknowledge cancellation only after owned work is stopped; persist terminal state and prove the next job starts cleanly after recovery.
**Auto-proceed:** NO — Coordinator review after D.

## P0 preemption
Any v1.0.27 reference-machine failure in update, backend health, generation, viewport, persistence, storage, cancellation or Stage-C correctness immediately outranks B–D.
