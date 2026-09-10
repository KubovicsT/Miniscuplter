# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.20`
- **Stable release target commit:** `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
- **Current development branch:** `v1.0.21`
- **v1.0.21 base:** exact published v1.0.20 target `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
- **Overall completion:** **58% acceptance-weighted**
- **Coordinator roadmap:** `docs/TECHNICAL_ROADMAP.md`
- **Coordinator history:** `docs/COORDINATOR_LOG.md`

The Coordinator roadmap was written before v1.0.20 publication and still describes publication as the immediate P0. Actual Git/release truth now supersedes that completed step; preserve its post-publication ordering rather than reopening release work.

## Completed this Dev Cycle

- Reconciled actual latest release, branches and autonomous-release state.
- Confirmed the previous release-controller defect: an expected nonzero `gh release view` probe left PowerShell `$LASTEXITCODE` nonzero after otherwise-successful validation.
- Patched `release-control` without weakening request validation, exact-SHA binding, branch-freeze, build/export/hash/smoke or immutability safeguards.
- Resubmitted v1.0.20 against the exact final branch head.
- Autonomous-release run `34508060099` passed all gates: C#/Core, Python/runtime, job regressions, geometry regressions, release audit, verified Godot 4.7.2 download, real Windows export, output/hash checks, installer build, silent installer smoke-install, immutable-target recheck, tag creation and GitHub Release publication.
- Verified GitHub latest release is `v1.0.20` targeting `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`.
- Created forward-only branch `v1.0.21` from that exact published target.
- Reconciled `PROJECT_STATUS.md` and this HANDOFF on v1.0.21. Published v1.0.20 remains untouched.

## Shipped Stage-C foundation in v1.0.20

- accepted immutable 2D baseline;
- revision-bound 3D generation/candidate identity;
- explicit Apply and durable restore;
- Core-authoritative mapped transforms with save/reload and transaction Undo/Redo;
- one bounded sculpt/edit commit path creating immutable child `MeshRevision` state;
- stale edit/cleanup protection and lineage preservation;
- revision-bound cleanup;
- exact durable-state validated STL export.

This is a release-complete foundation slice, not yet a target-machine-accepted Stage-C milestone.

## Exact next task

1. Inspect any new user/reference-machine evidence from released v1.0.20 before coding.
2. Run/obtain the complete Stage-C acceptance path on the GTX 1080 / 8 GB VRAM / 16 GB RAM reference machine: `2D → accept baseline → 3D → visible/editable mesh → transform/sculpt → save/reload → cleanup → exact validated STL export`.
3. During the same session, verify:
   - **MS-009:** viewport/grid/model/gizmo visibility and interaction;
   - **MS-013:** storage containment and emitted paths/process environment;
   - **MS-022:** one intended lightweight/default 3D provider, elapsed time and practical RAM/VRAM behavior;
   - **MS-004:** cancellation/recovery where practical.
4. If any reproducible defect appears, reuse/create the appropriate MS issue, record exact evidence, and fix forward on v1.0.21. A reproduced blank viewport or data-loss/storage-severity regression outranks planned architecture work.
5. If target-machine acceptance is green, record the evidence and allow the next Coordinator review to sequence broader MS-019/MS-020 work. Do not independently broaden the critical path before that evidence.

## Current priority order

1. **MS-018** — complete target-machine Stage-C qualification.
2. **MS-009** — viewport/grid/model/gizmo verification; reproduced blank viewport becomes immediate P0.
3. **MS-013** — target-PC storage containment verification.
4. **MS-022** — qualify one intended lightweight/default 3D route on GTX 1080 / 16 GB.
5. **MS-004** — cancellation/recovery verification during real jobs.
6. **MS-019 / MS-020** — broader architecture work after acceptance unless a concrete blocking regression requires earlier action.

## Release policy

v1.0.20 is published and immutable. Never modify its tag/release. All fixes belong on v1.0.21+. The autonomous `release-control` path is now proven through a successful full Windows release and remains the preferred publication mechanism for future ready versions.

## User input

No product/design decision is currently required. The important dependency is real reference-machine testing of released v1.0.20. If the user reports a concrete runtime/UI/provider/storage failure, treat that evidence as the immediate implementation baton.
