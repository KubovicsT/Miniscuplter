# Miniscuplter Handoff

> Current execution baton. TECHNICAL_ROADMAP owns strategy.

## Release / branch state
- Stable: `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2`.
- Writable: `v1.0.28`; no release freeze.
- Current bootstrap HEAD lineage still has two stale 1.0.27 identity surfaces: `ai_backend/app.py` and `tools/release_audit.py`.
- 2026-09-12 v1.0.27 GTX 1080 evidence preempts MS-020 fallback work: grid absent; workspace corrections below are user-required.
- v1.0.25→v1.0.27 launcher staying closed is consistent with MS-029's known old-updater transition limitation; verify again on the next update initiated by v1.0.27.

## CURRENT — A: complete v1.0.28 bootstrap
**Outcome:** bump only the two remaining stale identity surfaces to 1.0.28 and obtain green exact-head Core/build CI.
**Constraints:** never mutate published v1.0.27; no product work before bootstrap acceptance.
**Auto-proceed:** YES.

## NEXT — B: P0 viewport grid restoration (MS-009)
**Outcome:** identify the actual render/grid ownership failure seen on released v1.0.27 and restore a visible neutral Blender-like grid at launch and through resize.
**Constraints:** no blind timer/repaint/world-repair loop; preserve native viewport sizing and non-occluding grid semantics.
**Acceptance:** regression coverage + exact-head CI; code remains FIXED - NEEDS USER VERIFICATION until a released/reference-machine retest.
**Auto-proceed:** YES.

## NEXT — C: user-directed workspace correction (MS-026/MS-027)
**Outcome:** keep the accepted viewport tool buttons; move the multi-line AI console to the bottom center between scene tree/right panel, not app-wide, with contextual actions vertically stacked on its right; tile resource telemetry 2×2 in the bottom-left; replace the cube with a Blender-style circular XYZ orientation gizmo with no cube body.
**Constraints:** retain one command/action authority, durable layout preferences and compact viewport dominance.
**Acceptance:** targeted UI regressions + exact-head CI; target-machine UX remains user-verification gated.
**Auto-proceed:** YES.

## NEXT — D: v1.0.28 integration checkpoint
Re-run strongest Core/C#/Python/runtime/geometry/release-audit/packaging gates; reconcile current docs and stop at a release-worthy checkpoint for Coordinator review.
**Auto-proceed:** NO — COORDINATOR REVIEW REQUESTED.

## Preemption
Any new update/backend/generation/persistence/storage/cancellation/data-safety failure outranks B–D.
