# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.21`
- **Stable release target commit:** `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8`
- **Current development branch:** `v1.0.22`
- **v1.0.22 base:** exact published v1.0.21 target
- **Overall completion:** **57% acceptance-weighted**
- **Immediate P0:** `MS-023` duplicate generation ownership / lost 3D persistence; `MS-009` v1.0.21 verification proceeds in parallel

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

1. Reconcile latest release/CI and exact v1.0.22 HEAD before editing.
2. Treat **MS-023** as the immediate engineering P0. Reproduce from code/tests rather than asking the user to spend another ~7 minutes on Hunyuan merely to prove it again.
3. Audit all Generate-3D event wiring. When the v1.0.20+ Stage-C bridge is installed, explicitly remove/disable the v1.0.17 legacy handler as well as older handlers so exactly one production owner remains.
4. Preserve the Stage-C semantics: successful inference registers a durable Ready/Conflict candidate; do not silently import it as a committed transient object. Candidate preview may be visible, but Apply is the explicit transactional transition into the persistent editable object.
5. Add regression/audit coverage proving only one authoritative handler and proving generate → candidate → Apply → save/reload restores the same ObjectId/active MeshRevision. Verify Move/Rotate/Scale only on the genuinely mapped applied object.
6. Fix first-object UX coupled to this flow: remove automatic **Starter sphere** creation from launch/New Scene/recovery paths, allow a clean empty project, and automatically select/frame the first imported/applied generated object. Do not use the old sphere as an implicit scale reference.
7. Continue **MS-009** reference-PC verification on released v1.0.21 in parallel. The grid/ground must be non-occluding; the user must never need to look above/below an opaque plane to see the generated model.
8. Retest **MS-024** whole-window resizing for the black seams/gaps shown by the user. If v1.0.21 did not eliminate them, fix forward on v1.0.22 without reintroducing competing viewport-size owners.
9. Resume the complete MS-018 acceptance flow after MS-023/MS-009 are usable: accepted 2D baseline → qualified 3D candidate → Apply → visible/editable persisted mesh → save/restart restore → transform/sculpt → cleanup → exact validated STL export.
10. During provider qualification preserve the real Hunyuan evidence already obtained (~402 s; Task Manager snapshot ~97% GPU, ~5.4/8 GB dedicated VRAM, ~5.0/15.9 GB RAM, ~73 °C). Treat snapshots as observations, not peaks.
11. **MS-026 resource graphs** are now a user-requested planned feature. Design a low-overhead ~1 Hz local telemetry panel with GPU utilization, dedicated VRAM, RAM, GPU temperature and secondary CPU where available, plus current provider/stage/elapsed time and compact post-job peak summary. Implement after current correctness blockers unless minimal telemetry directly helps MS-022.
12. Do not broaden into full UI rewrite, provider proliferation, full Job Broker reconstruction or unrelated legacy cleanup.

## Current priority order

1. **MS-023** — confirmed 3D generation/candidate/persistence blocker.
2. **MS-009** — v1.0.21 reference-machine viewport verification; fix forward on v1.0.22 if it fails.
3. **MS-018** — complete Stage-C target qualification.
4. **MS-024 / MS-025** — responsive resize seams and starter-sphere removal.
5. **MS-013 / MS-022 / MS-004** — storage, provider/resource qualification, cancellation/recovery.
6. **MS-026** — planned in-app resource telemetry supporting provider qualification.
7. **MS-019 / MS-020** — broader architecture after acceptance unless concrete evidence makes it blocking.

## Explicit non-priorities

Do not start a full UI rewrite, another viewport overlay, broad legacy deletion, full sculpt migration, Job Broker reconstruction, Rig & Pose work, kitbash expansion or provider proliferation while MS-009/MS-018 acceptance is unresolved.

## User input

No product/design decision is required. The MS-023 persistence failure is already sufficiently reproduced. The next useful user input is the inexpensive v1.0.21 viewport/resize retest; do not require another long Hunyuan run until the Stage-C handler/persistence fix is available.
