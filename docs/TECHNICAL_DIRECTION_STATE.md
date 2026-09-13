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
- stable_release: v1.0.32
- writable_release_line: v1.0.33
- objective: qualify the released v1.0.32 end-to-end recovery checkpoint on the Windows / GTX 1080 reference machine, then use real runtime evidence to choose the next v1.0.33 implementation slice
- reference_machine: Windows; GTX 1080 8 GB VRAM; 16 GB RAM
- evidence_rule: reference-machine GUI/GPU/runtime evidence outranks CI for runtime acceptance

## Priority order
1. reference-machine verification of v1.0.32 cancellation/retry, direct generation insertion, rendering/readability, grounding/scale, Smart Select, resize/workspace and transform interaction
2. immediately repair any reproduced P0/P1 regression before adding feature breadth
3. after the recovery checkpoint is verified, design the exact Refinement/Kitbash interaction with the user and implement it as a coherent product surface
4. later Rig & Pose and Cleanup & Export implementation
5. provider expansion / broad sculpt-system work only when evidence makes it a higher-value dependency

## Strategic dependencies
- v1.0.32 is a verified published build and is now the runtime acceptance target
- v1.0.33 is the authoritative writable successor with canonical VERSION bootstrap complete
- user/reference-machine evidence determines whether the recovery issues close or preempt new work
- generated-result UX must preserve Core transaction/identity/stale-result authority rather than moving durable state into Godot
- physical size and transforms retain one canonical Core representation

## Accepted product direction
- local-first behavior preserved
- stable IDs and immutable revisions preserved
- transactional state/history preserved
- STL remains export/interchange, never authoritative project storage
- migration before legacy removal
- viewport remains dominant visual workspace
- resource telemetry belongs in the bottom-left application workspace without covering viewport/command UI
- AI command input has a dedicated non-overlay bottom-center area
- side rails remain stable through resize while the central viewport absorbs client-area changes
- orientation remains Blender-style circular XYZ gizmo without cube body
- successful 3D generation inserts directly; regeneration is the rejection path while transactional identity/stale-result guards remain mandatory
- generated geometry is grounded and begins at a coherent user-facing physical scale
- transform UX supports precise axis manipulation plus intuitive direct movement and visible rotation rings
- 3D enhancement/redesign moves toward dedicated Refinement and Kitbash areas; exact UI remains intentionally reversible and user-approved

## Deferred work
- exact Refinement/Kitbash UI contract until v1.0.32 verification feedback
- Rig & Pose implementation
- Cleanup & Export product-surface completion
- provider-family expansion
- full sculpt rewrite
- speculative cloud services
- non-critical provider fallback expansion

## Release policy state
- Coordinator owns release readiness, chunk size and publication
- Dev prepares release-worthy checkpoints but never publishes
- published releases/tags and their semantic branches are immutable history
- v1.0.32 is published/frozen
- v1.0.33 is the authoritative writable successor
- no further release consideration until a coherent v1.0.33 scope is established from user verification or another evidence-backed priority
