# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Latest validated implementation checkpoint:** `d4aef5d55170ef45298e9db942cb250d78e911d2`.
- **Latest Coordinator planning commits:** roadmap `61c1ad61e9cf6e9d265d50db53daa35e3bae69d3`, coordinator log `042dcc2a7ddda6f410d28522861e350fb7c88594`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** MS-030 packaged backend health/runtime ownership → MS-029 updater restart → MS-009 resize-induced viewport darkening → user-directed MS-027 UI tranche → remaining Stage-C acceptance. Serious correctness/persistence/viewport/data-safety/storage/cancellation regressions preempt fallback work.

## Release state

v1.0.25 is published and immutable. v1.0.26 remains writable. Coordinator reviewed the viewport-drag checkpoint and decided **KEEP v1.0.26 ACCUMULATING**; no release freeze/request is active for v1.0.26.

## Completed v1.0.26 checkpoint

The previously authorized viewport-drag MS-019 seam is complete and validated. Mapped Move/Rotate/Scale gestures capture durable Core transform plus presentation start state, commit only the resulting gesture delta/scale ratio through Core, reject stale state, and reproject/restore Godot presentation from durable Core truth. Do not repeat or broaden this seam.

## Exact next task

1. **Fix MS-030 first.** Reproduce the packaged v1.0.25 path where 3D generation fails at backend health even though Repair AI Runtime succeeds.
2. Capture the exact backend path, selected Python interpreter, process lifetime/exit code and startup stderr; confirm or reject the interpreter-divergence hypothesis.
3. Make backend runtime ownership deterministic: use the exact environment Repair validated, or validate any alternate interpreter against the same fingerprint/import contract before launch. Do not silently fall back to arbitrary Python.
4. Strengthen Repair so success proves the backend can actually start and answer health, not only that packages/imports/CUDA checks pass.
5. Improve the failure diagnostic to expose selected interpreter/backend path/process exit and bounded recent stderr instead of only “Use Repair AI Runtime.”
6. Add focused tests and strongest packaged Windows/runtime validation. Keep MS-030 FIXED - NEEDS USER VERIFICATION until the reference machine reaches provider resolution/inference.
7. After MS-030, resume the already recorded order: MS-029 → MS-009 → bounded MS-027 UI tranche. Ground-placement/MS-019 remains deferred. Dev does not publish releases.


## User verification dependency

The reference-machine report that the launcher opens briefly and closes immediately after update is accepted as MS-029 reproduction evidence. Manually reopen `Miniscuplter.Launcher.exe` once; only report back immediately if that manual launch also closes, because that would be a second startup failure rather than the identified post-update health-probe termination.

The v1.0.25 Stage-C test is now blocked at local backend health even after a successful Repair; no more 3D-generation retries are needed until a new build is available. Other independent checks may continue:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job. The current v1.0.25 screenshots already establish both that the command strip/view-cube/help density/right-panel prose do not meet UI acceptance **and** that splitter resize still changes/darkens the viewport/grid presentation (MS-009). No additional reproduction steps are needed before Dev investigates.

No product/design decision is currently required.
