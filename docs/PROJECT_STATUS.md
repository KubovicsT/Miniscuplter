# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.21`
- **Stable release target commit:** `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8`
- **Current development branch:** `v1.0.22`
- **v1.0.22 branch base:** exact published v1.0.21 target
- **Overall completion:** **57% acceptance-weighted**

v1.0.21 is published and immutable. All subsequent fixes belong on v1.0.22+.

## Current phase

The bounded Stage-C foundation shipped in v1.0.20. Reference-machine testing then reopened **MS-009** because v1.0.20 rendered the viewport but the resting grid/model were dark and the appearance changed after splitter resize settled.

v1.0.21 is the narrow evidence-driven viewport remediation release. It establishes one normal resize/world ownership path, removes routine legacy size/full-repair competition, replaces legacy tab full-repair behavior with lightweight native refresh, and uses a neutral gray studio-style viewport presentation with stronger diagnostics.

MS-009 remains **FIXED - NEEDS USER VERIFICATION**, not resolved, because the failure is render-driver/reference-machine dependent.

## v1.0.21 release validation

Autonomous release run `34513308015` passed against exact candidate `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8`:

- exact request/SHA and branch-freeze validation: **PASS**;
- C# editor/launcher/updater/Core builds and Core regressions: **PASS**;
- Python compilation/runtime dependency resolution: **PASS**;
- core/job regressions: **PASS**;
- real geometry regressions and v1.0.21 release audit: **PASS**;
- verified Godot 4.7.2 .NET + export templates: **PASS**;
- real Windows release build/export: **PASS**;
- versioned release outputs and ZIP hash verification: **PASS**;
- generated installer silent smoke-install: **PASS**;
- immutable target recheck and GitHub publication: **PASS**.

GitHub latest release now reports `v1.0.21` at the exact candidate with installer, ZIP and SHA sidecar assets.

## New reference-machine Stage-C evidence

The same v1.0.20 session that produced the first useful Hunyuan-mini result also exposed a production-integration blocker:

- Hunyuan-mini completed successfully in about **402 s** on the GTX 1080 reference PC.
- A Task Manager snapshot during the run showed about **97% GPU**, **5.4/8.0 GB dedicated VRAM**, **5.0/15.9 GB system RAM** and **73 °C GPU temperature**. These are useful observed values, not established peaks.
- The generated mesh appeared in the viewport, but the Stage-C UI still said **`3D candidate: none`**.
- After closing and reopening Miniscuplter, the 2D image returned but the generated 3D model did not.
- Code review confirms duplicate legacy/Stage-C Generate-button handlers can let the legacy path import a transient scene mesh while the Stage-C handler exits on the shared busy flag.

This is tracked as **MS-023** and blocks end-to-end Stage-C acceptance. The acceptance-weighted estimate is reduced from 58% to **57%** because real-machine evidence shows the shipped UI is not actually exercising the durable candidate/apply/persistence path for this successful generation.

Additional user findings are tracked as **MS-024** (black seams/gaps on window resize), **MS-025** (remove oversized starter sphere), and **MS-026** (planned in-app resource telemetry/graphs). The opaque floor/grid also remains part of MS-009 acceptance because it must not hide geometry.

## Current priorities

1. **MS-023 — active engineering P0:** fix duplicate legacy/Stage-C 3D-generation ownership so successful generation becomes a durable candidate/applied object and survives restart.
2. **MS-009 — parallel P0 verification dependency:** retest released v1.0.21 for launch/resize/settle, non-occluding grid/model/gizmo readability and stability.
3. **MS-018:** resume/complete full Stage-C target qualification once MS-023 is fixed and MS-009 is usable.
4. **MS-024 / MS-025:** fix residual whole-window resize seams and remove the oversized automatic starter sphere.
5. **MS-013 / MS-022 / MS-004:** storage containment, provider/resource qualification and cancellation/recovery.
6. **MS-026:** add low-overhead in-app resource telemetry after current correctness blockers, using it to support provider qualification.
7. **MS-019 / MS-020:** broader architecture remains behind acceptance unless concrete evidence makes it blocking.

## Immediate engineering priority

While v1.0.21 viewport verification remains a parallel user dependency, v1.0.22 engineering should first correct MS-023 because it is a confirmed production persistence blocker. Do not spend another long target-machine generation merely to reproduce it: the screenshot/restart evidence plus duplicate handler wiring are sufficient to implement a deterministic fix and regression test. After that, use v1.0.21/v1.0.22 reference-machine testing to close MS-009 and resume the complete MS-018 flow.

## User input currently required

Reference-machine verification of released v1.0.21 is now the critical dependency. No product/design decision is required.
