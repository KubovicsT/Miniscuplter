# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` until the active v1.0.23 publication succeeds.
- **Current writable development branch:** `v1.0.24`.
- **Frozen release source:** `v1.0.23` at exact boundary `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Latest validated v1.0.23 code/test checkpoint:** `1665a9c9fabd8a3d0f1f4f8ed79411f749e158f6`; subsequent commits through the frozen HEAD are documentation-only and exact-head branch CI is green.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Coordinator release boundary

The complete planned bounded MS-027 sequence has accumulated into a coherent, substantial, independently testable UI/acceptance-support increment: workspace preferences/UI scaling/tooltips; direct viewport tools; synchronized hierarchy; view cube and selected-object orbit; unified AI command/history dispatcher; MS-026 resource telemetry; and density/spacing cleanup. The branch is therefore large enough to release rather than continue accumulating unrelated work.

The Coordinator established the v1.0.23 release boundary at `a6532003bc6ecfcf79fc15afa3ee772cb117b982` and created v1.0.24 from that exact SHA so Dev can continue while publication runs. v1.0.23 is frozen: do not commit application or documentation changes there while its release request is active.

## Immediate Dev baton on v1.0.24

1. Check first for new reference-machine evidence. Any serious Stage-C/persistence/viewport/storage/cancellation issue is P0.
2. Do not invent another MS-027 UI slice: the planned sequence is complete.
3. If user evidence remains unavailable, begin the next already-approved structural objective conservatively: **MS-020 durable Job Broker ownership/resource locking/cancellation/crash recovery**, starting with a bounded architecture/implementation slice that directly improves long-running local job reliability and preserves current Stage-C behavior.
4. Keep Core durable-state authority, Godot presentation/input authority and Python inference/geometry authority intact.
5. Do not modify the frozen v1.0.23 branch or its release request.
6. If Coordinator reports a concrete v1.0.23 release-source failure, make only the directed minimum fix on the frozen source path and propagate the equivalent fix forward to v1.0.24.

## User verification dependency

Reference-machine testing remains required. Prefer the newest successfully published build when available. Exercise Generate 3D → candidate → Apply → save/close/reopen → same object/revision; resize/presentation; no starter sphere/opaque floor; transform/sculpt; cleanup/export; storage containment; cancellation/recovery; and the resource panel during a long AI job.
