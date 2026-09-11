# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.24` at `784f408efba8a876b889fd704e051f22229de368`.
- **Current writable development branch:** `v1.0.25`.
- **Current validated implementation checkpoint:** `c6d129ed78a991b809783cf3457a7524029fd95e`.
- **Current bounded MS-019 transform seam implementation:** `ec54845c19ed632640f813d1ee76ce2708638389`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.24. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

v1.0.24 is published and immutable. v1.0.25 is the writable forward branch. Dev created no release-control request, tag, or GitHub Release.

## v1.0.25 work completed in this baton

### Bounded MS-019 Move authority seam

`ec54845c19ed632640f813d1ee76ce2708638389` narrows duplicate transform authority for the four mapped 1 mm Move buttons:

- mapped Stage-C objects derive their durable transform from Core project state, not an already-mutated Godot scene node;
- the command delta is applied to `session.Current.Objects[objectId].Transform.Position` under the existing Stage-C gate;
- durable Core state is saved first and then projected back to Godot presentation;
- legacy scene mutation remains available only for unmapped/pre-migration objects;
- focused `StageCAuthorityRetirementTests` wiring guards against mapped Move commands regressing to generic scene-observed persistence.

Rotate/Scale/ground/viewport-drag authority was deliberately not broadened.

### v1.0.25 branch identity repair

The branch-bootstrap semantic-version defect is fixed at `c6d129ed78a991b809783cf3457a7524029fd95e`. All audited user/tool-visible identity surfaces now agree on `1.0.25`: launcher, updater, Godot assembly, installer, Windows export metadata, backend API, editor display and strict release audit. Published v1.0.24 was not changed.

## Exact-head validation

Exact-head CI for `c6d129ed78a991b809783cf3457a7524029fd95e` is green:

- `core-foundation` run `34597074576`: PASS;
- `build` run `34597074521`: PASS;
- semantic-version branch identity: PASS;
- C# editor/launcher/updater/Core restore/build: PASS;
- backend Python compile/dependency resolution: PASS;
- core logic and execution/job regressions: PASS;
- real geometry regressions: PASS;
- strict release audit: PASS;
- portable package/layout and ZIP SHA-256 sidecar: PASS;
- Inno Setup installer-definition compilation: PASS;
- release/publication jobs correctly skipped for the development-branch push.

This is a useful fully validated checkpoint. It is informational only and does **not** freeze v1.0.25.

## Exact next task

1. Consume any v1.0.24 reference-machine evidence first; serious Stage-C/runtime/storage/viewport/persistence/cancellation regressions preempt fallback work.
2. Do not broaden the completed Move-authority seam speculatively. The Coordinator ordered one proven duplicate-authority seam at a time; wait for current Coordinator sequencing before selecting Rotate/Scale/selection/persistence as another seam.
3. Do not broaden MS-020 or resume opportunistic MS-027 UI expansion without evidence/Coordinator direction.
4. Continue ordinary implementation immediately if Coordinator sequencing identifies another bounded v1.0.25 task; this checkpoint itself is not a development stop or release freeze.
5. Do not create release-control, tags, or GitHub Releases; publication remains Coordinator-owned.

## User verification dependency

Test released v1.0.24:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job.

No product/design decision is currently required.
