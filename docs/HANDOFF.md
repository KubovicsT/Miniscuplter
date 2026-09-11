# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.23` at `bda683264448fc8b51c7c538db61f8c0487a699a`.
- **Current writable development branch:** `v1.0.24`.
- **Published v1.0.23 is immutable.** Dev must not mutate v1.0.23 or release-control.
- **Latest fully validated v1.0.24 checkpoint:** `e3dfba9aa26d7045b4bf9602920a484c443789c7`.
- **Latest bounded MS-019 implementation/test commit:** `0e8aed592b75f714dd64482f65ddf701ce209a0f`; exact-head Actions validation remains pending because no push workflow was created for the connector-originated commits during this run.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.23. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

v1.0.23 published successfully through autonomous release run `34575505010`, targeting exactly `bda683264448fc8b51c7c538db61f8c0487a699a`. All further application/documentation work belongs on v1.0.24 or later.

Coordinator decision remains: **v1.0.24 should keep accumulating**. A checkpoint is informational and does not freeze development; Dev does not initiate publication.

## Completed bounded MS-020 seam

The ordered v1.0.24 Job Broker reliability work remains intentionally bounded: heavyweight runtime ownership, shared component-operation ownership, truthful cancellation, minimal lifecycle tombstone persistence, restart reconciliation, fail-closed corrupt state, and bounded recovery reads. Fully validated checkpoint: `e3dfba9aa26d7045b4bf9602920a484c443789c7`.

Do **not** broaden MS-020 into a generalized persistent queue or isolated-worker architecture without new evidence/Coordinator direction.

## Completed bounded MS-019 seam this run

Coordinator ordered exactly one duplicate-authority retirement seam at an already-migrated Stage-C boundary. The historical v1.0.9 3D generation entry point was the smallest concrete duplicate owner:

- before: `V109Generate3DAsync` independently chose an image, executed `_ai.Generate3DRoutedAsync`, validated STL and directly inserted the mesh into the scene;
- canonical replacement already existed in `V1020Generate3DAsync`, which binds the accepted immutable image revision, carries Stage-C job/project/object identity, persists the generated immutable candidate revision and requires explicit Apply/Discard;
- now: `V109Generate3DAsync` is compatibility-only and immediately delegates to `V1020Generate3DAsync`;
- migration compatibility is preserved so historical composition/unsubscribe hooks still compile and behave safely;
- new `Core.Tests/StageCAuthorityRetirementTests.cs` guards that the old layer cannot regain `Generate3DRoutedAsync` authority and that the canonical Stage-C binding/transport/register/apply owner remains present.

Code commit: `23d652abe50f1bb8faa28410582d2acac8e29512`.
Focused regression commit: `0e8aed592b75f714dd64482f65ddf701ce209a0f`.

Review of the two-commit diff from `d9dfe02...` shows only `Main.V109Experience.cs` (-29/+3) plus the focused regression (+39). The resulting legacy method was re-read from branch and confirmed to be a one-line Stage-C delegate.

## Validation status

- Static source/diff review: PASS.
- Focused regression source added to the Core.Tests module-initializer suite: PRESENT.
- Exact-head GitHub Actions: **NOT RUN / NOT CLAIMED** for `0e8aed592...`; the contents-API commits did not produce a branch push workflow in this run despite `build.yml` being configured for `v*` pushes.
- Therefore `0e8aed592...` is an implementation/test checkpoint, **not yet promoted to the latest fully validated/release-worthy checkpoint**.
- The previous fully validated checkpoint remains `e3dfba9...` until normal exact-head CI executes.

Do not weaken release gates or claim C#/Core/Python/package validation that did not execute.

## Exact next task

1. Re-bootstrap actual branch/release/Coordinator state and consume any released-v1.0.23 user evidence first.
2. Check whether exact-head CI has subsequently run for the MS-019 commit/docs descendants; if so, inspect failures and fix only defects attributable to this bounded seam.
3. Do **not** start a second MS-019 seam solely because the critical path remains externally blocked. The Coordinator explicitly ordered one bounded seam and this run completed it.
4. Follow the next Coordinator/HANDOFF sequencing decision. If a serious reproduced Stage-C/runtime/storage/viewport regression arrives, it preempts fallback work immediately.
5. Never mutate published v1.0.23 or release-control.

## User verification dependency

Test released v1.0.23:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job.

No product/design decision is currently required.