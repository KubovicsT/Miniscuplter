# Miniscuplter Handoff

> Immediate and downstream execution baton. TECHNICAL_ROADMAP owns whole-system strategy; this file tells Dev what it may execute continuously.

Last updated: 2026-09-12

## Repository / release state

- **Latest published stable at handoff time:** `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Frozen source:** `v1.0.26` at exact candidate `a41e0419ba40fd1118775e8f516b1a31145f18d8`.
- **v1.0.26 release-control request:** submitted on `release-control`; autonomous release workflow is running.
- **Current writable development branch:** `v1.0.27`.
- **Forward-branch reconciliation:** the earlier premature/orphan v1.0.27 was fast-forwarded to the actual frozen v1.0.26 candidate as part of the real release transition.
- **v1.0.27 identity:** mechanically bootstrapped to 1.0.27 across launcher, updater/editor assembly, installer, Windows export metadata, backend API, editor display and release audit.
- **Dev automation state:** paused by user/Manager state. Coordinator did not and must not enable it.

## Released-candidate contents

v1.0.26 freezes a coherent acceptance/reliability tranche:
- Core-authoritative viewport Move/Rotate/Scale drag persistence;
- MS-030 canonical packaged FastAPI/Uvicorn startup and Repair health ownership;
- MS-029 updater health-probe lifecycle fix;
- MS-009 retirement of competing resize/world-repair owners;
- MS-027 user-directed compact workspace tranche.

MS-030, MS-009 and MS-029 remain **FIXED - NEEDS USER VERIFICATION** where applicable. The current MS-027 tranche is implemented but also needs target-machine UX verification.

## CURRENT OBJECTIVE — A: validate v1.0.27 bootstrap / preserve release isolation

**Outcome**  
Ensure the forward branch is internally coherent while v1.0.26 publishes.

**Constraints**
- never mutate frozen v1.0.26 while its release request is active;
- v1.0.27 remains the only writable application branch;
- no product-feature changes until semantic-version identity and exact-head CI are green;
- do not modify release-control from Dev.

**Dependencies**  
None.

**Acceptance / stop condition**
- semantic identity is 1.0.27 on all audited surfaces;
- exact-head Core/build/release-audit/package validation is green;
- docs identify v1.0.27 as writable and v1.0.26 as frozen/publishing.

**Preemption**  
Any v1.0.26 release failure that requires a source/candidate fix.

**Auto-proceed:** YES → Objective B when Dev is enabled and no release/user blocker exists.

## NEXT OBJECTIVE — B: bounded MS-019 mapped-object ground-placement authority

**Outcome**  
Move one remaining scene-observed transform persistence seam onto durable Core transactional authority.

**Constraints**
- Godot remains live presentation/input owner;
- Core remains durable transform/history owner;
- derive from stable object/revision/transform state;
- reject stale state before persistence;
- project committed Core state back into Godot and restore from Core on failure;
- focused regression must prevent scene-observed durable persistence from returning;
- do not combine with broad selection retirement, sculpt rewrite, provider work or MS-020 expansion.

**Dependencies**  
Objective A accepted; v1.0.26 release is not failing; no higher-priority reference-machine evidence.

**Acceptance / stop condition**  
Ground placement persists through the durable transactional path with stale/failure regressions and exact-head validation green.

**Preemption**  
Any v1.0.26 release failure or new target-machine evidence about backend health, viewport, update behavior, persistence/data loss or Stage-C correctness.

**Auto-proceed:** YES → Objective C if no preemption.

## NEXT OBJECTIVE — C: bounded durable selection/picking authority seam

**Outcome**  
Advance Stage-D practical editing by moving one concrete selection/picking seam away from widget/name/scene authority onto stable object/revision identity.

**Constraints**
- one bounded seam only;
- no scene-tree redesign;
- selection binds to stable object/revision identity;
- Godot owns hit-testing/presentation, Core owns durable selection-dependent state where persistence/history requires it;
- topology/revision changes must invalidate or explicitly transfer selection rather than silently reusing stale indices;
- preserve current viewport responsiveness.

**Dependencies**  
Objective B accepted; no new Critical acceptance evidence.

**Acceptance / stop condition**  
One production selection path uses stable identity with stale-revision regression coverage and no transform/sculpt/Stage-C regression.

**Preemption**  
Reference-machine failure from v1.0.26, release regression, persistence/data-safety issue, or Coordinator reprioritization.

**Auto-proceed:** NO — Coordinator should review after this seam because v1.0.26 target-machine evidence is expected to materially influence Stage-D sequencing.

## Queue-depth note

The queue intentionally contains three substantial objectives rather than speculative 3–6 hour breadth beyond selection. v1.0.26 publication and imminent GTX 1080 acceptance can legitimately preempt migration work. This is sufficient safe forward work without diluting Stage-C acceptance.

## User verification dependency

After v1.0.26 publishes, test in this order:
1. update/reopen behavior;
2. Repair AI Runtime;
3. Generate 3D reaching provider resolution/inference;
4. right-splitter and whole-window resize invariance;
5. compact command console/view cube/tools/help UI;
6. full Stage-C candidate Apply → save/reopen → edit/sculpt → cleanup → exact STL export, including storage/cancellation observations.
