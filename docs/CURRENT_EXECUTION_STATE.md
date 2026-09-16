# Miniscuplter Current Execution State

## Release state
- stable: v1.0.37
- writable: v1.0.38
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.38
- execution_hold: none
- release_cadence_target: approximately two meaningful runtime-test releases per active development day when coherent/green batches exist; cadence target is not a timer-based publication rule
- v1.0.37: PUBLISHED from the exact validated candidate after autonomous release rebuilt/tested/exported/hashed/smoke-installed and published installer + portable ZIP + checksum

## Reference-machine verification queue
The recent runtime/UI/provider fixes remain NEEDS USER VERIFICATION until packaged reference-machine testing. Green CI is not runtime verification. New reproduced severe regressions preempt only the affected path; independent safe work continues.

## Current objective
### I — Continue Stage-D durable-state/history convergence on v1.0.38
- state: READY FOR COORDINATOR CHECKPOINT REVIEW — IMPLEMENTATION COMPLETE; CODE CHECKPOINT GREEN
- release_boundary: v1.0.37 is immutable. v1.0.38 is the sole writable semantic branch. Pending user verification is local, not a global stop.

### Accepted v1.0.38 attachment checkpoint
- mapped snap saves through Core and reconstructs presentation from persisted Core state; caller legacy DTO state is no longer reapplied
- mapped detach revalidates project, exact attachment and child identity under the Core gate; current/stale exact records are removed transactionally and rebuilt from Core without legacy fallback
- save/reopen and undo/redo coverage proves stale attachment removal does not resurrect/promote stale records
- the dead legacy attachment projection writer is removed; remaining `_v07Attachments` use is read-side presentation or supported legacy save/export compatibility only
- exact-head Core and full build/package/installer validation is green at the returned checkpoint; no release/publication action was taken

### Latest v1.0.38 editing/history checkpoint
- Stage-C edit undo/redo now routes from the authoritative top Core transaction and its affected stable object IDs, independent of current viewport selection; asynchronous history changes are exact-transaction guarded and every live affected presentation is rebuilt from restored Core state
- revision-bound V1027 sculpt authority is now the sole mapped durable sculpt commit owner; the duplicate V1020 mapped capture/commit path and its handler-order suppression state are retired while legacy presentation-history cleanup remains bounded
- Smart Selection persistence transactionally replaces all current same-object/same-kind bindings with one new immutable binding; undo/redo retains and restores the prior/new bindings and their history-reachable assets
- focused local Core/source-contract checks and release audit pass; exact code-head Core and full build/package/installer validation is green

### Ordered Dev queue
1. **COMPLETE at `b3398fd` — Stage-C edit history routing must not depend on whichever object is currently selected.**
   - subsystem/path: `Scripts/Main.V1020StageCEditing.cs`, especially `V1020UndoStageCAware`, `V1020RedoStageCAware`, `V1020UndoRedoStageCAsync`, plus focused Core/source-contract tests.
   - evidence/authorization: attachment and Smart Selection transactions are routed from the top Core transaction independent of viewport selection, but ordinary Stage-C editing history falls back to legacy `Undo()`/`Redo()` unless the currently selected mapped object is also affected. A transform/sculpt transaction for object A can therefore become unreachable through Core history after selection is cleared or moved to object B.
   - implementation direction: route a top `StageCEditing` transaction from its authoritative affected stable object IDs rather than current presentation selection. Apply Core undo/redo transactionally, save, and rebuild every still-live affected mapped presentation from restored Core state. Preserve legacy undo only when the top durable transaction is not a recognized Core transaction.
   - acceptance: focused coverage proves transform/sculpt Core undo/redo still works after selection clear/switch, wrong current selection cannot divert the operation into legacy history, save/reopen remains coherent, and exact-head Core/full validation is green.
   - continuation: after this lands, inspect whether candidate or other recognized Core transaction types have the same selection-dependent routing gap; only extend where concrete evidence exists.

2. **COMPLETE at `374ed18` — Retire the duplicated mapped-sculpt commit authority that currently relies on event-handler ordering to suppress a second commit.**
   - subsystem/path: `Scripts/Main.V1020StageCEditing.cs` mapped sculpt fields/capture/release/`V1020CommitSculptStrokeAsync` versus `Scripts/Main.V1027SelectionAuthority.cs` revision-bound sculpt observer/commit path; `Core.Tests/StageDSculptAuthorityTests.cs`.
   - evidence/authorization: the viewport currently installs both `V1027ObserveRevisionBoundSculpt` and `V1020ObserveViewportEditingCommit`. Both capture mapped sculpt state on press; correctness on release depends on the V1027 handler setting `_v1020SculptGestureActive = false` before the older V1020 observer can commit. That is duplicate mutation authority with ordering-dependent suppression, contrary to migration-before-legacy-removal once the replacement path is proven.
   - implementation direction: make the revision-bound V1027 path the sole mapped sculpt durable commit owner. Remove/quarantine the obsolete mapped V1020 sculpt capture/commit state without disturbing transform observation or any genuinely unmapped legacy presentation fallback.
   - acceptance: one mapped stroke can create at most one immutable Core mesh revision/history transaction regardless of handler ordering; stale selection still fails closed and restores Core presentation; focused regression/source-contract coverage plus exact-head Core/full validation green.
   - continuation: once duplicate mapped sculpt authority is gone, inspect only adjacent mapped editing mutation paths for the same dual-owner pattern.

3. **COMPLETE at `308d938` — Enforce one current durable Smart Selection binding per object/kind instead of accumulating competing current bindings.**
   - subsystem/path: `Scripts/Main.V1027ProtectedRegionAuthority.cs`, `Core/StageCSelection.cs`, `Core.Tests/ProtectedRegionPresentationSafetyTests.cs` and selection persistence/history tests.
   - evidence/authorization: each changed Smart Selection snapshot calls `BindRevisionSelection` with a new ID, while the previous current `smart-select-vertex-weights` binding is not transactionally replaced/retired first. Reconciliation later chooses the newest current binding, meaning multiple same-object/same-kind current bindings can coexist as competing durable authority and retain assets unnecessarily.
   - implementation direction: add/use an exact transactional replacement semantic for the current object/kind binding so a successful new snapshot leaves one current binding while undo/redo can restore the prior binding and its immutable asset. Asset cleanup must continue respecting current + undo/redo history references; failed save/persistence must restore/reconcile prior Core authority and never delete a history-reachable snapshot.
   - acceptance: repeated Smart Selection changes leave exactly one current same-object/same-kind binding; undo/redo restores the correct prior/new binding and presentation; save/reopen selects the authoritative binding without newest-wins ambiguity; failed persistence remains fail-closed; focused tests and exact-head Core/full validation green.
   - continuation: after this lands, inspect protected-region consumers for assumptions that multiple current same-kind bindings are valid and remove only proven obsolete compatibility behavior.

4. **Bounded convergence inspection / checkpoint return.**
   - after objectives 1–3, inspect the adjacent Stage-D history/editing/selection bridges for another concrete duplicate-authority or stale-history seam. This is ONE discovery objective, not synthetic queue depth.
   - if a concrete seam is evidenced, implement the smallest coherent migration/history slice and record it. If not, return factual evidence and the green checkpoint rather than manufacturing cleanup.
   - return a coherent runtime-test/release checkpoint when the implemented batch is canonically reconciled and exact-head Core/full build-package validation is green. Coordinator owns release readiness/chunking/publication; Dev never publishes.
   - bounded result: generated-candidate apply/discard/conflict transactions are durable Core history but remain unrecognized by the shared undo/redo router; a coherent follow-up must reconcile candidate metadata, created/removed scene objects and candidate controls from the restored Core state. This is concrete adjacent evidence for Coordinator planning, but it is broader than a safe tail slice for this checkpoint.

### Queue-depth note
- Coordinator bounded source inspection after the attachment checkpoint identified three concrete independently executable migration/history objectives above; queue starvation from the 17:00 Dev cycle is therefore resolved by evidence-backed work rather than generic discovery wording.
- objectives are ordered by durable-history correctness and duplicate-authority risk, not by a slice quota. Dev should continue within remaining budget after each meaningful transition unless a real stop boundary exists.

### Dev continuation boundary
- after each meaningful transition reassess remaining budget and continue the highest-value authorized safe work until finalization or a real stop boundary
- normal pending CI is a wait state when likely to resolve within remaining budget; use wait time for safe inspection/documentation/independent reversible work
- a conclusively recovered process/write incident is not automatically run-ending when known-good state and a safer continuation path are established
- if bounded inspection finds no evidence-backed migration/cleanup work, stop rather than manufacture work and return the evidence to Coordinator

### Acceptance / preemption
- v1.0.38 VERSION identity must remain synchronized and exact-head validation must remain green after mutations
- any failed packaged reference-machine retest becomes release-relevant ISSUE_STATE evidence and may preempt the matching v1.0.38 slice
- user-verification-pending items do not block independent safe work unless they expose severe regression/data-loss/safety risk or invalidate the architecture being changed
- Coordinator owns release readiness/chunking/publication; Dev never publishes

## Deferred / user-owned design
- MS-041 Refinement/Kitbash exact UI/interaction design remains user-approval work.
- MS-042 Rig & Pose and Cleanup & Export remain intentionally later work.

## Architecture constraints
- Core remains durable-state and transform authority.
- Godot remains presentation/input authority.
- Python remains inference/geometry authority.
- Preserve local-first/storage containment, immutable published releases/branches, stable IDs/immutable revisions, transactional state/history, migration-before-legacy-removal and stale-result rejection.
