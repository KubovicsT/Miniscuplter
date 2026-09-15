# Miniscuplter Current Execution State

## Release state
- stable: v1.0.35
- writable: v1.0.36
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.36; exact bootstrap .NET/Python/package validation green
- execution_hold: none
- release_cadence_target: approximately two meaningful runtime-test releases per active development day when coherent/green batches exist; cadence target is not a timer-based publication rule
- current_release_slice: v1.0.36 is a fresh small-batch envelope; target one coherent UI-neutral ownership-convergence story after roughly 2–4 related meaningful changes or about 3–6 hours of active development, then return it to Coordinator instead of extending the version with adjacent cleanup

## Published checkpoint
### v1.0.35
- state: PUBLISHED / IMMUTABLE
- candidate: e8a76893c03e002efe45c354a228f931cdc9fd1c
- publication: autonomous release-control succeeded; tag and latest GitHub Release resolve to the exact candidate; installer, ZIP and SHA-256 assets are present
- runtime-test value: revision-safe Refinement/attachment dependency hardening plus visible fail-closed presentation behavior: Smart Selection clears prior-object weights on stable mapped-object switches, and mapped attachment fine-tune controls clear when selection/projection authority disappears
- source rule: do not mutate v1.0.35

## Pending reference-machine verification
### F — verify the latest released v1.0.35 checkpoint on the Windows / GTX 1080 reference machine
- state: WAITING FOR USER REFERENCE-MACHINE EVIDENCE; NON-BLOCKING FOR INDEPENDENT WORK
- verify prior carried-forward runtime behaviors:
  1. MS-033 cancel an active 3D generation, then immediately generate again without restarting the app
  2. MS-036 successful 3D generation inserts directly without a redundant Apply step
  3. MS-037 generated exterior surfaces render correctly and neutral-gray shading makes surface detail readable
  4. MS-039 generated model rests on the grid at a sensible initial scale
  5. MS-040 Smart Select resolves and performs semantic selection without Not Found errors
  6. MS-035 ordinary window resize produces no black gutters and preserves the fixed side rails
  7. MS-026/MS-027 telemetry and AI command areas occupy their dedicated non-overlay bottom workspace
  8. MS-038 direct Move dragging works and Rotate exposes usable visible axis rings while precise axis controls remain functional
- verify v1.0.35-specific behavior when practical:
  9. switching Smart Selection between Core-mapped objects does not leave the previous object's weights visually active
  10. clearing selection or selecting a Core-mapped object without a matching legacy attachment projection clears mapped attachment fine-tune controls instead of retaining the prior object's values
- acceptance: failures are promoted/reopened in ISSUE_STATE and preempt lower-priority work as appropriate
- continuation: user availability is not a global development stop

## Current objective
### H — v1.0.36 finish the next UI-neutral attachment/selection authority seam
- state: ACTIVE
- outcome: remove remaining evidence-backed cases where legacy Godot presentation state can appear authoritative after Core attachment/selection identity or revision authority changes, without committing the unresolved Refinement/Kitbash UI design
- release_boundary: v1.0.35 is published/frozen. v1.0.36 is the only authoritative writable semantic branch.

### Ordered Dev queue
1. **Mapped Kitbash read-side authority audit/fix**
   - inspect remaining attachment presentation/read paths used for Core-mapped objects: attachment status, parent/child identity, socket/fine-tune projection and selection-driven refresh
   - where a concrete authority leak exists, make stable Core ObjectId + AttachmentRecord state authoritative and fail closed when the matching durable record/projection is absent or stale
   - retain display-name/legacy DTO fallback only for objects not yet represented by stable Core identity; do not redesign the user-facing Kitbash controls
2. **Attachment transaction/history/load reconciliation**
   - verify mapped presentation after snap/detach/fine-tune/rebind plus undo/redo and save/reopen
   - fix evidence-backed cases where stale/absent Core attachment state can resurrect prior controls/placement or where current durable state is not re-projected after history/load transitions
   - preserve explicit stale/rebind semantics; no automatic revision transfer without compatibility evidence
3. **Smart Selection / protected-region presentation lifecycle**
   - inspect object replacement/removal, candidate-driven revision advancement, undo/redo and load reconciliation for remaining local-weight/binding presentation that can outlive stable ObjectId + mesh-revision authority
   - fix only concrete fail-closed defects; keep existing selection UX and Core durable binding model
4. **Close the coherent v1.0.36 checkpoint**
   - add focused regression coverage for every changed authority seam, including save/reopen or history coverage where the defect crosses persistence/history
   - preserve exact-head attribution while CI is pending; normal CI/package waits are not early-stop boundaries when likely to finish inside the Dev budget
   - once roughly 2–4 meaningful related changes form a runtime-testable batch, run final exact-head Core + full build/package validation and return to Coordinator; roll adjacent cleanup to v1.0.37

### Acceptance
- for Core-mapped objects, legacy display-name/DTO state never overrides or visually outlives current durable attachment/selection authority
- revision-invalid selection/protected-region or attachment presentation fails closed instead of silently following a newer mesh revision
- mapped attachment presentation reconciles correctly after relevant Core transactions, undo/redo and save/reopen
- no user-owned Refinement/Kitbash UI decision is introduced
- final coherent checkpoint has exact-head Core and full build/package gates green

### Dependencies / preemption / continuation
- dependency: v1.0.36 bootstrap is validated and writable; no release request exists
- preemption: any reproduced P0/P1 regression from the latest user-tested release becomes the next implementation priority after Coordinator reconciliation
- CI continuation: after focused/Core validation is green, Dev may overlap a pending full build/package with another closely related independent reversible slice when exact-head attribution stays clear and the next slice does not depend on the pending result; reconcile any failure before dependent work or release return
- stop: strategic/user authority is required; no evidence-backed safe work remains; a real release freeze/no-race conflict exists; or continuing would compound a severe regression/data-loss/safety risk

## Deferred / user-owned design
- MS-041 Refinement/Kitbash: structural direction is accepted, but exact UI/interaction design remains user-approval work.
- MS-042 Rig & Pose and Cleanup & Export remain intentionally later work.

## Architecture constraints
- Core remains durable-state and transform authority.
- Godot remains presentation/input authority.
- Python remains inference/geometry authority.
- Preserve local-first/storage containment, immutable revisions/stable IDs, transactional state/history and stale-result rejection.
