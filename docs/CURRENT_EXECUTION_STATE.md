# Miniscuplter Current Execution State

## Release state
- stable: v1.0.32
- writable: v1.0.33
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.33
- execution_hold: none

## Pending reference-machine verification
### F — verify the released v1.0.32 recovery checkpoint on the reference machine
- state: WAITING FOR USER REFERENCE-MACHINE EVIDENCE; NON-BLOCKING FOR INDEPENDENT WORK
- outcome: verify the v1.0.32 fixes against the Windows / GTX 1080 reference machine while independent v1.0.33 work continues
- release_evidence: v1.0.32 release-control publication completed successfully; tag, exact release candidate and installer/ZIP/hash assets were independently verified; validated forward branch v1.0.33 and VERSION 1.0.33 bootstrap completed, and the bootstrap exact-head build is green
- verify:
  1. MS-033 cancel an active 3D generation, then immediately generate again without restarting the app
  2. MS-036 successful 3D generation inserts directly without a redundant Apply step
  3. MS-037 generated exterior surfaces render correctly and neutral-gray shading makes surface detail readable
  4. MS-039 generated model rests on the grid at a sensible initial scale
  5. MS-040 Smart Select resolves and performs semantic selection without Not Found errors
  6. MS-035 ordinary window resize produces no black gutters and preserves the fixed side rails
  7. MS-026/MS-027 telemetry and AI command areas occupy their dedicated non-overlay bottom workspace
  8. MS-038 direct Move dragging works and Rotate exposes usable visible axis rings while precise axis controls remain functional
- acceptance: user/reference-machine evidence passes the above behaviors; failures are promoted/reopened in ISSUE_STATE and preempt lower-priority work as appropriate
- continuation: keep verification-dependent issues pending until user evidence arrives; do not treat user availability as a global development stop

## Current objective
### G — continue independent v1.0.33 foundation work
- state: ACTIVE
- outcome: advance safe work that does not depend on the pending v1.0.32 runtime verification
- current: revision-bound Refinement candidate/dependency persistence coverage is complete and exact-head Core/full build/package validation is green; next is UI-neutral Core attachment authority convergence
- completed_this_slice: exact object/input-revision binding is enforced at apply time; candidate output must descend from the bound input; stale or invalid lineage is preserved as conflict; Ready candidates support transactional apply/discard with undo/redo coverage; Failed/Conflict candidates cannot silently apply; save/reopen preserves exact candidate and protected-region revision bindings; applying a candidate advances the object revision while the prior protected-region binding becomes explicitly stale rather than silently rebinding; applied-candidate state and that stale dependency survive a second save/reopen
- next:
  1. exercise the guarded patch-control schema-2 new-text-file path with the intended UI-neutral Core attachment authority source and validate the resulting exact target head
  2. converge attachment create/update/remove semantics on stable Core AttachmentRecord/ObjectId identity and ProjectSession transactions without changing unresolved user-facing Kitbash UI
  3. add save/reopen and revision-change attachment dependency coverage, preserving explicit transfer/invalidation semantics
  4. stop before irreversible or user-owned Refinement/Kitbash UI decisions
- process_blocker: AUTO-INC-009 recovery is implemented in patch-control with an exact-HEAD guarded new-text-file operation; incident remains open until the first live schema-2 request succeeds. Existing-file development remains safe and should continue if that request is delayed or fails.
- preemption: any reproduced v1.0.32 P0/P1 regression becomes the next implementation priority after Coordinator reconciliation
- stop: strategic/user authority is required; no independent authorized work remains; a real release freeze starts; or continuing would compound a severe regression/data-loss/safety risk
- continuation: Dev continues automatically through valid independent work; Coordinator replenishes the queue without waiting for ordinary user runtime availability

## Deferred / user-owned design
- MS-041 Refinement/Kitbash: structural direction is accepted, but exact UI/interaction design remains user-approval work.
- MS-042 Rig & Pose and Cleanup & Export remain intentionally later work.

## Architecture constraints
- Core remains durable-state and transform authority.
- Godot remains presentation/input authority.
- Python remains inference/geometry authority.
- Preserve local-first/storage containment, immutable revisions/stable IDs, transactional state/history and stale-result rejection.
