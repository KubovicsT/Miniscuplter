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
- current: revision-safe Refinement, attachment and protected-region dependency hardening now fails closed across durable and presentation state: loaded Ready candidates reconcile stale input/invalid output lineage immediately and candidate reconciliation explicitly rejects missing target identity, stale Smart Selection weights are cleared when their durable binding is stale and stable live object identity cannot be proven, and attachment records persist stale status as soon as either bound mesh revision is no longer active
- completed_this_slice: Core attachments bind exact parent/child mesh revisions, become stale/non-authoritative when either revision advances, persist that stale state through transactions/save/reopen, reconcile legacy/current-marked stale bindings during session construction and ReplaceFromLoad, support explicit transactional rebind, reject stale updates, preserve exact state through undo/redo, and drive mapped Kitbash attachment actions through stable ObjectId/AttachmentRecord authority. Focused regression coverage now exercises revision invalidation symmetrically for both parent and child mesh advancement, proves stale updates fail closed in either direction, proves candidate-driven attachment invalidation restores current/stale semantics across undo/redo, and proves the exact stale binding survives save/reopen without silently transferring to the candidate output revision. The existing Kitbash UI remains unchanged; its legacy DTO is a presentation/compatibility projection for mapped objects rather than durable placement authority. Mapped stale attachments no longer continue driving placement from display-name DTO state. Unmigrated legacy objects retain the existing fallback until they acquire stable Core object identity. Smart Selection grow/shrink/smooth now reconciles the changed weight array into the existing durable revision-bound selection persistence path instead of leaving refined weights only in Godot memory. Stale protected-region presentation clears stale vertex weights even when the live Godot object can no longer be mapped to stable Core identity, and stale presentation binding/restore-failure pointers are retired so revision-invalid weights cannot remain visually or behaviorally authoritative. Ready Refinement candidates are conflicted as soon as their output revision is not descended from their bound input revision, loaded Ready candidates reconcile against the current active input revision plus output ancestry before the session exposes them, and reconciliation now explicitly conflicts a Ready candidate if its target object cannot be resolved instead of allowing later lineage checks to mask the missing dependency. Project-state validation already rejects missing-object candidate graphs, so this is a defense-in-depth fail-closed guard for mutation/reconciliation paths rather than a new persistence format. Focused regression coverage also proves that a reconciled candidate conflict and its diagnostic reason survive save/reopen and that rejected Apply on the reopened conflict cannot mutate the project. Focused regression coverage guards loaded candidate reconciliation, durable attachment stale reconciliation and protected-region fail-closed presentation. Exact-head Core and full build/package validation for the latest implementation checkpoint are green.
- load_reconciliation_contract: session construction and ReplaceFromLoad repairs that change stale candidate or attachment state advance the durable revision and preserve the pre-repair saved revision, so repaired sessions are dirty and saveable; unchanged valid loads remain clean. Exact-head Core and full build/package validation are green.
- attachment_revision_contract:
  1. every durable attachment binds the exact active parent mesh revision and exact active child mesh revision present when the attachment is created or explicitly rebound
  2. if either bound object advances to a different active mesh revision, the attachment does not silently transfer or continue driving authoritative placement; its durable BindingStatus is reconciled to stale until an explicit Core transaction resolves it
  3. session construction and ReplaceFromLoad fail closed legacy/current-marked attachment records whose exact revision bindings no longer match active parent/child revisions
  4. automatic transfer remains disallowed without explicit compatibility evidence; no automatic transfer path was introduced
  5. topology-changing or unknown-compatibility edits therefore default to stale/non-authoritative state rather than silent deletion or transfer
  6. mapped Godot Kitbash actions resolve stable Core object identity and commit snap/detach/fine-tune through StageDAttachments; stale mapped attachments fail closed, do not keep following sockets from legacy display-name state, and no longer project stale legacy fine-tune values into the controls after Core authority is lost
  7. legacy DTO/display-name behavior remains only as a migration fallback for objects that are not yet represented by stable Core identity; no user-facing Kitbash workflow change was introduced
  8. explicit attachment rebind persists its exact Rebound status and parent/child revision pair across save/reopen, while session construction and ReplaceFromLoad fail closed a revision-mismatched record even if legacy state marks it Rebound
- selection_dependency_contract:
  1. Smart Selection durable bindings remain tied to exact ObjectId + mesh revision identity
  2. revision advancement invalidates stale vertex-weight bindings instead of reusing indices against different topology
  3. grow/shrink/smooth/invert produces a new selection-weight snapshot that is queued immediately through the same revision-bound persistence seam
  4. stale durable protected-region bindings clear stale live weights whenever stable live object identity cannot be proven, rather than allowing presentation state to outlive Core revision authority
  5. candidate-driven revision advancement makes the bound selection stale, while undo restores current selection semantics and redo restores stale semantics against the exact durable revision history
  6. Smart Selection persistence only acknowledges the exact saved weight/query snapshot; changes made while a save is in flight are immediately reconciled and queued afterward, so an older async save cannot falsely mark newer refined weights durable
  7. failed pre-binding Smart Selection persistence removes an unreferenced snapshot asset, while a binding already accepted by Core retains its asset if the later project save fails
  8. explicit Smart Selection clear removes all durable smart-select bindings for the object through a serialized Core transaction and suppresses restore while that removal is pending, so cleared live weights cannot be resurrected by reconciliation or save/reopen
- next:
  1. continue revision-safe Refinement candidate/dependency foundations where no user-facing interaction decision is required, including remaining dependency preservation/invalidation and migration coverage
  2. close any remaining UI-neutral stale presentation paths for candidates, attachments or selections after Core authority changes
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
