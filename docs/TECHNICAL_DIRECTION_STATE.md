# Miniscuplter Technical Direction State

This file is the authoritative medium/long-horizon technical-direction state owned by the Project Coordinator. It stores project strategy facts only; behavioral rules remain in automation prompts. `docs/TECHNICAL_ROADMAP.md` is legacy/read-only context.

- last_reconciled: 2026-09-13

## Architecture ownership
- durable_project_state: Core
- presentation_and_input: Godot
- inference_and_geometry: Python
- runtime_setup_and_delivery: launcher/updater
- architecture_mode: selective refactor; bounded ownership convergence; no broad rewrite

## Current strategic objective
- stable_release: v1.0.31 @ 7a2dcfd238c29639935f72de8ed635ca4efe3725
- writable_release_line: v1.0.32
- objective: use v1.0.31 reference-machine evidence to harden the complete local 2D→3D modeling loop: cancellation/retry runtime health, immediate generated-result visibility, correct mesh rendering, physical placement/scale, direct manipulation, Smart Select and non-overlapping workspace composition
- reference_machine: Windows; GTX 1080 8 GB VRAM; 16 GB RAM
- evidence_rule: reference-machine GUI/GPU/runtime evidence outranks CI for runtime acceptance

## Priority order
1. cancel/retry runtime lifecycle correctness; no app restart required
2. successful generation immediately visible/usable with stale-result and durable-identity guarantees preserved
3. generated surface rendering, grid-ground placement and user-facing physical size/scale correctness
4. Smart Select packaging plus resize/workspace/direct-manipulation usability
5. coherent Refinement/Kitbash product surface after core loop correctness; later Rig & Pose and Cleanup & Export implementation

## Strategic dependencies
- v1.0.31 proves Hunyuan generation can complete on the GTX 1080 (~390 s observed), so current work is reliability/interaction correctness rather than provider replacement
- v1.0.32 editing/history and Runtime Repair health-version hardening remain valid foundations
- any reproduced P0 reference-machine failure preempts release consideration
- generated-result UX simplification must preserve Core transaction/identity/stale-result authority rather than moving durable state into Godot
- physical size must have one canonical representation; UI controls must not create competing scale authority

## Accepted product direction
- local-first behavior preserved
- stable IDs and immutable revisions preserved
- transactional state/history preserved
- STL remains export/interchange, never authoritative project storage
- migration before legacy removal
- viewport remains dominant visual workspace
- accepted direct viewport tool buttons preserved
- resource telemetry belongs in the bottom-left application workspace and must not cover the viewport/AI command area
- AI command input gets a dedicated non-overlay bottom-center area expanded across available width between left and right rails
- side-panel widths remain stable through ordinary resize; central viewport absorbs client-area size changes; no black exterior gutters
- orientation remains Blender-style circular XYZ gizmo without cube body
- successful 3D generation should become visible/inserted directly rather than requiring an invisible-candidate Apply step; regeneration is the rejection path, while transactional identity/stale-result guards remain mandatory
- newly inserted generated geometry is grounded by its lowest point and starts near a user-selected physical model height/scale; grid scale/physical sizing are user-facing before generation
- transform UX supports axis precision plus intuitive direct model manipulation; rotation gains ring/circle controls
- 3D enhancement/redesign moves toward dedicated Refinement and Kitbash areas after 3D; exact UI remains intentionally reversible and will be worked out with the user

## Deferred work
- Rig & Pose implementation (user explicitly deferred)
- Cleanup & Export product-surface completion (user explicitly deferred; existing export correctness remains maintained)
- exact Refinement/Kitbash UI contract beyond the approved structural direction
- provider-family expansion
- full sculpt rewrite
- speculative cloud services
- non-critical provider fallback expansion

## Release policy state
- Coordinator owns release readiness, chunk size and publication
- Dev prepares release-worthy checkpoints but never publishes
- only actual release-control initiation freezes the source version branch
- published releases/tags and their semantic branches are immutable history
- v1.0.31 is published/frozen at exact candidate 7a2dcfd238c29639935f72de8ed635ca4efe3725
- v1.0.32 is the authoritative writable successor
