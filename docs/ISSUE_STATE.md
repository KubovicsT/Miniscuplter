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
- state: REOPENED ON RELEASED v1.0.35 - REPRODUCED
- priority: P1 PREEMPTION
- evidence: screenshots show black rectangles at window edges after resize; side panels/layout do not behave as fixed-width rails with viewport absorbing size delta. Source audit found the programmatic top-level `VBoxContainer` was anchored under a non-Control `Node`, so it did not have a reliable client-area resize owner, while the right rail was also deliberately resized as 27% of available width.
- current_implementation_state: v1.0.32 binds the top-level workspace root to `Viewport.SizeChanged` and the current visible client rect, keeps the right workspace rail at its intended 330-unit width, and lets the central viewport absorb horizontal size changes while preserving the v1.0.19 `SubViewportContainer.Stretch` render-size authority. `WorkspaceResizeContractTests` guards the client-area/root and fixed-rail contract. Core-foundation run 34747505948 and full build run 34747505866 are green at exact code/test head `c7507a6384f3fc4ebdbd32dee263507b7fb66027`.
- verification_state: FAILED on v1.0.35 reference machine: black bars remain at the left/right in normal proportions and move to top/bottom when the window becomes sufficiently narrow; major controls otherwise stay comparatively stable during resize
- desired_state: left/right panels retain intended sizes and the central viewport expands/contracts to consume available client area with no black gutters

### MS-036 — generated candidate is not reviewable before Apply
- severity: High generation UX/correctness visibility
- state: VERIFIED FIXED ON RELEASED v1.0.35
- priority: P0/P1
- evidence: successful generation in v1.0.31 reports identity-verified candidate ready but viewport/scene do not display it until Apply; user cannot review the candidate as instructed
- current_implementation_state: v1.0.32 preserves stale/conflict guards but automatically applies a non-conflicting identity-verified generation into canonical Core project state and immediately inserts the matching mesh presentation; the redundant Apply gate is no longer part of the normal successful generation path
- verification_state: PASSED on v1.0.35 reference machine: generation succeeded and the result auto-inserted without an Apply step
- desired_state: generation result is immediately visible and reviewable. User direction: remove the redundant Apply step and insert a successful generation directly; unsuitable results can be regenerated. Preserve transactional/stale-result guards while simplifying the UX.

### MS-037 — generated mesh renders with shell/back-side appearance
- severity: High viewport/render correctness
- state: REOPENED ON RELEASED v1.0.35 - REPRODUCED
- priority: P1
- evidence: applied generated knight in v1.0.31 appears visually inside-out/shell-like, as if non-user-facing/back surfaces dominate
- current_implementation_state: v1.0.32 repairs generated mesh winding/normals before export and uses correctness-oriented back-face culling with neutral matte material plus stronger directional studio lighting instead of masking orientation problems with two-sided rendering; exact-head CI for the implementation is green
- verification_state: FAILED on v1.0.35 reference machine: generated mesh still visibly shows backside/shell-like surfaces
- desired_state: normal/front-facing generated surface renders correctly from the active camera; diagnose normals/winding/material/culling rather than masking the symptom

### MS-038 — transform interaction incomplete
- severity: Medium/High modeling UX
- state: PARTIALLY VERIFIED / ROTATE REOPENED ON v1.0.35
- priority: P1
- evidence: v1.0.35 reference machine verifies direct model Move works and rotation rings are visible/useable; Rotate still behaves incorrectly because rotation does not occur around the expected stable object/origin axes and some interaction failures snap the model back toward a prior rotation
- desired_state: intuitive direct model drag where appropriate plus axis constraints; rotation exposes visible rotation rings/circles while retaining precise axis manipulation

### MS-039 — generated-object placement and scale are unsuitable
- severity: High generation/modeling UX
- state: PARTIAL PASS / REOPENED PRESENTATION SEQUENCING ON v1.0.35
- priority: P1
- evidence: inserted model in v1.0.31 has origin at grid zero so geometry is roughly bisected by the grid; generated object is tiny and scale is not user-adjustable
- current_implementation_state: v1.0.32 computes one Core-owned initial transform from generated mesh bounds: uniform scaling targets a 100-unit largest dimension, X/Z bounds are centered around workspace origin, and minimum Y is translated to grid Y=0. The transform is persisted in canonical `ProjectObject.Transform`, so save/reopen/edit paths consume the same authority rather than a viewport-only offset. Targeted `GeneratedObjectPlacementTests` plus exact-head build run 34746648597 are green at `9353b21686fe34d7699b391c9469fd8643349e53`.
- verification_state: PARTIAL on v1.0.35 reference machine: result first appears very small, then after a noticeable delay enlarges and snaps to the grid. Final placement/scale is improved, but the visible multi-stage insertion is not coherent acceptance behavior
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


## v1.0.35 reference-machine findings — 2026-09-15

### MS-014 — Quality presets are opaque / custom presets unavailable
- severity: High settings/runtime UX regression
- state: REOPENED ON RELEASED v1.0.35
- priority: P1/P2
- evidence: Settings > Quality shows a preset selector and descriptive text, but does not expose the actual adjustable values for the selected preset and no visible custom preset creation/edit controls are available; this contradicts the historical v1.0.17 completion record
- desired_state: users can inspect the parameters controlled by built-in presets and create/clone/rename/save/delete custom presets from the current supported Settings UI

### MS-043 — New project leaks previous project 2D state
- severity: Critical project-state isolation / data correctness
- state: OPEN - REPRODUCED ON v1.0.35
- priority: P0/P1 PREEMPTION
- evidence: pressing New after a project with generated 3D clears the 3D model but leaves the previous project's 2D image visible; screenshot also shows stale scene/selection presentation after reset
- desired_state: New establishes one clean authoritative project boundary; prior project 2D image/baseline, 3D objects, selections, tool bindings and project-scoped presentation cannot leak into the new project

### MS-044 — project reset leaves disposed MeshInstance3D in Stage-D path
- severity: High runtime/state-transition correctness
- state: FIXED IN v1.0.36 - NEEDS USER VERIFICATION
- priority: P1 PREEMPTION
- evidence: after New, released v1.0.35 reports Stage-D sculpt commit failed safely / durable Core restored because a disposed Godot.MeshInstance3D was accessed
- current_implementation_state: v1.0.36 per-frame stable-selection reconciliation now detects an invalid/disposed _selected before status/gizmo consumers run, clears both _selected and its stable viewport binding, and refreshes the gizmo. Focused ProtectedRegionPresentationSafetyTests guard this fail-closed cleanup. The implementation checkpoint and recovered current head both have green exact-head Core and full build/package/installer validation.
- verification_state: packaged reference-machine New-project retest required. Confirm the disposed MeshInstance3D Stage-D error no longer appears. This does not close MS-043: the separate previous-project 2D image leak remains reproduced/open.
- desired_state: project reset retires scene-node bindings atomically enough that no later Stage-D action can reference disposed presentation objects; recovery remains fail-closed without surfacing routine reset errors

### MS-045 — 3D camera view resets on tab switch
- severity: High modeling/navigation UX
- state: OPEN - REPRODUCED ON v1.0.35
- priority: P1
- evidence: zoom/orientation in 3D is lost when switching to another tab and back
- desired_state: ordinary 2D/3D/product-tab switching preserves the 3D viewport camera transform/zoom unless the user explicitly frames or resets the view

### MS-046 — RMB orbit uses unstable/wrong pivot
- severity: High modeling/navigation UX
- state: OPEN - REPRODUCED ON v1.0.35
- priority: P1
- evidence: right-drag orbit snaps toward origin and orbits a point that appears to move with the camera rather than a stable selected-object/world pivot
- desired_state: orbit uses a stable selected-object pivot when selection exists and a coherent stable fallback otherwise; camera translation must not drag the orbit pivot

### MS-047 — concept/image-edit prompt text is not restored
- severity: Medium workflow continuity
- state: OPEN - REPRODUCED ON v1.0.35
- priority: P2
- evidence: the last AI prompt disappears from the concept/image-edit prompt textbox after application reopen
- desired_state: when the relevant project/session is restored, the most recent user prompt text remains available for iteration unless explicitly cleared

### MS-048 — no user-visible project Open/Load entry point
- severity: High project workflow completeness
- state: OPEN - REPRODUCED ON v1.0.35
- priority: P1/P2
- evidence: released v1.0.35 exposes New/Import/Export but no visible Load/Open Project action, preventing the user from explicitly exercising the charter's save/reload workflow
- desired_state: provide a clear user-accessible project open/load/recovery entry point consistent with the canonical project-store model and safe migration/recovery rules

### MS-049 — 2D Enhance Selected Region backend contract mismatch
- severity: High 2D AI editing regression
- state: FIXED IN v1.0.36 - NEEDS USER VERIFICATION
- priority: P1 PREEMPTION
- evidence: reference-machine v1.0.35 "Enhance Selected Region" fails immediately with "2D detail generation failed: detail_2d() takes from 4 to 5 positional arguments but 6 were given". v1.0.36 now carries a bounded app_v1036 migration that retires only the legacy /detail-2d and /detail-3d routes and registers typed request contracts aligned with AIClient/detail_pipeline; serve.py imports that migration before serving canonical app:app.
- current_implementation_state: implemented in v1.0.36 with focused typed-contract coverage. The current exact writable head preserves canonical backend entry-point behavior and core-foundation is green.
- adjacent_code_state: the structurally inconsistent /detail-3d contract was aligned in the same bounded migration, but no reference-machine 3D detail behavior is claimed fixed without runtime evidence.
- verification_state: packaged reference-machine Enhance Selected Region retest required; confirm request/provider/path validation and actual detail generation complete without the v1.0.35 arity mismatch.
- desired_state: 2D Enhance reaches the selected local image-detail provider through one typed request contract, preserves safe path/output validation, and completes without arity/request-field mismatch.

### MS-050 — Hunyuan3D 2.1 Windows install fails on long checkout paths
- severity: High AI-runtime/model-install blocker
- state: OPEN - REPRODUCED ON v1.0.35
- priority: P1 PREEMPTION
- evidence: reference-machine install under X:\Minisculpter\Minisculpter\AIData reaches the Hunyuan3D-2.1 Git checkout, repeatedly reports "Filename too long" for upstream hy3dshape/tools/mini_trainset/preprocessed/... files, completes file transfer, then fails checkout and preserves the deterministic hunyuan21-shape partial stage. Current source routes Hunyuan through model_manager_v105._ensure_clone -> model_manager._clone_fresh, and _clone_fresh invokes a normal shallow git clone without per-command long-path handling.
- resume_risk: _ensure_clone currently returns as soon as target/.git exists. A failed checkout can therefore leave a repository metadata directory that is not a valid complete runtime worktree, so a later Resume may incorrectly skip clone/checkout repair.
- desired_state: install/resume succeeds from the existing Windows AIData root without requiring global Git/Windows reconfiguration or data relocation; long-path handling is contained to Miniscuplter's Git invocation; interrupted clone state is validated/repaired rather than trusted from .git presence alone; safe reusable staged payload remains resumable.
- suggested_validation: focused clone-command/partial-worktree regression coverage plus packaged Windows install/resume verification at a path depth at least as long as the reported reference-machine root

### MS-051 — TripoSR torchmcubes build isolation failure
- severity: High AI-runtime/model-install blocker
- state: OPEN - REPRODUCED ON v1.0.35
- priority: P1 PREEMPTION
- evidence: reference-machine TripoSR install preserves ~0.06 GB of deterministic staged data, clones tatsy/torchmcubes, then fails during pyproject metadata generation because the isolated build environment cannot import torch. The upstream package error explicitly requires PyTorch to be installed/visible and recommends building without isolation. Current Miniscuplter source installs torchmcubes through model_manager._install_triposr_dependencies -> _pip_install using ordinary pip isolation together with unrelated dependencies.
- root_cause: torchmcubes has dynamic metadata that imports/detects PyTorch at build time; pip's temporary isolated build environment does not inherit the packaged host environment's installed torch, so metadata generation fails before the dependency can build.
- desired_state: TripoSR install/resume verifies that the Miniscuplter host environment has the required torch, installs torchmcubes through a contained no-build-isolation path, retains normal build isolation for unrelated packages, preserves safe staged data across failure/retry, and completes without unnecessary source/model redownload.
- suggested_validation: focused command-construction test proving torchmcubes alone receives --no-build-isolation, host-torch preflight/fail-closed coverage, and packaged Windows install/resume verification from a preserved partial stage

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
- state: FIXED IN v1.0.32 - NEEDS USER VERIFICATION
- priority: P0
- evidence: v1.0.31 reference machine can save a project with generated/accepted baseline but reopen/reload acceptance remains unavailable from current UI; canonical persistence repair exists in v1.0.32 and must be exercised once Open/Load is user-accessible
- desired_state: accepted baseline and revision identity survive save/reopen and are restored transactionally before dependent presentation

### MS-032 — direct 3D transform controls
- severity: High modeling UX
- state: PARTIALLY VERIFIED ON v1.0.35; see MS-038
- priority: P1
- evidence: v1.0.35 reference machine confirms direct Move works and visible Rotate rings are draggable; Rotate correctness remains reopened under MS-038
- desired_state: direct object manipulation with stable transform authority and precise gizmo behavior
