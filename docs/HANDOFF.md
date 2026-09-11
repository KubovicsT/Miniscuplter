# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.24` at `784f408efba8a876b889fd704e051f22229de368`.
- **Current writable development branch:** `v1.0.25`.
- **Current fully validated implementation checkpoint:** `94554d21519cb06c286a686631d2ad44ca6648b7`.
- **Current documentation HEAD after this handoff:** this commit; inspect Git for its exact SHA before mutation.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.24. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

v1.0.24 is published and immutable. v1.0.25 is the writable forward branch. No release-control request, tag, GitHub Release, or Coordinator freeze was created by Dev.

## Work completed in this run

### Bounded MS-019 Scale authority seam

Coordinator sequencing explicitly approved mapped Scale ±5% as the next bounded fallback seam while reference-machine acceptance remained unavailable.

`Core.Tests/StageCAuthorityRetirementTests.cs` was extended first at `3ad632925e6a6fd512e5dcee44bdf6f1aca2c102` to require mapped scale nudges to derive from durable Core state and to reject regression to the generic scene-observed transform hook.

`94554d21519cb06c286a686631d2ad44ca6648b7` then changes the editor seam:

- mapped `Scale +5%` and `Scale -5%` buttons now use `V1020CommitScaleCommandAsync`;
- requested scale starts from `session.Current.Objects[objectId].Transform.Scale`, not the already-mutated Godot node;
- only the requested uniform factor (`1.05` or `0.95`) is applied; durable position and rotation are preserved;
- Stage-C saves the Core transaction and projects the resulting durable state back to Godot presentation;
- failure restores presentation from the current durable object state;
- ground placement, viewport-drag persistence, selection retirement and broader persistence cleanup deliberately remain unchanged.

This follows the Coordinator rule of one bounded proven MS-019 seam at a time.

## Exact-head validation

Exact-head CI for implementation/test checkpoint `94554d21519cb06c286a686631d2ad44ca6648b7` is green:

- `core-foundation` run `34607736187`: **PASS**;
- `build` run `34607736306`: **PASS**;
- semantic-version branch identity: PASS;
- C# editor/launcher/updater/Core restore/build: PASS;
- backend Python compile/dependency resolution: PASS;
- core logic and execution/job regressions: PASS;
- real geometry regressions: PASS;
- strict release audit: PASS;
- portable package/layout and ZIP SHA-256 sidecar: PASS;
- Inno Setup installer-definition compilation: PASS.

`94554d21...` is therefore a useful fully validated release-worthy checkpoint. It is informational only and does **not** freeze v1.0.25. Documentation commits after it are bookkeeping and do not supersede the validated implementation checkpoint.

## Exact next task

1. Consume any new v1.0.24 reference-machine evidence first; serious Stage-C/runtime/storage/viewport/persistence/cancellation regressions preempt fallback work.
2. The bounded Scale seam is complete; do **not** continue directly into ground placement, viewport-drag persistence, selection retirement or persistence cleanup without current Coordinator sequencing.
3. If the Coordinator orders another MS-019 fallback seam and no target-machine regression exists, take exactly one proven seam with a focused regression and stop after validation.
4. Do not broaden MS-020 or resume opportunistic MS-027 UI work without evidence/Coordinator direction.
5. Do not create release-control, tags or GitHub Releases; publication remains Coordinator-owned.

## User verification dependency

Test released v1.0.24:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job.

No product/design decision is currently required.
