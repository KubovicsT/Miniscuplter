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
- current: attachment revision authority is implemented and validated; continue revision-safe Refinement candidate/dependency foundations without choosing user-facing interaction design
- completed_this_slice: Core attachments now bind exact parent/child mesh revisions at creation, derive stale/non-authoritative state when either active revision advances, support explicit transactional rebind to current revisions, reject stale updates, retain stale records for recovery/removal, and preserve exact binding/status state through save/reopen and undo/redo. Focused regression coverage is wired into the normal Core suite; exact-head Core, Python/runtime, release-audit and full packaging validation are green.
- attachment_revision_contract:
  1. every durable attachment binds the exact active parent mesh revision and exact active child mesh revision present when the attachment is created or explicitly rebound
  2. if either bound object advances to a different active mesh revision, the attachment does not silently transfer or continue driving authoritative placement; the durable binding resolves stale until an explicit Core transaction resolves it
  3. automatic transfer remains disallowed without explicit compatibility evidence; no automatic transfer path was introduced
  4. topology-changing or unknown-compatibility edits therefore default to stale/non-authoritative state rather than silent deletion or transfer
  5. Godot may visualize stale attachments and request rebind/remove, but Core remains the sole durable authority for current/stale/rebound semantics
- next:
  1. continue revision-safe Refinement candidate/dependency foundations where no user-facing interaction decision is required, including explicit discard/conflict behavior and dependency preservation/invalidation coverage
  2. converge the remaining legacy Godot attachment DTO/display-name ownership onto the Core attachment authority without changing the user-facing Kitbash workflow
  3. stop before irreversible or user-owned Refinement/Kitbash UI decisions
- acceptance: no revision-dependent attachment, selection or refinement dependency can silently survive an incompatible mesh revision change; save/reopen and undo/redo preserve exact durable state; exact-head Core and full build/package validation remain green
- process_blocker: none; the prior patch-control request-formatting failures were narrowed to malformed diff metadata and recovered with validated small patches
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
