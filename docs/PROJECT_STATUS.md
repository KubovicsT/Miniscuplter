# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-12

## Current state

- **Latest published stable:** `v1.0.26` at `a41e0419ba40fd1118775e8f516b1a31145f18d8`.
- **v1.0.26 publication:** complete; exact-SHA validation, Godot Windows export, installer smoke-install, immutable tag and GitHub Release succeeded.
- **Current writable branch:** `v1.0.27`.
- **Latest fully validated engineering checkpoint:** `3d63204c368ca6b6564b7e1b74868f3636183668`.
- **v1.0.27 identity/bootstrap:** green across semantic identity, Core/editor builds, backend lifecycle, release audit and packaging.
- **Overall completion:** approximately **64% acceptance-weighted**; reference-machine acceptance remains a major unresolved component.

## v1.0.27 engineering progress

- **MS-019 mapped ground placement:** the mapped-object production path now derives grounding from stable Core object/revision/transform state and the immutable mesh asset, rejects stale state, commits transactionally and restores Godot presentation from Core on failure.
- **Bounded viewport selection seam:** Godot still owns ray hit-testing, while mapped viewport picks bind to stable Core object + mesh-revision identity before presentation selection. Whole-object selection revision transfer is explicit and stale-revision regression-covered.
- Exact checkpoint CI at `3d63204c...` passed Core/editor C# builds/tests, Python/runtime and backend lifecycle tests, geometry regressions, release audit, portable package/hash verification and installer-definition compilation.

## Acceptance still pending

Released `v1.0.26` fixes remain dependent on GTX 1080 reference-machine verification where CI cannot prove renderer/GPU/runtime behavior, especially MS-030 backend health, MS-009 viewport resize invariance, MS-029 update/reopen behavior and the MS-027 compact-workspace tranche.

## Critical path

The currently authorized Dev queue has been completed through the bounded selection seam. Coordinator review is required before further Stage-D sequencing; imminent v1.0.26 target-machine evidence may legitimately reprioritize the next work.

## User dependency

No product decision is currently required. Reference-machine testing of v1.0.26 remains the highest-value user input: update/reopen, Repair AI Runtime, Generate 3D, resize invariance, compact workspace UX, then the complete Stage-C Apply → save/reopen → edit/sculpt → cleanup → exact STL export flow with storage/cancellation observations.
