# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Latest validated implementation checkpoint:** `d4aef5d55170ef45298e9db942cb250d78e911d2`.
- **Latest Coordinator planning commits:** roadmap `61c1ad61e9cf6e9d265d50db53daa35e3bae69d3`, coordinator log `042dcc2a7ddda6f410d28522861e350fb7c88594`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** MS-029 self-update launcher termination hotfix → MS-009 resize-induced viewport darkening root-cause fix → user-directed MS-027 UI tranche → continuing Stage-C acceptance. Serious correctness/persistence/viewport/data-safety/storage/cancellation regressions preempt fallback work.

## Release state

v1.0.25 is published and immutable. v1.0.26 remains writable. Coordinator reviewed the viewport-drag checkpoint and decided **KEEP v1.0.26 ACCUMULATING**; no release freeze/request is active for v1.0.26.

## Completed v1.0.26 checkpoint

The previously authorized viewport-drag MS-019 seam is complete and validated. Mapped Move/Rotate/Scale gestures capture durable Core transform plus presentation start state, commit only the resulting gesture delta/scale ratio through Core, reject stale state, and reproject/restore Godot presentation from durable Core truth. Do not repeat or broaden this seam.

## Exact next task

1. **Preempt the mapped-object ground-placement seam. Fix MS-029 first.**
2. Reproduce from code/tests the successful update path in `Updater/Program.cs`: the updated launcher writes the health token, then `VerifyLauncherStartup` kills it in `finally`, and the committed success path never launches the launcher normally.
3. Implement the smallest safe post-update restart/health protocol. Preserve rollback when health validation fails. On successful commit, the updated launcher must remain/rerun normally without user intervention.
4. Account for transition compatibility: the updater that installs the next release is the updater already present in v1.0.25. Do not assume the newly packaged updater controls that first transition; document any unavoidable one-time manual-reopen behavior or implement a safe compatible bridge if feasible.
5. Add focused regression coverage for successful health-token validation + normal post-commit launcher availability and failed health validation + rollback.
6. Run strongest launcher/updater/C# packaging/release-audit validation available. Record MS-029 state and the exact checkpoint in PROJECT_STATUS/ISSUES/HANDOFF.
7. After the bounded MS-029 hotfix/checkpoint, **fix MS-009 before MS-027 or MS-019**. The v1.0.25 reference machine still shows the viewport/grid turning materially darker after right-panel/splitter resize. Instrument initial vs post-resize SubViewport/world/environment/camera/grid state; identify the root state change; keep Stretch as normal resize owner and do not reintroduce competing `SubViewport.Size` writes or blind delayed repair loops.
8. Validate the MS-009 fix with the strongest available Windows/Godot render test, but leave the issue IN PROGRESS/FIXED-NEEDS-USER-VERIFICATION until reference-machine resize retest.
9. Then execute the user-directed MS-027 UI acceptance tranche: multi-line scrollable AI command console; actual interactive 3D view cube; compact viewport tool controls without instructional paragraphs; right-panel explanatory prose replaced by small circular `i` hover help. Keep this tranche presentation-only and reuse existing action/state owners.
10. Ground-placement/MS-019 remains deferred. Dev does not create release-control, tags or GitHub Releases.

## User verification dependency

The reference-machine report that the launcher opens briefly and closes immediately after update is accepted as MS-029 reproduction evidence. Manually reopen `Miniscuplter.Launcher.exe` once; only report back immediately if that manual launch also closes, because that would be a second startup failure rather than the identified post-update health-probe termination.

Continue testing released v1.0.25 on the reference Windows / GTX 1080 machine:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job. The current v1.0.25 screenshots already establish both that the command strip/view-cube/help density/right-panel prose do not meet UI acceptance **and** that splitter resize still changes/darkens the viewport/grid presentation (MS-009). No additional reproduction steps are needed before Dev investigates.

No product/design decision is currently required.
