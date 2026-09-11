# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.23` at `bda683264448fc8b51c7c538db61f8c0487a699a`.
- **Current writable development branch:** `v1.0.24`.
- **Published v1.0.23 is immutable.** Dev must not mutate v1.0.23 or release-control.
- **Latest validated v1.0.24 implementation/test checkpoint:** `e3dfba9aa26d7045b4bf9602920a484c443789c7`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.23. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

v1.0.23 published successfully after the Coordinator repaired a complete release-identity mismatch across launcher/updater/Godot assembly/installer/Windows export/backend/editor/audit surfaces.

Autonomous release run `34575505010` passed the exact-SHA gates, real Godot Windows export, versioned output/hash verification and silent installer smoke test, then published tag/Release `v1.0.23` targeting exactly `bda683264448fc8b51c7c538db61f8c0487a699a`.

All further application and documentation work belongs on v1.0.24 or later.

## Completed bounded MS-020 seam

The v1.0.24 Job Broker reliability work remains intentionally bounded:

1. heavyweight `3d-generate` work has one process-local runtime owner;
2. component install/update/repair/remove shares that owner;
3. cancellation remains `cancelling` and retains ownership until physical terminal acknowledgement; late success is discarded;
4. a compact lifecycle journal under Miniscuplter-controlled storage records only the minimum heavyweight job tombstone/context;
5. stale running/cancelling state reconciles after backend restart to inactive `interrupted`/`cancelled`, without reacquiring the GPU lease or auto-applying output;
6. corrupt/unsupported journal state fails closed;
7. recovery reads are bounded before decode/parse, including a grow-between-stat-and-read guard.

Validated implementation/test checkpoint: `e3dfba9aa26d7045b4bf9602920a484c443789c7`.

This ordered restart-reconciliation seam is complete. **Do not broaden MS-020 into a generalized persistent queue or isolated-worker architecture without new evidence.**

## Exact next task

1. Re-bootstrap actual release/branch/CI state and re-read ROADMAP / COORDINATOR_LOG.
2. Consume new **released v1.0.23** Stage-C/storage/cancellation evidence immediately; serious reproduced defects preempt everything else.
3. Work only on writable `v1.0.24`; never mutate published `v1.0.23` or `release-control`.
4. Do not continue generalized MS-020 expansion.
5. If target-machine evidence remains unavailable, execute exactly one bounded **MS-019 duplicate-authority retirement seam** at an already-migrated Stage-C boundary.
6. Preferred first seam: remove/delegate one remaining historical Stage-C generation/persistence authority already superseded by the final acceptance owner, but only where regression coverage proves the replacement owner. If that seam is already inert, choose the smallest equivalent duplicate authority in transform/selection/persistence.
7. Do not change user-facing workflow, provider breadth or product scope. Preserve migration compatibility until replacement authority is proven.
8. Stop after one bounded seam, validate strongly, and record a new useful checkpoint.

## Release/checkpoint rule

`e3dfba9aa26d7045b4bf9602920a484c443789c7` remains a useful validated v1.0.24 checkpoint. It is **not** a freeze and does not authorize publication.

Coordinator decision: **v1.0.24 should keep accumulating.** Consider the next release boundary after another coherent authority-retirement increment or meaningful target-machine fixes materially increase release value.

## User verification dependency

Test released v1.0.23:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job.
