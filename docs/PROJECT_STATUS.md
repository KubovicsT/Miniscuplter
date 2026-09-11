# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.24`.
- **Stable release target:** `784f408efba8a876b889fd704e051f22229de368`.
- **Frozen v1.0.25 release boundary:** `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Overall completion:** **57% acceptance-weighted**.

v1.0.24 is published and immutable. v1.0.25 is frozen for Coordinator-owned publication. All new development belongs on v1.0.26.

## v1.0.25 release chunk

v1.0.25 accumulated the complete bounded MS-019 transform-command authority set:

- mapped Move nudges derive position from durable Core project state;
- mapped Rotate Y nudges derive rotation from durable Core project state;
- mapped Scale nudges derive scale from durable Core project state;
- each operation commits through transactional Stage-C state and projects the committed result back to Godot;
- failures restore presentation from durable state;
- focused regression guards prevent these mapped commands from returning to generic scene-observed persistence.

Current exact release boundary is `a7fc4bcf5f771c18060e5aee7c98026131731c2a`. Exact-head Core and broader build CI were green before the release freeze.

## Release state

Coordinator decided the accumulated v1.0.25 scope is coherent and substantial enough for publication.

- v1.0.25 source branch: **FROZEN**.
- v1.0.26: writable forward branch created from the exact v1.0.25 boundary.
- v1.0.25 publication is Coordinator-owned through autonomous release-control.
- v1.0.24 remains latest stable until the v1.0.25 release workflow succeeds.

Do not write application or documentation commits to frozen v1.0.25 while its release request is active.

## v1.0.26 bootstrap

The forward branch was created from the frozen release SHA. Coordinator began aligning v1.0.26 version identity, but a connector safety block prevented completing every audited identity surface in this review.

Before ordinary implementation, Dev should:
1. finish all remaining `1.0.25 → 1.0.26` version identity surfaces;
2. run exact-head branch validation;
3. update HANDOFF/STATUS with the validated v1.0.26 bootstrap SHA.

This is forward-branch work only and must not alter frozen v1.0.25.

## Critical path

Stage-C reference-machine acceptance remains P0:

`accepted 2D baseline → local 3D generation → candidate → Apply → save → close/reopen → same durable object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence immediately preempts fallback work.

## Next execution direction

After v1.0.26 identity/bootstrap is green and if reference-machine evidence remains unavailable, take exactly one bounded MS-019 seam: **viewport-drag transform commit authority** for mapped Stage-C objects.

Do not combine it with ground placement, selection retirement, broad persistence cleanup, MS-020 expansion or new MS-027 UI work.

## User dependency

No product/design decision is required. Test the latest published immutable release on the reference machine; once v1.0.25 publication succeeds, prefer that release for the next full Stage-C pass.
