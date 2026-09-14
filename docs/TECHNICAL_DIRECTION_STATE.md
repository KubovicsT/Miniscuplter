# Miniscuplter Technical Direction State

This file is the authoritative medium/long-horizon technical-direction state owned by the Project Coordinator. It stores project strategy facts only; behavioral rules remain in automation prompts. `docs/TECHNICAL_ROADMAP.md` is legacy/read-only context.

- last_reconciled: 2026-09-14

## Architecture ownership
- durable_project_state: Core
- presentation_and_input: Godot
- inference_and_geometry: Python
- runtime_setup_and_delivery: launcher/updater
- architecture_mode: selective refactor; bounded ownership convergence; no broad rewrite

## Current strategic objective
- stable_release: v1.0.33
- writable_release_line: v1.0.34
- objective: use v1.0.33 as the latest reference-machine runtime-test checkpoint while v1.0.34 continues the next bounded UI-neutral Stage-D/E ownership-convergence batch
- reference_machine: Windows; GTX 1080 8 GB VRAM; 16 GB RAM
- evidence_rule: reference-machine GUI/GPU/runtime evidence outranks CI for runtime acceptance
- preemption_rule: any reproduced P0/P1 regression in the latest user-tested release preempts lower-priority current-version foundation work after Coordinator reconciliation

## Priority order
1. keep v1.0.32 cancellation/retry, direct insertion, rendering/readability, grounding/scale, Smart Select, resize/workspace and transform interaction open for reference-machine verification
2. in parallel, converge attachment/Kitbash durable authority onto Core stable object/attachment identity and transactional persistence/history without changing the unresolved user-facing UI
3. establish revision-bound Refinement candidate/dependency foundations: exact object/input revision, selection/protected-region bindings, output lineage, stale/conflict handling, apply/discard and undo/redo semantics
4. explicitly preserve/transfer/invalidate revision-dependent selections, protected regions and attachments across topology/revision changes, with save/reopen coverage
5. only after the durable foundation and relevant runtime evidence are sound, design the exact Refinement/Kitbash interaction with the user; Rig & Pose and Cleanup & Export remain later

## Strategic dependencies
- v1.0.33 is the latest verified published build and preferred runtime acceptance target
- v1.0.34 is the authoritative writable successor with canonical VERSION bootstrap complete
- user/reference-machine verification is a local acceptance dependency, not a global stop while independent safe work exists
- Core already contains stable AttachmentRecord, CandidateRecord, object/revision identity and revision-bound selection models; legacy Godot attachment paths still include display-name/DTO ownership and must converge rather than become a second durable authority
- stale AI/refinement outputs must become explicit conflict/discard state and must never overwrite newer revisions
- generated-result UX, attachments, transforms and physical size must preserve one canonical Core representation

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
- durable Refinement/Kitbash foundations may progress before exact UI design when they are presentation-neutral and compatible with the accepted Core/Godot/Python authority split

## Deferred work
- exact Refinement/Kitbash UI contract
- Rig & Pose implementation
- Cleanup & Export product-surface completion
- provider-family expansion
- full sculpt rewrite
- speculative cloud services
- non-critical provider fallback expansion

## Release policy state
- Coordinator owns release readiness, chunk size and publication; Dev prepares release-worthy checkpoints but never publishes
- published releases/tags and their semantic branches are immutable history
- active-development cadence target: approximately two meaningful runtime-test releases per day when safe/coherent work supports it; this is a planning target, never a clock-triggered publication requirement
- normal release envelope: one coherent test theme containing roughly 2–4 meaningful related changes or about 3–6 hours of active development; once that batch is coherent, validated and useful to test, adjacent nonessential work rolls forward to the next semantic version instead of prolonging the current one
- every ordinary release should provide useful runtime feedback value through visible workflow change, reliability/recovery behavior, persistence/history behavior, performance/runtime behavior, or a regression-prone foundation change that merits target-machine testing
- do not publish an isolated trivial bug fix as a normal version; bundle it with the smallest coherent related test slice unless severity, testing blockage or release-safety risk justifies an expedited fix
- do not hold a ready testable batch for perfection, unrelated cleanup, or completion of an entire architectural workstream
- candidate gates remain strict: exact-head automated validation/package gates green, canonical state coherent, no known release-blocking regression, and no active publication conflict; pending user verification of prior releases is not itself a publication blocker
- after successful publication, immediately establish the validated next semantic-version branch/VERSION bootstrap so continuing work lands in a fresh release envelope
- v1.0.33 is published/frozen as the first small-batch checkpoint under this policy
- v1.0.34 is the authoritative writable successor and begins a fresh release envelope; choose a small coherent subset of the remaining foundation work, prefer runtime-testable grouping, and roll excess adjacent work forward rather than prolonging the version
