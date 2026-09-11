# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Latest validated implementation checkpoint:** `d4aef5d55170ef45298e9db942cb250d78e911d2`.
- **Latest Coordinator planning commits:** roadmap `61c1ad61e9cf6e9d265d50db53daa35e3bae69d3`, coordinator log `042dcc2a7ddda6f410d28522861e350fb7c88594`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance on released v1.0.25. Serious correctness/persistence/viewport/data-safety/storage/cancellation regressions preempt fallback work.

## Release state

v1.0.25 is published and immutable. v1.0.26 remains writable. Coordinator reviewed the viewport-drag checkpoint and decided **KEEP v1.0.26 ACCUMULATING**; no release freeze/request is active for v1.0.26.

## Completed v1.0.26 checkpoint

The previously authorized viewport-drag MS-019 seam is complete and validated. Mapped Move/Rotate/Scale gestures capture durable Core transform plus presentation start state, commit only the resulting gesture delta/scale ratio through Core, reject stale state, and reproject/restore Godot presentation from durable Core truth. Do not repeat or broaden this seam.

## Exact next task

1. Consume any new v1.0.25 reference-machine evidence first. Any serious Stage-C/runtime/storage/viewport/persistence/cancellation regression preempts fallback work.
2. If no such evidence is available, implement exactly one bounded MS-019 seam: **mapped-object ground placement transform authority**.
3. Preserve the existing viewport/input presentation owner. Derive requested placement from durable Core object/revision/transform state; commit through the existing Core transactional transform path; reject stale state before persistence; project committed Core state back into Godot; restore from Core on failure.
4. Add focused regression coverage preventing ground placement from returning to scene-observed durable persistence.
5. Do not combine this with selection retirement, broad persistence cleanup, sculpt architecture, MS-020 expansion or MS-027 UI modernization.
6. Validate exact HEAD, record the checkpoint in execution-state docs, and continue only under newer Coordinator direction. Dev does not create release-control, tags or GitHub Releases.

## User verification dependency

Test released v1.0.25 on the reference Windows / GTX 1080 machine:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job.

No product/design decision is currently required.
