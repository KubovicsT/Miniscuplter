# Miniscuplter Technical Direction State

This file is the authoritative medium/long-horizon technical-direction state owned by the Project Coordinator. It stores project strategy facts only; behavioral rules remain in automation prompts. `docs/TECHNICAL_ROADMAP.md` is legacy/read-only context.

- last_reconciled: 2026-09-16

## Architecture ownership
- durable_project_state: Core
- presentation_and_input: Godot
- inference_and_geometry: Python
- runtime_setup_and_delivery: launcher/updater
- architecture_mode: selective refactor; bounded ownership convergence; no broad rewrite

## Current strategic objective
- stable_release: v1.0.36
- writable_release_line: v1.0.37
- objective: complete the deferred attachment/history and Smart Selection/protected-region ownership convergence as one coherent v1.0.37 runtime-test checkpoint, then publish when exact-head release gates and canonical state are coherent
- reference_machine: Windows; GTX 1080 8 GB VRAM; 16 GB RAM
- evidence_rule: reference-machine GUI/GPU/runtime evidence outranks CI for runtime acceptance
- preemption_rule: any reproduced P0/P1 regression in the latest user-tested release preempts the affected lower-priority path after Coordinator reconciliation; pending verification alone does not globally stop independent safe work

## Priority order
1. Stage-D attachment undo/load reconstruction: durable attachment/library identity and history stay Core-owned; Godot rebuilds transient presentation from stable records across load/undo/redo
2. attachment read-side/history reconciliation: mapped current/history views resolve the same durable identity and duplicate legacy authority is retired only after parity is proven
3. Smart Selection/protected-region lifecycle convergence: semantic binding/history is Core-owned where durable semantics require it; stale/asynchronous presentation operations fail closed
4. checkpoint integration and runtime-test release boundary: keep exact-head Core/full build/package/installer gates green, canonical state coherent and publish a meaningful v1.0.37 checkpoint without adding unrelated speculative scope
5. after v1.0.37 publication, establish the validated next semantic branch/VERSION before further product work; new v1.0.36 reference-machine failures preempt only their affected path unless severe enough to require broader containment

## Strategic dependencies
- v1.0.36 is the latest published corrective runtime-test release; its reference-machine verification queue remains local acceptance evidence, not a global stop
- v1.0.37 is the sole writable successor with VERSION 1.0.37 and the attachment/history/protected-region checkpoint implemented and exact-head validated
- Core contains stable AttachmentRecord, CandidateRecord, object/revision identity and revision-bound selection models; presentation reconstruction must not become a second durable authority
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
- v1.0.36 is published/frozen as the current corrective runtime-test checkpoint
- v1.0.37 is the authoritative writable successor and its attachment/history/protected-region ownership batch is READY FOR COORDINATOR CHECKPOINT REVIEW with exact-head Core/full build/package/installer validation green
- no additional speculative implementation belongs in v1.0.37 before release disposition; if final candidate/canonical review remains coherent, publish this checkpoint and roll adjacent work forward
