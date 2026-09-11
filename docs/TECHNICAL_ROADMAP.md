# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, release chunking and version scope. HANDOFF owns the immediate Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.24` at `784f408efba8a876b889fd704e051f22229de368`
Frozen release source: `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`
Current writable development branch: `v1.0.26`
Acceptance-weighted completion: **57%**

## 1. Current technical objective

Complete **Stage-C reference-machine acceptance** on the latest published release while preserving bounded forward architectural convergence on v1.0.26 when acceptance evidence is unavailable.

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

v1.0.25 accumulated a coherent MS-019 transform-authority chunk: mapped Move, Rotate and Scale commands now derive requested transforms from durable Core state, commit transactionally, and project committed state back into Godot. Focused regressions guard against returning these mapped commands to scene-observed persistence.

## 3. Release decision

**FREEZE v1.0.25 FOR RELEASE.**

Release boundary: `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.

Rationale:
- current v1.0.25 HEAD is green in Core and broader branch CI;
- the accumulated Move + Rotate + Scale authority retirement is coherent and materially larger than a single checkpoint;
- release identity on the frozen source is aligned to 1.0.25;
- no known release-blocking regression is recorded;
- this is a meaningful immutable build for target-machine transform/persistence testing.

v1.0.25 is frozen. No further commits belong on that branch while release-control is active.

v1.0.26 was created from the exact frozen SHA and is the only writable development branch.

## 4. Ordered critical path

### P0 — Consume reference-machine evidence

Any reproduced persistence, data-loss, viewport, storage, cancellation, provider or Stage-C blocker immediately preempts fallback work. Test the latest published immutable build, not development snapshots.

### P1 — Complete Stage-C acceptance

Required evidence: accepted 2D baseline; intended local 3D route; visible Ready/Conflict candidate; explicit Apply; save/reopen same object/revision; transforms plus one supported sculpt/edit path; cleanup/exact STL export; viewport/starter-scene checks; storage containment; provider/resource evidence; cancellation/recovery.

### P2 — v1.0.26 bootstrap

Before ordinary implementation:
1. complete v1.0.26 version identity across all audited surfaces;
2. obtain green exact-head branch CI;
3. reconcile HANDOFF/STATUS to that validated v1.0.26 bootstrap.

The Coordinator updated some forward-branch identity surfaces, but a connector safety block prevented completing the full set in this review. Dev owns the remaining ordinary implementation-level identity reconciliation.

### P3 — bounded MS-019 fallback after bootstrap

If acceptance evidence is still unavailable and v1.0.26 is green, take exactly one next MS-019 seam:

**viewport-drag transform commit authority** for mapped Stage-C objects.

Requirements:
- preserve the existing authoritative viewport tool/input owner;
- capture the durable object transform at gesture start;
- commit the requested move/rotate/scale result through Core transactional state rather than treating an already-mutated scene node as durable truth;
- project committed durable state back to Godot;
- restore from durable state on failure/stale conflict;
- add focused regression coverage;
- stop after this seam.

Do not combine this with ground placement, selection retirement, broad persistence cleanup or another UI modernization slice.

### P4 — after Stage-C acceptance

1. finish MS-020 only for lifecycle gaps exposed by evidence;
2. continue MS-019 outward migration/legacy-authority retirement;
3. make provider tier decisions from measured MS-022 results;
4. broaden Stage-D practical editing/sculpt/parts work.

## 5. Current issue priorities

1. **MS-018 — Critical / IN PROGRESS:** Stage-C acceptance.
2. **MS-023 — Critical / FIXED - NEEDS USER VERIFICATION:** durable generation/apply/reload ownership.
3. **MS-009 — Critical / FIXED - NEEDS USER VERIFICATION:** viewport stability/readability.
4. **MS-024 — High / FIXED - NEEDS USER VERIFICATION:** client-fill/resize seams.
5. **MS-013 — High / FIXED - NEEDS USER VERIFICATION:** storage containment.
6. **MS-022 — High / IN PROGRESS:** practical provider qualification.
7. **MS-004 — High / FIXED - NEEDS USER VERIFICATION:** cancellation/recovery.
8. **MS-020 — High / IN PROGRESS:** expand only from evidence.
9. **MS-019 — High architectural risk / IN PROGRESS:** continue only bounded proven authority retirement.
10. **MS-025 — Medium / FIXED - NEEDS USER VERIFICATION:** starter scene removal.
11. **MS-027 — Medium:** substantially implemented; no further opportunistic UI expansion ordered.

## 6. Architecture guards

- Do not create parallel state owners while retiring legacy paths.
- Migration-before-removal remains mandatory.
- Job durability stays compact.
- AI results remain revision-bound candidates/conflicts.
- User-observed GUI/GPU/runtime issues remain unresolved until real-machine evidence passes.
- Release identity is one coordinated contract across every user/tool-visible surface.
- New presentation work should prefer stable version-neutral components.

## 7. Continuous-development release discipline

- Dev records validated checkpoints and continues development.
- Checkpoints are evidence, not freezes.
- Coordinator decides release chunk size from current branch HEAD.
- Only an actual release request freezes the source branch.
- A writable forward semantic-version branch must exist before or as publication starts.
- Never rewind a moving branch to publish an older checkpoint.
- Never rewrite a published release/tag.

## 8. Next Coordinator-level objectives for Dev

1. Work only on writable v1.0.26.
2. Finish v1.0.26 version identity and exact-head green bootstrap first.
3. Consume new reference-machine evidence before fallback work.
4. If acceptance remains unavailable, implement only the bounded viewport-drag transform-authority seam.
5. Stop and validate after that seam, record a checkpoint, then continue only under the next roadmap direction.
6. Do not broaden MS-020 or resume MS-027 UI expansion without evidence.
7. Keep completion at 57% until acceptance evidence changes it.

## 9. User dependency

No new product-level decision is required.

Test the latest published release end to end:

`Generate 3D → candidate → Apply → save → close/reopen → same durable object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

plus viewport resize/presentation, starter-scene removal, storage containment, cancellation/recovery and resource behavior.
