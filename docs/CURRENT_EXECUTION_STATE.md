# Miniscuplter Current Execution State

## Release state
- stable: v1.0.35
- writable: v1.0.36
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.36
- execution_hold: none
- release_cadence_target: approximately two meaningful runtime-test releases per active development day when coherent/green batches exist; cadence target is not a timer-based publication rule
- v1.0.36_release_boundary: REJECTED FOR NOW due reproduced v1.0.35 reference-machine regressions that preempt lower-priority ownership-convergence release work

## Latest reference-machine evidence — v1.0.35
- direct successful 3D insertion is verified: generation succeeds and auto-inserts without Apply
- direct Move is verified working
- Rotate rings are visible and draggable, but rotation pivot/rollback behavior is incorrect
- generated object eventually scales/grounds, but first appears tiny and changes size/placement after a noticeable delay
- generated mesh still renders backside/shell-like surfaces
- New clears the 3D model but leaves the previous project's 2D image visible and can surface a Stage-D failure referencing a disposed Godot.MeshInstance3D
- black exterior gutters remain; on narrow resize they move from left/right to top/bottom while the main layout itself stays relatively stable
- telemetry still overlaps the viewport; the AI command area is not visible in either 2D or 3D
- 3D camera view resets across tab switches
- RMB orbit snaps toward origin / uses a pivot that appears to move with the camera rather than a stable selected-object pivot
- Settings > Quality does not expose preset parameters or visible custom-preset creation despite historical MS-014 implementation claims
- the last concept/image-edit prompt text is lost on application reopen
- no user-visible project Load/Open entry point is available, so explicit saved-project reload testing cannot be performed from the UI
- Smart Select, attachment-control switching, cancellation/retry and several save/reopen cases remain untested in this v1.0.35 pass

## Current objective
### I — v1.0.36 reference-machine regression recovery
- state: ACTIVE / PREEMPTING PRIOR RELEASE REVIEW
- outcome: turn the user's v1.0.35 runtime findings into one coherent corrective release before resuming lower-priority Stage-D attachment-history architecture work
- release_boundary: v1.0.35 remains immutable. v1.0.36 remains the only writable semantic branch. The already-green attachment/Smart-Selection ownership fixes remain in v1.0.36 but are not sufficient for publication while reproduced user-facing regressions remain.

### Ordered Dev queue
1. Project lifecycle isolation — P0/P1
   - reproduce New after a generated/edited project
   - make New establish a genuinely fresh project: previous 2D image/baseline, 3D presentation, selection/tool bindings and project-scoped UI state must not survive unless explicitly intended
   - diagnose/fix the disposed Godot.MeshInstance3D Stage-D commit path so stale scene-node references cannot participate after project reset
   - add focused project-reset regression coverage; preserve Core transactional safety
2. Workspace composition regressions — P1
   - reopen MS-035/MS-026/MS-027 from v1.0.35 evidence
   - eliminate exterior black gutters at supported sizes, including the narrow-window top/bottom variant
   - place telemetry in its dedicated non-overlay bottom-left workspace and restore the dedicated AI command area in both 2D and 3D
   - validate resize/maximize/restore and tab switching without viewport or bottom-workspace loss
3. 3D render / transform / navigation correctness — P1
   - reopen MS-037 and the Rotate portion of MS-038; treat MS-039 as partial only
   - fix generated-mesh exterior/backface correctness on the packaged reference path
   - make Rotate use the correct stable object/pivot transform and prevent unexplained rollback to a previous rotation after interaction failure
   - preserve 3D camera zoom/orientation across 2D↔3D tab switches and make RMB orbit use a stable selected-object/world pivot rather than snapping to origin or drifting with camera translation
   - remove the visible tiny-model -> delayed scale/ground sequencing; final Core-owned initial transform should present coherently when the object becomes visible
4. Project/settings/prompt UX continuity — P1/P2
   - reactivate MS-014: expose what each quality preset changes and restore/create visible custom preset controls
   - provide a user-visible project Open/Load entry point consistent with the charter's save/reload workflow, or reconcile the UI to an explicit project-browser/recovery model
   - preserve last concept/image-edit prompt text across ordinary app reopen when the project/session is restored
5. Close the corrective checkpoint
   - preserve the already-landed v1.0.36 mapped attachment and orphaned Smart Selection fail-closed fixes
   - keep Stage-D attachment undo/load schema work deferred until the reproduced runtime regressions above are contained
   - run focused regressions plus final exact-head Core and full build/package/installer validation
   - return one coherent v1.0.36 runtime-test checkpoint to Coordinator; do not publish autonomously

### Acceptance / preemption
- P0/P1 user-observed regressions outrank the prior attachment-history architecture queue
- New must not leak prior project visual/data state and must not touch disposed scene objects
- workspace has no exterior gutters, telemetry is non-overlay, and AI command input is visible
- generated exterior surfaces render correctly; transform/navigation behavior is stable and pivot-correct
- generation can present its final scale/ground transform without a conspicuous delayed second reposition
- project reload/settings/prompt entry points are testable from the UI
- any new severe data-loss/runtime regression preempts remaining work
- pending untested items (MS-033/MS-040 and v1.0.35 attachment/selection switching) remain local verification dependencies, not a global stop

## Architecture decision deferred
- MSG-20260915-DEV-006 Stage-D attachment undo/load reconstruction remains valid evidence.
- Coordinator disposition: DEFER the durable-metadata-vs-transient-projection choice until the current reference-machine P0/P1 corrective batch is stabilized. Do not add new durable attachment schema merely to unblock invisible follow-on work while basic runtime acceptance is failing.

## Deferred / user-owned design
- MS-041 Refinement/Kitbash exact UI/interaction design remains user-approval work.
- MS-042 Rig & Pose and Cleanup & Export remain intentionally later work.

## Architecture constraints
- Core remains durable-state and transform authority.
- Godot remains presentation/input authority.
- Python remains inference/geometry authority.
- Preserve local-first/storage containment, immutable revisions/stable IDs, transactional state/history and stale-result rejection.
