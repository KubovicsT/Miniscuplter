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
- stable_release: v1.0.30
- release_line: v1.0.31
- objective: extend the now-transactional 2D-to-3D result path through durable generated-object rehydration, basic cleanup and exact-scope export while preserving storage containment and offline/local-first behavior
- acceptance_weighted_completion: approximately 68 percent
- reference_machine: Windows; GTX 1080 8 GB VRAM; 16 GB RAM
- evidence_rule: reference-machine GUI/GPU/runtime evidence outranks CI for runtime acceptance

## Priority order
1. generated-object save/reload, revision, selection and edit continuity
2. transactional basic cleanup and exact durable-revision export scope
3. Stage-C storage/offline containment audit
4. integrated v1.0.31 thin-slice validation and Coordinator review

## Strategic dependencies
- v1.0.30 generation result/cancellation ownership is complete and published
- v1.0.31 semantic bootstrap and exact-head baseline are green
- cleanup/export integrity depends on durable generated-object rehydration
- storage/offline audit follows the working thin slice and must not introduce duplicate storage authority
- any new reference-machine P0 evidence preempts planned downstream work

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
- STL remains export/interchange, never authoritative project storage
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
- v1.0.30 is published and frozen; v1.0.31 is the legitimate writable successor branch
