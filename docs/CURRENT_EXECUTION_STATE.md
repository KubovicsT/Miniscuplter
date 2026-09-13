# Miniscuplter Current Execution State

## Release state
- stable: v1.0.31 @ 7a2dcfd238c29639935f72de8ed635ca4efe3725
- writable: v1.0.32
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.32
- execution_hold: AUTOMATION/RELEASE INFRA REVIEW REQUESTED — Objective E2 is blocked after the approved patch-control route exhausted bounded retries; AUTO-INC-006 is open for Automation Manager

## Reference-machine gate
- state: FAILED / ACTIONABLE EVIDENCE CAPTURED
- source: released v1.0.31 on Windows / GTX 1080, user screenshots and direct observations 2026-09-13
- positive evidence: grid is visible; transform toolbar functions through axes; after app restart Hunyuan generation completed successfully in ~390 s and produced an identity-verified result; applying it created a scene object; after closing and reopening the app with that generated 3D object present, the object was restored and remained selectable/transformable with viewport Move/Rotate tools, confirming generated-object rehydration/edit continuity on the reference machine
- blocking evidence: generated candidate is invisible until Apply (MS-036); generated mesh appears shell/back-side rendered (MS-037); generated placement/scale unsuitable (MS-039); Smart Select unavailable (MS-040). MS-033 cancel -> retry is fixed in v1.0.32 code but remains a user-verification gate.
- UX evidence: telemetry and AI command layout still rejected (MS-026/MS-027); resize black gutters remain (MS-035); direct manipulation/rotation incomplete (MS-038); refinement controls require coherent redesign (MS-041)

## Current objective
### E — v1.0.31 reference-machine P0/P1 regression recovery
- state: E1 COMPLETE; E2 BLOCKED ON PATCH-CONTROL REVIEW
- outcome: make the released-generation path immediately retryable, visible, correctly rendered and sensibly inserted, then restore the highest-impact workspace/selection usability failures without regressing durable Stage-C transaction/identity guarantees
- completed:
  1. E1 P0 / MS-033: traced cancel recovery to editor-owned backend restart; removed stale `1.0.26` health-version gate in `BackendLauncher` by deriving the packaged editor assembly version while retaining instance-token identity. Added health-protocol regression coverage. Exact implementation/test head `282d61e8e479058179fa3a903c5ba0d9962f5569`; core-foundation run `34727090127` and full build run `34727090071` green. State is FIXED IN v1.0.32 - NEEDS USER VERIFICATION.
- current_blocker:
  2. E2 P0/P1 / MS-036: user-approved direct insertion/auto-apply implementation requires editing large existing `Scripts/Main.V1020StageCBridge.cs`. The validated patch-control request failed closed three times before target-branch mutation (runs `34727223762`, `34727296910`, `34727357199`). Bounded retries are exhausted. AUTO-INC-006 records the unresolved patch-control/request-handling blocker; Automation Manager owns recovery. Do not bypass patch-control or weaken its guards.
- queued_after_blocker:
  3. E3 P1: diagnose/fix MS-037 front/back surface rendering and MS-039 ground placement; lowest model point rests on grid; add coherent user-facing pre-generation physical size/model-height and grid-scale controls without introducing duplicate scale authority
  4. E4 P1: fix MS-040 Smart Select packaging/status and MS-035 resize/client-area composition; then reconcile MS-026/MS-027 dedicated telemetry/AI-command areas and MS-038 direct manipulation/rotation rings
  5. E5: integrate/regress v1.0.32 checkpoint and return to Coordinator review; MS-041 Refinement/Kitbash product-surface implementation may begin only after E1-E4 correctness and with exact UI details kept reversible pending user design approval
- constraints: Core remains durable-state authority, Godot presentation/input authority, Python inference/geometry authority; preserve local-first/storage containment, immutable revisions/stable IDs and stale-result rejection; do not solve shell rendering by blindly disabling correctness-oriented culling/normals checks; keep user-facing scale represented by one canonical transform/physical-size model
- acceptance: exact targeted regressions plus strongest relevant exact-head validation green; cancel/retry succeeds without restart; successful generation is visible without redundant Apply; rendered exterior faces are correct; inserted model rests on grid at intended scale; Smart Select resolves; resize has no black gutters; workspace controls do not overlap viewport
- stop: any architecture/product choice that would create new durable authority or irreversible Refinement/Kitbash UX contract requires Coordinator/user review
- continuation: resume E2 only after Manager resolves AUTO-INC-006 and a safe patch-capable route is available; then continue automatically through E3-E5 when dependencies permit, ending at coordinator_review_required

## Deferred / user-owned design
- MS-041: add dedicated Refinement and Kitbash areas after 3D; exact UI to be worked out with user after core generation/viewport correctness.
- MS-042: Rig & Pose and Cleanup & Export are intentionally later work.

## Completed engineering checkpoint retained
- prior v1.0.32 B-C checkpoint remains valid evidence: editing/history continuity hardened and Runtime Repair health-version drift fixed; exact-head engineering validation at b5fc65e8ac1eda6b26c8f68e37c30af771204297 was green before this user-evidence preemption.
