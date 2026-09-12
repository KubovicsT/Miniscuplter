# Miniscuplter Technical Direction State

This file is the authoritative medium/long-horizon technical-direction state owned by the Project Coordinator. It stores project strategy facts only; behavioral rules remain in automation prompts. `docs/TECHNICAL_ROADMAP.md` is legacy/read-only context.\n\n- last_reconciled: 2026-09-12

## Architecture ownership
- durable_project_state: Core
- presentation_and_input: Godot
- inference_and_geometry: Python
- runtime_setup_and_delivery: launcher/updater
- architecture_mode: selective refactor; bounded ownership convergence; no broad rewrite

## Current strategic objective
- release_line: v1.0.29
- objective: harden generation job lifetime and cancellation/recovery while v1.0.28 reference-machine acceptance proceeds
- acceptance_weighted_completion: approximately 64 percent
- reference_machine: Windows; GTX 1080 8 GB VRAM; 16 GB RAM
- evidence_rule: reference-machine GUI/GPU/runtime evidence outranks CI for runtime acceptance

## Priority order
1. complete v1.0.29 semantic-version bootstrap
2. MS-020 — durable generation job/revision envelope
3. MS-020 — cancellation and recovery closure
4. integrated v1.0.29 validation and Coordinator review

## Strategic dependencies
- product work depends on bootstrap completion
- viewport acceptance follows generation and persistence correctness
- workspace composition follows viewport correctness
- release review follows integrated validation and canonical-state reconciliation

## Current deferred work
- provider-family expansion
- broad Rig/Pose expansion
- broad kitbash work
- full sculpt rewrite
- generalized scheduler/platform infrastructure
- speculative cloud services
- non-critical provider fallback expansion beyond current correctness needs

## Accepted product direction
- local-first behavior preserved
- stable IDs and immutable revisions preserved
- transactional state/history preserved
- migration before legacy removal
- viewport remains dominant visual workspace
- accepted direct viewport tool buttons preserved
- AI console target: bottom center between scene tree and right panel
- contextual AI actions target: vertical stack beside console
- resource telemetry target: compact 2x2 bottom-left
- orientation target: Blender-style circular XYZ gizmo without cube body

## Release policy state
- Coordinator owns release readiness, chunk size and publication
- Dev prepares release-worthy checkpoints but never publishes
- only actual release-control initiation freezes the source version branch
- published releases/tags are immutable
