# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.23` at `bda683264448fc8b51c7c538db61f8c0487a699a`.
- **Current writable development branch:** `v1.0.25`.
- **Frozen release source:** `v1.0.24` at exact boundary `784f408efba8a876b889fd704e051f22229de368`.
- **Latest fully validated v1.0.24 implementation/test checkpoint:** `d7a72ce112ff9827f3ee314e281e7e894d2dff4c`; exact v1.0.24 boundary HEAD subsequently passed branch CI.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.23. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

Coordinator judged accumulated v1.0.24 scope sufficiently coherent/substantial for release: bounded MS-020 job ownership/cancellation/restart durability plus one bounded MS-019 Stage-C authority-retirement seam and its attributable strict-audit repair.

Release boundary is exact v1.0.24 HEAD `784f408efba8a876b889fd704e051f22229de368`. v1.0.24 is frozen for publication; do not commit application or documentation changes there while release-control is pending.

v1.0.25 was created from that exact boundary before release initiation and is the only writable forward semantic-version development branch.

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

### MS-019 bounded authority-retirement seam

The historical `V109Generate3DAsync` owner now delegates to canonical `V1020Generate3DAsync`. Focused regression coverage prevents the old layer from regaining provider-routing/direct-scene-import authority and proves candidate/Apply ownership remains canonical. Strict release audit was updated to follow actual migrated authority without weakening its checks.

## Exact next task on v1.0.25

1. Consume any released-v1.0.23 reference-machine evidence first.
2. If a serious Stage-C/runtime/storage/viewport/persistence/cancellation regression exists, fix it forward on v1.0.25 immediately.
3. If acceptance remains unavailable, inspect the next smallest proven duplicate-authority seam under MS-019, but do not broaden migration speculatively. Prefer a seam whose replacement owner is already covered by Stage-C/Core tests.
4. Do not resume opportunistic MS-027 UI expansion.
5. Do not mutate frozen v1.0.24, published v1.0.23, or release-control; release publication belongs to Coordinator.
6. Record useful checkpoints while continuing development; checkpoints do not freeze v1.0.25.

## User verification dependency

Test released v1.0.23:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job.

No product/design decision is currently required.