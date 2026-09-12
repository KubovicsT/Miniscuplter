# Miniscuplter Handoff

> Current execution baton. TECHNICAL_ROADMAP owns strategy.

## Release / branch state
- Stable: `v1.0.26` at `a41e0419ba40fd1118775e8f516b1a31145f18d8`.
- `v1.0.27` is **FROZEN** at candidate `7d40d06cb4084403db3193ec77ab77b71baa24e2`; autonomous release-control is running.
- Forward transition branch: `v1.0.28`, created from the exact frozen candidate.
- `v1.0.28` is **BOOTSTRAP-ONLY**, not ordinary writable development yet. Launcher/updater/editor/installer/export/display identity is being moved to 1.0.28; backend/audit/lifecycle identity and exact-head CI remain to be completed.
- Dev scheduler is currently disabled; Coordinator did not change it.

## CURRENT — A: complete v1.0.28 bootstrap
**Outcome:** coherent 1.0.28 identity on every audited surface and green exact-head Core/build CI.
**Constraints:** never mutate frozen v1.0.27; mechanical identity only; no product changes until green.
**Acceptance:** release audit, backend lifecycle, editor/Core, runtime, geometry and packaging gates green at exact HEAD.
**Preempt:** any v1.0.27 publication failure requiring candidate/source repair.
**Auto-proceed:** YES after v1.0.27 publishes and A is green.

## NEXT — B: durable local job envelope (MS-020)
Persist one canonical job record for generation with stable job ID, immutable project/object/revision input context, stage/state and contained artifacts. Recover truthful terminal/incomplete state after backend restart. No provider expansion.
**Auto-proceed:** YES if no target-machine blocker.

## NEXT — C: heavyweight runtime ownership gate
Make one authoritative local ownership gate serialize heavyweight GPU inference against conflicting runtime install/repair/remove work. Preserve local-first behavior and existing provider routing.
**Auto-proceed:** YES.

## NEXT — D: truthful cancellation/recovery
For one production generation path, acknowledge cancellation only after owned work is actually stopped; persist terminal state and prove the next job starts cleanly after restart/recovery.
**Auto-proceed:** NO — Coordinator review after D.

## P0 preemption
Any v1.0.26/v1.0.27 reference-machine failure in update, backend health, generation, viewport, persistence, storage, cancellation or Stage-C correctness immediately outranks B–D.
