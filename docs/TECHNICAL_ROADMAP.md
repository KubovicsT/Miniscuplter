# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. The Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, release chunking and version scope. `HANDOFF.md` owns the immediate Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
Frozen release source: `v1.0.23`
Current v1.0.23 release-fix candidate: `bda683264448fc8b51c7c538db61f8c0487a699a` pending exact-head branch validation before release-control retry
Current writable development branch: `v1.0.24`
Latest validated v1.0.24 implementation/test checkpoint: `e3dfba9aa26d7045b4bf9602920a484c443789c7`
Acceptance-weighted completion: **57%**

## 1. Current technical objective

Complete **Stage-C reference-machine acceptance** while preserving continuous development on v1.0.24 and repairing the already-frozen v1.0.23 release source without weakening release gates.

Accepted thin slice:

`2D source → accepted durable image baseline → local 3D generation → identity-bound candidate → explicit Apply → durable editable object → Move/Rotate/Scale + bounded sculpt/edit → save/reload → cleanup → exact validated STL export`

Released v1.0.22 still requires target-machine verification for MS-023/MS-009/MS-024/MS-025 and the full MS-018 flow. Any new serious target-machine evidence immediately preempts fallback architecture work.

## 2. Direction assessment

**Direction: PRESERVE.**

The selective-refactor architecture remains correct:

- Core owns durable project/object/revision/history state.
- Godot owns viewport/input/presentation.
- Python owns inference/geometry execution.
- Stable IDs, immutable revisions and transactional history remain the durable-state model.
- STL remains interchange/export, not project authority.
- Storage stays within Miniscuplter-controlled roots.
- Published releases remain immutable and fixes move forward only.

v1.0.24 MS-020 work has converged successfully rather than expanding into generalized infrastructure. The bounded seam now has:

- one heavyweight runtime owner;
- shared ownership across model/runtime mutation operations;
- cancellation that retains ownership until physical terminal acknowledgement;
- compact durable heavyweight-job lifecycle/tombstone state;
- startup reconciliation of abandoned running/cancelling work;
- fail-closed corrupt/unsupported journal behavior;
- bounded journal reads that cannot allocate from an attacker/corruption-sized file before rejecting it.

The explicitly ordered MS-020 restart-reconciliation slice is therefore **complete enough to stop broadening** while Stage-C acceptance is unavailable.

## 3. Release state — v1.0.23

v1.0.23 remains frozen for Coordinator-owned publication. v1.0.24 remains writable and independent.

### Failed retry diagnosis

The second autonomous release attempt failed correctly during release audit before Windows export. The previous repair advanced only launcher and installer version metadata to 1.0.23. The strict audit still expected 1.0.22 because several user/tool-visible identity surfaces remained stale.

The complete audited identity set was inconsistent:

- launcher: 1.0.23;
- installer: 1.0.23;
- updater: 1.0.22;
- Godot C# assembly: 1.0.22;
- Windows file/product export metadata: 1.0.22;
- backend API version: 1.0.22;
- editor displayed version: 1.0.22;
- release-audit expected version: 1.0.22.

This was a release-source defect, not a reason to weaken the audit.

### Current repair

Coordinator advanced the entire audited v1.0.23 identity set to 1.0.23 while leaving application scope unchanged. Current frozen-source candidate is `bda683264448fc8b51c7c538db61f8c0487a699a`.

Exact-head branch validation must finish green before release-control is pointed at this candidate. Once green, update the existing v1.0.23 release request to this exact SHA and let the complete autonomous exact-SHA Windows/Godot/export/hash/installer-smoke pipeline run again.

No unrelated source or documentation changes belong on v1.0.23.

## 4. Ordered critical path

### P0 — Preserve Stage-C target-machine evidence priority

At every Dev/Coordinator cycle, consume new reference-machine evidence first.

Any reproduced persistence, data-loss, viewport, storage, cancellation, provider or Stage-C blocker immediately preempts fallback work.

### P1 — Complete v1.0.23 publication safely

Coordinator-owned only:

1. wait for exact-head v1.0.23 branch validation at `bda683...`;
2. if red, diagnose and repair only the concrete release-source defect;
3. if green, update the existing release-control request to the exact current frozen HEAD;
4. supervise exact-SHA validation, real Godot Windows export, release audit, package/hash verification and installer smoke test;
5. verify immutable tag/Release target if publication succeeds;
6. never rewrite an already published tag/release.

Dev continues only on v1.0.24 throughout.

### P2 — v1.0.24 fallback: one bounded MS-019 authority-retirement seam

Because the ordered MS-020 restart-reconciliation seam is complete, **do not keep generalizing the Job Broker merely because acceptance is externally blocked**.

If no new target-machine evidence exists, the next structural fallback is one bounded outward migration under MS-019:

**Retire one proven duplicate legacy authority at an already-migrated Stage-C seam.**

Selection criteria:
- must touch a behavior already proven by Core/Stage-C tests;
- must reduce competing authority rather than add another compatibility layer;
- must not change product UX or broaden provider scope;
- must preserve migration compatibility until replacement behavior is validated;
- must have focused regression coverage proving the replacement owner remains authoritative;
- stop after one seam and record a checkpoint.

Preferred first seam: eliminate/delegate a remaining historical Stage-C generation/persistence owner that is already superseded by the final v1.0.22 acceptance owner, without removing compatibility paths that lack regression coverage. If inspection shows that seam is already fully inert, choose the smallest equivalent duplicate authority in transform/selection/persistence rather than inventing new infrastructure.

### P3 — Stage-C acceptance completion

When target testing is available, finish:

1. accepted 2D baseline;
2. intended local 3D route;
3. visible Ready/Conflict candidate;
4. explicit Apply;
5. save → close → reopen → same object/revision;
6. Move/Rotate/Scale and one supported sculpt/edit path;
7. cleanup and exact STL export;
8. viewport resize/presentation and starter-scene checks;
9. storage containment;
10. provider/resource evidence;
11. cancellation/recovery.

### P4 — After Stage-C acceptance

Absent new evidence, sequence:

1. finish MS-020 only to the degree required by proven real-job lifecycle gaps;
2. continue MS-019 outward migration and legacy-authority retirement;
3. make provider Fast/Balanced/Quality decisions from measured MS-022 evidence;
4. broaden Stage-D practical editing/sculpt/parts work.

## 5. Current issue priorities

1. **MS-018 — Critical / IN PROGRESS:** Stage-C acceptance.
2. **MS-023 — Critical / FIXED - NEEDS USER VERIFICATION:** durable generation/apply/reload ownership.
3. **MS-009 — Critical / FIXED - NEEDS USER VERIFICATION:** viewport stability/readability.
4. **MS-024 — High / FIXED - NEEDS USER VERIFICATION:** client-fill/resize seams.
5. **MS-013 — High / FIXED - NEEDS USER VERIFICATION:** storage containment.
6. **MS-022 — High / IN PROGRESS:** practical provider qualification.
7. **MS-004 — High / FIXED - NEEDS USER VERIFICATION:** cancellation/recovery.
8. **MS-020 — High / IN PROGRESS:** bounded job authority/restart seam now established; do not broaden speculatively.
9. **MS-019 — High architectural risk / IN PROGRESS:** next fallback is one authority-retirement seam.
10. **MS-025 — Medium / FIXED - NEEDS USER VERIFICATION:** starter scene removal.
11. **MS-027 — Medium:** planned bounded modernization sequence is substantially implemented; no further opportunistic UI expansion is currently ordered.

## 6. v1.0.24 release-chunk decision

**KEEP ACCUMULATING. Do not freeze v1.0.24 yet.**

Rationale:
- v1.0.23 publication is still being repaired and has not reached users;
- v1.0.24 currently contains a coherent but narrow MS-020 reliability seam, not yet a sufficiently substantial next release chunk by itself;
- continuous-development policy favors another coherent structural increment before Coordinator establishes the next boundary;
- recording `e3dfba9...` as a useful checkpoint does not imply a freeze.

A future v1.0.24 release boundary should be considered after one additional coherent authority-retirement increment or after important target-machine fixes materially increase the value of the release.

## 7. Architecture guards

- Do not create parallel state owners while retiring legacy paths.
- Migration-before-removal remains mandatory.
- New UI/presentation work should prefer stable version-neutral components, not additional `Main.V10xx...` layers.
- Job durability stays compact; do not turn the current lifecycle journal into a generalized persistent queue without evidence.
- AI results remain revision-bound candidates/conflicts; never silently overwrite newer durable state.
- User-observed GUI/GPU/runtime issues remain unresolved until real-machine evidence passes.
- Release identity must be branch/version consistent across every user/tool-visible surface before publication.

## 8. Release/version discipline

Continuous-development model:

- Dev records useful validated checkpoints and keeps working.
- Checkpoints are evidence, not freezes.
- Coordinator decides release chunk size using current branch HEAD.
- A Coordinator release request creates the freeze for that semantic-version source branch.
- The next forward semantic-version branch is writable while publication runs.
- Failed publication keeps the source frozen until the concrete failure is diagnosed.
- Never publish an older checkpoint by rewinding a moving branch.
- Never rewrite a published release/tag.

## 9. Target hardware / acceptance constraints

Reference machine:
- Windows;
- GTX 1080;
- 8 GB dedicated VRAM;
- 16 GB RAM;
- limited system-drive free space.

Existing Hunyuan-mini observation: roughly 402 s runtime, around 97% GPU, ~5.4/8 GB dedicated VRAM, ~5 GB RAM in the supplied snapshot, ~73 °C GPU. These are observations, not measured peaks.

## 10. Next Coordinator-level objectives for Dev

1. Consume new released-build target-machine evidence first.
2. Stay on writable v1.0.24; never mutate frozen v1.0.23 or release-control.
3. Do not broaden MS-020 after the completed restart-reconciliation seam.
4. If acceptance evidence remains unavailable, retire exactly one proven duplicate Stage-C authority under MS-019 and add focused regression coverage.
5. Stop after that bounded seam and record a useful validated checkpoint; continue only according to HANDOFF/next Coordinator sequencing.
6. Keep completion at 57% until acceptance evidence justifies change.
7. Preserve local-first/storage/data/revision/transaction boundaries.

## 11. User dependency

No new product-level decision is required.

The principal user dependency remains reference-machine testing of the latest published build, especially:

`Generate 3D → candidate visible → Apply → save → close/reopen → same 3D object/revision`

plus right-panel/whole-window resizing, no starter sphere/opaque floor, transform/sculpt, cleanup/export, storage containment, and cancellation/resource behavior.

Autonomous development can continue on v1.0.24.
