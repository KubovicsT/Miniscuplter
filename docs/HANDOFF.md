# Miniscuplter Handoff

> Operational baton for the next development run. Keep this concise and update it at the end of every meaningful work session. A new agent must still inspect the actual branch/commits/tests rather than trusting this file blindly.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.18`
- **Stable application commit:** `ce2d876fc145e615d63bd8d9fc809610f6038301`
- **Current development branch:** `v1.0.19`
- **Latest code commit from this autonomous run:** `59757d04eeb0f9e19cd7eb8fc812701373e0d03f` — `Make viewport sizing deterministic across legacy handlers`
- **Overall completion estimate:** remains 56% acceptance-weighted; this change removes a concrete viewport ownership conflict but does not count as real-machine acceptance proof.

## Read first

1. `docs/PROJECT_CHARTER.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ISSUES.md`
4. `docs/DECISIONS.md`
5. this file
6. `docs/REFACTOR_PLAN.md`
7. relevant recent commits/code/tests on the current development branch

The repository/code/release state wins if any document is stale. Update the documents when a mismatch is found.

## Work completed in the latest run

The v1.0.19 viewport pipeline was reconciled against older viewport layers. A concrete ownership conflict was found:

- v1.0.19 configured `SubViewportContainer.Stretch = true` and documented the container as the owner of render-target dimensions;
- legacy v1.0.9 and v1.0.17 code still explicitly assigns `SubViewport.Size` on resize and repeatedly during viewport synchronization;
- v1.0.18 processing also calls the v1.0.17 sync path continuously.

This meant two incompatible sizing contracts were active at once and could make the native 3D render surface timing-dependent during startup, tab changes and splitter resizing.

Commit `59757d04...` makes the current transitional architecture deterministic: v1.0.19 now deliberately uses `Stretch = false` and one explicit host-to-SubViewport sizing contract until the legacy sizing handlers are removed as part of MS-019. Diagnostics now show both host and render-target dimensions so a real-machine blank viewport can immediately reveal a size mismatch.

No published release was modified.

## Validation state

The change was pushed to `v1.0.19`, which triggered the normal `build` and `core-foundation` GitHub Actions workflows. At the end of the coding portion of this run, those workflows had started and were still running. The next run must inspect their final conclusions before building on the change. If either fails, diagnose/fix that failure first.

Real GUI/render verification remains impossible in CI and MS-009 must stay open/in-progress until tested on the target Windows/GTX 1080 machine.

## Highest-priority unresolved work

### 1. MS-009 — 3D viewport/grid/model/gizmo reliability

Status remains **IN PROGRESS**. v1.0.19 now has explicit world/camera/material/grid/gizmo enforcement plus a deterministic transitional sizing contract. This is stronger than the previous code but still needs real-machine verification.

### 2. MS-018 — Stage-C end-to-end thin slice not qualified

Primary acceptance gap remains:

`2D → accept durable baseline → one qualified 3D provider → visible/editable mesh → save/reload → cleanup → validated STL`

on GTX 1080 8 GB + 16 GB RAM, including cancellation recovery and contained storage.

### 3. MS-013 — storage containment

v1.0.18/v1.0.19 contain substantial containment work, but representative real-machine operations still need verification for stray C:/AppData/TEMP writes.

### 4. MS-020 / MS-019 — Job Broker and legacy architecture migration

Continue moving one vertical slice at a time onto stable IDs/revisions/project history. The duplicate viewport sizing ownership found in this run is another concrete example of why overlapping version-layer ownership must be retired safely.

### 5. MS-022 — provider qualification

Add provider self-tests/readiness states and target-hardware benchmarks. Prefer one trustworthy default 3D route before adding provider breadth.

## Release policy

Do not mutate published releases. Do not publish v1.0.19 merely because an autonomous run is ending.

Publish v1.0.19 only when the intended batch is coherent and all required gates pass: C#/Python/core/geometry/release audit, real Godot Windows export, artifact/hash verification and installer smoke test. Runtime/UI issues remain `FIXED - NEEDS USER VERIFICATION` until tested on the user's actual machine.

## Exact next action

1. Check the final `build` and `core-foundation` results for code commit `59757d04eeb0f9e19cd7eb8fc812701373e0d03f`.
2. If green, continue MS-009 reconciliation by inspecting remaining viewport/world ownership paths for conflicts with `Main.V1019ViewportPipeline.cs`; avoid another compatibility overlay unless a distinct root cause is proven.
3. Then advance MS-022/Stage-C by adding an early provider readiness/self-test contract for the chosen lightweight 3D route, so dependency/device failures happen before a long generation job.
4. Keep MS-009 and MS-013 awaiting real-machine verification; do not raise the 56% completion estimate from this code-only change.
