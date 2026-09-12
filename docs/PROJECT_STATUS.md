# Miniscuplter Project Status

> Secondary dashboard only. Authoritative execution/strategy/issue truth lives in CURRENT_EXECUTION_STATE, TECHNICAL_DIRECTION_STATE and ISSUE_STATE.

Last reconciled: 2026-09-12

## Current state
- Stable: `v1.0.29` @ `6d3b6360b14cffb88eecb00a05835bb053b4a9b7`
- Writable: `v1.0.30`; not frozen
- Acceptance-weighted completion: approximately **66%**
- v1.0.29 was published after full release-control validation, Windows export, installer smoke test and exact-SHA publication.
- v1.0.30 semantic bootstrap is complete; root VERSION and generated version surfaces identify as 1.0.30 and the bootstrap baseline is green.
- Current branch head contains Coordinator state/dashboard reconciliation only after the validated bootstrap/application baseline.

## v1.0.29 user verification pending
- MS-020 Generate 3D ownership plus cancel/retry
- MS-031 accepted-baseline save/close/reopen restoration
- MS-009 viewport grid/resize behavior
- MS-026/MS-027 requested workspace composition
- MS-029 updater behavior from installed v1.0.27 to v1.0.29
- MS-030 packaged backend health after Runtime Repair

## v1.0.30 direction
1. harden transactional generation-result commit, stale-result rejection, recovery and save/reload continuity;
2. audit viewport/state ownership and remove only proven safely migrated redundancy;
3. integrated validation and Coordinator release review.

Any new v1.0.29 reference-machine P0 regression preempts planned v1.0.30 cleanup work.
