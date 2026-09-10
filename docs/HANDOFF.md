# Miniscuplter Handoff

> Operational baton for the next development run. Keep this concise and update it at the end of every meaningful work session. A new agent must still inspect the actual branch/commits/tests rather than trusting this file blindly.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.18`
- **Stable application commit:** `ce2d876fc145e615d63bd8d9fc809610f6038301`
- **Current development branch:** `v1.0.19`
- **Last code-bearing HEAD before project-management bootstrap:** `87601d0f343e9117c172097ad9a64cef574b1f0f`
- **Current HEAD:** resolve from Git at run start; documentation updates advance the branch.
- **Overall completion estimate:** 56% acceptance-weighted; see `PROJECT_STATUS.md`.

## Read first

1. `docs/PROJECT_CHARTER.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ISSUES.md`
4. `docs/DECISIONS.md`
5. this file
6. `docs/REFACTOR_PLAN.md`
7. relevant recent commits/code/tests on `v1.0.19`

The repository/code/release state wins if any document is stale. Update the documents when a mismatch is found.

## What was already present on v1.0.19 before this handoff

`v1.0.19` was already an active development branch, 12 commits ahead of v1.0.18 branch state. The compare includes substantive changes in:

- `Scripts/Main.V1019ViewportPipeline.cs` — native SubViewport repair/rebind pipeline, grid/model/material/camera/selection/gizmo enforcement and render diagnostics;
- Stage-B `Core` project models/history/store and legacy importer;
- updater/application-update hardening;
- backend storage containment/canonicalization;
- geometry/runtime/provider contracts and tests;
- release-audit coverage.

The latest observed v1.0.19 build/core-foundation workflows at code HEAD `87601d0f...` passed.

Do not reset v1.0.19 to v1.0.18 or recreate the work that is already there.

## Project-management bootstrap completed in this session

The repository now has canonical project memory designed for autonomous continuation:

- `PROJECT_CHARTER.md` — mission, boundaries, finished-product definition, architecture, requirements, Astra direction and release rules;
- `PROJECT_STATUS.md` — weighted completion, current branch/release state, working/partial/missing areas and immediate priorities;
- `ISSUES.md` — stable MS-xxx issue ledger with symptoms, attempts, outcomes and verification status;
- `DECISIONS.md` — durable product/architecture decisions and escalation rules;
- `HANDOFF.md` — this baton.

No application code was changed by this bootstrap.

## Highest-priority unresolved work

### 1. MS-009 — 3D viewport/grid/model/gizmo reliability

v1.0.19 contains the strongest viewport repair attempt so far, including a native `SubViewportContainer`, explicit world/camera ownership, starter mesh, visible material, grid rebuild, selection/gizmo update and render probe diagnostics.

**Next action:** inspect the v1.0.19 viewport pipeline and related legacy viewport code for conflicting ownership or duplicate repair paths. Strengthen deterministic tests/diagnostics where possible. When the v1.0.19 batch is complete and release gates pass, publish it and request real-machine verification. If still blank, use the render diagnostics to isolate whether pixels, world, camera, materials or UI surface are failing.

### 2. MS-018 — Stage-C end-to-end thin slice not qualified

Primary product acceptance gap remains:

`2D → accept durable baseline → one qualified 3D provider → visible/editable mesh → save/reload → cleanup → validated STL`

on GTX 1080 8 GB + 16 GB RAM, including cancellation recovery and contained storage.

### 3. MS-013 — storage containment

v1.0.18 introduced `AppDataRoot`; v1.0.19 adds more backend/Windows containment work. Real-machine verification must confirm representative operations no longer write significant working artifacts into arbitrary C: AppData/TEMP paths.

### 4. MS-020 / MS-019 — Job Broker and legacy architecture migration

Continue moving one vertical slice at a time onto stable IDs/revisions/project history. Avoid adding new permanent state to legacy widgets when the new core can own it.

### 5. MS-022 — provider qualification

Add provider self-tests/readiness states and target-hardware benchmarks. Prefer one trustworthy default 3D route before adding more optional provider breadth.

## Autonomous engineering policy

Continue independently unless a decision materially changes product scope, UX direction with hard-to-reverse tradeoffs, user-data safety, backward compatibility, payment/credentials/external services, minimum hardware, or local-first/privacy assumptions.

When ordinary engineering choices arise:

- make a reasoned decision;
- implement/test it;
- document it;
- continue.

When blocked on one task, log the blocker and work on the highest-value independent task instead of stopping the whole project.

## Release policy

Do not mutate published releases.

For v1.0.19:

1. finish the coherent intended batch;
2. review as a senior engineer;
3. update `PROJECT_STATUS.md`, `ISSUES.md`, `DECISIONS.md` if needed and rewrite this handoff;
4. run C#/Python/core/geometry/release-audit/packaging validation;
5. perform real Godot Windows export;
6. verify hashes/artifacts;
7. smoke-install the installer;
8. publish immutable v1.0.19;
9. verify `/releases/latest`;
10. start future application changes on `v1.0.20`.

User-observed runtime/UI fixes should remain `FIXED - NEEDS USER VERIFICATION` until tested on the actual machine.

## Required end-of-run handoff

Before every autonomous run ends:

- update issue statuses and append new attempts/results;
- update workstream completion only with evidence;
- record current stable/development versions;
- record what changed and tests run;
- record any user decision or real-machine verification required;
- replace this file's `Next action` with the exact next engineering task.

## Exact next action

Reconcile the existing v1.0.19 code as a whole, with special focus on **MS-009 viewport pipeline conflicts and Stage-C reliability**, then continue the highest-value unblocked implementation work. Do not release merely because a scheduled run is ending; release when the v1.0.19 batch is coherent and all required gates pass.
