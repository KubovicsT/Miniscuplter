# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.21`
- **Stable release target commit:** `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8`
- **Current development branch:** `v1.0.22`
- **v1.0.22 base:** exact published v1.0.21 target
- **Overall completion:** **58% acceptance-weighted**
- **Immediate dependency:** reference-machine verification of `MS-009`

v1.0.21 is published and immutable. Never commit fixes to v1.0.21; all subsequent changes belong on v1.0.22+.

## What shipped in v1.0.21

The narrow Coordinator-directed MS-009 viewport remediation shipped without broadening application scope:

- `SubViewportContainer.Stretch=true` is the normal render-target size owner;
- v1.0.9 responsive and v1.0.18 per-frame legacy size writers defer under the native pipeline;
- the v1.0.17 periodic viewport-size timer is stopped under native ownership;
- ordinary splitter resize no longer triggers delayed full world repair;
- legacy workflow-tab full repair is replaced by lightweight native frame/presentation refresh;
- the native pipeline uses one owned World3D and only rebinds the existing scene world during initial configuration or genuine recovery;
- viewport presentation uses neutral dark-gray background, neutral minor/major grid, colored axes, neutral-gray model shading, and studio key/fill/ambient lighting;
- diagnostics expose resize ownership, render-target dimensions, world configuration, light counts and frame luminance/contrast;
- release audit guards these ownership/presentation invariants.

MS-009 remains **FIXED - NEEDS USER VERIFICATION**, not RESOLVED, because the reported failure depends on the real Windows render-driver/reference-machine behavior.

## Release validation

Autonomous release run `34513308015` validated exact candidate `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8` and completed successfully:

- exact request/SHA validation: PASS;
- C# and Core builds/tests: PASS;
- Python/runtime dependency checks: PASS;
- core/job regressions: PASS;
- geometry regressions + v1.0.21 release audit: PASS;
- verified Godot 4.7.2 .NET/export templates: PASS;
- real Windows export/package build: PASS;
- output/version/hash verification: PASS;
- installer creation + silent installer smoke-install: PASS;
- source-branch immutability recheck: PASS;
- immutable tag/GitHub Release publication: PASS.

GitHub latest-release state was verified after publication: `v1.0.21` targets exactly `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8` and includes installer, ZIP and SHA sidecar assets.

## Exact next task

1. Reconcile latest Git/release/CI and v1.0.22 HEAD before any edit.
2. Do **not** perform speculative additional viewport refactoring without new target-machine evidence.
3. The critical next evidence is a reference-PC test of released v1.0.21. Verify:
   - viewport appearance immediately after launch;
   - appearance/state remains identical while dragging the right-panel divider and after resize settles;
   - 2D → 3D → other workflow tab switching does not alter the viewport palette/world;
   - model curvature/details are readable under the neutral-gray studio lighting;
   - minor/major grid and axes remain clearly visible;
   - selection highlight and transform gizmo remain visible and interactive;
   - if anything remains wrong, capture the viewport diagnostics/render-probe text and screenshot.
4. If any resize-dependent, dark/unreadable or blank viewport symptom remains, reopen MS-009 as active P0 and fix forward only on v1.0.22. Use the new diagnostics to prove the actual failing seam rather than adding another overlay or generalized repair layer.
5. If the viewport is accepted, mark MS-009 RESOLVED with reference-machine evidence and resume **MS-018** Stage-C target qualification.
6. Stage-C acceptance then proceeds through accepted 2D baseline → qualified lightweight 3D → visible/editable mesh → save/reload → transform/sculpt edit → cleanup → exact validated STL export, while collecting storage, cancellation and resource evidence.
7. Follow Coordinator ordering after MS-018: MS-013 storage containment, MS-022 intended lightweight/default provider qualification, MS-004 cancellation/recovery, then broader MS-019/MS-020 work unless new evidence changes priority.

## Current priority order

1. **MS-009** — reference-machine verification of released v1.0.21; immediate P0 again if it fails.
2. **MS-018** — complete Stage-C target qualification after viewport acceptance.
3. **MS-013** — storage containment verification.
4. **MS-022** — GTX 1080 / 16 GB lightweight/default 3D-provider qualification.
5. **MS-004** — real cancellation/recovery stress test.
6. **MS-019 / MS-020** — broader architecture only after acceptance unless concrete evidence makes one blocking.

## Explicit non-priorities

Do not start a full UI rewrite, another viewport overlay, broad legacy deletion, full sculpt migration, Job Broker reconstruction, Rig & Pose work, kitbash expansion or provider proliferation while MS-009/MS-018 acceptance is unresolved.

## User input

No product/design decision is required. The next useful user input is the v1.0.21 reference-machine viewport retest and, on failure, its screenshot/diagnostic text.
