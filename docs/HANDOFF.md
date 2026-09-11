# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Current fully validated implementation checkpoint:** `d4aef5d55170ef45298e9db942cb250d78e911d2`.
- **Status bookkeeping through:** `077044932cc85c27c6ac0673280f14dde18fd85b`; inspect Git for this HANDOFF commit's exact SHA before mutation.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.25. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

v1.0.25 has been successfully published from the Coordinator-frozen exact boundary `a7fc4bcf5f771c18060e5aee7c98026131731c2a` and is immutable. v1.0.26 remains the writable forward development branch. Dev did not create release-control, tags or GitHub Releases.

## Work completed in this run

### v1.0.26 identity bootstrap

The new forward branch initially retained stale 1.0.25 identity on several audited surfaces. Dev reconciled all remaining v1.0.26 identity surfaces forward-only on v1.0.26: updater, installer, exported Windows metadata, editor display version, backend version and strict release-audit expectation. The semantic-version branch gate now passes.

### Bounded MS-019 viewport-drag transform authority seam

Coordinator sequencing explicitly authorized exactly one viewport-drag transform authority seam while target-machine evidence remained unavailable.

Implementation/test sequence:

- `ee51d25de4d923e7bbae4ff5512dc99f3fd36912` — Godot keeps live viewport Move/Rotate/Scale interaction, while Stage-C captures durable Core state plus presentation start state and commits only the resulting gesture delta/scale ratio back through Core;
- `46d84afa12819085a64a2ec31cf04beef4346bc9` — focused authority-retirement regression coverage;
- first exact-head C# validation exposed definite-assignment errors in the new capture seam;
- `d4aef5d55170ef45298e9db942cb250d78e911d2` — initializes the capture locals explicitly and is the final validated implementation checkpoint.

Behavior at the validated checkpoint:

- mapped viewport gesture start captures ObjectId, active immutable mesh revision, durable Core transform and Godot presentation transform;
- Move persists `durable position + presentation movement delta`;
- Rotate persists `durable rotation + presentation rotation delta`;
- Scale persists the durable scale multiplied by the uniform presentation gesture ratio;
- a changed active mesh revision or durable transform rejects the gesture as stale before persistence;
- success saves the Stage-C transaction and reprojects durable Core state into Godot;
- failure restores presentation from current durable Core state;
- ground placement, selection retirement, sculpt architecture, broader persistence cleanup, MS-020 and MS-027 were deliberately not broadened.

This preserves the intended authority split: Core owns durable project transform truth; Godot owns responsive viewport presentation/input.

## Exact-head validation

Exact-head validation for implementation checkpoint `d4aef5d55170ef45298e9db942cb250d78e911d2` is green:

- `core-foundation` run `34614524259`: **PASS**;
- `build` run `34614524187`: **PASS**;
- semantic-version branch identity: PASS;
- C# editor/launcher/updater/Core restore/build and canonical Core tests: PASS;
- backend Python compile/dependency resolution: PASS;
- core logic and execution/job regressions: PASS;
- real geometry regressions: PASS;
- strict release audit: PASS;
- portable package/layout and ZIP SHA-256 sidecar: PASS;
- Inno Setup installer-definition compilation: PASS;
- branch `full-windows-release` / `publish-release`: correctly SKIPPED.

`d4aef5d5...` is a useful fully validated release-worthy checkpoint. It is informational only and does **not** freeze v1.0.26. Documentation commits after it are bookkeeping and do not supersede the implementation checkpoint.

## Exact next task

1. Consume any new v1.0.25 reference-machine evidence first; serious Stage-C/runtime/storage/viewport/persistence/cancellation regressions preempt all fallback work.
2. The Coordinator-authorized single v1.0.26 viewport-drag authority seam is complete. Do **not** invent a second MS-019 authority-retirement seam from this handoff alone.
3. Bootstrap against current TECHNICAL_ROADMAP/Coordinator state on the next run and follow any newer explicit sequencing. If no newer sequencing exists and no serious evidence is available, defer additional architecture expansion rather than broadening MS-020 or MS-027 independently.
4. Do not create release-control, tags or GitHub Releases; release readiness/freeze/publication remain Coordinator-owned.

## User verification dependency

Test released v1.0.25 on the reference Windows / GTX 1080 machine:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job.

No product/design decision is currently required.
