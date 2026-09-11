# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.23`.
- **Stable release target:** `bda683264448fc8b51c7c538db61f8c0487a699a`.
- **Current writable development branch:** `v1.0.24`.
- **Latest validated v1.0.24 implementation/test checkpoint:** `e3dfba9aa26d7045b4bf9602920a484c443789c7`.
- **Overall completion:** **57% acceptance-weighted**.

v1.0.23 is published and immutable. All further changes belong on v1.0.24 or later.

## v1.0.23 publication outcome

The final Coordinator repair aligned all audited release identity surfaces to 1.0.23. Autonomous release run `34575505010` then passed:

- exact request/source SHA and immutability validation;
- C# editor/launcher/updater/Core build and Core tests;
- Python/runtime and job regressions;
- geometry regressions and strict release audit;
- verified Godot 4.7.2 Windows export;
- versioned package/ZIP/hash verification;
- `Miniscuplter-Setup-1.0.23.exe` silent installer smoke test;
- immutable target recheck;
- tag and GitHub Release publication.

Published tag/Release target exactly `bda683264448fc8b51c7c538db61f8c0487a699a`.

The earlier failed retries remain useful release-safety evidence: semantic version identity must stay synchronized across launcher, updater, Godot assembly, installer, Windows export metadata, backend, editor display and release audit.

## Current v1.0.24 progress — bounded MS-020 reliability seam complete

With Stage-C acceptance externally blocked, v1.0.24 now has:

- one explicit heavyweight runtime owner for 3D generation;
- shared ownership across component install/update/repair/remove;
- truthful cancellation retaining ownership until terminal acknowledgement and discarding late success;
- minimum durable heavyweight-job lifecycle/tombstone state under controlled storage;
- startup reconciliation of prior-process running/cancelling work to inactive interrupted/cancelled state;
- no GPU-lease reacquisition and no stale candidate auto-apply after restart;
- fail-closed corrupt/unsupported lifecycle state;
- bounded recovery reads before decode/parse, including oversized/growing-file regression coverage.

Validated checkpoint: `e3dfba9aa26d7045b4bf9602920a484c443789c7`. This seam is intentionally bounded, not a generalized persistent queue or isolated-provider-worker architecture.

## Critical path

Stage-C reference-machine acceptance remains P0 on **released v1.0.23**:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence immediately preempts fallback work.

## Next execution direction

Do not broaden MS-020 speculatively.

If no new target-machine evidence arrives, the next bounded fallback is **one MS-019 duplicate-authority retirement seam** at an already-migrated Stage-C boundary. Prefer removing/delegating a historical Stage-C generation/persistence owner already superseded by the final acceptance owner, but only where regression coverage proves replacement authority. If that seam is already inert, choose the smallest equivalent duplicate authority in transform/selection/persistence.

Stop after one bounded seam, validate strongly, and record another useful checkpoint.

## Release chunk decision

**v1.0.24 should keep accumulating.** The current MS-020 seam is useful and coherent but not yet a sufficiently substantial next release chunk by itself. A checkpoint is not a freeze.

## User dependency

No product/design decision is required. Reference-machine verification of released v1.0.23 remains the external dependency. Autonomous development can continue on v1.0.24.
