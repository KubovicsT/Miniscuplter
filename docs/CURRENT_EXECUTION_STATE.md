# Miniscuplter Current Execution State

## Release state
- stable: v1.0.35
- writable: v1.0.36
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.36
- execution_hold: none
- release_cadence_target: approximately two meaningful runtime-test releases per active development day when coherent/green batches exist; cadence target is not a timer-based publication rule
- v1.0.36_release_boundary: COORDINATOR REVIEW — implementation checkpoint is exact-head green and the evidence-backed corrective mutation queue is exhausted; publication still requires canonical issue reconciliation and final release-candidate verification, while user-visible fixes remain NEEDS USER VERIFICATION rather than release-verified

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
### I — v1.0.36 corrective checkpoint closure
- state: COORDINATOR REVIEW / CANONICAL RECONCILIATION
- outcome: convert the reproduced v1.0.35 runtime findings into one coherent runtime-test release without reopening speculative implementation work
- release_boundary: v1.0.35 remains immutable. v1.0.36 is the only writable semantic branch. Current exact implementation head is green across Core, .NET/C#, Python/runtime/geometry/release-audit, portable package and installer. User-visible corrections remain `FIXED IN v1.0.36 - NEEDS USER VERIFICATION` until packaged reference-machine retest.

### Corrective implementation status
1. Provider install/resume — MS-050/MS-051
   - code-complete/green: Hunyuan checkout uses contained long-path handling plus partial-worktree verification/recovery; TripoSR verifies host torch and builds torchmcubes with contained no-build-isolation; deterministic partial stages remain reusable
   - packaged reference-machine install/resume verification remains required
2. 2D detail endpoint contract — MS-049
   - code-complete/green with typed 2D/3D contracts while preserving canonical `app:app`; packaged Enhance Selected Region retest remains required
3. Project lifecycle isolation — MS-043/MS-044
   - code-complete/green: New retires prior project-scoped 2D/baseline/candidate presentation, establishes a fresh durable Stage-C ProjectState/session, and disposed selection is cleared before consumers
   - packaged New-project isolation retest remains required
4. Workspace composition — MS-035 and telemetry/AI-command composition
   - code-complete/green: top-level workspace owns the full client rect; docked Resource Telemetry layout authority no longer overlays the viewport; focused composition guards are present
   - resize/maximize/restore and AI-command visibility require packaged reference-machine verification
5. 3D render / transform / navigation — MS-037/MS-038/MS-039/MS-045/MS-046
   - code-complete/green evidence includes camera tab continuity, stable selected-object RMB orbit pivot, generated presentation-normal repair, immediate generated-transform projection, direct Rotate drag, object-origin Rotate gizmo pivot, and fail-closed prevention of overlapping durable transform gestures that could snap back to stale rotation
   - packaged reference-machine verification remains required for shell/backside appearance, Rotate pivot/rollback, and tiny-model/delayed scale-ground behavior; if Rotate still reproduces, inspect the remaining runtime/Euler path from this verified head rather than adding speculative fixes now
6. Project/settings/prompt UX continuity — MS-014/MS-047/MS-048
   - code-complete/green evidence includes visible Quality preset parameters/custom-preset path, persisted project prompt continuity, and Open Project promoted beside New
   - packaged user verification remains required

### Dev continuation boundary
- no evidence-backed corrective mutation remains authorized solely from the existing v1.0.35 observations; the latest Dev run intentionally stopped with `authorized_work_remaining_at_intent: NO` after exact-head green validation and Coordinator handoff
- Dev must not repeat the landed corrective batch or invent speculative fixes merely to consume run time
- next Dev implementation is triggered by a concrete Coordinator acceptance gap, failed release-candidate validation, or new packaged/reference-machine evidence
- Stage-D attachment undo/load reconstruction remains deferred until this corrective checkpoint is released/verified enough to resume lower-priority architecture work

### Release acceptance / preemption
- exact implementation head `150051c7310f2ac8fe685ba68ad04c99f9e7983e` is the latest reconciled corrective checkpoint before the subsequent documentation-only execution-state reconciliation
- exact-head Core, .NET/C#, Python/runtime/geometry/release-audit, portable package and installer validation are green
- all user-observed runtime/UI fixes remain `FIXED IN v1.0.36 - NEEDS USER VERIFICATION`; green CI does not convert them to reference-machine VERIFIED
- pending untested MS-033/MS-040 and attachment/selection switching remain local verification dependencies, not global implementation blockers
- Coordinator owns final ISSUE_STATE reconciliation and release-candidate decision/publication; Dev never publishes

## Architecture decision deferred
- MSG-20260915-DEV-006 Stage-D attachment undo/load reconstruction remains valid evidence.
- Coordinator disposition: DEFER the durable-metadata-vs-transient-projection choice until the current reference-machine corrective checkpoint is published/verified enough to resume lower-priority Stage-D work.

## Deferred / user-owned design
- MS-041 Refinement/Kitbash exact UI/interaction design remains user-approval work.
- MS-042 Rig & Pose and Cleanup & Export remain intentionally later work.

## Architecture constraints
- Core remains durable-state and transform authority.
- Godot remains presentation/input authority.
- Python remains inference/geometry authority.
- Preserve local-first/storage containment, immutable revisions/stable IDs, transactional state/history and stale-result rejection.
