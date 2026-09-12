# Miniscuplter Active Issue State

This file is the authoritative current-state ledger for active, release-relevant, or user-verification-pending issues. Historical issue narratives remain in `docs/ISSUES.md`, which is legacy/read-only context.

## MS-020 — AI job/runtime ownership
- severity: Critical generation/runtime correctness
- state: FIXED - NEEDS USER VERIFICATION
- priority: P0
- latest_reference_machine_evidence: released v1.0.27 Generate 3D failed because heavyweight generation ownership conflicted with model/runtime release lifecycle
- current_implementation_state: v1.0.29 persists a stable generation job/project/input-revision/output-object envelope before submission, verifies returned identity, prevents stale/obsolete results from silently applying, preserves envelope identity across save/reload, transactionally retires cancelled envelopes, rejects late cancelled results, gives retries fresh identities, and retains the existing backend hard-cancel/restart health gate before replacement heavyweight work; focused regressions and exact-head integrated CI are green
- desired_state: one authoritative heavyweight runtime owner through active 2D-to-3D generation; no unload/release conflict with the active job; cancelled/stale work cannot overlap or apply against replacement work
- verification_state: GTX 1080 v1.0.29 RELEASED-BUILD GENERATE 3D + CANCEL/RETRY RETEST REQUIRED

## MS-031 — accepted-baseline persistence across reopen
- severity: Critical persistence/state-restoration defect
- state: FIXED - NEEDS USER VERIFICATION
- priority: P0
- first_reference_machine_evidence: released v1.0.27 retained the accepted concept image after project reopen but lost accepted-baseline state and immediate Generate 3D eligibility
- current_implementation_state: accepted-baseline restoration now runs after UI composition on reopen; targeted save/reopen restoration regression and exact-head CI are green
- desired_state: baseline identity, accepted status and generation eligibility persist and restore together across save/close/reopen
- verification_state: GTX 1080 v1.0.29 RELEASED-BUILD SAVE/CLOSE/REOPEN RETEST REQUIRED

## MS-009 — viewport grid/render ownership
- severity: Critical
- state: FIXED - NEEDS USER VERIFICATION
- priority: after MS-020 and MS-031
- latest_reference_machine_evidence: released v1.0.27 on GTX 1080 has no visible viewport grid
- current_implementation_state: grid bars now render two-sided with targeted regression coverage; exact-head CI is green
- desired_state: visible neutral non-occluding grid at launch and across resize without duplicate render/world ownership
- verification_state: GTX 1080 v1.0.29 RELEASED-BUILD GRID/RESIZE RETEST REQUIRED

## MS-026 — resource telemetry layout
- severity: Medium product/observability
- state: FIXED - NEEDS USER VERIFICATION
- latest_reference_machine_evidence: telemetry worked in v1.0.27 but wide horizontal layout consumed too much workspace
- current_implementation_state: telemetry is compacted into a bottom-left 2x2 overlay; workspace regression and exact-head CI are green
- desired_state: compact 2x2 bottom-left telemetry without materially reducing inference performance
- verification_state: v1.0.29 TARGET-MACHINE UX RETEST REQUIRED

## MS-027 — workspace composition
- severity: High product/UX acceptance
- state: FIXED - NEEDS USER VERIFICATION
- latest_reference_machine_evidence: v1.0.27 viewport tool buttons accepted; multiline AI console location/width rejected; telemetry placement rejected; cube orientation control rejected
- current_implementation_state: AI console moved to viewport bottom center; contextual actions remain adjacent; compact telemetry applied; orientation cube replaced by explicit circular XYZ axis control; accepted viewport tools preserved; exact-head CI is green
- desired_state: bottom-center multiline AI console between scene tree and right panel; vertical contextual actions; compact 2x2 bottom-left telemetry; Blender-style circular XYZ gizmo without cube body; accepted viewport tools preserved
- verification_state: v1.0.29 TARGET-MACHINE UX RETEST REQUIRED

## MS-029 — updater leaves launcher closed
- severity: Critical release-path regression
- state: FIXED - NEEDS USER VERIFICATION
- latest_reference_machine_evidence: v1.0.25 to v1.0.27 update left launcher closed; still consistent with the immutable old-updater transition limitation
- desired_state: update initiated by installed v1.0.27 leaves the healthy updated launcher open; failed health still rolls back safely
- verification_state: v1.0.29 IS NOW PUBLISHED — TEST UPDATE FROM INSTALLED v1.0.27 TO v1.0.29

## MS-030 — packaged backend health after Runtime Repair
- severity: Critical
- state: FIXED - NEEDS USER VERIFICATION
- current_implementation_state: canonical packaged server entry point and isolated Repair health identity are implemented
- desired_state: Repair succeeds only after its own backend is healthy; Generate 3D then passes backend health into provider resolution/inference
- verification_state: GTX 1080 v1.0.29 RELEASED-BUILD RETEST REQUIRED

## Historical ledger
- legacy_source: docs/ISSUES.md
- historical_attempts_and_old_issue_records: preserved in legacy source and Git history
