# Miniscuplter Current Execution State

> Authoritative current Coordinator↔Dev state. Strategy lives in TECHNICAL_DIRECTION_STATE; HANDOFF and TECHNICAL_ROADMAP are legacy/read-only.

## Release state
- stable: `v1.0.27` @ `7d40d06cb4084403db3193ec77ab77b71baa24e2`
- writable: `v1.0.28`
- release_freeze: false
- publication_active: false
- exact_head: `4b6ea637ca572b7e7983bfbba051635e21a5b40c`

## CURRENT — F: v1.0.28 integration checkpoint
- status: COMPLETE — COORDINATOR REVIEW REQUESTED
- outcome: A–E are implemented and integrated; strongest branch CI is green at exact HEAD
- validation: Core foundation green; full build green including semantic identity, C#/.NET, Python/runtime/job tests, workspace regressions, geometry, backend lifecycle, release audit, portable package/hash and installer-definition compilation
- acceptance/stop: met for automated checkpoint; released-build GTX 1080 verification still required for user-observed runtime/UI defects
- continuation: coordinator_review_required

## Completed queue
- A: v1.0.28 semantic-version bootstrap complete
- B / MS-020: Generate 3D model-release calls preserve the active heavyweight job owner
- C / MS-031: accepted baseline is restored after UI composition on reopen with regression coverage
- D / MS-009: viewport grid bars render two-sided with targeted regression coverage
- E / MS-026, MS-027: bottom-center AI console, compact 2x2 telemetry and circular XYZ orientation control implemented; accepted viewport tools preserved

## Next
Coordinator owns release-readiness/chunk review. Dev must not invent additional scope until CURRENT_EXECUTION_STATE is replenished or release-state direction changes.
