# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.23`.
- **Stable release target:** `bda683264448fc8b51c7c538db61f8c0487a699a`.
- **Current writable development branch:** `v1.0.24`.
- **Latest fully validated v1.0.24 implementation/test checkpoint:** `d7a72ce112ff9827f3ee314e281e7e894d2dff4c`.
- **Bounded MS-019 implementation/test seam:** `0e8aed592b75f714dd64482f65ddf701ce209a0f`; migration-aware release-audit repair/validation checkpoint: `d7a72ce112ff9827f3ee314e281e7e894d2dff4c`.
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

With Stage-C acceptance externally blocked, v1.0.24 has:

- one explicit heavyweight runtime owner for 3D generation;
- shared ownership across component install/update/repair/remove;
- truthful cancellation retaining ownership until terminal acknowledgement and discarding late success;
- minimum durable heavyweight-job lifecycle/tombstone state under controlled storage;
- startup reconciliation of prior-process running/cancelling work to inactive interrupted/cancelled state;
- no GPU-lease reacquisition and no stale candidate auto-apply after restart;
- fail-closed corrupt/unsupported lifecycle state;
- bounded recovery reads before decode/parse, including oversized/growing-file regression coverage.

The original MS-020 checkpoint was `e3dfba9aa26d7045b4bf9602920a484c443789c7`. This seam is intentionally bounded, not a generalized persistent queue or isolated-provider-worker architecture.

## Current v1.0.24 progress — bounded MS-019 authority retirement

The Coordinator-ordered fallback seam is implemented at the already-migrated Stage-C generation boundary:

- historical `V109Generate3DAsync` no longer owns provider execution, STL validation, or direct scene insertion;
- that compatibility entry point now immediately delegates to canonical `V1020Generate3DAsync`;
- canonical Stage-C generation continues to own durable baseline binding, end-to-end generation identity, immutable candidate registration, explicit Apply/Discard, and persistence;
- focused `StageCAuthorityRetirementTests` source wiring guards prevent `Generate3DRoutedAsync` from returning to the historical v1.0.9 layer and require the Stage-C binding/transport/candidate/apply owner to remain present.

Implementation/test commit: `0e8aed592b75f714dd64482f65ddf701ce209a0f` (code change began at `23d652abe50f1bb8faa28410582d2acac8e29512`).

Exact-head CI subsequently exposed one defect attributable to this migration: the strict release audit still required provider execution/validation/direct-import tokens to remain in the retired v1.0.9 owner. `d7a72ce112ff9827f3ee314e281e7e894d2dff4c` repairs the audit without weakening it: compatibility UI/cancellation remain checked in v1.0.9, while generation transport, STL validation, immutable candidate persistence, explicit review/Apply and scene insertion through Apply are now required from the canonical Stage-C owner. It also explicitly fails if `Generate3DRoutedAsync` returns to the legacy layer.

Exact-head validation for `d7a72ce...` is green:

- `core-foundation` run `34582114598`: PASS;
- `build` run `34582114592`: PASS;
- semantic-version identity, Python compile/dependency resolution, core logic, execution/job and real geometry regressions: PASS;
- strict release audit: PASS;
- C# editor/launcher/updater/Core restore/build: PASS;
- portable package/layout, ZIP SHA-256 and installer-definition compilation: PASS;
- release/publication jobs correctly skipped because this is a development-branch push.

This is exactly one bounded MS-019 seam plus its attributable validation repair; do not expand into broad legacy removal without Coordinator sequencing or new evidence.

## Critical path

Stage-C reference-machine acceptance remains P0 on **released v1.0.23**:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence immediately preempts fallback work.

## Next execution direction

Do not broaden MS-020 or MS-019 speculatively. The specifically ordered authority-retirement seam and its exact-head validation repair are complete and fully validated. Await the next Coordinator sequencing decision while continuing to consume any new v1.0.23 target-machine evidence first.

If a serious reproduced Stage-C/runtime/storage/viewport regression arrives, it preempts fallback work immediately.

## Release chunk decision

**v1.0.24 should keep accumulating.** The current work is a useful validated checkpoint, not a freeze, and Dev does not own publication.

## User dependency

No product/design decision is required. Reference-machine verification of released v1.0.23 remains the external dependency. Autonomous development continues under Coordinator sequencing.