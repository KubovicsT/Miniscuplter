# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Current Dev implementation/test HEAD:** `1e6ed3541de6de4bec19838e71595e4b76dbedfa`; exact-head GitHub Actions were queued at handoff time, so this is **not yet recorded as release-worthy**.
- **Latest previously fully validated implementation checkpoint:** `d4aef5d55170ef45298e9db942cb250d78e911d2`.
- **Latest Coordinator planning commits:** roadmap `61c1ad61e9cf6e9d265d50db53daa35e3bae69d3`, coordinator log `042dcc2a7ddda6f410d28522861e350fb7c88594`; Coordinator also inserted `fc0903e15734c5425ebd0962339e7db9c8b90cb6` during this Dev run to elevate MS-030 above MS-029.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** MS-030 packaged backend health/runtime ownership → MS-029 updater restart → MS-009 resize-induced viewport darkening → user-directed MS-027 UI tranche → remaining Stage-C acceptance.

## Release state

v1.0.25 is published and immutable. v1.0.26 remains writable. No release freeze/request is active for v1.0.26. Dev did not create release-control state, tags or releases.

## This Dev run

### MS-029 bounded updater hotfix

- `9f36f44f7b792c2bbdf51cbdaa81cf72b1bebf6`: `VerifyLauncherStartup` now tracks confirmed health. A launcher that has written the health token remains the normal post-update launcher instead of being killed by `finally`; failed/timed-out probes are still terminated before rollback.
- `e6ae3d7d0c8186398fec94aa756f75ca9fe2c89d`: added CI-gated source regression coverage for health success survival plus failure rollback/restored-launcher restart.
- Transition constraint remains: an update **from published v1.0.25** is executed by the updater already installed in v1.0.25. The fixed updater packaged in v1.0.26 cannot retroactively control that first transition, so v1.0.25 → v1.0.26 may still require one manual launcher reopen. Do not add a risky detached-process bridge merely to hide this one-time compatibility limit.

### MS-030 P0 packaged backend-health/runtime ownership

Coordinator raised MS-030 during this run, and Dev immediately preempted further MS-029/MS-009 work.

- `4efd840eaa774a87c7dd8ec8aaba98479e02e266`: editor backend launch now uses exactly `<backend>/.venv/Scripts/python.exe`, the environment Repair creates/validates. Removed preference for `Runtime/Python/python.exe` and removed arbitrary PATH-Python fallback. Startup/readiness errors now include backend path, selected interpreter, exit state and bounded recent stderr.
- `6dcb754b552f6254d6ab6165ecf94725f354bc2d`: Repair AI Runtime now starts the repaired backend with that same interpreter and requires a real `http://127.0.0.1:7868/health` response before reporting success; the temporary probe process tree is always cleaned up.
- `1e6ed3541de6de4bec19838e71595e4b76dbedfa`: added CI-gated regression coverage preventing interpreter divergence, arbitrary Python fallback, loss of startup diagnostics, or Repair success without real backend health validation.

This confirms the repository-level interpreter-divergence hypothesis and removes the duplicate runtime authority. Reference-machine verification is still required before MS-030 can move beyond **FIXED - NEEDS USER VERIFICATION**.

## Validation state

- Exact-head CI for `1e6ed3541de6de4bec19838e71595e4b76dbedfa` was queued at end of run (`build` plus `core-foundation`). Do **not** claim this checkpoint green until those runs complete.
- The branch's prior checkpoint `d4aef5d5...` remains the latest fully validated checkpoint until then.
- If CI fails, inspect the failing exact-head job first and fix the defect without weakening runtime/health/update safety contracts.

## Exact next task

1. Resolve exact-head CI for `1e6ed3541de6de4bec19838e71595e4b76dbedfa`; fix any compile/test/package/audit regression found.
2. If green, record that SHA (or its coherent superseding fix SHA) as the latest useful implementation checkpoint. Keep MS-030 **FIXED - NEEDS USER VERIFICATION** until the reference machine reaches backend health/provider resolution using the packaged build.
3. Preserve the MS-029 hotfix already in ancestry. After MS-030 validation, finish MS-029 validation/documentation without inventing extra updater architecture.
4. Then follow Coordinator order: MS-009 resize-induced viewport darkening → bounded user-directed MS-027 UI tranche. Ground-placement/MS-019 remains deferred.
5. Dev does not publish releases.

## User verification dependency

No product/design decision is required.

For released v1.0.25, do not keep retrying Repair or 3D generation: the backend-health failure is sufficiently reproduced for MS-030. The post-update close is also sufficiently reproduced for MS-029. Manually reopening `Miniscuplter.Launcher.exe` once is useful only to distinguish the known updater termination from a separate launcher startup failure; report it only if the manual launch also closes.

When a build containing the MS-030 fix becomes available, the decisive retest is: Repair AI Runtime must finish only after backend health succeeds, then Generate 3D must pass backend health and reach provider resolution/inference. MS-030 remains user-verification-dependent until that happens.

Independent v1.0.25 checks may continue for save/reopen persistence, mapped Move/Rotate/Scale/sculpt, viewport resizing/presentation, storage containment and cancellation/recovery, but no additional reproduction is needed for the already-recorded MS-009 resize darkening or MS-027 UI acceptance gaps.
