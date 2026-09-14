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
- current: revision-safe Refinement candidate/dependency hardening now reconciles loaded durable Ready candidates immediately: stale input bindings or invalid output lineage fail closed as durable conflicts during session construction and ReplaceFromLoad instead of remaining apparently applicable until Apply
- completed_this_slice: Core attachments bind exact parent/child mesh revisions, become stale/non-authoritative when either revision advances, support explicit transactional rebind, reject stale updates, preserve exact state through save/reopen and undo/redo, and now drive mapped Kitbash attachment actions through stable ObjectId/AttachmentRecord authority. The existing Kitbash UI remains unchanged; its legacy DTO is a presentation/compatibility projection for mapped objects rather than durable placement authority. Mapped stale attachments no longer continue driving placement from display-name DTO state. Unmigrated legacy objects retain the existing fallback until they acquire stable Core object identity. Smart Selection grow/shrink/smooth now reconciles the changed weight array into the existing durable revision-bound selection persistence path instead of leaving refined weights only in Godot memory. Ready Refinement candidates are conflicted as soon as their output revision is not descended from their bound input revision, and loaded Ready candidates are now reconciled against the current active input revision plus output ancestry before the session exposes them. Focused regression coverage verifies stale loaded candidates conflict immediately, invalid output lineage conflicts immediately, valid loaded candidates remain Ready, and ReplaceFromLoad performs the same reconciliation. Exact-head Core, C#, Python/runtime, release-audit and full packaging validation for the implementation changes are green.
- attachment_revision_contract:
  1. every durable attachment binds the exact active parent mesh revision and exact active child mesh revision present when the attachment is created or explicitly rebound
  2. if either bound object advances to a different active mesh revision, the attachment does not silently transfer or continue driving authoritative placement; the durable binding resolves stale until an explicit Core transaction resolves it
  3. automatic transfer remains disallowed without explicit compatibility evidence; no automatic transfer path was introduced
  4. topology-changing or unknown-compatibility edits therefore default to stale/non-authoritative state rather than silent deletion or transfer
  5. mapped Godot Kitbash actions resolve stable Core object identity and commit snap/detach/fine-tune through StageDAttachments; stale mapped attachments fail closed and do not keep following sockets from legacy display-name state
  6. legacy DTO/display-name behavior remains only as a migration fallback for objects that are not yet represented by stable Core identity; no user-facing Kitbash workflow change was introduced
- selection_dependency_contract:
  1. Smart Selection durable bindings remain tied to exact ObjectId + mesh revision identity
  2. revision advancement invalidates stale vertex-weight bindings instead of reusing indices against different topology
  3. grow/shrink/smooth produces a new selection-weight snapshot that is queued immediately through the same revision-bound persistence seam
- next:
  1. continue revision-safe Refinement candidate/dependency foundations where no user-facing interaction decision is required, including remaining dependency preservation/invalidation and migration coverage
  2. close remaining UI-neutral migration gaps where revision-dependent protected regions or candidates could still be interpreted from stale presentation state instead of Core identity/revision bindings
  3. stop before irreversible or user-owned Refinement/Kitbash UI decisions
- acceptance: no revision-dependent attachment, selection or refinement dependency can silently survive an incompatible mesh revision change; save/reopen and undo/redo preserve exact durable state; exact-head Core and full build/package validation remain green
- process_blocker: none; recurring patch-request formatting friction remained fail-closed and recovered within the bounded retry contract; AUTO-INC-013 carries the process evidence
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
