# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. The Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, version scope, release readiness and release publication. `HANDOFF.md` owns the immediate Dev Cycle baton and release-candidate handoff.

Last coordinator review: 2026-09-10
Current stable release: `v1.0.22`
Stable release target: `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
Current development branch: `v1.0.23`
Repository HEAD reviewed before this roadmap update: `47722291cf771865c21304fd245dc2c2bab51103`
Acceptance-weighted completion: **57%**

## 1. Current technical objective

Complete **Stage-C reference-machine acceptance** on released v1.0.22 while keeping v1.0.23 as a narrow forward acceptance-support and bounded UI-modernization branch.

The accepted thin slice remains:

`2D source → accepted durable image baseline → local 3D generation → identity-bound candidate → explicit Apply → durable editable object → Move/Rotate/Scale + bounded sculpt/edit → save/reload → cleanup → exact validated STL export`

Released v1.0.22 contains bounded fixes for the concrete v1.0.21 findings:

- **MS-023** — one authoritative Stage-C Generate-3D owner;
- **MS-009** — stable/readable viewport presentation and non-occluding grid behavior;
- **MS-024** — whole-window client-fill correction;
- **MS-025** — automatic starter sphere removal.

These remain **FIXED - NEEDS USER VERIFICATION** until retested on the reference Windows / GTX 1080 / 8 GB VRAM / 16 GB RAM machine.

When acceptance is externally blocked and no higher-priority correctness work exists, exactly one bounded MS-027 modernization slice may advance.

## 2. Direction assessment

**Direction: PRESERVE.**

The selective-refactor architecture remains correct:

- Core owns durable project/object/revision/history state.
- Godot owns viewport/input/presentation.
- Python owns inference/geometry execution.
- Stable IDs, immutable revisions and transactional history remain the state model.
- STL remains export/interchange, not project authority.
- Local Job Broker remains the intended long-running execution architecture.
- Storage remains inside Miniscuplter-controlled roots.
- Published releases are immutable and fixes move forward by semantic version.

Recent Dev trajectory is converging rather than drifting:

1. workspace preference persistence / UI scale / tooltips;
2. direct viewport tool controls reusing the existing V1018 tool owner;
3. synchronized collapsible scene hierarchy in a version-neutral presentation file, reusing existing scene/object selection authority.

The temporary MS-028 compile regression was repaired and exact-head branch CI is green.

## 3. Release ownership and current release decision

Release ownership belongs exclusively to the Coordinator.

Dev prepares and validates candidate SHAs and records them in HANDOFF/PROJECT_STATUS. It does not create release-control requests or publish releases.

### Current v1.0.23 decision: NOT READY FOR RELEASE REVIEW

No release request is being created in this review.

Reasons:
- HANDOFF does not mark a v1.0.23 candidate **READY FOR COORDINATOR RELEASE REVIEW**;
- the branch is still an active development branch with another Coordinator-approved bounded slice available while Stage-C acceptance is externally blocked;
- current green branch CI proves development validation but not the full exact-SHA Windows/Godot release pipeline;
- v1.0.22 target-machine verification remains outstanding and can still surface a serious acceptance blocker that should preempt UI continuation.

This is not an indefinite delay. Dev should continue only the next bounded roadmap slice or any newly surfaced acceptance blocker. When Dev believes v1.0.23 forms a coherent meaningful testable increment, it should stop altering that candidate, record the exact SHA and validation evidence, and mark it **READY FOR COORDINATOR RELEASE REVIEW**.

The Coordinator will then independently decide whether to freeze and publish it.

## 4. Current milestone — Stage-C reference-machine acceptance

Acceptance requires:

1. **Generation/persistence — MS-023 / MS-018**
   - accepted 2D baseline;
   - intended local 3D route completes;
   - Ready/Conflict candidate appears;
   - explicit Apply creates/advances the durable object/revision;
   - save → close → reopen restores the same object and active mesh revision.

2. **Viewport/editability — MS-009 / MS-024 / MS-025**
   - initial viewport readable;
   - right-panel resize does not alter viewport appearance;
   - whole-window resize leaves no black seams;
   - grid does not occlude generated geometry;
   - no starter sphere after launch/New/repair;
   - first real object can be selected/framed;
   - Move/Rotate/Scale and one supported sculpt/edit path work.

3. **Cleanup/export**
   - cleanup follows durable revision authority;
   - export scope is explicit;
   - validated STL matches durable project state.

4. **Storage — MS-013**
   - project/model/cache/temp/job artifacts remain inside controlled roots.

5. **Provider qualification — MS-022**
   - one practical local 3D route works on reference hardware;
   - preserve elapsed time and resource evidence.

6. **Cancellation/recovery — MS-004**
   - cancel/restart does not poison the next job or corrupt project state.

## 5. Ordered critical path

### P0 — Consume v1.0.22 reference-machine evidence

At every Dev Cycle start, check for new released-v1.0.22 user evidence.

Any reproduced persistence, data-loss, viewport, storage, cancellation or Stage-C blocker immediately preempts MS-027.

### P1 — Complete Stage-C acceptance

Once reference-machine testing is available, finish MS-018 through persistence, transforms/sculpt, cleanup/export, storage, provider evidence and cancellation/recovery.

### P2 — Opportunistic MS-027 only while P0/P1 are externally blocked

Completed bounded slices:

1. workspace splitter persistence + UI/font scale + tooltip infrastructure;
2. direct Select/Move/Rotate/Scale/Sculpt tool strip;
3. synchronized collapsible scene hierarchy.

Next allowed fallback slice:

4. **view cube + selected-object orbit pivot**.

Requirements:
- reuse existing camera yaw/pitch/focus/distance authority;
- cube orientation reflects current camera;
- face snaps: Front/Back/Left/Right/Top/Bottom;
- edges/corners may provide diagonal/isometric snaps if still bounded;
- normal orbit pivots around current selected entity when present;
- preserve empty-scene focus behavior;
- no duplicate camera state machine;
- no durable project-state changes;
- preserve viewport/render and viewport-tool ownership;
- add focused validation and stop before AI console/telemetry.

Subsequent order:
5. unified AI command console/history through one authoritative dispatcher;
6. MS-026 resource telemetry;
7. density/spacing cleanup and retirement of superseded explanatory UI.

### P3 — Candidate preparation / Coordinator release review

When Dev reaches a coherent v1.0.23 increment:
- exact candidate SHA must be recorded;
- relevant branch validation must be green;
- known release-blocking regressions must be absent;
- docs must describe candidate scope accurately;
- HANDOFF/STATUS must explicitly say **READY FOR COORDINATOR RELEASE REVIEW**;
- Dev must stop publication actions.

Coordinator then:
- rechecks repository/release truth and candidate evidence;
- decides readiness independently;
- if ready, writes the exact release-control request and freezes the source branch;
- supervises exact-SHA release validation/publication;
- verifies immutable release target;
- reconciles docs and directs the next forward semantic-version branch.

### P4 — Post-Stage-C structural work

Absent new evidence:
1. MS-020 durable Job Broker ownership/resource locking/cancellation/crash recovery;
2. MS-019 outward migration from proven Stage-C seams and duplicate legacy-authority retirement;
3. provider Fast/Balanced/Quality policy from measured MS-022 results;
4. broader Stage-D editing/sculpt/parts work.

## 6. Issue priority

1. **MS-023 — Critical / FIXED - NEEDS USER VERIFICATION.**
2. **MS-018 — Critical / IN PROGRESS.**
3. **MS-009 — Critical / FIXED - NEEDS USER VERIFICATION.**
4. **MS-024 — High / FIXED - NEEDS USER VERIFICATION.**
5. **MS-013 — High / FIXED - NEEDS USER VERIFICATION.**
6. **MS-022 — High / IN PROGRESS.**
7. **MS-004 — High / FIXED - NEEDS USER VERIFICATION.**
8. **MS-025 — Medium / FIXED - NEEDS USER VERIFICATION.**
9. **MS-027 — Medium / OPPORTUNISTIC.**
10. **MS-026 — Medium / PLANNED inside MS-027.**
11. **MS-028 — RESOLVED:** development-only tool-strip compile regression fixed and validated.

## 7. MS-027 architecture guard

The modernization must reduce, not extend, version-derived UI composition.

Current `Main.V1023...` files are migration bridges, not the target architecture. The new scene hierarchy correctly moved to a stable version-neutral presentation file; continue that direction.

Prefer stable version-neutral components/services and one authoritative underlying owner:

- viewport tool state/input: existing V1018 path until deliberately migrated;
- scene/object identity: Core/project authority;
- selection: one synchronized owner;
- camera state: one existing camera/focus owner;
- AI actions: one dispatcher;
- UI preferences: presentation-only state outside ProjectStore.

Migration rule: move authority, prove behavior, then retire superseded legacy presentation paths.

## 8. v1.0.23 intended scope

v1.0.23 remains a **forward acceptance-fix + bounded opportunistic UI branch**.

Already included:
- layout persistence / UI scale / tooltips;
- direct viewport tools;
- synchronized scene hierarchy;
- MS-028 repair.

Still allowed:
- narrow fixes for v1.0.22 reference-machine failures;
- acceptance diagnostics/regressions;
- one bounded MS-027 slice at a time while acceptance is externally blocked;
- candidate-preparation documentation.

Not in scope without new evidence or Coordinator change:
- monolithic UI rewrite;
- broad Main.V* removal;
- full Job Broker reconstruction;
- full sculpt migration;
- Rig & Pose modernization;
- kitbash expansion;
- provider proliferation;
- unrelated feature breadth.

## 9. Target hardware / resource constraints

Reference acceptance machine:
- Windows;
- GTX 1080;
- 8 GB dedicated VRAM;
- 16 GB RAM;
- limited system-drive free space.

Current Hunyuan-mini evidence:
- about 402 s runtime;
- snapshot around 97% GPU;
- about 5.4/8 GB dedicated VRAM;
- about 5 GB system RAM used;
- about 73 °C GPU.

These are observations, not proven peaks.

MS-026 should remain local, low-overhead, roughly 1 Hz, omit unavailable sensors gracefully, and associate resource samples with provider/job stage/elapsed time.

## 10. Technical risks

1. **False acceptance:** CI cannot close target-only Godot/CUDA/UI issues.
2. **Release-owner confusion:** Dev must stop at candidate preparation; Coordinator alone decides/freezes/publishes.
3. **UI overlay accretion:** continue moving new presentation work toward version-neutral components.
4. **Duplicate authority:** view cube, hierarchy and AI console are high-risk seams.
5. **Acceptance displacement:** UI fallback remains subordinate to Stage-C.
6. **Storage regression:** preference/telemetry/job data must stay inside controlled roots.
7. **Provider overclaim:** one successful run is useful evidence, not a support matrix.
8. **Release/version drift:** published releases remain immutable; release-request branches freeze while active.

## 11. Next Coordinator-level objectives for Dev Cycle

1. At every cycle start, consume new v1.0.22 reference-machine evidence before fallback work.
2. If target evidence exposes a serious blocker, fix it forward narrowly on v1.0.23.
3. If acceptance remains externally blocked, implement only the bounded view-cube + selected-orbit-pivot slice next.
4. Reuse current camera/selection owners; do not create parallel camera state.
5. Prefer stable version-neutral UI components.
6. Stop after one bounded fallback slice.
7. Keep completion at 57% until acceptance evidence changes it.
8. Do not create/update release requests, tags or GitHub Releases.
9. When v1.0.23 is genuinely coherent for publication, record the exact candidate SHA/evidence and mark **READY FOR COORDINATOR RELEASE REVIEW** before further candidate-invalidating work.

## 12. User input / verification dependency

No new product-level decision is required.

The principal user dependency remains reference-machine testing of released v1.0.22, especially:

`Generate 3D → candidate visible → Apply → save → close/reopen → same 3D object/revision`

plus right-panel resize, whole-window resize, no starter sphere/opaque floor, transform/sculpt, cleanup/export and storage behavior.

Autonomous engineering can continue under the current roadmap.

---

## User override — continuous-development release model (2026-09-10)

This section supersedes any earlier roadmap wording that says Dev must stop after marking a candidate ready for Coordinator review.

- Release ownership remains exclusively with the Coordinator.
- Dev may record exact release-worthy checkpoint SHAs and validation evidence, then continue roadmap work. A checkpoint is advisory and does not freeze the branch.
- Coordinator owns release chunk size and should prefer meaningful accumulated releases rather than publishing every green checkpoint.
- Only an actual Coordinator release request freezes the semantic-version source branch.
- The release candidate is the exact current HEAD at the chosen release boundary; do not rewind a moving version branch to an older checkpoint.
- Before or as release-control is initiated, create/use the next forward semantic-version development branch from that frozen SHA and make it the writable Dev branch.
- Dev continues on the forward branch while the prior version publishes.
- Frozen release branches must not move while release-control is active. If release-control fails, diagnose first, then manage the smallest safe fix-forward path and propagate required fixes to the forward branch.

For current v1.0.23, development continues until the Coordinator explicitly chooses a release boundary. Recording a checkpoint alone is not a stop condition.
