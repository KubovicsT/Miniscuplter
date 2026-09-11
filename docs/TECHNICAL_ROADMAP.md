# Miniscuplter Technical Roadmap

> Authoritative technical sequencing owned by the Project Coordinator. PROJECT_CHARTER/DECISIONS own product truth; HANDOFF owns the immediate Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`
Current writable development branch: `v1.0.26`
Reviewed branch HEAD: `f4bc5a5daf8633b4078db02969d044caaf730f01`
Validated implementation checkpoint: `d4aef5d55170ef45298e9db942cb250d78e911d2`
Acceptance-weighted completion: **57%**

## 1. Current objective

Complete Stage-C reference-machine acceptance on released v1.0.25 while continuing bounded architectural convergence on v1.0.26 whenever target-machine evidence is unavailable.

Accepted thin slice remains:

`2D source → accepted durable image baseline → local 3D generation → identity-bound candidate → explicit Apply → durable editable object → Move/Rotate/Scale + bounded sculpt/edit → save/reload → cleanup → exact validated STL export`

Reference-machine GUI/GPU/runtime evidence outranks CI.

## 2. Direction assessment

**PRESERVE.** Core owns durable project/object/revision/history state; Godot owns viewport/input/presentation; Python owns inference/geometry execution. Stable IDs, immutable revisions, transactional history, storage containment, migration-before-removal and immutable published releases remain hard architectural guards.

v1.0.26 has now completed the Coordinator-authorized viewport-drag transform authority seam. Gesture start captures durable Core transform plus presentation start state; Move/Rotate/Scale persist only the gesture delta/ratio through Core and reproject durable truth. Stale revision/transform conflicts fail closed and presentation restores from Core. This is convergence toward one durable owner rather than another bridge.

## 3. Release decision

**KEEP v1.0.26 ACCUMULATING.**

The current HEAD is coherent and green, and `d4aef5d5...` is a useful release-worthy checkpoint, but the post-v1.0.25 accumulated product increment is still only one bounded authority-retirement seam. That is not yet a sufficiently substantial Coordinator-curated release chunk. No release freeze/request is justified. v1.0.26 remains writable and Dev should continue.

## 4. Ordered critical path

### P0 — Consume reference-machine evidence
Any reproduced persistence, data-loss, viewport, storage, cancellation, provider or Stage-C blocker immediately preempts fallback work. Test the latest published immutable build, v1.0.25.

### P1 — Complete Stage-C acceptance
Required evidence: accepted 2D baseline; intended local 3D route; visible Ready/Conflict candidate; explicit Apply; save/reopen same object/revision; transforms plus one supported sculpt/edit path; cleanup/exact STL export; viewport/starter-scene checks; storage containment; provider/resource evidence; cancellation/recovery.

### P2 — bounded MS-019 fallback
If reference-machine evidence remains unavailable, take exactly one next authority-retirement seam: **mapped-object ground placement transform authority**.

Requirements:
- preserve the existing viewport/input presentation owner;
- derive the requested ground-placement transform from durable Core object/revision state, not scene-observed durable truth;
- commit through the existing Core transactional transform path;
- reject stale object/revision/transform state before persistence;
- project committed Core state back into Godot and restore from Core on failure;
- add focused regression coverage preventing reintroduction of scene-observed persistence;
- stop and validate after this seam.

Do not combine this with selection retirement, broad persistence cleanup, sculpt architecture, MS-020 expansion or MS-027 UI modernization.

### P3 — after next checkpoint
Coordinator should review combined v1.0.26 trajectory and release chunk size again. Do not infer another MS-019 seam without newer roadmap direction.

### P4 — after Stage-C acceptance
Finish MS-020 only for lifecycle gaps exposed by evidence; continue outward MS-019 migration; make provider-tier decisions from measured MS-022 results; then broaden Stage-D practical editing/sculpt/parts work.

## 5. Current issue priorities

1. MS-018 — Critical / IN PROGRESS: Stage-C acceptance.
2. MS-023 — Critical / FIXED - NEEDS USER VERIFICATION: durable generation/apply/reload ownership.
3. MS-009 — Critical / FIXED - NEEDS USER VERIFICATION: viewport stability/readability.
4. MS-024 — High / FIXED - NEEDS USER VERIFICATION: client-fill/resize seams.
5. MS-013 — High / FIXED - NEEDS USER VERIFICATION: storage containment.
6. MS-022 — High / IN PROGRESS: practical provider qualification.
7. MS-004 — High / FIXED - NEEDS USER VERIFICATION: cancellation/recovery.
8. MS-020 — High / IN PROGRESS: expand only from evidence.
9. MS-019 — High architectural risk / IN PROGRESS: bounded proven authority retirement only.
10. MS-025 — Medium / FIXED - NEEDS USER VERIFICATION: starter scene removal.
11. MS-027 — Medium: opportunistic fallback only; currently preempted by MS-019 sequencing.

## 6. Release discipline

Dev records validated checkpoints and continues. Checkpoints are evidence, not freezes. Coordinator freezes only exact current branch HEAD, creates the next forward semantic-version branch from that exact SHA, then initiates release-control. Never rewind a moving branch to an older checkpoint and never rewrite a published release/tag.

## 7. User dependency

No product/design decision is required. Test released v1.0.25 on the reference Windows / GTX 1080 machine:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery, and resource behavior during a long AI job.
