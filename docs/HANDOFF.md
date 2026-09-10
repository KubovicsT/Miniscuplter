# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.20`
- **Stable release target commit:** `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
- **Current development branch:** `v1.0.21`
- **v1.0.21 base:** exact published v1.0.20 target
- **Validated viewport/version code candidate:** `c00bfc8a8468b364204a7848eff165d639838b90`
- **Overall completion:** **58% acceptance-weighted**
- **Immediate P0:** `MS-009` viewport regression, now **FIXED - NEEDS USER VERIFICATION** in code

This HANDOFF/documentation sequence advances the branch beyond the code candidate. Resolve the exact final live v1.0.21 HEAD before creating the autonomous release request; never reuse `c00bfc8...` merely because its code validation passed.

## What v1.0.21 changes

The narrow Coordinator-directed viewport fix is implemented without expanding into broader architecture work.

Normal viewport ownership is now intentionally singular:

- `SubViewportContainer.Stretch=true` owns normal child render-target sizing;
- v1.0.9 responsive code keeps its legacy fallback but defers all `SubViewport.Size` writes once the native pipeline is active;
- v1.0.18 stops calling the legacy size synchronizer every frame under the native pipeline;
- the v1.0.17 periodic viewport timer is stopped when native ownership is installed;
- ordinary `ViewportHost.Resized` events no longer queue a delayed full world repair;
- the v1.0.17 workflow-tab full-repair handler is disconnected and replaced with a lightweight native frame/presentation refresh;
- v1.0.19 no longer creates a fresh explicit `World3D` and then also enables `OwnWorld3D`; the existing scene world is rebound only on initial configuration or genuine recovery.

Readability is also moved toward the user's requested Blender-like solid-workspace hierarchy:

- neutral dark-gray background;
- neutral gray ground/grid with stronger major lines and colored axes;
- mid/light neutral-gray mesh material during pipeline recovery;
- neutral key/fill/ambient studio lighting;
- diagnostics report Stretch ownership, render-target size, owned-world state, light counts and frame luminance/contrast.

Release audit now fails if the removed delayed repair/competing world creation return, or if the legacy timer/per-frame/responsive size writers stop deferring under the native pipeline.

## Validation

For code candidate `c00bfc8a8468b364204a7848eff165d639838b90`:

- `core-foundation` exact-code CI: **PASS**;
- C# editor/launcher/updater/Core builds: **PASS**;
- Python compile/runtime dependency resolution: **PASS**;
- core logic/execution regressions: **PASS**;
- real geometry regressions: **PASS**;
- v1.0.21 release audit including new viewport assertions: **PASS**;
- portable package layout and ZIP SHA verification: **PASS**;
- installer-definition compilation was still running at the last documentation write and must be rechecked before release request.

The autonomous release workflow must still independently rebuild the exact final documentation-inclusive branch HEAD, perform the real Godot 4.7.2 Windows export, verify release outputs/hashes, build and silently smoke-install the installer, recheck the frozen branch SHA, then publish the immutable tag/release.

## Exact next task

1. Reconcile actual latest release, exact v1.0.21 HEAD and current CI state.
2. Confirm the broad branch packaging/installer-definition gate for the code candidate completed successfully. If it failed, diagnose/fix before release.
3. Do not add unrelated application work to v1.0.21.
4. Ensure `PROJECT_STATUS.md`, `ISSUES.md`, and this HANDOFF remain coherent with the intended narrow viewport release. `DECISIONS.md` needs no change because no durable product/architecture decision changed.
5. Resolve the exact final v1.0.21 branch HEAD after all pre-release documentation commits.
6. Create `release-requests/v1.0.21.json` on permanent `release-control` with `version` and `source_branch` both `v1.0.21` and `candidate_sha` equal to that exact full HEAD.
7. From that request onward, freeze v1.0.21 completely while autonomous release validation is pending.
8. Follow the autonomous run to definitive success or a genuine failing gate. Do not weaken exact-SHA, build, Godot export, hash, installer-smoke or immutability safeguards.
9. On successful publication, verify latest GitHub release/tag is v1.0.21 and targets the exact candidate.
10. Before any further repository change, create/use forward-only `v1.0.22`; reconcile post-release docs there.
11. Reference-machine retest of v1.0.21 is then the critical dependency. Verify:
    - appearance immediately after launch;
    - no appearance/state change during vs after divider drag/settle;
    - stable 2D ↔ 3D tab switching;
    - readable neutral-gray model curvature/details;
    - visible grid/major lines/axes;
    - selection/gizmo visibility and interaction;
    - viewport diagnostics/render-probe values if a failure remains.
12. If any resize-dependent or dark/unreadable viewport symptom remains, MS-009 immediately returns to active P0 and must be fixed forward on v1.0.22. If accepted, resume MS-018 Stage-C target qualification, then MS-013/MS-022/MS-004 per Coordinator ordering.

## Current priority order

1. **MS-009** — publish/retest narrow v1.0.21 viewport fix; remains unresolved until target-PC evidence.
2. **MS-018** — full Stage-C qualification after viewport acceptance.
3. **MS-013** — storage containment verification.
4. **MS-022** — reference-hardware 3D provider qualification.
5. **MS-004** — cancellation/recovery.
6. **MS-019 / MS-020** — broader architecture only after acceptance unless new evidence makes one a blocker.

## Explicit non-priorities

Do not turn this release into a full UI rewrite, another viewport overlay, broad legacy deletion, full sculpt migration, Job Broker reconstruction, Rig & Pose work, kitbash expansion, or provider proliferation.

## Release policy

v1.0.20 is immutable. v1.0.21 is a narrow evidence-driven viewport fix and is appropriate to release once all gates are green because the remaining acceptance question is render-driver/reference-machine behavior that CI cannot prove.

## User input

No product/design decision or manual release action is required. After v1.0.21 publication, reference-machine verification is required before MS-009 can be marked RESOLVED.
