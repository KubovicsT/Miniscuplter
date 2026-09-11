# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22`.
- **Stable release target:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Frozen release source:** `v1.0.23`, Coordinator-owned; Dev must not mutate it or release-control.
- **Current writable development branch:** `v1.0.24`.
- **Latest validated v1.0.24 implementation/test checkpoint:** `e3dfba9aa26d7045b4bf9602920a484c443789c7`.
- **Overall completion:** **57% acceptance-weighted**.

## Release state

v1.0.23 publication has not succeeded; v1.0.22 remains the latest stable build. Dev continues forward only on v1.0.24 and records useful validated checkpoints without initiating publication.

## Current v1.0.24 progress — bounded MS-020 reliability seam

With Stage-C acceptance still externally blocked, the Coordinator-approved v1.0.24 work has established:

- one explicit heavyweight runtime owner for 3D generation;
- shared ownership across component install/update/repair/remove;
- truthful cancellation that retains ownership until physical terminal acknowledgement and discards late success;
- minimum durable heavyweight-job lifecycle/tombstone state under Miniscuplter-controlled storage;
- atomic lifecycle persistence with compact whitelisted Stage-C identity context only;
- startup reconciliation of prior-process running/cancelling work to inactive interrupted/cancelled state;
- no GPU-lease reacquisition and no stale candidate auto-apply on restart;
- fail-closed corrupt/unsupported lifecycle state.

This remains a bounded seam, not a generalized persistent queue or isolated-provider-worker architecture.

### Oversized journal fail-closed hardening

Validated checkpoint `e3dfba9aa26d7045b4bf9602920a484c443789c7` closes a concrete safety gap in the restart seam. The lifecycle journal intended to cap persisted state at 256 KiB, but the prior loader read the whole file before checking size. Recovery now rejects an obviously oversized file before opening and, critically, caps the actual binary read to `limit + 1` bytes before UTF-8 decode/JSON parsing. This keeps startup recovery memory-bounded even if the file changes between size check and read.

A focused regression writes an oversized managed journal and verifies recovery becomes inactive `recovery-error`, reports the size violation, and never claims heavyweight ownership. This work reuses MS-020 and does not broaden architecture.

## Release-version identity hardening

The branch-derived semantic-version CI guard remains active on `v1.*` development branches and verifies launcher, updater, editor assembly, installer, Windows file/product metadata, backend API, displayed editor version and release audit agree with the semantic-version branch name. Version drift is treated as a release-safety blocker rather than deferred until publication.

## Validation

Exact implementation/test checkpoint `e3dfba9aa26d7045b4bf9602920a484c443789c7` is green:

- exact-head `core-foundation`: **PASS**;
- broader `build` run `34572788267`: **PASS**;
- branch-derived semantic-version identity guard: **PASS**;
- C#/Core builds: **PASS**;
- Python compilation/runtime dependency resolution: **PASS**;
- core logic and execution/job regressions: **PASS**;
- oversized lifecycle journal recovery regression: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package layout/hash: **PASS**;
- installer-definition compilation: **PASS**;
- full Windows release/publication jobs correctly skipped on the ordinary development push.

This is a useful implementation checkpoint, not a release freeze. Dev did not initiate publication.

## Critical path

Stage-C reference-machine acceptance remains P0:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence preempts further fallback work immediately.

## Next execution rule

The explicitly ordered MS-020 restart-reconciliation seam is complete. Dev should first consume new reference-machine evidence and any newer Coordinator sequencing. Without either, autonomous work should be limited to concrete correctness/regression hardening within already-approved Stage-C seams rather than speculative broker expansion.

## User dependency

No product/design decision is required. Reference-machine verification remains the external dependency on latest published `v1.0.22`. The v1.0.24 job/restart hardening remains development-only until a Coordinator-owned release reaches the user.
