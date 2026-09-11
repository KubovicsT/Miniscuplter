# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.24` at `784f408efba8a876b889fd704e051f22229de368`.
- **Current writable development branch:** `v1.0.25`.
- **v1.0.25 branch bootstrap HEAD before Dev work:** `04f6b1fde65fd59e164dfe36e2666fe07babd2ae`.
- **Current bounded MS-019 implementation commit:** `ec54845c19ed632640f813d1ee76ce2708638389`.
- **Validation state:** source/diff review is clean, but exact-head CI is **not green** because the new `v1.0.25` branch inherited `1.0.24` identity across release/version surfaces. Build run `34592546736` fails at the semantic-version identity gate before Python/regression/release-audit steps; this is a branch-bootstrap defect, not evidence that the MS-019 move-authority change failed.
- **Latest fully validated implementation checkpoint:** released v1.0.24 boundary `784f408efba8a876b889fd704e051f22229de368` (the preceding v1.0.24 MS-019 checkpoint was `d7a72ce112ff9827f3ee314e281e7e894d2dff4c`).
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on the latest published build, now v1.0.24. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

v1.0.24 is published and immutable. Do not mutate it. v1.0.25 was created from the exact v1.0.24 release boundary and is the writable forward branch. No release-control request or publication action was created by Dev.

## Completed v1.0.24 release chunk

### MS-020 bounded reliability seam

- one heavyweight runtime owner;
- shared ownership across model/runtime mutation operations;
- cancellation retains ownership until physical terminal acknowledgement;
- compact durable heavyweight-job lifecycle/tombstone state;
- startup reconciliation of abandoned running/cancelling work;
- fail-closed corrupt/unsupported journal behavior;
- bounded journal reads before decode/parse.

Do not broaden this into a generalized persistent queue without acceptance evidence.

### MS-019 generation authority-retirement seam

The historical `V109Generate3DAsync` owner delegates to canonical `V1020Generate3DAsync`. Focused regression coverage prevents the old layer from regaining provider-routing/direct-scene-import authority and proves candidate/Apply ownership remains canonical. Strict release audit follows the migrated authority without weakening its checks.

## Current v1.0.25 bounded MS-019 seam

`ec54845c19ed632640f813d1ee76ce2708638389` narrows duplicate transform authority for the four 1 mm Move buttons:

- mapped Stage-C objects no longer derive the durable move transaction from the already-mutated Godot node;
- the command delta is applied to `session.Current.Objects[objectId].Transform.Position` under the existing Stage-C gate;
- the durable Core transform is saved first and projected back to Godot presentation;
- legacy scene mutation remains temporarily available for unmapped/pre-migration objects;
- focused `StageCAuthorityRetirementTests` source wiring prevents these mapped move nudges from regressing to the generic scene-observed persistence hook.

This is intentionally one bounded transform seam. Rotate/Scale/ground/viewport-drag authority was not broadened in this run.

## Immediate blocker before validating further v1.0.25 work

The semantic-version branch identity gate correctly detected that branch `v1.0.25` still contains `1.0.24` identity in all audited surfaces: launcher, updater, editor assembly, installer, Windows export metadata, backend API, editor display, and release audit. The next Dev action is to bump those surfaces forward to `1.0.25` on v1.0.25, then rerun/consume exact-head CI. Do **not** weaken or bypass the gate.

The MS-019 implementation commit must not be called release-worthy until that exact forward identity repair is complete and the normal C#/Core/Python/execution/geometry/release-audit/package gates pass.

## Exact next task on v1.0.25

1. Reconcile all audited version-identity surfaces from `1.0.24` to `1.0.25` on the forward branch only; preserve published v1.0.24 unchanged.
2. Run/consume exact-head CI. If the MS-019 move-authority seam exposes an attributable compile/test/audit defect, fix it before doing new feature work.
3. Consume any v1.0.24 reference-machine evidence immediately; serious Stage-C/runtime/storage/viewport/persistence/cancellation regressions preempt fallback work.
4. If CI is green and acceptance remains unavailable, stop at this one MS-019 seam unless Coordinator explicitly sequences another bounded authority-retirement slice.
5. Do not resume opportunistic MS-027 UI expansion.
6. Do not create release-control, tags, or GitHub Releases; publication remains Coordinator-owned.

## User verification dependency

Test released v1.0.24:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job.

No product/design decision is currently required.