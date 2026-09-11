# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Current writable development branch:** `v1.0.24`.
- **Frozen release source:** `v1.0.23`; Dev must not mutate it or `release-control`.
- **Current v1.0.23 release-fix candidate:** `bda683264448fc8b51c7c538db61f8c0487a699a`, pending exact-head branch validation before Coordinator updates release-control.
- **Latest validated v1.0.24 implementation/test checkpoint:** `e3dfba9aa26d7045b4bf9602920a484c443789c7`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

`v1.0.22` remains the latest published release.

The second v1.0.23 autonomous publication attempt failed correctly during strict release audit because version identity was only partially advanced: launcher/installer were 1.0.23 while updater, Godot assembly, Windows export metadata, backend API, editor status and the audit itself still declared 1.0.22.

Coordinator repaired the full audited v1.0.23 identity set without changing application scope. The frozen release source is now `bda683264448fc8b51c7c538db61f8c0487a699a`.

Dev must continue only on v1.0.24. Coordinator owns the remaining v1.0.23 exact-head validation, release-control retry, publication verification and immutable release transition.

## Completed bounded MS-020 seam

The current v1.0.24 Job Broker work remains intentionally bounded:

1. heavyweight `3d-generate` work has one process-local runtime owner;
2. component install/update/repair/remove shares that owner;
3. cancellation remains `cancelling` and retains ownership until physical terminal acknowledgement; late success is discarded;
4. a compact lifecycle journal under Miniscuplter-controlled storage records only the minimum heavyweight job tombstone/context;
5. stale running/cancelling state reconciles after backend restart to inactive `interrupted`/`cancelled`, without reacquiring the GPU lease or auto-applying output;
6. corrupt/unsupported journal state fails closed;
7. recovery reads are bounded before decode/parse, including a grow-between-stat-and-read guard.

Implementation/test checkpoint: `e3dfba9aa26d7045b4bf9602920a484c443789c7`.

This ordered restart-reconciliation seam is complete. **Do not broaden MS-020 into a generalized persistent queue or isolated-worker architecture without new evidence.**

## Validation

Exact v1.0.24 implementation/test checkpoint `e3dfba9aa26d7045b4bf9602920a484c443789c7` is green:

- Core foundation: PASS;
- broader build: PASS;
- semantic-version identity guard: PASS;
- C# editor/launcher/updater/Core: PASS;
- Python/runtime dependency resolution: PASS;
- core logic/execution/job regressions: PASS;
- oversized lifecycle journal recovery regression: PASS;
- geometry regressions: PASS;
- release audit: PASS;
- portable package/hash validation: PASS;
- installer-definition compilation: PASS.

Documentation commits after this checkpoint do not supersede the validated implementation/test checkpoint.

## Exact next task

1. Re-bootstrap actual release/branch/CI state and read current ROADMAP / COORDINATOR_LOG.
2. Consume new reference-machine Stage-C/storage/cancellation evidence immediately; serious reproduced defects preempt everything else.
3. Stay on writable `v1.0.24`; never mutate frozen `v1.0.23` or `release-control`.
4. The ordered MS-020 restart seam is complete. Do not continue generalized Job Broker expansion.
5. If target-machine evidence remains unavailable, execute exactly one bounded **MS-019 duplicate-authority retirement seam** at an already-migrated Stage-C boundary.
6. Preferred first seam: remove/delegate one remaining historical Stage-C generation/persistence authority that is already superseded by the final v1.0.22 acceptance owner, but only where regression coverage proves the replacement owner. If inspection shows that seam is already inert, choose the smallest equivalent duplicate authority in transform/selection/persistence.
7. Do not change user-facing workflow, provider breadth or product scope. Preserve migration compatibility until replacement authority is proven.
8. Stop after one bounded seam, validate strongly, and record a new useful checkpoint.

## Release/checkpoint rule

`e3dfba9aa26d7045b4bf9602920a484c443789c7` remains a useful validated v1.0.24 checkpoint. It is **not** a freeze and does not authorize publication.

Coordinator decision: **v1.0.24 should keep accumulating.** It does not yet contain a sufficiently substantial next release chunk while v1.0.23 publication is still being repaired. Consider the next release boundary only after another coherent authority-retirement increment or important target-machine fixes materially increase release value.

## User verification dependency

Reference-machine testing remains required on the latest published build:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job.
