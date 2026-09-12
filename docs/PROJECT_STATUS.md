# Miniscuplter Project Status

> Secondary dashboard only. Authoritative execution/strategy/issue truth lives in CURRENT_EXECUTION_STATE, TECHNICAL_DIRECTION_STATE and ISSUE_STATE.

Last reconciled: 2026-09-12

## Current state
- Stable: `v1.0.28` @ `6ed31b98fe426e0ca0184530a279abce43b7031f`
- Writable: `v1.0.29`; not frozen
- Acceptance-weighted completion: approximately **64%**
- v1.0.28 was published after full release-control validation, Windows export, installer smoke test and immutable exact-SHA publication.
- v1.0.29 is in mechanical semantic-version bootstrap; application identity still needs the 1.0.28→1.0.29 bump before ordinary work.

## v1.0.28 user verification pending
- MS-020 Generate 3D ownership fix
- MS-031 accepted-baseline reopen persistence
- MS-009 viewport grid/resize behavior
- MS-026/MS-027 requested workspace composition
- MS-029 launcher behavior on the v1.0.27→v1.0.28 update
- MS-030 packaged backend health after Runtime Repair

## v1.0.29 direction
1. finish semantic-version bootstrap and exact-head validation;
2. harden durable generation job/revision identity;
3. close cancellation/recovery lifecycle behavior;
4. integrated validation and Coordinator review.

Any new v1.0.28 reference-machine regression preempts v1.0.29 reliability hardening.
