# Miniscuplter Handoff

> Immediate Dev execution baton. TECHNICAL_ROADMAP owns whole-system sequencing.

Last updated: 2026-09-12

## Repository / release state

- **Latest published stable:** `v1.0.26` at `a41e0419ba40fd1118775e8f516b1a31145f18d8`; publication completed successfully and remains immutable.
- **Current writable development branch:** `v1.0.27`.
- **Latest fully validated engineering checkpoint:** `3d63204c368ca6b6564b7e1b74868f3636183668`.
- No active release request freezes `v1.0.27`.

## Completed authorized queue

### A — v1.0.27 bootstrap validation — ACCEPTED
Backend lifecycle expectation was aligned to 1.0.27. Semantic identity, Core/editor build, runtime/backend lifecycle, release audit and package validation are green.

### B — MS-019 mapped ground-placement authority — ACCEPTED
Mapped ground placement no longer persists an already-mutated Godot scene transform. It captures stable object/revision/transform identity, derives Y=0 placement from the exact immutable mesh asset plus durable Core transform, rejects stale state transactionally, saves Core state and restores Godot presentation from Core on failure. Legacy fallback remains only for unmigrated objects.

### C — bounded durable viewport selection seam — ACCEPTED
Godot remains ray-hit/presentation owner, but mapped production viewport picks now bind stable Core `ObjectId + MeshRevisionId` before presentation selection. Whole-object selection explicitly rebinds when the active revision advances; missing Core objects invalidate the binding. Behavioral and source regressions cover stale-revision transfer and prevent direct scene-node identity from returning to this production pick path.

## Validation

Exact checkpoint `3d63204c...` passed:
- editor/Launcher/Updater/Core C# builds and Core regressions;
- semantic-version and backend lifecycle checks;
- Python/job/geometry regressions and release audit;
- portable package layout + SHA-256 verification;
- installer-definition compilation.

Full Windows release/publish jobs were correctly skipped because this is a development branch, not a release tag.

## CURRENT OBJECTIVE — COORDINATOR REVIEW REQUESTED

The authorized A → B → C queue is exhausted and Objective C explicitly had **Auto-proceed: NO**. Coordinator should choose the next Stage-D/acceptance sequence using current repository state and forthcoming `v1.0.26` GTX 1080 evidence. Dev should not invent further strategic work until HANDOFF/ROADMAP supplies a new authorized objective.

Safe work while waiting: read-only validation/reproduction only; no new roadmap expansion.

## User verification dependency

Highest-value external evidence remains released `v1.0.26`: update/reopen → Repair AI Runtime → Generate 3D → resize invariance → compact workspace UX → full Stage-C Apply/save-reopen/edit-sculpt/cleanup/exact-STL flow, including storage/cancellation observations.
