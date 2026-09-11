# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, release chunking and version scope. HANDOFF owns the immediate Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.24` at `784f408efba8a876b889fd704e051f22229de368`
Current writable development branch: `v1.0.25`
Acceptance-weighted completion: **57%**

## 1. Current technical objective

Complete **Stage-C reference-machine acceptance** on published v1.0.24 while preserving bounded forward architectural convergence on v1.0.25 when acceptance evidence is unavailable.

Accepted thin slice:

`2D source → accepted durable image baseline → local 3D generation → identity-bound candidate → explicit Apply → durable editable object → Move/Rotate/Scale + bounded sculpt/edit → save/reload → cleanup → exact validated STL export`

Reference-machine GUI/GPU/runtime evidence outranks CI and remains the principal missing acceptance evidence.

## 2. Direction assessment

**Direction: PRESERVE.**

- Core owns durable project/object/revision/history state.
- Godot owns viewport/input/presentation.
- Python owns inference/geometry execution.
- Stable IDs, immutable revisions and transactional history remain the durable-state model.
- STL remains interchange/export, not project authority.
- Storage stays within Miniscuplter-controlled roots.
- Published releases are immutable; fixes move forward only.

v1.0.24 successfully published its bounded MS-020 reliability seam plus the first proven MS-019 Stage-C authority-retirement seam. v1.0.25 has since advanced exactly one further bounded MS-019 seam: mapped Rotate Y nudges now derive from durable Core transform state and project the committed result back into Godot presentation. This is the intended migration pattern: move one authority seam, prove it, then proceed outward.

## 3. Current release decision

**KEEP v1.0.25 ACCUMULATING.**

Current branch HEAD is `1808de62d3bdb2b26c26e95cdd33ec01b54d4eaa`; the fully validated implementation checkpoint beneath the documentation commit is `e0666bbd7e56d340f112238f78c836b958a64675`. Exact-head branch CI is green.

One bounded Rotate-authority seam after v1.0.24 is useful but not yet a sufficiently substantial Coordinator-curated release chunk. No v1.0.25 release freeze or release-control request is warranted now. Dev remains free to continue under the bounded sequencing below.

## 4. Ordered critical path

### P0 — Consume released-v1.0.24 reference-machine evidence

Any reproduced persistence, data-loss, viewport, storage, cancellation, provider or Stage-C blocker immediately preempts fallback work. Test the published v1.0.24 build, not development snapshots.

### P1 — Complete Stage-C acceptance

Required evidence: accepted 2D baseline; intended local 3D route; visible Ready/Conflict candidate; explicit Apply; save/reopen same object/revision; transforms plus one supported sculpt/edit path; cleanup/exact STL export; viewport/starter-scene checks; storage containment; provider/resource evidence; cancellation/recovery.

### P2 — v1.0.25 fallback while acceptance remains external

Continue MS-019 only one proven duplicate-authority seam at a time. The completed Generate-3D and Rotate seams establish the migration pattern.

**Next approved seam: mapped Scale ±5% authority.**

Requirements:
- derive requested scale from durable Core object transform, not already-mutated Godot presentation;
- preserve durable position and rotation;
- commit through the established Core transactional transform path;
- project committed durable state back to Godot;
- restore presentation from durable state on failure;
- add focused regression coverage preventing fallback to the generic scene-observed transform hook;
- stop after this seam and record a validated checkpoint.

Do not combine Scale with Place-on-Y=0, viewport-drag transform persistence, selection retirement or a broad legacy purge in the same bounded slice.

### P3 — After Stage-C acceptance

1. finish MS-020 only for real lifecycle gaps exposed by acceptance;
2. continue MS-019 outward migration/legacy-authority retirement;
3. make provider Fast/Balanced/Quality decisions from measured MS-022 results;
4. broaden Stage-D practical editing/sculpt/parts work.

## 5. Current issue priorities

1. **MS-018 — Critical / IN PROGRESS:** Stage-C acceptance.
2. **MS-023 — Critical / FIXED - NEEDS USER VERIFICATION:** durable generation/apply/reload ownership.
3. **MS-009 — Critical / FIXED - NEEDS USER VERIFICATION:** viewport stability/readability.
4. **MS-024 — High / FIXED - NEEDS USER VERIFICATION:** client-fill/resize seams.
5. **MS-013 — High / FIXED - NEEDS USER VERIFICATION:** storage containment.
6. **MS-022 — High / IN PROGRESS:** practical provider qualification.
7. **MS-004 — High / FIXED - NEEDS USER VERIFICATION:** cancellation/recovery.
8. **MS-020 — High / IN PROGRESS:** bounded reliability seam shipped in v1.0.24; expand only from evidence.
9. **MS-019 — High architectural risk / IN PROGRESS:** continue only bounded proven authority retirement.
10. **MS-025 — Medium / FIXED - NEEDS USER VERIFICATION:** starter scene removal.
11. **MS-027 — Medium:** bounded modernization substantially implemented; no further opportunistic UI expansion ordered.

## 6. Architecture guards

- Do not create parallel state owners while retiring legacy paths.
- Migration-before-removal remains mandatory.
- Job durability stays compact; do not turn lifecycle state into a generalized persistent queue without evidence.
- AI results remain revision-bound candidates/conflicts; never silently overwrite newer durable state.
- User-observed GUI/GPU/runtime issues remain unresolved until real-machine evidence passes.
- Release identity must remain coherent across every user/tool-visible surface.
- New presentation work should prefer stable version-neutral components, not new version-derived UI layers.

## 7. Continuous-development release discipline

- Dev records validated checkpoints and continues development.
- Checkpoints are evidence, not freezes.
- Coordinator decides release chunk size from current branch HEAD.
- v1.0.25 remains writable until a future explicit Coordinator release boundary.
- Never rewind a moving branch to publish an older checkpoint.
- Never rewrite a published release/tag.

## 8. Target hardware / acceptance constraints

Reference machine: Windows, GTX 1080, 8 GB dedicated VRAM, 16 GB RAM, limited system-drive free space.

Existing Hunyuan-mini observation: roughly 402 s runtime, around 97% GPU, ~5.4/8 GB dedicated VRAM, ~5 GB RAM in the supplied snapshot, ~73 °C GPU. These are observations, not measured peaks.

## 9. Next Coordinator-level objectives for Dev

1. Consume new v1.0.24 reference-machine evidence first.
2. Work only on writable v1.0.25; v1.0.24 is published and immutable.
3. If acceptance remains unavailable, implement exactly the mapped Scale ±5% MS-019 authority seam described above.
4. Stop after that seam, validate strongly and record a release-worthy checkpoint; do not wait for Coordinator afterward.
5. Do not broaden MS-020 or resume opportunistic MS-027 UI work without evidence/Coordinator direction.
6. Keep completion at 57% until acceptance evidence justifies change.
7. Preserve local-first/storage/data/revision/transaction boundaries.

## 10. User dependency

No new product-level decision is required.

Test released v1.0.24, especially:

`Generate 3D → candidate visible → Apply → save → close/reopen → same 3D object/revision`

plus right-panel/whole-window resizing, no starter sphere/opaque floor, transform/sculpt, cleanup/export, storage containment, and cancellation/resource behavior.

Autonomous development can continue on v1.0.25.