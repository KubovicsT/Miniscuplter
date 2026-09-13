# Miniscuplter Current Execution State

## Release state
- stable: v1.0.31 @ 7a2dcfd238c29639935f72de8ed635ca4efe3725
- writable: v1.0.32
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.32
- execution_hold: COORDINATOR REVIEW REQUIRED

## Reference-machine gate
- state: FAILED / ACTIONABLE EVIDENCE CAPTURED; v1.0.32 FIXES NEED USER VERIFICATION
- source: released v1.0.31 on Windows / GTX 1080, user screenshots and direct observations 2026-09-13
- positive evidence: grid is visible; transform toolbar functions through axes; accepted 2D baseline and generated 3D object both persist across reopen; after app restart Hunyuan generation completed successfully in ~390 s
- v1.0.32 fixes awaiting user verification: MS-026/MS-027 dedicated non-overlay telemetry/AI command workspace; MS-033 cancel -> immediate retry; MS-035 client-area resize/fixed rail behavior; MS-036 automatic generated-object insertion without Apply; MS-037 exterior/winding plus neutral readable viewport shading; MS-038 direct Move interaction and visible rotation rings; MS-039 canonical Core-owned placement/scale with lowest point on grid; MS-040 Smart Select editor/backend semantic route contract
- remaining engineering evidence: none in the authorized E1-E5 recovery queue; Coordinator review owns next scope/release decision

## Current objective
### E — v1.0.31 reference-machine P0/P1 regression recovery
- state: E1 COMPLETE; E2 COMPLETE; E3 IMPLEMENTATION COMPLETE / NEEDS USER VERIFICATION; E4 IMPLEMENTATION COMPLETE / NEEDS USER VERIFICATION; E5 COMPLETE; COORDINATOR REVIEW REQUIRED
- outcome: make the released-generation path immediately retryable, visible, correctly rendered and sensibly inserted, then restore the highest-impact workspace/selection usability failures without regressing durable Stage-C transaction/identity guarantees
- completed:
  1. E1 P0 / MS-033: editor-owned backend restart health-version drift fixed at `282d61e8e479058179fa3a903c5ba0d9962f5569`; targeted and full validation green there. State is FIXED IN v1.0.32 - NEEDS USER VERIFICATION.
  2. E2 P0/P1 / MS-036: successful identity-verified 3D generations now transactionally apply and insert directly into the visible scene while conflicts remain preserved for review. Exact-head build run `34736924339` is green. State is FIXED IN v1.0.32 - NEEDS USER VERIFICATION.
  3. E3 P1 / MS-037 + MS-039: winding/normals repair plus correctness-oriented back-face culling and neutral studio shading are implemented. Generated meshes now receive one Core-owned initial transform derived from mesh bounds: uniform target largest dimension 100 world units, X/Z centered, minimum Y grounded at Y=0. This canonical transform is persisted in `ProjectObject.Transform` and therefore survives save/reopen/edit without a Godot-only placement authority. Targeted `GeneratedObjectPlacementTests` are wired into Core.Tests; exact-head build run `34746648597` is green at `9353b21686fe34d7699b391c9469fd8643349e53`. Both issues remain NEEDS USER VERIFICATION for visual/physical acceptance.
  4. E4 / MS-040 sub-objective: Smart Select `Not Found` was traced to a concrete API mismatch: the editor posts mesh queries to `/semantic-select`, while the packaged backend exposed only the obsolete `/smart-select` image contract. v1.0.32 now exposes the exact mesh semantic route, uses contained mesh-input validation, and forwards to the existing CLIPSeg/geometry fallback selector. `SmartSelectContractTests` guards the client/backend contract. Core-foundation run `34747264663` and full build run `34747264693` are green at exact code/test head `edf0376b55b3e5cf202dcf35544ee00d4efb35ed`. State is FIXED IN v1.0.32 - NEEDS USER VERIFICATION.
  5. E4 / MS-035 sub-objective: resize gutters/layout drift were traced to a top-level programmatic UI root lacking an explicit client-area resize owner under the non-Control scene root, plus proportional right-rail resizing. v1.0.32 now syncs that root to the visible viewport client rect, holds the right rail at 330 units and lets the central viewport absorb size changes without reintroducing competing SubViewport size authority. `WorkspaceResizeContractTests` guards the contract. Core-foundation run `34747505948` and full build run `34747505866` are green at exact code/test head `c7507a6384f3fc4ebdbd32dee263507b7fb66027`. State is FIXED IN v1.0.32 - NEEDS USER VERIFICATION.
  6. E4 / MS-026 + MS-027 workspace composition: one bottom application-workspace dock now owns a fixed 300-unit telemetry lane, an expandable bottom-center AI command lane and a 330-unit right-rail alignment gutter. The AI command surface no longer belongs to `ViewportHost`; contextual actions are horizontal to preserve prompt width. Existing telemetry is reparented into the lower-left dock immediately after installation. `WorkspaceAcceptanceTests` rejects viewport-overlay command ownership and requires the bottom-dock composition contract. Core-foundation run `34748891791` and full build/package run `34748891535` are green at exact code/test head `ae8f24a6d460d182be5cd8332634b87d4bda9763`. Both issues are FIXED IN v1.0.32 - NEEDS USER VERIFICATION.
  7. E4 / MS-038 direct manipulation: existing precise axis handles remain authoritative for constrained Move/Rotate/Scale; Move now also supports direct model dragging in a camera-facing view plane, and Rotate exposes visible/hit-testable X/Y/Z rings. Direct and constrained interactions reuse the existing Stage-C transform gesture observer so Core remains the only durable transform authority. Implementation head `69788871b7f68d6ad14f705e22e9bdb987901d54`; full exact-head build/package run `34750856469` is green. State is FIXED IN v1.0.32 - NEEDS USER VERIFICATION.
  8. E5 integrated qualification: reconciled checkpoint `0602623d434db9546aec4e09a98e41325ec1dd43` passed exact-head core-foundation run `34751071049` and full build/package run `34751071058`, including C# build, Python/runtime/core logic, backend lifecycle, geometry, workspace regression, release audit and packaging. No additional candidate defect was found.
- current:
  9. COORDINATOR REVIEW REQUIRED: evaluate the qualified v1.0.32 checkpoint and decide next scope/release action. Dev must not publish or initiate release control.
- queued:
  10. MS-041 Refinement/Kitbash implementation remains deferred pending Coordinator/user-approved UI direction after this checkpoint.
- constraints: Core remains durable-state authority, Godot presentation/input authority, Python inference/geometry authority; preserve local-first/storage containment, immutable revisions/stable IDs and stale-result rejection; do not solve shell rendering by blindly disabling correctness-oriented culling/normals checks; default viewport material/shading is presentation state, not durable geometry authority; keep user-facing scale represented by one canonical transform/physical-size model
- acceptance: exact targeted regressions plus strongest relevant exact-head validation green; cancel/retry succeeds without restart; successful generation is inserted/visible without redundant Apply; rendered exterior faces are correct and surface details are legible with neutral gray shading against the viewport; inserted model rests on grid at intended scale; Smart Select resolves; resize has no black gutters; workspace controls do not overlap viewport; direct transform interaction is intuitive and rotation rings are visible
- stop: Coordinator review now required; any architecture/product choice that would create new durable authority or irreversible Refinement/Kitbash UX contract requires Coordinator/user review
- continuation: Dev stops queue advancement at this Coordinator-review hold

## Deferred / user-owned design
- MS-041: add dedicated Refinement and Kitbash areas after 3D; exact UI to be worked out with user after core generation/viewport correctness.
- MS-042: Rig & Pose and Cleanup & Export are intentionally later work.

## Completed engineering checkpoint retained
- prior v1.0.32 B-C checkpoint remains valid evidence: editing/history continuity hardened and Runtime Repair health-version drift fixed; exact-head engineering validation at b5fc65e8ac1eda6b26c8f68e37c30af771204297 was green before this user-evidence preemption.