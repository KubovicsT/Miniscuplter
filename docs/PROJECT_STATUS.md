# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22` until v1.0.23 publication completes.
- **Stable release target:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Frozen v1.0.23 release boundary:** `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Current writable development branch:** `v1.0.24`, created from that exact release boundary.
- **Latest fully validated v1.0.23 code/test checkpoint:** `1665a9c9fabd8a3d0f1f4f8ed79411f749e158f6`; later commits to the boundary are documentation-only and exact-head branch CI is green.
- **Overall completion:** **57% acceptance-weighted**.

## v1.0.23 release scope

Coordinator judged the accumulated version sufficiently coherent and substantial for publication. It contains the complete planned bounded MS-027 acceptance-support/UI-modernization sequence:

1. workspace preferences, UI scaling and tooltips;
2. direct viewport Select/Move/Rotate/Scale/Sculpt controls;
3. synchronized collapsible scene hierarchy;
4. camera-linked view cube and selected-object orbit pivot;
5. unified AI command/history surface delegating to authoritative AI action owners;
6. MS-026 local resource telemetry for GPU/VRAM/RAM/temperature/job context;
7. density/spacing cleanup and retirement of superseded always-visible instructional copy;
8. MS-028 compile-regression repair.

This is a meaningful user-testable increment even though Stage-C target-machine acceptance remains incomplete. Publication does not close any user-observed issue that still requires reference-machine verification.

## Critical path

Stage-C reference-machine acceptance remains P0. The required end-to-end evidence is:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery.

If that evidence remains unavailable, v1.0.24 may begin the next already-approved structural objective: bounded MS-020 Job Broker durability/resource-locking/cancellation/crash-recovery work. New serious acceptance findings preempt it immediately.

## Release ownership

Release publication is Coordinator-owned. v1.0.23 is frozen while release-control is active. Dev continues only on v1.0.24. Published tags/releases are immutable and any defect is fixed forward.

## User dependency

No product/design decision is required. Reference-machine verification remains the external dependency; when v1.0.23 is published, prefer testing that newest build because it also exposes the completed MS-027 UI and telemetry surfaces.
