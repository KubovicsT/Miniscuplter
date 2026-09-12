# Miniscuplter Technical Direction State

This file is the authoritative medium/long-horizon technical-direction state owned by the Project Coordinator. It stores project strategy facts only; behavioral rules remain in automation prompts. `docs/TECHNICAL_ROADMAP.md` is legacy/read-only context.

- last_reconciled: 2026-09-12

## Architecture ownership
- durable_project_state: Core
- presentation_and_input: Godot
- inference_and_geometry: Python
- runtime_setup_and_delivery: launcher/updater
- architecture_mode: selective refactor; bounded ownership convergence; no broad rewrite

## Current strategic objective
- stable_release: v1.0.29
- release_line: v1.0.30
- objective: convert the now-durable generation-job lifetime into a fully transactional generation-result workflow, then remove only proven duplicate authority along the 3D/viewport path
- acceptance_weighted_completion: approximately 66 percent
- reference_machine: Windows; GTX 1080 8 GB VRAM; 16 GB RAM
- evidence_rule: reference-machine GUI/GPU/runtime evidence outranks CI for runtime acceptance

## Priority order
1. harden generation-result commit, stale-result rejection, recovery and save/reload continuity
2. audit viewport/state ownership convergence and remove safely migrated redundancy where it affects correctness
3. integrated v1.0.30 validation and Coordinator review

## Strategic dependencies
- v1.0.30 semantic bootstrap is complete and exact-head baseline is green
- generation-result durability follows the v1.0.29 job-envelope/cancellation foundation
- viewport/state cleanup follows generation/persistence correctness and must preserve migration-before-removal
- reference-machine evidence may preempt planned cleanup when it reproduces a P0 defect
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
- v1.0.29 is published and frozen; v1.0.30 is the legitimate writable successor branch
