# Miniscuplter Current Execution State

## Release state
- stable: v1.0.31 @ 7a2dcfd238c29639935f72de8ed635ca4efe3725
- writable: v1.0.32
- release_freeze: false
- publication_active: false
- branch_bootstrap: complete; VERSION 1.0.32
- execution_hold: none; AUTO-INC-006 recovered and patch-control is operational

## Reference-machine gate
- state: FAILED / ACTIONABLE EVIDENCE CAPTURED
- source: released v1.0.31 on Windows / GTX 1080, user screenshots and direct observations 2026-09-13
- positive evidence: grid is visible; transform toolbar functions through axes; accepted 2D baseline and generated 3D object both persist across reopen; after app restart Hunyuan generation completed successfully in ~390 s
- blocking evidence: generated candidate is invisible until Apply (MS-036); generated mesh appears shell/back-side rendered and nearly flat/unlit with insufficient surface-detail readability (MS-037); generated placement/scale unsuitable (MS-039); Smart Select unavailable (MS-040). MS-033 cancel -> retry is fixed in v1.0.32 code but remains a user-verification gate.
- UX evidence: telemetry and AI command layout still rejected (MS-026/MS-027); resize black gutters remain (MS-035); direct manipulation/rotation incomplete (MS-038); refinement controls require coherent redesign (MS-041)

## Current objective
### E — v1.0.31 reference-machine P0/P1 regression recovery
- state: E1 COMPLETE; E2 IMPLEMENTED / VALIDATION RECONCILIATION REQUIRED
- outcome: make the released-generation path immediately retryable, visible, correctly rendered and sensibly inserted, then restore the highest-impact workspace/selection usability failures without regressing durable Stage-C transaction/identity guarantees
- completed:
  1. E1 P0 / MS-033: editor-owned backend restart health-version drift fixed at `282d61e8e479058179fa3a903c5ba0d9962f5569`; targeted and full validation green there. State is FIXED IN v1.0.32 - NEEDS USER VERIFICATION.
  2. E2 implementation / MS-036: Manager recovered patch-control without weakening exact-head/blob/immutable-branch/fast-forward guards; validated patch run `34728224507` produced writable commit `01b1818a911b763346794b53cb96a7a8ee4de6a7` (`fix: insert successful 3D generation directly into scene`). The dispatched build compiled and packaged successfully but failed `release_audit.py` because the audit still asserts the superseded explicit candidate-review/Apply behavior. This is now Dev-owned candidate/audit reconciliation, not an infrastructure hold.
- current:
  3. Reconcile release-audit expectations/tests with the user-approved direct-insertion behavior while preserving transactional/stale-result guards; validate exact head. Do not revert direct insertion merely to satisfy stale audit assertions.
- queued:
  4. E3 P1: diagnose/fix MS-037 exterior-face rendering plus viewport readability: correct normals/winding/culling as appropriate, use a neutral Blender-like light-gray matte default model presentation distinct from the darker background, and provide lighting/shading that reveals surface form instead of a flat silhouette. Fix MS-039 grounding and physical scale: lowest model point rests on grid and one canonical user-facing size/scale model controls insertion.
  5. E4 P1: fix MS-040 Smart Select packaging/status and MS-035 resize/client-area composition; then reconcile MS-026/MS-027 dedicated telemetry/AI-command areas and MS-038 direct manipulation/rotation rings.
  6. E5: integrate/regress v1.0.32 checkpoint and return to Coordinator review; MS-041 Refinement/Kitbash implementation waits until E1-E4 correctness, with exact UI details reversible pending user design approval.
- constraints: Core remains durable-state authority, Godot presentation/input authority, Python inference/geometry authority; preserve local-first/storage containment, immutable revisions/stable IDs and stale-result rejection; do not solve shell rendering by blindly disabling correctness-oriented culling/normals checks; default viewport material/shading is presentation state, not durable geometry authority; keep user-facing scale represented by one canonical transform/physical-size model
- acceptance: exact targeted regressions plus strongest relevant exact-head validation green; cancel/retry succeeds without restart; successful generation is inserted/visible without redundant Apply; rendered exterior faces are correct and surface details are legible with neutral gray shading against the viewport; inserted model rests on grid at intended scale; Smart Select resolves; resize has no black gutters; workspace controls do not overlap viewport
- stop: any architecture/product choice that would create new durable authority or irreversible Refinement/Kitbash UX contract requires Coordinator/user review
- continuation: Dev should reconcile E2 audit/test expectations now, then continue automatically through E3-E5 when dependencies permit, ending at coordinator_review_required

## Deferred / user-owned design
- MS-041: add dedicated Refinement and Kitbash areas after 3D; exact UI to be worked out with user after core generation/viewport correctness.
- MS-042: Rig & Pose and Cleanup & Export are intentionally later work.

## Completed engineering checkpoint retained
- prior v1.0.32 B-C checkpoint remains valid evidence: editing/history continuity hardened and Runtime Repair health-version drift fixed; exact-head engineering validation at b5fc65e8ac1eda6b26c8f68e37c30af771204297 was green before this user-evidence preemption.
