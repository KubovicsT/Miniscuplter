# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.23` at `bda683264448fc8b51c7c538db61f8c0487a699a`.
- **Current writable development branch:** `v1.0.24`.
- **Published v1.0.23 is immutable.** Dev must not mutate v1.0.23 or release-control.
- **Latest fully validated v1.0.24 implementation/test checkpoint:** `d7a72ce112ff9827f3ee314e281e7e894d2dff4c`.
- **Bounded MS-019 authority-retirement commit:** `0e8aed592b75f714dd64482f65ddf701ce209a0f`.
- **Attributable release-audit repair / validated checkpoint:** `d7a72ce112ff9827f3ee314e281e7e894d2dff4c`.
- **Project-status reconciliation commit:** `45bcf4f07aca82196590e78ddc84a84f5b2123bc`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.23. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

v1.0.23 published successfully through autonomous release run `34575505010`, targeting exactly `bda683264448fc8b51c7c538db61f8c0487a699a`. All further application/documentation work belongs on v1.0.24 or later.

Coordinator decision remains: **v1.0.24 should keep accumulating**. A checkpoint is informational and does not freeze development; Dev does not initiate publication.

No release-control request, tag, or publication action was created by Dev in this run.

## Completed bounded MS-020 seam

The ordered v1.0.24 Job Broker reliability work remains intentionally bounded: heavyweight runtime ownership, shared component-operation ownership, truthful cancellation, minimal lifecycle tombstone persistence, restart reconciliation, fail-closed corrupt state, and bounded recovery reads. Original fully validated checkpoint: `e3dfba9aa26d7045b4bf9602920a484c443789c7`.

Do **not** broaden MS-020 into a generalized persistent queue or isolated-worker architecture without new evidence/Coordinator direction.

## Completed bounded MS-019 seam

Coordinator ordered exactly one duplicate-authority retirement seam at an already-migrated Stage-C boundary. The historical v1.0.9 3D generation entry point was the smallest concrete duplicate owner:

- before: `V109Generate3DAsync` independently chose an image, executed `_ai.Generate3DRoutedAsync`, validated STL and directly inserted the mesh into the scene;
- canonical replacement already existed in `V1020Generate3DAsync`, which binds the accepted immutable image revision, carries Stage-C job/project/object identity, persists the generated immutable candidate revision and requires explicit Apply/Discard;
- now: `V109Generate3DAsync` is compatibility-only and immediately delegates to `V1020Generate3DAsync`;
- migration compatibility is preserved so historical composition/unsubscribe hooks still compile and behave safely;
- `Core.Tests/StageCAuthorityRetirementTests.cs` guards that the old layer cannot regain `Generate3DRoutedAsync` authority and that the canonical Stage-C binding/transport/register/apply owner remains present.

Code commit: `23d652abe50f1bb8faa28410582d2acac8e29512`.
Focused regression commit: `0e8aed592b75f714dd64482f65ddf701ce209a0f`.

## Exact-head CI defect found and repaired this run

Exact-head Actions did run for the MS-019/docs descendant and exposed a seam-attributable release-audit defect. The strict audit still assumed the retired v1.0.9 method owned provider routing, STL validation and direct scene import, so build run `34577544261` failed only the release-audit step with stale expectations (`Validating generated STL`, `Importing mesh into scene`, `Generate3DRoutedAsync` in the legacy file).

Repair commit `d7a72ce112ff9827f3ee314e281e7e894d2dff4c` updates the audit to follow actual authority without weakening the gate:

- v1.0.9 must still expose compatibility status/progress/cancel UI;
- the legacy method must delegate to `V1020Generate3DAsync` and must not contain `Generate3DRoutedAsync`;
- cancellation continuity across the compatibility/canonical seam remains required;
- canonical Stage-C must contain bound generation identity, `Generate3DStageCAsync`, STL validation, immutable candidate registration, explicit review/Apply, `StageCGeneration.ApplyCandidate`, and scene insertion only from the Apply path.

This preserves the reason for the audit while removing a requirement that contradicted the completed authority migration.

## Validation status

Exact-head checkpoint `d7a72ce112ff9827f3ee314e281e7e894d2dff4c` is fully green:

- `core-foundation` run `34582114598`: PASS;
- `build` run `34582114592`: PASS;
- semantic-version branch identity: PASS;
- Python source compile and dependency resolution: PASS;
- core logic + execution/job regressions: PASS;
- real geometry regressions: PASS;
- strict release audit: PASS;
- C# editor/launcher/updater/Core restore/build: PASS;
- portable package/layout: PASS;
- ZIP SHA-256 sidecar: PASS;
- Inno installer-definition compilation: PASS;
- `full-windows-release` / `publish-release`: correctly SKIPPED on development-branch push.

Therefore `d7a72ce...` supersedes `e3dfba9...` as the latest fully validated/release-worthy informational checkpoint. It does **not** freeze v1.0.24.

## Exact next task

1. Re-bootstrap actual branch/release/Coordinator state and consume any released-v1.0.23 user evidence first.
2. Follow the next Coordinator sequencing decision. The explicitly ordered one-seam MS-019 fallback is complete and validated, including its attributable audit repair.
3. Do **not** start a second MS-019 seam, broaden MS-020, or resume opportunistic MS-027 work without Coordinator direction merely because target-machine acceptance is still external.
4. If a serious reproduced Stage-C/runtime/storage/viewport/persistence/cancellation regression arrives, it preempts fallback work immediately.
5. Never mutate published v1.0.23 or release-control.

## User verification dependency

Test released v1.0.23:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job.

No product/design decision is currently required.