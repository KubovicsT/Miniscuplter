# Miniscuplter Active Issue State

This file is the authoritative current-state ledger for active, release-relevant, or user-verification-pending issues. Historical issue narratives remain in `docs/ISSUES.md`, which is legacy/read-only context.

## v1.0.31 reference-machine findings — 2026-09-13

### MS-033 — cancel/retry can strand packaged AI backend
- severity: Critical generation/runtime correctness
- state: REPRODUCED ON RELEASED v1.0.31
- priority: P0 preemption
- evidence: GTX 1080 packaged v1.0.31; Generate 3D -> cancel -> retry failed first as user-cancelled, then with local AI backend health-check failure; closing/restarting the app restored generation and a later generation completed in ~390 s
- desired_state: cancel/retry leaves one healthy authoritative runtime owner and immediate retry works without app restart; no stale job/backend lifecycle survives cancellation

### MS-035 — resize leaves black exterior gutters and does not preserve panel layout
- severity: High workspace/resize UX
- state: REPRODUCED ON RELEASED v1.0.31
- priority: P1
- evidence: screenshots show black rectangles at window edges after resize; side panels/layout do not behave as fixed-width rails with viewport absorbing size delta
- desired_state: left/right panels retain intended sizes and the central viewport expands/contracts to consume available client area with no black gutters

### MS-036 — generated candidate is not reviewable before Apply
- severity: High generation UX/correctness visibility
- state: REPRODUCED ON RELEASED v1.0.31
- priority: P0/P1
- evidence: successful generation reports identity-verified candidate ready but viewport/scene do not display it until Apply; user cannot review the candidate as instructed
- desired_state: generation result is immediately visible and reviewable. User direction: remove the redundant Apply step and insert a successful generation directly; unsuitable results can be regenerated. Preserve transactional/stale-result guards while simplifying the UX.

### MS-037 — generated mesh renders with shell/back-side appearance
- severity: High viewport/render correctness
- state: REPRODUCED ON RELEASED v1.0.31
- priority: P1
- evidence: applied generated knight appears visually inside-out/shell-like, as if non-user-facing/back surfaces dominate
- desired_state: normal/front-facing generated surface renders correctly from the active camera; diagnose normals/winding/material/culling rather than masking the symptom

### MS-038 — transform interaction incomplete
- severity: Medium/High modeling UX
- state: REPRODUCED ON RELEASED v1.0.31
- priority: P1
- evidence: Select/Move/Rotate tools work through axis handles, but dragging the model itself does not perform expected constrained/direct move; rotate lacks a rotation-circle/ring interaction
- desired_state: intuitive direct model drag where appropriate plus axis constraints; rotation exposes visible rotation rings/circles while retaining precise axis manipulation

### MS-039 — generated-object placement and scale are unsuitable
- severity: High generation/modeling UX
- state: REPRODUCED ON RELEASED v1.0.31
- priority: P1
- evidence: inserted model origin is placed at grid zero so geometry is roughly bisected by the grid; generated object is tiny and scale is not user-adjustable
- desired_state: on insertion translate object so its lowest world-space point rests on the grid; expose user-facing grid scale and desired model height/size before generation so the inserted result starts near intended physical scale

### MS-040 — Smart Select AI unavailable
- severity: High feature regression
- state: REPRODUCED ON RELEASED v1.0.31
- priority: P1
- evidence: packaged UI repeatedly reports Smart Select AI `Not Found`; semantic selection does not work
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
- state: REOPENED / PARTIAL REGRESSION; see MS-033
- priority: P0
- latest_reference_machine_evidence: v1.0.31 can complete generation, but cancel -> retry can leave the packaged backend unhealthy until app restart
- desired_state: one authoritative heavyweight runtime owner; cancellation/retry isolation; no restart required

### MS-031 — accepted-baseline persistence across reopen
- severity: Critical persistence/state-restoration defect
- state: FIXED - NEEDS USER VERIFICATION
- priority: P0
- current_implementation_state: accepted-baseline restoration runs after UI composition on reopen; targeted save/reopen regression and CI green
- verification_state: explicit v1.0.31 save/close/reopen baseline eligibility result not yet reported

### MS-009 — viewport grid/render ownership
- severity: Critical
- state: PARTIALLY VERIFIED / RESIZE REGRESSION; see MS-035
- latest_reference_machine_evidence: v1.0.31 grid is visible, including with generated object, but resize still produces black exterior gutters
- desired_state: visible neutral grid plus robust resize/client-area composition

### MS-026 — resource telemetry layout
- severity: Medium product/observability
- state: REOPENED UX
- latest_reference_machine_evidence: v1.0.31 telemetry still overlays the viewport/AI-command region instead of occupying the requested bottom-left app-window area
- desired_state: telemetry lives in the lower-left application workspace area without covering viewport or command UI

### MS-027 — workspace composition
- severity: High product/UX acceptance
- state: REOPENED UX
- latest_reference_machine_evidence: v1.0.31 AI command line remains an overlay and is too narrow; user requires a dedicated non-overlay area expanded across the available bottom-center width; telemetry placement also remains wrong
- desired_state: dedicated AI command area outside the main viewport, expanded across available bottom-center space; bottom-left telemetry outside viewport; contextual actions compose without overlap; accepted viewport tools and orientation control preserved

### MS-029 — updater leaves launcher closed
- severity: Critical release-path regression
- state: FIXED - NEEDS USER VERIFICATION
- verification_state: update-path reference-machine retest still required

### MS-030 — packaged backend health after Runtime Repair
- severity: Critical
- state: FIXED - NEEDS USER VERIFICATION / RELATED TO MS-033
- current_implementation_state: v1.0.32 engineering checkpoint additionally derives repaired-backend expected health version from launcher assembly instead of stale hardcoded backend version
- verification_state: reference-machine Runtime Repair -> Generate retest still required; cancellation lifecycle failure tracked separately in MS-033

### MS-032 — failed generation envelope retirement
- severity: High generation/persistence correctness
- state: FIXED
- priority: completed v1.0.31 P0 preemption
- latest_evidence: terminal non-cancelled failures retire exact durable envelope and persist retirement before in-memory binding clears; exact-head validation green
- source_finding: AMF-001 / GitHub Issue #2

## Additional observed v1.0.31 state
- starter-sphere UI remnant: 3D panel can report/select `Starter sphere` while no sphere is visible in viewport; fold into scene/selection-authority investigation unless it proves independently causal.
- generated model itself successfully completed on GTX 1080 in about 390 s after app restart, using up to roughly 5.8 GB VRAM and high GPU utilization; this is positive provider/runtime evidence but does not close MS-033/MS-036/MS-037/MS-039.

## Historical ledger
- legacy_source: docs/ISSUES.md
- historical_attempts_and_old issue records: preserved in legacy source and Git history
