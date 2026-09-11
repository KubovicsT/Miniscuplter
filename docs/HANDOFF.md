# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.24` at `784f408efba8a876b889fd704e051f22229de368`.
- **Current writable development branch:** `v1.0.25`.
- **Current fully validated implementation checkpoint:** `c6d129ed78a991b809783cf3457a7524029fd95e`.
- **Latest bounded MS-019 implementation checkpoint:** `e0666bbd7e56d340f112238f78c836b958a64675` (Rotate Y ±5° authority retirement; exact-head CI still running when handed off).
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

## Validation state

Previous fully validated checkpoint `c6d129ed78a991b809783cf3457a7524029fd95e` remains green across Core/C#, Python/runtime, execution/job, geometry, release audit, packaging/hash and installer-definition gates.

Exact-head CI for Rotate implementation `e0666bbd7e56d340f112238f78c836b958a64675` started normally:

- `core-foundation` run `34602202639`: **IN PROGRESS** when last checked;
- `build` run `34602202701`: **IN PROGRESS** when last checked;
- within build, semantic-version branch identity, backend source compilation/dependency resolution, core logic tests and execution-foundation tests had already passed;
- remaining C#/Core build, geometry, release-audit, packaging/hash and installer steps were still executing.

Therefore `e0666bbd...` is **not yet recorded as release-worthy**. It becomes a useful validated checkpoint only after exact-head CI is green. Do not confuse the documentation commits after it with a separately validated implementation checkpoint.

## Exact next task

1. Consume any new v1.0.24 reference-machine evidence first; serious Stage-C/runtime/storage/viewport/persistence/cancellation regressions preempt fallback work.
2. Reconcile runs `34602202639` and `34602202701` for implementation SHA `e0666bbd7e56d340f112238f78c836b958a64675`.
3. If either fails, diagnose and fix only defects attributable to this Rotate seam, then rerun exact-head validation.
4. If both pass, update HANDOFF/PROJECT_STATUS to mark `e0666bbd...` fully validated/release-worthy and stop this seam there; do **not** roll directly into Scale/ground/viewport-drag authority without current Coordinator sequencing.
5. Do not broaden MS-020 or resume opportunistic MS-027 UI work without evidence/Coordinator direction.
6. Do not create release-control, tags or GitHub Releases; publication remains Coordinator-owned.

## User verification dependency

Test released v1.0.24:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job.

No product/design decision is currently required.
