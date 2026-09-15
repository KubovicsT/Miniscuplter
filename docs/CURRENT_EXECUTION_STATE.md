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
- Hunyuan3D 2.1 Shape install fails on the reference Windows machine during the companion Git checkout because upstream mini-trainset paths exceed Windows filename/path handling; clone reaches 100% but checkout fails, and the deterministic partial stage is retained for resume
- TripoSR install also fails on the reference Windows machine before model download completes: torchmcubes metadata generation runs in pip's isolated build environment, cannot see the already-installed host PyTorch, and aborts with the upstream instruction to build without isolation; the deterministic TripoSR partial stage is retained for resume

## Current objective
### I — v1.0.36 reference-machine regression recovery
- state: ACTIVE / PREEMPTING PRIOR RELEASE REVIEW
- outcome: turn the user's v1.0.35 runtime findings into one coherent corrective release before resuming lower-priority Stage-D attachment-history architecture work
- release_boundary: v1.0.35 remains immutable. v1.0.36 remains the only writable semantic branch. The already-green attachment/Smart-Selection ownership fixes remain in v1.0.36 but are not sufficient for publication while reproduced user-facing regressions remain.

### Ordered Dev queue
1. Windows 3D provider install/resume blockers — P1 reference-machine blockers
   - v1.0.36 implementation checkpoint is code-complete/green for MS-050/MS-051: Hunyuan checkout uses per-invocation long-path configuration plus partial-worktree verification/recovery, and TripoSR builds torchmcubes after host torch verification with contained no-build-isolation; deterministic partial stages remain reusable
   - reference-machine packaged install/resume remains `FIXED IN v1.0.36 - NEEDS USER VERIFICATION`
2. 2D detail endpoint contract — P1 deterministic runtime regression
   - implementation checkpoint landed in v1.0.36: a bounded app_v1036 migration retires only the legacy /detail-2d and /detail-3d routes, installs typed request contracts aligned with AIClient/detail_pipeline, and serve.py imports the migration before serving canonical app:app
   - focused typed-contract coverage is present and exact-head core-foundation is green; treat MS-049 as FIXED IN v1.0.36 - NEEDS USER VERIFICATION, not release-verified
   - reference-machine acceptance still requires packaged Enhance Selected Region to complete without the released v1.0.35 arity/request mismatch and without provider/path-validation regression
3. Project lifecycle isolation — P0/P1
   - disposed-selection sub-slice is FIXED IN v1.0.36 - NEEDS USER VERIFICATION: per-frame stable-selection reconciliation clears invalid/disposed selection before status/gizmo consumers
   - fresh-project sub-slice is now code-complete/green: New clears project-scoped 2D/baseline/candidate presentation and replaces the Stage-C working project with a fresh Core ProjectState/session before new baseline work can inherit prior revisions
   - reference-machine acceptance still requires New after a generated/edited project to show no prior 2D image/baseline/candidate and no disposed MeshInstance3D error
4. Workspace composition regressions — P1
   - exterior-gutter root cause fixed in v1.0.36 code: the programmatic top-level VBox had FullRect anchors without a Control parent while also receiving manual size; it now uses explicit TopLeft anchors and synchronizes its full client rect from the root viewport on resize, with a focused contract guard
   - telemetry/AI-command composition remains next; reference-machine resize/maximize/restore acceptance remains required
5. 3D render / transform / navigation correctness — P1
   - camera tab continuity and RMB selected-object pivot fixes are already landed/code-green in v1.0.36; reference-machine verification remains required
   - remaining: generated-mesh exterior/backface correctness, Rotate stable pivot/rollback, and tiny-model -> delayed scale/ground sequencing
6. Project/settings/prompt UX continuity — P1/P2
   - Open Project is now promoted beside New in the top toolbar; packaged user verification remains required
   - remaining: MS-014 quality preset/custom controls and last concept/image-edit prompt continuity across ordinary reopen
7. Close the corrective checkpoint
   - preserve already-landed v1.0.36 mapped attachment/orphaned Smart Selection fixes plus the current runtime-recovery fixes
   - keep Stage-D attachment undo/load schema work deferred until reproduced runtime regressions are contained
   - run focused regressions plus final exact-head Core and full build/package/installer validation
   - return one coherent v1.0.36 runtime-test checkpoint to Coordinator; do not publish autonomously

### Acceptance / preemption
- current exact writable head includes provider install/resume hardening, typed 2D/3D detail contracts, disposed-selection safety, durable New-project isolation, camera/orbit continuity, visible Open Project access, and explicit full-client workspace root ownership
- latest exact-head validation for the implementation checkpoint is green across core-foundation, C#/.NET, Python/runtime/geometry/release-audit, portable package and installer
- all user-observed runtime/UI fixes remain `FIXED IN v1.0.36 - NEEDS USER VERIFICATION` until packaged reference-machine retest
- Manager owns the prior malformed-write process-incident follow-up; it does not globally freeze product work because source recovery was conclusive
- P0/P1 user-observed regressions outrank the prior attachment-history architecture queue
- pending untested items (MS-033/MS-040 and attachment/selection switching) remain local verification dependencies, not a global stop

## Architecture decision deferred
- MSG-20260915-DEV-006 Stage-D attachment undo/load reconstruction remains valid evidence.
- Coordinator disposition: DEFER the durable-metadata-vs-transient-projection choice until the current reference-machine P0/P1 corrective batch is stabilized.

## Deferred / user-owned design
- MS-041 Refinement/Kitbash exact UI/interaction design remains user-approval work.
- MS-042 Rig & Pose and Cleanup & Export remain intentionally later work.

## Architecture constraints
- Core remains durable-state and transform authority.
- Godot remains presentation/input authority.
- Python remains inference/geometry authority.
- Preserve local-first/storage containment, immutable revisions/stable IDs, transactional state/history and stale-result rejection.
