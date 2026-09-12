# Miniscuplter Technical Roadmap

> Authoritative end-to-end sequencing. HANDOFF owns immediate execution state.

Latest stable: `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2`
Writable branch: `v1.0.28` (bootstrap first)
Acceptance-weighted completion: **approximately 64%**

## Whole-system direction
Preserve the selective-refactor architecture: Core owns durable project/object/revision/history state; Godot owns presentation/input; Python owns inference/geometry; launcher/updater owns runtime setup and delivery safety. Retire duplicate authority through bounded production seams, not a broad rewrite.

## Current P0 reference-machine evidence
Released v1.0.27 testing on the Windows / GTX 1080 reference machine materially changes priority:
- **MS-009:** the 3D grid is absent again in the released viewport. This is a critical renderer/workspace acceptance failure and outranks fallback infrastructure.
- **MS-027:** the multi-line AI console works, but its composition is rejected: it must move to the bottom center between scene tree and right panel, not span the app; contextual buttons belong in a vertical stack on its right.
- **MS-026/MS-027:** resource telemetry is visible, but should be compactly tiled 2×2 in the bottom-left.
- **MS-027:** the cube orientation control is rejected. Replace it with a Blender-style circular XYZ orientation gizmo; no cube body is required. Current viewport tool buttons are accepted.
- **MS-029:** v1.0.25→v1.0.27 left the launcher closed. Because the transition was initiated by the immutable v1.0.25 updater, this matches the documented one-transition limitation and does not yet falsify the later fix. Verify on the next update initiated from v1.0.27.

Reference-machine evidence outranks CI for renderer/UI/update-runtime acceptance.

## v1.0.28 critical path
### A — finish forward-version bootstrap
Change only the remaining stale 1.0.27 identities in `ai_backend/app.py` and `tools/release_audit.py`; obtain green exact-head Core/build CI before ordinary product work.

### B — restore authoritative viewport grid (MS-009)
Find the actual renderer/grid ownership seam responsible for the released v1.0.27 disappearance. Restore a neutral visible non-occluding grid at launch and across resize while preserving native viewport sizing. Do not add another timer, blind delayed repaint, or duplicate world owner.

### C — correct compact workspace composition (MS-026/MS-027)
Keep the accepted direct viewport tool buttons. Recompose, without duplicating command/state authority:
- bottom-center multi-line command console confined between left scene tree and right properties/workflow panel;
- contextual command buttons vertically stacked immediately to its right;
- 2×2 compact resource telemetry at bottom-left;
- Blender-style circular XYZ orientation gizmo instead of the cube;
- durable layout preferences and viewport dominance preserved.

### D — integration / release checkpoint
Run the strongest Core/C#/Python/runtime/geometry/release-audit/packaging gates and stop for Coordinator release review. User-observed renderer/UI items remain **FIXED - NEEDS USER VERIFICATION** until a released/reference-machine retest.

A→B→C may auto-proceed; D requires Coordinator review.

## Deferred fallback engineering
MS-020 durable job envelope → heavyweight runtime ownership → truthful cancellation/recovery remains architecturally valid, but is deferred behind the concrete v1.0.27 P0 acceptance failures above. Resume only after the current product-facing regressions are contained or Coordinator explicitly reorders.

## Explicit non-priorities
No provider-family expansion, broad Rig/Pose/kitbash work, scene-state rewrite, full sculpt rewrite, speculative cloud services, or generalized scheduler while P0 viewport/workspace acceptance is open.

## User dependency
No new product decision is required: the user has supplied the target UX. After a corrected v1.0.28 release, retest grid visibility across launch/resize, the bottom workspace composition/orientation gizmo, and whether the launcher remains open on an update initiated by v1.0.27.
