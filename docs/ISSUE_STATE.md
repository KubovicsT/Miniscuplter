# Miniscuplter Active Issue State

This file is the authoritative current-state ledger for active, release-relevant, or user-verification-pending issues. Historical issue narratives remain in `docs/ISSUES.md`, which is legacy/read-only context.

## MS-020 — AI job/runtime ownership
- severity: Critical for current v1.0.28 workflow
- state: IN PROGRESS
- priority: P0
- latest_reference_machine_evidence: released v1.0.27 Generate 3D fails because heavyweight generation ownership conflicts with model/runtime release or unload lifecycle
- desired_state: one authoritative heavyweight runtime owner through active 2D-to-3D generation; no unload/release conflict with the active job
- architecture_scope: local-first runtime containment; Python inference/geometry; durable job identity remains compatible with Core state authority
- verification_state: NEEDS CODE FIX AND GTX 1080 USER VERIFICATION
- related_current_objective: B

## MS-031 — accepted-baseline persistence across reopen
- severity: Critical persistence/state-restoration defect
- state: OPEN
- priority: P0
- first_reference_machine_evidence: released v1.0.27 retains the accepted concept image after project reopen but loses accepted-baseline state and immediate Generate 3D eligibility
- desired_state: baseline identity, accepted status and generation eligibility persist and restore together across save/close/reopen
- architecture_scope: Core remains durable-state authority; no duplicate UI-only acceptance state
- verification_state: NEEDS IMPLEMENTATION, SAVE/REOPEN REGRESSION, AND USER VERIFICATION
- related_current_objective: C

## MS-009 — viewport grid/render ownership
- severity: Critical
- state: IN PROGRESS
- priority: after MS-020 and MS-031
- latest_reference_machine_evidence: released v1.0.27 on GTX 1080 has no visible viewport grid
- desired_state: visible neutral non-occluding grid at launch and across resize without duplicate render/world ownership
- verification_state: NEEDS CODE FIX AND GTX 1080 USER VERIFICATION
- related_current_objective: D

## MS-026 — resource telemetry layout
- severity: Medium product/observability
- state: IN PROGRESS
- latest_reference_machine_evidence: telemetry works in v1.0.27 but wide horizontal layout consumes too much workspace
- desired_state: compact 2x2 bottom-left telemetry without materially reducing inference performance
- verification_state: NEEDS UI CORRECTION AND USER VERIFICATION
- related_current_objective: E

## MS-027 — workspace composition
- severity: High product/UX acceptance
- state: IN PROGRESS
- latest_reference_machine_evidence: v1.0.27 viewport tool buttons accepted; multiline AI console location/width rejected; telemetry placement rejected; cube orientation control rejected
- desired_state: bottom-center multiline AI console between scene tree and right panel; vertical contextual actions; compact 2x2 bottom-left telemetry; Blender-style circular XYZ gizmo without cube body; accepted viewport tools preserved
- verification_state: NEEDS UI CORRECTION AND USER VERIFICATION
- related_current_objective: E

## MS-029 — updater leaves launcher closed
- severity: Critical release-path regression
- state: FIXED - NEEDS USER VERIFICATION
- latest_reference_machine_evidence: v1.0.25 to v1.0.27 update left launcher closed; still consistent with the immutable old-updater transition limitation
- desired_state: update initiated by installed v1.0.27 leaves the healthy updated launcher open; failed health still rolls back safely
- verification_state: WAITING FOR NEXT RELEASE TRANSITION FROM v1.0.27

## MS-030 — packaged backend health after Runtime Repair
- severity: Critical
- state: FIXED - NEEDS USER VERIFICATION
- current_implementation_state: canonical packaged server entry point and isolated Repair health identity are implemented
- desired_state: Repair succeeds only after its own backend is healthy; Generate 3D then passes backend health into provider resolution/inference
- verification_state: GTX 1080 RELEASED-BUILD RETEST REQUIRED

## Historical ledger
- legacy_source: docs/ISSUES.md
- historical_attempts_and_old_issue_records: preserved in legacy source and Git history
