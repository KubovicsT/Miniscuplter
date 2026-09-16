# Miniscuplter Current Execution State

## Release state
- stable: v1.0.36
- writable: v1.0.37
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.37
- execution_hold: none
- release_cadence_target: approximately two meaningful runtime-test releases per active development day when coherent/green batches exist; cadence target is not a timer-based publication rule
- v1.0.36: PUBLISHED from exact validated candidate after canonical ISSUE_STATE reconciliation; autonomous release rebuilt/tested/exported/hashed/smoke-installed and published installer + portable ZIP + checksum

## v1.0.36 reference-machine verification queue
The v1.0.35 reproduced regressions are code-fixed in v1.0.36 but remain NEEDS USER VERIFICATION until the packaged v1.0.36 reference-machine pass. Green CI is not runtime verification.
- MS-050/MS-051 provider install/resume: Hunyuan long-path/partial-worktree recovery and TripoSR torchmcubes host-torch/no-build-isolation path
- MS-049 Enhance Selected Region typed backend contract
- MS-043/MS-044 New-project isolation and disposed-selection retirement
- MS-035 workspace/client-area composition and telemetry/AI-command visibility
- MS-037/MS-038/MS-039/MS-045/MS-046 generated-surface rendering, Rotate pivot/rollback, immediate scale/ground projection, camera continuity and stable RMB orbit pivot
- MS-014/MS-047/MS-048 Quality/custom-preset controls, prompt persistence and Open Project
- MS-033/MS-040 plus attachment/selection switching remain previously unverified and should be included when practical

## Current objective
### I — Resume deferred attachment/history ownership convergence on v1.0.37
- state: READY FOR DEV
- outcome: continue the lower-priority architecture work that was deliberately deferred while v1.0.36 runtime regressions preempted it
- release_boundary: v1.0.36 is immutable. v1.0.37 is the sole writable semantic branch. Pending v1.0.36 user verification is local, not a global stop; any newly reproduced severe regression preempts this queue.

### Accepted execution slices
- Stage-D attachment undo/load reconstruction — ACCEPTED on v1.0.37 at `2d2a5b2`
  - schema 8 durably stores optional part-library identity; schema 7 remains loadable and upgrades on save without inventing missing identity
  - mapped attachment undo, redo and project load rebuild the same fail-closed Godot projection from authoritative Core records using stable object IDs
  - focused migration/history/source-contract tests, release audit, Core validation and full build/package/installer validation are green
- Attachment read-side/history reconciliation — COMPLETED on v1.0.37 through `e450451`
  - selection-time projection derives library identity from Core and fails closed for migrated schema-7 records without durable identity
  - mapped projections rejected by Core are retired from legacy export while genuinely unmapped compatibility behavior remains intact
- Smart Selection Core-history convergence — ACCEPTED on v1.0.37 through `d93eb2f`
  - selection bind/clear transactions route through Core undo/redo and reconcile Godot presentation afterward
  - immutable selection snapshots remain available while current, undo or redo state can restore their binding

### Ordered Dev queue
1. Stage-D attachment undo/load reconstruction — COMPLETED from architecture evidence in MSG-20260915-DEV-006
   - re-inspect the current Core attachment/revision model and transient Godot projection before mutation
   - choose the smallest migration-consistent path that keeps durable attachment identity/history in Core and presentation reconstruction in Godot; do not persist transient scene-node identity
   - make undo/redo and project-load reconstruction converge on the same stable attachment/revision authority
   - preserve migration-before-legacy-removal and stale-result rejection; add focused persistence/history/reconstruction tests
2. Attachment read-side/history reconciliation — COMPLETED
   - finish mapped attachment read-side convergence so reload, undo/redo and current-revision selection resolve the same durable attachment identities
   - remove or quarantine duplicate legacy authority only after migrated paths and tests prove parity
   - validate save/reopen plus history traversal without presentation-only state becoming canonical
3. Smart Selection / protected-region lifecycle convergence — IN PROGRESS
   - continue the previously deferred selection/protected-region ownership work on top of stable attachment identity
   - ensure semantic selection/protected-region state survives only where product semantics require it and stale bindings fail closed across project/history transitions
   - keep Godot input/presentation separate from Core durable selection/history authority
4. Checkpoint integration and runtime-test boundary
   - reconcile canonical docs after each accepted architecture slice, keep exact-head Core/full build/package validation green, and return one coherent v1.0.37 runtime-test checkpoint rather than isolated micro-releases
   - if new v1.0.36 packaged evidence arrives, record it in ISSUE_STATE and preempt only the affected critical path; independent safe architecture work continues when possible

### Dev continuation boundary
- queue is intentionally deep enough for multiple productive 40-minute cycles; slice count is descriptive, not a stopping quota
- Dev should reassess remaining budget after each accepted slice and continue the highest-value safe authorized work until finalization or a real stop boundary
- normal pending CI is not by itself an early-stop boundary when remaining budget can be used safely
- a conclusively recovered write/process incident is not by itself an early-stop boundary when known-good state is restored and safe authorized work remains
- do not reopen already-landed v1.0.36 fixes speculatively without new runtime evidence

### Acceptance / preemption
- v1.0.37 VERSION identity must remain synchronized and exact-head validation must remain green after mutations
- any failed v1.0.36 reference-machine retest becomes release-relevant ISSUE_STATE evidence and may preempt the matching v1.0.37 slice
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
