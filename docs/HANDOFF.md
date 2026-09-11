# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.24` at `784f408efba8a876b889fd704e051f22229de368`.
- **Current writable development branch:** `v1.0.25`.
- **Current fully validated implementation checkpoint:** `e0666bbd7e56d340f112238f78c836b958a64675`.
- **Current documentation HEAD after this handoff:** this commit; inspect Git for its exact SHA before mutation.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.24. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

v1.0.24 is published and immutable. v1.0.25 is the writable forward branch. Dev created no release-control request, tag, GitHub Release, or release-control file.

## Work completed in this run

### Bounded MS-019 Rotate authority seam

The next proven transform seam was limited to the two mapped Rotate Y ±5° commands.

`Core.Tests/StageCAuthorityRetirementTests.cs` was extended first at `9cd1f139d170fdce8255a6e64e56ee0bf950faf7` to require mapped rotate nudges to derive from durable Core state and to reject regression to the generic scene-observed transform hook.

`e0666bbd7e56d340f112238f78c836b958a64675` then changes the editor seam:

- mapped Rotate Y +5° / -5° buttons now use `V1020CommitRotateCommandAsync`;
- the requested transform starts from `session.Current.Objects[objectId].Transform.RotationEuler`, not from the already-mutated Godot node;
- only the requested Y-axis delta is applied; durable position and scale are preserved;
- Stage-C saves the Core transaction and then projects the resulting durable state back to Godot presentation;
- failure restores presentation from the current durable object state;
- Scale ±5%, Place on Y=0, and viewport-drag transform persistence deliberately remain unchanged for later sequencing.

This follows the Coordinator rule of one bounded proven MS-019 seam at a time.

## Exact-head validation

Exact-head CI for implementation/test checkpoint `e0666bbd7e56d340f112238f78c836b958a64675` is green:

- `core-foundation` run `34602202639`: **PASS**;
- `build` run `34602202701`: **PASS**;
- semantic-version branch identity: PASS;
- C# editor/launcher/updater/Core restore/build: PASS;
- backend Python compile/dependency resolution: PASS;
- core logic and execution/job regressions: PASS;
- real geometry regressions: PASS;
- strict release audit: PASS;
- portable package/layout and ZIP SHA-256 sidecar: PASS;
- Inno Setup installer-definition compilation: PASS.

`e0666bbd...` is therefore a useful fully validated release-worthy checkpoint. It is informational only and does **not** freeze v1.0.25. Documentation commits after it are bookkeeping and do not supersede the validated implementation checkpoint.

## Exact next task

1. Consume any new v1.0.24 reference-machine evidence first; serious Stage-C/runtime/storage/viewport/persistence/cancellation regressions preempt fallback work.
2. The bounded Rotate seam is complete; do **not** continue directly into Scale/ground/viewport-drag/selection/persistence authority without current Coordinator sequencing.
3. If the Coordinator orders another MS-019 fallback seam and no target-machine regression exists, take exactly one proven seam with a focused regression and stop after validation.
4. Do not broaden MS-020 or resume opportunistic MS-027 UI work without evidence/Coordinator direction.
5. Do not create release-control, tags or GitHub Releases; publication remains Coordinator-owned.

## User verification dependency

Test released v1.0.24:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job.

No product/design decision is currently required.
