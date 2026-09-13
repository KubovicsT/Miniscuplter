# Miniscuplter Active Issue State

This file is the authoritative current-state ledger for active, release-relevant, or user-verification-pending issues. Historical issue narratives remain in `docs/ISSUES.md`, which is legacy/read-only context.

## v1.0.31 reference-machine findings — 2026-09-13

### MS-033 — cancel/retry can strand packaged AI backend
- severity: Critical generation/runtime correctness
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- priority: P0 preemption
- evidence: GTX 1080 packaged v1.0.31; Generate 3D -> cancel -> retry failed first as user-cancelled, then with local AI backend health-check failure; closing/restarting the app restored generation and a later generation completed in ~390 s. Dev traced cancellation recovery through `AIClient` and the owned-backend restart path and found `BackendLauncher.WaitForBackendReadyAsync` still required stale health version `1.0.26`, so a successfully restarted v1.0.32 backend could be rejected for the full readiness timeout and poison the immediate retry path.
- current_implementation_state: v1.0.32 editor-owned backend restart now derives expected health version from the packaged editor assembly while retaining isolated instance-token matching; regression guard added in `RuntimeRepairHealthProtocolTests`; core-foundation run 34727090127 and full build run 34727090071 are green at exact head `282d61e8e479058179fa3a903c5ba0d9962f5569`
- verification_state: reference-machine cancel -> immediate retry retest still required on a packaged build containing the v1.0.32 fix
- desired_state: cancel/retry leaves one healthy authoritative runtime owner and immediate retry works without app restart; cancellation must retire durable state and explicitly terminate/acknowledge the matching backend job before retry eligibility, with no stale job/backend lifecycle surviving cancellation

### MS-035 — resize leaves black exterior gutters and does not preserve panel layout
- severity: High workspace/resize UX
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- priority: P1
- evidence: screenshots show black rectangles at window edges after resize; side panels/layout do not behave as fixed-width rails with viewport absorbing size delta. Source audit found the programmatic top-level `VBoxContainer` was anchored under a non-Control `Node`, so it did not have a reliable client-area resize owner, while the right rail was also deliberately resized as 27% of available width.
- current_implementation_state: v1.0.32 binds the top-level workspace root to `Viewport.SizeChanged` and the current visible client rect, keeps the right workspace rail at its intended 330-unit width, and lets the central viewport absorb horizontal size changes while preserving the v1.0.19 `SubViewportContainer.Stretch` render-size authority. `WorkspaceResizeContractTests` guards the client-area/root and fixed-rail contract. Core-foundation run 34747505948 and full build run 34747505866 are green at exact code/test head `c7507a6384f3fc4ebdbd32dee263507b7fb66027`.
- verification_state: packaged reference-machine resize/maximize/restore retest required; confirm no black exterior gutters and that the right rail remains stable while the viewport absorbs size delta
- desired_state: left/right panels retain intended sizes and the central viewport expands/contracts to consume available client area with no black gutters

### MS-036 — generated candidate is not reviewable before Apply
- severity: High generation UX/correctness visibility
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- priority: P0/P1
- evidence: successful generation in v1.0.31 reports identity-verified candidate ready but viewport/scene do not display it until Apply; user cannot review the candidate as instructed
- current_implementation_state: v1.0.32 preserves stale/conflict guards but automatically applies a non-conflicting identity-verified generation into canonical Core project state and immediately inserts the matching mesh presentation; the redundant Apply gate is no longer part of the normal successful generation path
- verification_state: packaged reference-machine generation must confirm a successful result appears automatically in the scene without pressing Apply
- desired_state: generation result is immediately visible and reviewable. User direction: remove the redundant Apply step and insert a successful generation directly; unsuitable results can be regenerated. Preserve transactional/stale-result guards while simplifying the UX.

### MS-037 — generated mesh renders with shell/back-side appearance
- severity: High viewport/render correctness
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- priority: P1
- evidence: applied generated knight in v1.0.31 appears visually inside-out/shell-like, as if non-user-facing/back surfaces dominate
- current_implementation_state: v1.0.32 repairs generated mesh winding/normals before export and uses correctness-oriented back-face culling with neutral matte material plus stronger directional studio lighting instead of masking orientation problems with two-sided rendering; exact-head CI for the implementation is green
- verification_state: packaged reference-machine visual retest required to confirm exterior faces and surface detail read correctly on GTX 1080
- desired_state: normal/front-facing generated surface renders correctly from the active camera; diagnose normals/winding/material/culling rather than masking the symptom

### MS-038 — transform interaction incomplete
- severity: Medium/High modeling UX
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- priority: P1
- evidence: Select/Move/Rotate tools work through axis handles, but dragging the model itself does not perform expected constrained/direct move; rotate lacks a rotation-circle/ring interaction
- desired_state: intuitive direct model drag where appropriate plus axis constraints; rotation exposes visible rotation rings/circles while retaining precise axis manipulation

### MS-039 — generated-object placement and scale are unsuitable
- severity: High generation/modeling UX
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- priority: P1
- evidence: inserted model in v1.0.31 has origin at grid zero so geometry is roughly bisected by the grid; generated object is tiny and scale is not user-adjustable
- current_implementation_state: v1.0.32 computes one Core-owned initial transform from generated mesh bounds: uniform scaling targets a 100-unit largest dimension, X/Z bounds are centered around workspace origin, and minimum Y is translated to grid Y=0. The transform is persisted in canonical `ProjectObject.Transform`, so save/reopen/edit paths consume the same authority rather than a viewport-only offset. Targeted `GeneratedObjectPlacementTests` plus exact-head build run 34746648597 are green at `9353b21686fe34d7699b391c9469fd8643349e53`.
- verification_state: packaged reference-machine generation must confirm sensible apparent size and lowest-point grid contact; user-adjustable pre-generation desired physical size remains a later product-surface enhancement rather than a second transform authority
- desired_state: on insertion translate object so its lowest world-space point rests on the grid; expose user-facing grid scale and desired model height/size before generation so the inserted result starts near intended physical scale

### MS-040 — Smart Select AI unavailable
- severity: High feature regression
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- priority: P1
- evidence: packaged v1.0.31 UI repeatedly reports Smart Select AI `Not Found`; semantic selection does not work. Dev traced the failure to an editor/backend API contract mismatch: `AIClient.SemanticSelectAsync` posts to `/semantic-select`, while the packaged FastAPI app exposed only a legacy `/smart-select` endpoint.
- current_implementation_state: v1.0.32 now exposes the mesh semantic-selection contract expected by the editor at `/semantic-select`, validates the mesh input through the normal contained-path guard, and forwards `input_path` + `query` to the existing local CLIPSeg/geometry-fallback selector. `SmartSelectContractTests` guards the route/request contract. Core-foundation run 34747264663 and full build run 34747264693 are green at exact code/test head `edf0376b55b3e5cf202dcf35544ee00d4efb35ed`.
- verification_state: packaged reference-machine Smart Select retest required; confirm a semantic query no longer returns HTTP `Not Found` and produces a selection (CLIPSeg when installed, geometry fallback otherwise)
- desired_state: packaged Smart Select dependency/status is correctly installed/resolved and semantic selection works, with graceful fallback that does not spam misleading errors

### MS-041 — 3D refinement workflow lacks coherent product surface
- severity: High product/UX
- state: USER-DIRECTED REDESIGN
- priority: P1 after generation correctness blockers
- evidence: Paint AI Mask has no useful preview; Generate 4 Preview Candidates and related refinement actions are exposed as disconnected buttons in the 3D panel
- desired_state: add dedicated Refinement and Kitbash product areas/tabs after 3D and move coherent 3D enhancement/redesign workflows there; exact UI/interaction design remains user-approval work

### MS-042 — Rig & Pose and Cleanup & Export surfaces are empty
- severity: Medium incomplete-product surface
- state: DEFERRED BY USER
- priority: later
- desired_state: implement these product areas in later scoped work; do not let empty-tab implementation preempt current generation/viewport/refinement correctness

## Existing issues updated by v1.0.31 evidence

### MS-020 — AI job/runtime ownership
- severity: Critical generation/runtime correctness
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION; see MS-033
- priority: P0
- latest_reference_machine_evidence: v1.0.31 can complete generation, but cancel -> retry can leave the packaged backend unhealthy until app restart
- current_implementation_state: cancellation recovery remains single-owner and restart-gated; the stale editor-owned backend health-version gate causing the observed retry failure is fixed at v1.0.32 exact head `282d61e8e479058179fa3a903c5ba0d9962f5569`
- desired_state: one authoritative heavyweight runtime owner; cancellation/retry isolation; no restart required

### MS-031 — accepted-baseline persistence across reopen
- severity: Critical persistence/state-restoration defect
- state: VERIFIED FIXED ON RELEASED v1.0.31
- priority: closed verification gate
- current_implementation_state: accepted-baseline restoration runs after UI composition on reopen; targeted save/reopen regression and CI green
- verification_state: reference-machine verified on Windows/GTX 1080: after app close/reopen the 2D result is already visible and Generate 3D is immediately available without re-accepting the image as baseline; generated 3D object persistence/edit continuity is also verified across reopen

### MS-009 — viewport grid/render ownership
- severity: Critical
- state: PARTIALLY VERIFIED / RESIZE FIX IN v1.0.32 NEEDS USER VERIFICATION; see MS-035
- latest_reference_machine_evidence: v1.0.31 grid is visible, including with generated object, but resize still produces black exterior gutters
- desired_state: visible neutral grid plus robust resize/client-area composition

### MS-026 — resource telemetry layout
- severity: Medium product/observability
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- latest_reference_machine_evidence: v1.0.31 telemetry still overlays the viewport/AI-command region instead of occupying the requested bottom-left app-window area
- current_implementation_state: v1.0.32 introduces one dedicated bottom workspace dock outside `ViewportHost`; telemetry is immediately reparented into its fixed 300-unit lower-left lane, while the command surface owns the expandable center lane and a 330-unit gutter preserves alignment with the fixed right rail. `WorkspaceAcceptanceTests` now rejects viewport-overlay ownership. Core-foundation run 34748891791 and full build/package run 34748891535 are green at exact head `ae8f24a6d460d182be5cd8332634b87d4bda9763`.
- verification_state: packaged reference-machine workspace retest required; confirm telemetry stays in the lower-left application area and never covers the viewport or AI command surface during resize/maximize/restore
- desired_state: telemetry lives in the lower-left application workspace area without covering viewport or command UI

### MS-027 — workspace composition
- severity: High product/UX acceptance
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- latest_reference_machine_evidence: v1.0.31 AI command line remains an overlay and is too narrow; user requires a dedicated non-overlay area expanded across the available bottom-center width; telemetry placement also remains wrong
- current_implementation_state: v1.0.32 moves the AI command console out of `ViewportHost` into the dedicated expandable bottom-center workspace lane, lays contextual actions horizontally to preserve useful prompt width, and composes telemetry separately at bottom-left. Regression coverage requires non-overlay command ownership and the bottom-dock telemetry/command/right-rail alignment contract. Core-foundation run 34748891791 and full build/package run 34748891535 are green at exact head `ae8f24a6d460d182be5cd8332634b87d4bda9763`.
- verification_state: packaged reference-machine workspace retest required; confirm the command area spans the available bottom-center width, contextual actions/history remain usable, and neither command nor telemetry covers the viewport
- desired_state: dedicated AI command area outside the main viewport, expanded across available bottom-center space; bottom-left telemetry outside viewport; contextual actions compose without overlap; accepted viewport tools and orientation control preserved

### MS-029 — updater leaves launcher closed
- severity: Critical release-path regression
- state: FIXED - NEEDS USER VERIFICATION
- verification_state: update-path reference-machine retest still required

### MS-030 — packaged backend health after Runtime Repair
- severity: Critical
- state: FIXED - NEEDS USER VERIFICATION / RELATED TO MS-033
- current_implementation_state: v1.0.32 derives expected repaired-backend health version from launcher assembly and editor-owned cancellation-restart health version from the packaged editor assembly instead of stale hardcoded backend versions; isolated instance-token checks remain required
- verification_state: reference-machine Runtime Repair -> Generate and cancel -> immediate retry retests still required

### MS-032 — failed generation envelope retirement
- severity: High generation/persistence correctness
- state: FIXED
- priority: completed v1.0.31 P0 preemption
- latest_evidence: terminal non-cancelled failures retire exact durable envelope and persist retirement before in-memory binding clears; exact-head validation green
- source_finding: AMF-001 / GitHub Issue #2

## Additional observed v1.0.31 state
- starter-sphere UI remnant: 3D panel can report/select `Starter sphere` while no sphere is visible in viewport; fold into scene/selection-authority investigation unless it proves independently causal.
- generated model itself successfully completed on GTX 1080 in about 390 s after app restart, using up to roughly 5.8 GB VRAM and high GPU utilization; this is positive provider/runtime evidence but does not close MS-033/MS-036/MS-037/MS-039.
- generated-object persistence/rehydration is positive: with the generated model present, closing and reopening v1.0.31 restored the 3D object and its viewport transform tools continued to work; this confirms the generated-object identity/edit-continuity part of the Stage-C path on the reference machine.
- accepted 2D baseline persistence is now also verified: after reopen the prior 2D result is visible and 3D generation can start immediately without another baseline-accept action.

## Historical ledger
- legacy_source: docs/ISSUES.md
- historical_attempts_and_old issue records: preserved in legacy source and Git history
