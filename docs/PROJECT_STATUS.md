# Miniscuplter Project Status

> Secondary dashboard only. Authoritative execution/strategy/issue truth lives in CURRENT_EXECUTION_STATE, TECHNICAL_DIRECTION_STATE and ISSUE_STATE.

Last reconciled: 2026-09-12

## Current state
- Stable: `v1.0.27` @ `7d40d06cb4084403db3193ec77ab77b71baa24e2`
- Writable: `v1.0.28`; no release freeze/publication
- Acceptance-weighted completion: approximately **64%**
- Bootstrap remains incomplete while `ai_backend/app.py` and `tools/release_audit.py` still identify as 1.0.27.

## Released-build acceptance blockers
- **MS-020 P0:** v1.0.27 Generate 3D fails from heavyweight runtime-ownership/lifecycle conflict.
- **MS-031 P0:** accepted 2D baseline image survives reopen, but accepted state/generation eligibility does not.
- **MS-009 Critical:** viewport grid is absent on released v1.0.27.
- **MS-026/MS-027:** viewport tool buttons are accepted; console/telemetry/orientation composition still requires the specified corrections.
- **MS-029:** v1.0.25→v1.0.27 launcher-close observation still requires verification on an update initiated by installed v1.0.27.
- **MS-030:** packaged backend-health fix remains NEEDS USER VERIFICATION.

## Current critical path
1. finish v1.0.28 semantic-version bootstrap and exact-head validation;
2. fix P0 MS-020 Generate 3D runtime ownership;
3. fix P0 MS-031 accepted-baseline persistence;
4. restore MS-009 viewport grid;
5. apply MS-026/MS-027 workspace corrections;
6. integrated validation and Coordinator release review.

## User dependency
No product decision is currently required. After a corrected release, verify baseline persistence, Generate 3D, grid behavior, workspace composition, backend health and the first updater transition initiated from v1.0.27.
