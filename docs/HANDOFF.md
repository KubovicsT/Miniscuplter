# Miniscuplter Handoff

> Immediate Dev execution baton. TECHNICAL_ROADMAP owns whole-system sequencing.

Last updated: 2026-09-12

## Repository / release state
- Stable: `v1.0.26` at `a41e0419ba40fd1118775e8f516b1a31145f18d8` (published, immutable).
- Writable branch: `v1.0.27`.
- Latest validated engineering checkpoint: `3d63204c368ca6b6564b7e1b74868f3636183668`; current docs HEAD `ab243c95406127c7f3d9685edb3eec1deb3797dc` has green Core/build CI.
- No active release request freezes `v1.0.27`.
- Prior A/B/C queue is accepted: 1.0.27 bootstrap, Core-authoritative ground placement, stable-ID viewport picking.

## CURRENT — D: revision-bound sculpt/edit seam
**Outcome:** make one production sculpt/edit path consume exact `ObjectId + active MeshRevisionId` and reject stale selection/revision state.
**Constraints:** Core owns durable revision/history state; Godot owns live input/presentation; preserve immutable mesh revisions and save/reload/undo semantics; no broad sculpt rewrite or Job Broker expansion.
**Dependencies:** A/B/C accepted; no new v1.0.26 reference-machine blocker.
**Acceptance:** one real sculpt/edit path fails closed on stale identity, advances immutable revision state transactionally, restores presentation from Core on failure, and passes focused + exact-head validation.
**Preemption:** any new backend/viewport/update/persistence/storage/cancellation/Stage-C failure from v1.0.26.
**Auto-proceed:** YES → E.

## NEXT — E: protected-region / selection dependency invalidation
**Outcome:** make one durable revision-dependent selection/protected-region path explicitly invalidate or transfer when topology/revision changes.
**Constraints:** reuse Core `SelectionBinding`; no generalized scene/resource rewrite; stale indices must never silently survive a revision change.
**Acceptance:** one production dependency is revision-bound with transfer/invalidation regressions and save/reload safety.
**Auto-proceed:** YES → F unless preempted.

## NEXT — F: Stage-D transactional edit-history closure
**Outcome:** prove one user-visible edit sequence across select → transform/sculpt → undo/redo → save/reopen restores complete dependent state rather than mesh-only state.
**Constraints:** use existing transactional history/project store; no provider/UI breadth.
**Acceptance:** focused automated round-trip covers object identity, active mesh revision, transform and revision-bound selection/dependency state with exact-head CI green.
**Auto-proceed:** NO — Coordinator review after F.

## Release / user evidence
Do **not** release v1.0.27 yet; current scope is still an accumulating authority-migration tranche. Released v1.0.26 reference-machine acceptance remains P0 and immediately preempts D/E/F.

User test order: update/reopen → Repair AI Runtime → Generate 3D → resize invariance → compact workspace UX → Apply/save-reopen/edit-sculpt/cleanup/exact-STL, including storage/cancellation observations.
