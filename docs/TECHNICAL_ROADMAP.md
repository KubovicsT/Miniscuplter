# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, release chunking and version scope. HANDOFF owns the immediate Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.23` at `bda683264448fc8b51c7c538db61f8c0487a699a`
Frozen release source: `v1.0.24` at `784f408efba8a876b889fd704e051f22229de368`
Current writable development branch: `v1.0.25`
Acceptance-weighted completion: **57%**

## 1. Current technical objective

Complete **Stage-C reference-machine acceptance** on published v1.0.23 while preserving continuous forward development on v1.0.25 during v1.0.24 publication.

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

v1.0.24 accumulated two coherent structural increments while acceptance was externally blocked: a bounded MS-020 reliability seam and one bounded MS-019 authority-retirement seam. This is enough for a meaningful Coordinator-curated release chunk; further accumulation would unnecessarily delay a useful reliability/migration increment.

## 3. Release decision

**FREEZE v1.0.24 FOR RELEASE at exact current HEAD `784f408efba8a876b889fd704e051f22229de368`.**

The boundary includes:
- truthful heavyweight runtime/component ownership;
- cancellation ownership until physical terminal acknowledgement;
- compact lifecycle/tombstone persistence and restart reconciliation;
- fail-closed/bounded recovery reads;
- retirement/delegation of historical v1.0.9 Generate-3D authority to canonical Stage-C generation;
- focused regression coverage;
- strict release-audit migration to the actual canonical authority.

Exact-head v1.0.24 branch CI is green. The release-control pipeline must still independently prove exact-SHA C#/Core/Python/job/geometry checks, strict audit, real Godot Windows export, package/hash verification and installer smoke test before publication.

v1.0.25 was created from the exact frozen boundary before release initiation and is now writable for continuous Dev.

## 4. Ordered critical path

### P0 — Consume released-v1.0.23 reference-machine evidence

Any reproduced persistence, data-loss, viewport, storage, cancellation, provider or Stage-C blocker immediately preempts fallback work. Test the published v1.0.23 build, not development snapshots.

### P1 — Complete Stage-C acceptance

Required evidence: accepted 2D baseline; intended local 3D route; visible Ready/Conflict candidate; explicit Apply; save/reopen same object/revision; transforms plus one supported sculpt/edit path; cleanup/exact STL export; viewport/starter-scene checks; storage containment; provider/resource evidence; cancellation/recovery.

### P2 — v1.0.25 fallback while acceptance remains external

Continue MS-019 only one proven duplicate-authority seam at a time. Select the smallest seam whose replacement owner is already established and regression-testable. Prefer transform/selection/persistence authority retirement after the completed generation seam. Preserve migration compatibility until replacement behavior is proven. Stop and record a checkpoint after each seam; do not let this become a broad legacy purge.

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
8. **MS-020 — High / IN PROGRESS:** bounded reliability seam complete; expand only from evidence.
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
- The v1.0.24 release request freezes only v1.0.24.
- v1.0.25 remains writable while publication runs.
- Failed publication keeps v1.0.24 frozen until the concrete failure is diagnosed.
- Never rewind a moving branch to publish an older checkpoint.
- Never rewrite a published release/tag.

## 8. Target hardware / acceptance constraints

Reference machine: Windows, GTX 1080, 8 GB dedicated VRAM, 16 GB RAM, limited system-drive free space.

Existing Hunyuan-mini observation: roughly 402 s runtime, around 97% GPU, ~5.4/8 GB dedicated VRAM, ~5 GB RAM in the supplied snapshot, ~73 °C GPU. These are observations, not measured peaks.

## 9. Next Coordinator-level objectives for Dev

1. Consume new v1.0.23 reference-machine evidence first.
2. Work only on writable v1.0.25; v1.0.24 is frozen for publication and v1.0.23 is immutable.
3. If acceptance remains unavailable, retire at most one next proven duplicate authority under MS-019, with focused regression coverage.
4. Do not broaden MS-020 or resume opportunistic MS-027 UI work without evidence/Coordinator direction.
5. Keep completion at 57% until acceptance evidence justifies change.
6. Preserve local-first/storage/data/revision/transaction boundaries.
7. Record useful release checkpoints but continue development unless a future Coordinator freeze explicitly applies to v1.0.25.

## 10. User dependency

No new product-level decision is required.

Test released v1.0.23, especially:

`Generate 3D → candidate visible → Apply → save → close/reopen → same 3D object/revision`

plus right-panel/whole-window resizing, no starter sphere/opaque floor, transform/sculpt, cleanup/export, storage containment, and cancellation/resource behavior.

Autonomous development can continue on v1.0.25.