# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.20`
- **Stable release target commit:** `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
- **Current development branch:** `v1.0.21`
- **v1.0.21 branch base:** exact published v1.0.20 target
- **Overall completion:** **58% acceptance-weighted**

v1.0.20 is published and immutable. All fixes belong on v1.0.21+.

## Current phase

The bounded Stage-C foundation shipped in v1.0.20:

`accepted 2D baseline → revision-bound 3D candidate → explicit Apply → Core-authoritative transform → one immutable sculpt/edit commit → save/reload → revision-bound cleanup → exact validated STL export`

Reference-machine testing reopened **MS-009** as the immediate P0. v1.0.20 renders the floor/grid and starter model, but the resting viewport is dark/unreadable and changes appearance after splitter resize settles.

## v1.0.21 viewport candidate

The narrow Coordinator-directed MS-009 fix is implemented without broadening application scope:

- `SubViewportContainer.Stretch` is the normal viewport-size owner once the native pipeline is installed;
- v1.0.9 responsive sizing defers instead of writing `SubViewport.Size` under the native pipeline;
- v1.0.18 no longer calls the legacy size synchronizer every frame under the native pipeline;
- the v1.0.17 periodic viewport timer is stopped once native ownership is established;
- ordinary host resize no longer schedules a delayed full world repair;
- workflow-tab changes replace the legacy full-repair handler with a lightweight native frame/presentation refresh;
- `OwnWorld3D` is established once and the existing scene world is rebound only during initial configuration or genuine recovery;
- background, grid, mesh material and key/fill/ambient lighting use a neutral studio-style gray presentation with clearer contrast;
- viewport diagnostics now expose resize ownership, world configuration, light counts and rendered-frame luminance;
- release audit now asserts the new ownership and presentation invariants;
- visible release identity has been advanced consistently to `1.0.21`.

This is a code-level fix only. MS-009 cannot be resolved until the immutable build is tested on the reference PC.

## Validation

For the v1.0.21 code candidate before final documentation commits:

- Stage-B/Core foundation CI: **PASS**;
- C# editor/launcher/updater/Core builds: **PASS**;
- Python compilation and dependency resolution: **PASS**;
- core logic and execution regressions: **PASS**;
- real geometry regressions: **PASS**;
- v1.0.21 release audit including viewport ownership assertions: **PASS**;
- portable package / installer-definition branch gate: **running at last reconciliation**.

The autonomous release controller remains the final authority for real Godot 4.7.2 Windows export, output/hash verification, installer creation and silent installer smoke-install before publication.

## Current priorities

1. **MS-009 — P0:** finish validation/publish narrow v1.0.21, then reference-PC launch/resize/settle/tab-switch verification.
2. **MS-018:** resume full Stage-C target qualification after viewport retest.
3. **MS-013:** storage containment verification.
4. **MS-022:** qualify one intended lightweight/default 3D provider on GTX 1080 / 16 GB.
5. **MS-004:** cancellation/recovery during a real job.
6. **MS-019 / MS-020:** broader architecture remains sequenced after acceptance unless concrete evidence changes the dependency order.

## Immediate engineering priority

Do not add unrelated v1.0.21 functionality. Finish release validation for the viewport candidate. If all release gates pass, publish v1.0.21 through `release-control` and keep MS-009 at `FIXED - NEEDS USER VERIFICATION` until the actual reference-machine result is known.

## User input currently required

No product/design decision is required. After v1.0.21 is published, the user should verify that the viewport stays visually identical before/during/after divider resize, remains readable after tab switches, and shows a clearly lit neutral-gray model/grid.
