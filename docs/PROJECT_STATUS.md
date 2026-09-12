# Miniscuplter Project Status

> Fast-moving dashboard; actual Git/release/CI state is authoritative.

Last reconciled: 2026-09-12

## Current state
- **Latest published stable:** `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2`.
- **Writable branch:** `v1.0.28`; not frozen.
- v1.0.28 bootstrap remains incomplete until `ai_backend/app.py` and `tools/release_audit.py` move from 1.0.27 to 1.0.28 and exact-head CI is green.
- **Overall completion:** approximately **64% acceptance-weighted**. New v1.0.27 reference-machine evidence confirms product-facing acceptance remains incomplete.

## v1.0.27 reference-machine evidence — 2026-09-12
- Update from v1.0.25 to v1.0.27 completed, but the launcher remained closed and had to be started manually. This is consistent with MS-029's known old-updater transition limitation because the update was initiated by the immutable v1.0.25 updater; verify the fix on the next update initiated by v1.0.27.
- **MS-009 failed:** the 3D viewport grid is absent again on the released build.
- **MS-027 partially accepted:** direct viewport tool buttons look good.
- **MS-027 failed/changed:** the AI console is correctly multi-line but remains at the top and too wide; user requires it at bottom center between the scene tree and right panel, with contextual action buttons vertically stacked at its right.
- **MS-026/MS-027 layout change:** resource graphs should be compact 2×2 tiles in the bottom-left.
- **MS-027 direction change:** replace the current cube control with a Blender-style circular XYZ orientation gizmo; no cube body is wanted.

## Current critical path
1. finish v1.0.28 mechanical version bootstrap and exact-head CI;
2. root-cause and restore the visible non-occluding viewport grid (MS-009);
3. implement the user-specified compact workspace corrections (MS-026/MS-027);
4. produce an integrated v1.0.28 checkpoint for Coordinator release review.

MS-020 job-lifetime work is temporarily deferred behind these concrete reference-machine failures.

## User dependency
No additional product decision is needed. After the corrected release, user verification is required for grid behavior across launch/resize, workspace placement/orientation behavior, and the first update initiated from v1.0.27.
