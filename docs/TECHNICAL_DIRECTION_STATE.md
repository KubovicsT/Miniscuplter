# Miniscuplter Technical Direction State

This file is the authoritative medium/long-horizon technical-direction state owned by the Project Coordinator. It stores project strategy facts only; behavioral rules remain in automation prompts. `docs/TECHNICAL_ROADMAP.md` is legacy/read-only context.

## Architecture ownership
- durable_project_state: Core
- presentation_and_input: Godot
- inference_and_geometry: Python
- runtime_setup_and_delivery: launcher/updater
- architecture_mode: selective refactor; bounded ownership convergence; no broad rewrite

## Current strategic objective
- release_line: v1.0.28
- objective: restore reliable end-to-end 2D-to-3D workflow and released-build acceptance before optional breadth
- acceptance_weighted_completion: approximately 64 percent
- reference_machine: Windows; GTX 1080 8 GB VRAM; 16 GB RAM
- evidence_rule: reference-machine GUI/GPU/runtime evidence outranks CI for runtime acceptance

## Priority order
1. complete v1.0.28 semantic-version bootstrap and exact-head validation
2. P0 MS-020 — Generate 3D heavyweight-runtime ownership correctness
3. P0 MS-031 — accepted-baseline persistence/restoration across save/close/reopen
4. MS-009 — visible stable non-occluding viewport grid
5. MS-026/MS-027 — bounded workspace composition corrections
6. integrated v1.0.28 validation and Coordinator release review

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
