# Miniscuplter Current Execution State

## Release state
- stable: v1.0.35
- writable: v1.0.36
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.36
- execution_hold: Coordinator release-boundary review for the bounded v1.0.36 ownership-convergence batch
- release_cadence_target: approximately two meaningful runtime-test releases per active development day when coherent/green batches exist; cadence target is not a timer-based publication rule

## Published checkpoint
### v1.0.35
- state: PUBLISHED / IMMUTABLE
- candidate: e8a76893c03e002efe45c354a228f931cdc9fd1c
- publication: autonomous release-control succeeded; tag and latest GitHub Release resolve to the exact candidate; installer, ZIP and SHA-256 assets are present
- source rule: do not mutate v1.0.35

## Pending reference-machine verification
### F — verify the latest released v1.0.35 checkpoint on the Windows / GTX 1080 reference machine
- state: WAITING FOR USER REFERENCE-MACHINE EVIDENCE; NON-BLOCKING FOR INDEPENDENT WORK
- verify carried-forward behaviors: MS-033 generation cancel/retry; MS-036 direct successful 3D insertion; MS-037 exterior rendering; MS-039 grounded initial placement; MS-040 Smart Select; MS-035 resize/no gutters; MS-026/MS-027 bottom workspace; MS-038 direct Move/Rotate; plus v1.0.35 Smart Selection target-switch and mapped attachment-control clearing
- acceptance: failures are promoted/reopened in ISSUE_STATE and preempt lower-priority work as appropriate

## Current objective
### H — v1.0.36 UI-neutral attachment/selection authority convergence
- state: READY FOR COORDINATOR RELEASE-BOUNDARY REVIEW
- outcome: bounded runtime-testable fail-closed ownership batch is assembled; Coordinator decides whether to publish this boundary or require the known attachment-history gap in the same version
- release_boundary: v1.0.35 is published/frozen. v1.0.36 is the only authoritative writable semantic branch.

### Completed v1.0.36 batch
1. mapped Snap no longer falls back to legacy attachment mutation when the selected child has stable Core identity but its socket owner cannot resolve to stable Core identity
2. mapped Detach with no durable Core attachment retires stale legacy projection instead of invoking legacy detach authority
3. mapped attachment read-side projection now requires the durable socket to exist and its current Godot owner to map to the exact durable Core ParentObjectId; missing/mismatched socket ownership fails closed
4. Smart Selection presentation now clears when its selection object is invalid, unmapped, or no longer exists in current Core state instead of leaving orphaned vertex weights visually authoritative
5. focused presentation regression guards cover the changed attachment and Smart Selection seams
- validation: implementation exact-head Core, .NET/C#, Python/runtime/geometry/release-audit and full package/installer gates are green. An intermediate compile failure in the socket-owner guard was diagnosed from CI and fixed before this checkpoint.

### Known follow-on / architecture input
- attachment transaction/history audit found that the UI undo router recognizes StageCEditing transactions but not Stage-D attachment transactions
- a durable detach removes the legacy attachment projection, while Core AttachmentRecord does not persist the legacy part-library/mount metadata needed to reconstruct full Godot placement after undo from Core alone
- Dev escalated this as MSG-20260915-DEV-006 rather than guessing a difficult-to-reverse durable schema choice; Coordinator may choose durable compatibility metadata + migration, or a bounded same-session retired-projection strategy, or another architecture
- this known history/load gap is not represented as fixed in v1.0.36

### Acceptance achieved for this bounded batch
- stable Core-mapped attachment commands no longer regain legacy authority on mapping gaps
- mapped attachment read-side presentation fails closed on absent/mismatched durable socket ownership
- orphaned Smart Selection presentation clears when stable Core object identity disappears
- no user-owned Refinement/Kitbash UI decision was introduced
- implementation checkpoint has exact-head Core and full build/package green

### Next
1. Coordinator reviews this release boundary. Dev does not broaden v1.0.36 while review is pending.
2. If accepted, Coordinator owns publication and forward VERSION bootstrap; attachment history/load architecture work rolls to v1.0.37.
3. If rejected for a concrete acceptance gap, Dev resumes only the requested bounded gap and restores exact-head Core + full build/package green before re-handoff.
4. User/reference-machine verification remains non-blocking for independent future work.

## Deferred / user-owned design
- MS-041 Refinement/Kitbash: structural direction is accepted, but exact UI/interaction design remains user-approval work.
- MS-042 Rig & Pose and Cleanup & Export remain intentionally later work.

## Architecture constraints
- Core remains durable-state and transform authority.
- Godot remains presentation/input authority.
- Python remains inference/geometry authority.
- Preserve local-first/storage containment, immutable revisions/stable IDs, transactional state/history and stale-result rejection.
