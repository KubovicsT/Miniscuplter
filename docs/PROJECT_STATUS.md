# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.22`
- **Stable release target commit:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
- **Current development branch:** `v1.0.23`
- **Current bounded implementation candidate before documentation commits:** `74ec73a14645071bb2742fb68f778bedd656aabd`
- **Overall completion:** **57% acceptance-weighted**

v1.0.22 is published and immutable. v1.0.23 is the forward application branch. No application changes belong on v1.0.22.

## Current phase

The Coordinator-defined critical path remains **Stage-C target-machine acceptance**. Released v1.0.22 contains the bounded acceptance fixes for the concrete v1.0.21 reference-machine findings:

1. **MS-023 — Stage-C generation ownership/persistence:** final composition removes historical Generate-3D owners and binds exactly one revision-aware `V1020Generate3DAsync` owner.
2. **MS-024 — resize/client-fill seams:** final composition reasserts FullRect/ExpandFill layout ownership without restoring manual `SubViewport.Size` writes.
3. **MS-025 — starter-scene/presentation residuals:** the historical starter sphere is removed from launch/New/recovery paths and the opaque legacy grid floor is hidden while useful grid/axes remain.

Those fixes remain **FIXED - NEEDS USER VERIFICATION** until the released v1.0.22 build is exercised on the reference machine. The next critical evidence is generate → Ready/Conflict candidate → Apply → save/close/reopen → same durable object/revision, plus viewport resizing/presentation, starter removal, cleanup/export, storage containment and one qualified lightweight 3D route.

## Opportunistic work while acceptance is externally blocked

The Coordinator explicitly permits one bounded **MS-027** slice when no higher-priority unblocked correctness task exists. v1.0.23 now contains the first such slice without changing project or AI ownership:

- `Main.V1023UiPreferences.cs` stores editor-only preferences under the authoritative Miniscuplter data root at `Settings/ui_preferences.json`;
- body and viewport/right-panel splitter positions persist across restarts with minimum-width clamps;
- Settings gains an **Interface** page with bounded 75–135% UI/font scaling and a workspace-layout reset;
- default font scale is reduced to 90% to begin the requested denser modeling UI;
- reusable hover-tooltip infrastructure covers core toolbar actions;
- the layer installs after `InstallV1022Acceptance()` and does not reference `ProjectStore` or own Stage-C generation actions;
- targeted static wiring guards were added to `tools/core_logic_tests.py`.

Implementation commits: `f169f6e3428a64804e8778c05353b9d07a87dfe3`, `5faa1e528e6a81fb8db15942c68b3da71391743d`, `74ec73a14645071bb2742fb68f778bedd656aabd`.

## Validation

For implementation commit `5faa1e528e6a81fb8db15942c68b3da71391743d`:

- Stage-B/Core build and regression suite: **PASS** (`core-foundation` run `34520603280`);
- C# editor/launcher/updater/Core builds: **PASS** (`build` run `34520603310`);
- Python compile/dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- geometry regressions and release audit: **PASS**;
- portable package layout/hash and installer-definition compile: **PASS**.

Commit `74ec73a14645071bb2742fb68f778bedd656aabd` adds explicit MS-027 static ownership/persistence guards. Its exact-head Core/build workflows were running at this reconciliation point; do not describe the test commit as fully validated until those workflows finish.

No release request has been submitted for v1.0.23. This secondary UI slice is not being published merely because branch validation is green; release remains readiness/scope based.

## Current priorities

1. **Reference-machine retest of released v1.0.22** for MS-023, MS-024, MS-025 and the complete MS-018 Stage-C path. Any reproduced correctness/persistence/viewport/data regression immediately preempts UI modernization.
2. In the same acceptance session collect MS-013 storage and MS-022 provider/resource evidence, plus MS-004 cancellation/recovery where practical.
3. If acceptance evidence is still unavailable and no higher-priority unblocked issue appears, continue exactly one next bounded MS-027 slice in Coordinator order: direct icon-based viewport tool strip using the existing tool owner; do not create duplicate tool/action state.
4. Broader MS-019/MS-020 architecture remains behind Stage-C acceptance unless concrete evidence changes dependency order.

## User input currently required

No product/design decision is required. The important external dependency is reference-machine verification of released v1.0.22; until that arrives, bounded MS-027 work may continue under the Coordinator fallback rule.
