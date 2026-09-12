# Miniscuplter Project Status

> Secondary dashboard only. Authoritative execution/strategy/issue truth lives in CURRENT_EXECUTION_STATE, TECHNICAL_DIRECTION_STATE and ISSUE_STATE.

Last reconciled: 2026-09-12

## Current state
- Stable: `v1.0.27` @ `7d40d06cb4084403db3193ec77ab77b71baa24e2`
- Writable: `v1.0.28`; no release freeze/publication
- Acceptance-weighted completion: approximately **64%**
- v1.0.28 bootstrap and the authorized A–E implementation queue are complete; Coordinator review is required at the integrated checkpoint.

## Released-build verification pending
- **MS-020:** fixed in code; GTX 1080 Generate 3D retest required.
- **MS-031:** fixed in code with reopen regression; released-build save/close/reopen retest required.
- **MS-009:** fixed in code with two-sided grid regression; GTX 1080 grid/resize retest required.
- **MS-026/MS-027:** requested workspace composition implemented; target-machine UX retest required.
- **MS-029:** updater behavior still needs a transition initiated from installed v1.0.27.
- **MS-030:** packaged backend-health fix remains NEEDS USER VERIFICATION.

## Current checkpoint
Integrated v1.0.28 application checkpoint: `4b6ea637ca572b7e7983bfbba051635e21a5b40c`. Exact-checkpoint Core and full build CI are green, including semantic identity, C#/.NET, Python/runtime/job tests, workspace regressions, geometry, backend lifecycle, release audit, portable packaging/hash and installer-definition compilation.

## User dependency
No product decision is required. After Coordinator release review and a corrected release, verify accepted-baseline reopen state, Generate 3D, grid/resize behavior, workspace composition, backend health and the updater transition from v1.0.27.
