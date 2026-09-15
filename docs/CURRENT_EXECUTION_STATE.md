# Miniscuplter Current Execution State

## Release state
- stable: v1.0.34
- writable: v1.0.35
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.35; bootstrap exact-head build/package green
- execution_hold: none
- release_cadence_target: approximately two meaningful runtime-test releases per active development day when coherent/green batches exist; cadence target is not a timer-based publication rule
- current_release_slice: v1.0.35 is a fresh small-batch envelope; aim for one coherent runtime-test story after roughly 2–4 related meaningful changes or about 3–6 hours of active development, then return it to Coordinator instead of extending the version with adjacent cleanup

## Pending reference-machine verification
### F — verify the latest released v1.0.34 checkpoint on the reference machine
- state: WAITING FOR USER REFERENCE-MACHINE EVIDENCE; NON-BLOCKING FOR INDEPENDENT WORK
- outcome: use v1.0.34 as the preferred current runtime-test target on the Windows / GTX 1080 reference machine while independent v1.0.35 work continues; the prior verification checklist remains applicable because those fixes are carried forward
- release_evidence: v1.0.34 release-control publication completed successfully; tag, exact release candidate and installer/ZIP/hash assets were independently verified; validated forward branch v1.0.35 and VERSION 1.0.35 bootstrap completed; bootstrap exact-head build/package is green
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
### G — continue independent v1.0.35 foundation work
- state: ACTIVE
- outcome: advance the next bounded, coherent runtime-test batch without depending on immediate user verification of v1.0.34
- release_boundary: v1.0.34 is published/frozen; no further product work may land there. v1.0.35 is the only authoritative writable semantic branch.
- current: revision-safe Refinement, attachment and protected-region dependency hardening now fails closed across durable and presentation state: loaded Ready candidates reconcile stale input/invalid output lineage immediately and candidate reconciliation explicitly rejects missing target identity, stale Smart Selection weights are cleared when their durable binding is stale and stable live object identity cannot be proven, and attachment records persist stale status as soon as either bound mesh revision is no longer active
- completed_this_slice: Core attachments bind exact parent/child mesh revisions, become stale/non-authoritative when either revision advances, persist that stale state through transactions/save/reopen, reconcile legacy/current-marked stale bindings during session construction and ReplaceFromLoad, support explicit transactional rebind, reject stale updates, preserve exact state through undo/redo, and drive mapped Kitbash attachment actions through stable ObjectId/AttachmentRecord authority. Focused regression coverage now exercises revision invalidation symmetrically for both parent and child mesh advancement, proves stale updates fail closed in either direction, proves candidate-driven attachment invalidation restores current/stale semantics across undo/redo, and proves the exact stale binding survives save/reopen without silently transferring to the candidate output revision. The existing Kitbash UI remains unchanged; its legacy DTO is a presentation/compatibility projection for mapped objects rather than durable placement authority. Mapped stale attachments no longer continue driving placement from display-name DTO state. Unmigrated legacy objects retain the existing fallback until they acquire stable Core object identity. Smart Selection grow/shrink/smooth now reconciles the changed weight array into the existing durable revision-bound selection persistence path instead of leaving refined weights only in Godot memory. Stale protected-region presentation clears stale vertex weights even when the live Godot object can no longer be mapped to stable Core identity, and stale presentation binding/restore-failure pointers are retired so revision-invalid weights cannot remain visually or behaviorally authoritative. Ready Refinement candidates are conflicted as soon as their output revision is not descended from their bound input revision, loaded Ready candidates reconcile against the current active input revision plus output ancestry before the session exposes them, and reconciliation now explicitly conflicts a Ready candidate if its target object cannot be resolved instead of allowing later lineage checks to mask the missing dependency. Project-state validation already rejects missing-object candidate graphs, so this is a defense-in-depth fail-closed guard for mutation/reconciliation paths rather than a new persistence format. Focused regression coverage also proves that a reconciled candidate conflict and its diagnostic reason survive save/reopen and that rejected Apply on the reopened conflict cannot mutate the project. Focused regression coverage guards loaded candidate reconciliation, durable attachment stale reconciliation and protected-region fail-closed presentation. Exact-head Core and full build/package validation for the latest implementation checkpoint are green.
- load_reconciliation_contract: session construction and ReplaceFromLoad repairs that change stale candidate or attachment state advance the durable revision and preserve the pre-repair saved revision, so repaired sessions are dirty and saveable; unchanged valid loads remain clean. Exact-head Core and full build/package validation are green.
- refinement_candidate_persistence_contract: when one Ready Refinement candidate advances an object away from a shared input revision, competing sibling candidates retain their exact input/output dependency identity as durable Conflict records across save/reopen; a valid persisted sibling Conflict reopens clean without a spurious reconciliation repair or dirty state; rejected Apply after reopen remains fail-closed and cannot mutate the applied output state; explicit discard of a reopened sibling Conflict is transactional, preserves the already-applied output revision, clears the stale diagnostic, round-trips through undo/redo, and persists/reopens as clean Discarded state. Exact-head Core and full build/package validation are green.
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
  9. successful durable Smart Selection clear removes retired snapshot assets only after project save succeeds and only when no surviving binding references them; failed durable removal re-enables reconciliation so recovered Core state is not hidden behind locally cleared presentation
  10. failed Smart Selection save recovery re-checks the recovered Core selection graph before snapshot cleanup, so a rolled-back binding cannot leak an orphan asset while any still-referenced asset is preserved
- next:
  1. close the remaining revision-safe Refinement candidate dependency edge cases that can be exercised together: stale/missing target or input identity, output-lineage conflict, terminal apply/discard behavior, and save/reopen + undo/redo preservation
  2. close the related stale presentation migration paths for protected-region/selection/candidate state after Core revision authority changes or stable object identity can no longer be proven; stale state must clear/fail closed rather than remain visually authoritative
  3. tighten the mapped Kitbash/attachment migration seam only where stable Core identity already exists, proving legacy display-name/DTO state cannot regain durable authority after stale/rebind transitions; do not redesign the user-facing Kitbash UI
  4. batch focused validation across these related slices and continue within the same 40-minute Dev cycle when safe; do not stop after one tiny assertion or test if another ordered slice remains authorized
  5. once 2–4 related meaningful changes form a coherent green runtime-test batch, return v1.0.35 to Coordinator for release and roll remaining adjacent work to v1.0.36
  6. continue to stop before irreversible or user-owned Refinement/Kitbash UI decisions
- acceptance: no revision-dependent attachment, selection or refinement dependency can silently survive an incompatible mesh revision change; save/reopen and undo/redo preserve exact durable state; exact-head Core and full build/package validation remain green
- process_blocker: none; recurring patch-request formatting friction remained fail-closed and recovered within the bounded retry contract; AUTO-INC-013 carries the process evidence
- preemption: any reproduced v1.0.32 P0/P1 regression becomes the next implementation priority after Coordinator reconciliation
- stop: strategic/user authority is required; no independent authorized work remains; a real release freeze starts; or continuing would compound a severe regression/data-loss/safety risk
- continuation: Dev continues automatically through valid independent work, but release slices are intentionally bounded; once the current version has a coherent green runtime-test batch, defer adjacent unfinished work to the next semantic version rather than extending the current release envelope

## Deferred / user-owned design
- MS-041 Refinement/Kitbash: structural direction is accepted, but exact UI/interaction design remains user-approval work.
- MS-042 Rig & Pose and Cleanup & Export remain intentionally later work.

## Architecture constraints
- Core remains durable-state and transform authority.
- Godot remains presentation/input authority.
- Python remains inference/geometry authority.
- Preserve local-first/storage containment, immutable revisions/stable IDs, transactional state/history and stale-result rejection.
