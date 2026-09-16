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
### I — Continue post-attachment/history convergence on v1.0.38
- state: IN PROGRESS — GREEN DEV CHECKPOINT
- release_boundary: v1.0.37 is immutable. v1.0.38 is the sole writable semantic branch. Pending user verification is local, not a global stop.

### Latest v1.0.38 Dev checkpoint
- repaired the forward-branch release identity across every audited runtime/package surface and the canonical backend lifecycle test
- extended the semantic-version guard to cover the lifecycle test's expected version so this bootstrap drift fails before backend startup
- revision-bound selection creation now rejects duplicate stable IDs both before and inside the Core transaction; focused coverage proves rejection cannot overwrite durable state or add history
- exact-head Core and full build/package/installer validation is green; no release/publication action was taken

### Accepted v1.0.37 checkpoint
- Stage-D attachment undo/load reconstruction — accepted and published
- attachment read-side/history reconciliation — accepted and published
- Smart Selection / protected-region lifecycle convergence — accepted and published
- exact candidate passed Core plus full build/package/installer and autonomous release gates

### Ordered Dev queue
1. Continue inspection from the immutable selection-identity checkpoint for remaining migration/legacy ownership seams around attachment/history/selection before mutation.
   - identify any still-duplicated legacy authority that can now be safely removed or quarantined because the migrated Core-owned paths are proven
   - preserve migration-before-legacy-removal and fail-closed stale-result behavior
2. Complete the next smallest coherent durable-state/history convergence slice exposed by that inspection.
   - Core remains durable-state authority; Godot remains presentation/input authority
   - add focused migration/history/source-contract coverage and keep exact-head Core validation green
3. Continue independent architecture cleanup that directly reduces duplicate authority or closes a known migration seam; do not invent speculative feature scope.
   - batch related safe work so the queue supports productive 40-minute Dev cycles
   - pending reference-machine verification is not a global stop
4. Return a coherent runtime-test/release checkpoint when the batch is meaningful and exact-head Core/full build-package validation is green.
   - Coordinator owns release readiness/chunking/publication; Dev never publishes

### Dev continuation boundary
- queue is intentionally outcome-driven; slice count is descriptive, not a stopping quota
- after each meaningful transition reassess remaining budget and continue the highest-value authorized safe work until finalization or a real stop boundary
- normal pending CI is a wait state when likely to resolve within remaining budget; use wait time for safe inspection/documentation/independent reversible work
- a conclusively recovered process/write incident is not automatically run-ending when known-good state and a safer continuation path are established
- if inspection finds no evidence-backed migration/cleanup work, stop rather than manufacture work and return the evidence to Coordinator

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
