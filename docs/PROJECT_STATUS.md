# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22`.
- **Stable release target:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Frozen release source:** `v1.0.23`, Coordinator-owned; Dev must not mutate it or release-control.
- **Current v1.0.23 release-fix candidate:** `bda683264448fc8b51c7c538db61f8c0487a699a`, awaiting exact-head branch validation before release-control retry.
- **Current writable development branch:** `v1.0.24`.
- **Latest validated v1.0.24 implementation/test checkpoint:** `e3dfba9aa26d7045b4bf9602920a484c443789c7`.
- **Overall completion:** **57% acceptance-weighted**.

## Release state

v1.0.23 publication has not succeeded, so v1.0.22 remains latest stable.

The previous v1.0.23 retry failed during strict release audit because release identity was only partially advanced. Launcher and installer declared 1.0.23, but updater, Godot assembly, Windows export metadata, backend API, editor status and the audit expectation still declared 1.0.22. The gate correctly blocked publication before the Windows export phase.

Coordinator has repaired the complete audited v1.0.23 identity set to 1.0.23 without broadening application scope. Exact-head branch CI at `bda683264448fc8b51c7c538db61f8c0487a699a` must finish green before the existing release-control request is advanced to this candidate.

Dev continues forward only on v1.0.24.

## Current v1.0.24 progress — bounded MS-020 reliability seam complete

With Stage-C acceptance still externally blocked, v1.0.24 now has:

- one explicit heavyweight runtime owner for 3D generation;
- shared ownership across component install/update/repair/remove;
- truthful cancellation retaining ownership until terminal acknowledgement and discarding late success;
- minimum durable heavyweight-job lifecycle/tombstone state under controlled storage;
- atomic lifecycle persistence with compact whitelisted Stage-C context;
- startup reconciliation of prior-process running/cancelling work to inactive interrupted/cancelled state;
- no GPU-lease reacquisition and no stale candidate auto-apply after restart;
- fail-closed corrupt/unsupported lifecycle state;
- bounded recovery reads before decode/parse, including an oversized/growing-file regression.

The validated implementation/test checkpoint is `e3dfba9aa26d7045b4bf9602920a484c443789c7`. This seam is intentionally bounded; it is not a generalized persistent queue or isolated-provider-worker architecture.

## Validation

Exact v1.0.24 checkpoint `e3dfba9aa26d7045b4bf9602920a484c443789c7` is green across Core, broader C# build, semantic-version identity guard, Python/runtime, execution/job regressions, geometry regressions, release audit, package/hash validation and installer-definition compilation. Full release/publication jobs correctly skipped on the ordinary development push.

## Critical path

Stage-C reference-machine acceptance remains P0:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence preempts fallback work immediately.

## Next execution direction

The ordered MS-020 restart-reconciliation seam is complete. Do not broaden it speculatively.

If no new target-machine evidence arrives, the next bounded fallback is **one MS-019 duplicate-authority retirement seam** at an already-migrated Stage-C boundary. Prefer removing/delegating a historical Stage-C generation/persistence owner already superseded by the final acceptance owner, but only where regression coverage proves replacement authority. If that seam is already fully inert, choose the smallest equivalent duplicate authority in transform/selection/persistence.

Stop after one bounded seam, validate strongly, and record another useful checkpoint.

## Release chunk decision

**v1.0.24 should keep accumulating.** The current MS-020 seam is useful and coherent but not yet a sufficiently substantial next release chunk while v1.0.23 publication is still being repaired. A checkpoint is not a freeze.

## User dependency

No product/design decision is required. Reference-machine verification remains the external dependency on the latest published build. Autonomous development can continue on v1.0.24.
