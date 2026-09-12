# Miniscuplter Current Execution State

> Authoritative current Coordinator↔Dev state. Strategy lives in TECHNICAL_ROADMAP; HANDOFF is legacy/read-only.

## Release state
- stable: `v1.0.27` @ `7d40d06cb4084403db3193ec77ab77b71baa24e2`
- writable: `v1.0.28`
- release_freeze: false
- publication_active: false
- bootstrap: incomplete — `ai_backend/app.py` and `tools/release_audit.py` still identify as 1.0.27

## CURRENT — A: v1.0.28 bootstrap
- priority: required gate
- outcome: remaining identity surfaces changed to 1.0.28; exact-head Core/full build green
- dependencies: none
- acceptance/stop: semantic identity, Core, .NET and packaging gates green at exact HEAD
- preemption: reference-machine correctness/persistence/data-safety evidence may preempt only after bootstrap is mechanically complete
- continuation: automatic

## NEXT — B: P0 Generate 3D runtime ownership
- issue: MS-020
- outcome: 2D→3D generation completes without model release/unload conflicting with active heavyweight generation ownership
- dependencies: A
- acceptance/stop: targeted ownership/lifecycle regressions + exact-head validation; remains NEEDS USER VERIFICATION until GTX 1080 generation succeeds
- preemption: update/backend/data-safety failures
- continuation: automatic

## NEXT — C: P0 accepted-baseline reopen persistence
- issue: MS-031
- outcome: retained accepted 2D concept reopens already accepted and immediately eligible for Generate 3D
- dependencies: A
- acceptance/stop: save/close/reopen regression proves baseline identity, accepted status and generation eligibility restore together
- preemption: data-loss/persistence failures
- continuation: automatic

## NEXT — D: viewport grid
- issue: MS-009
- outcome: visible neutral non-occluding grid at launch and through resize
- dependencies: B, C
- acceptance/stop: targeted renderer ownership regressions + exact-head validation; GTX 1080 retest required
- preemption: generation/persistence/data-safety failures
- continuation: automatic

## NEXT — E: workspace corrections
- issues: MS-026, MS-027
- outcome: bottom-center multiline AI console; vertical contextual actions; 2×2 bottom-left telemetry; circular XYZ orientation gizmo without cube body; accepted viewport tools preserved
- dependencies: D
- acceptance/stop: targeted UI regressions + exact-head validation; target-machine UX retest required
- preemption: correctness/persistence/runtime failures
- continuation: automatic

## NEXT — F: v1.0.28 integration checkpoint
- outcome: strongest integrated validation complete and release-worthy checkpoint available
- dependencies: A–E
- acceptance/stop: Core/C#/Python/runtime/geometry/release-audit/packaging green; canonical state reconciled
- preemption: any release blocker
- continuation: coordinator_review_required
