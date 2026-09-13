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
- current: revision-bound candidate/selection persistence foundations and UI-neutral Core attachment transaction authority are implemented; exact-head Core and full build/package validation are green
- completed_this_slice: save/reopen preserves exact candidate and protected-region revision bindings and keeps prior protected-region bindings explicitly stale after candidate revision advance; the guarded schema-2 patch-control new-text-file route was exercised successfully and AUTO-INC-009 was closed; Core now owns stable attachment create/update/remove transactions with stale-update/removal rejection, self-attachment rejection, undo/redo, durable save/reopen, and an explicit state removal primitive; no user-facing Kitbash interaction was chosen or changed
- next:
  1. define and implement the revision-dependency contract for attachments so topology/revision advance performs an explicit preserve/transfer/invalidate decision rather than silently retaining ambiguous attachment meaning
  2. add save/reopen and revision-advance coverage for that attachment dependency contract
  3. continue independent UI-neutral Refinement/Kitbash foundations only where Coordinator direction is already sufficient
  4. stop before irreversible or user-owned Refinement/Kitbash UI decisions
- process_blocker: none; AUTO-INC-009 is recovered and closed after successful live schema-2 creation plus exact-head green validation
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
