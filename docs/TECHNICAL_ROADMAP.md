# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. The Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, release chunking and version scope. `HANDOFF.md` owns the immediate Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.23` at `bda683264448fc8b51c7c538db61f8c0487a699a`
Current writable development branch: `v1.0.24`
Latest validated v1.0.24 implementation/test checkpoint: `e3dfba9aa26d7045b4bf9602920a484c443789c7`
Acceptance-weighted completion: **57%**

## 1. Current technical objective

Complete **Stage-C reference-machine acceptance** on the newly published v1.0.23 while preserving continuous development on v1.0.24.

Accepted thin slice:

`2D source → accepted durable image baseline → local 3D generation → identity-bound candidate → explicit Apply → durable editable object → Move/Rotate/Scale + bounded sculpt/edit → save/reload → cleanup → exact validated STL export`

v1.0.23 carries the v1.0.22 acceptance fixes plus the bounded MS-027 modernization sequence and passed the complete autonomous exact-SHA Windows release pipeline. This still does **not** substitute for reference-machine GUI/GPU/runtime acceptance.

## 2. Direction assessment

**Direction: PRESERVE.**

- Core owns durable project/object/revision/history state.
- Godot owns viewport/input/presentation.
- Python owns inference/geometry execution.
- Stable IDs, immutable revisions and transactional history remain the durable-state model.
- STL remains interchange/export, not project authority.
- Storage stays within Miniscuplter-controlled roots.
- Published releases are immutable; fixes move forward only.

v1.0.24 MS-020 work has converged into a bounded, testable reliability seam:

- one heavyweight runtime owner;
- shared ownership across model/runtime mutation operations;
- cancellation retains ownership until physical terminal acknowledgement;
- compact durable heavyweight-job lifecycle/tombstone state;
- startup reconciliation of abandoned running/cancelling work;
- fail-closed corrupt/unsupported journal behavior;
- bounded journal reads before decode/parse.

The explicitly ordered MS-020 restart-reconciliation slice is complete enough to stop broadening while Stage-C acceptance is unavailable.

## 3. v1.0.23 release outcome

**PUBLISHED SUCCESSFULLY.**

Release target/tag: `bda683264448fc8b51c7c538db61f8c0487a699a`.

The final release-source repair advanced the complete audited version-identity set to 1.0.23. Exact-head branch validation passed, then autonomous release run `34575505010` passed:

- exact request/source SHA and immutability checks;
- C# editor/launcher/updater/Core build and Core tests;
- Python/runtime dependency checks;
- core/job regressions;
- geometry regressions and strict release audit;
- verified Godot 4.7.2 .NET/templates;
- real Windows release build;
- versioned output and ZIP/hash verification;
- `Miniscuplter-Setup-1.0.23.exe` silent installer smoke test;
- immutable target recheck;
- lightweight `v1.0.23` tag creation;
- GitHub Release publication with verified installer/ZIP/hash assets.

The earlier failed retries remain useful release-safety history: version identity must be coherent across launcher, updater, Godot assembly, installer, Windows export metadata, backend, editor display and release audit. Do not weaken this gate.

v1.0.23 is now immutable. All future changes belong on v1.0.24 or later.

## 4. Ordered critical path

### P0 — Consume v1.0.23 reference-machine evidence

Any reproduced persistence, data-loss, viewport, storage, cancellation, provider or Stage-C blocker immediately preempts fallback work.

Test the exact published v1.0.23 build rather than development snapshots.

### P1 — Complete Stage-C acceptance

Required evidence:

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

### P2 — v1.0.24 fallback: one bounded MS-019 authority-retirement seam

If reference-machine evidence is still unavailable, retire exactly one proven duplicate legacy authority at an already-migrated Stage-C seam.

Selection criteria:
- behavior already proven by Core/Stage-C tests;
- reduces competing authority rather than adding another bridge;
- does not change product UX or provider breadth;
- preserves migration compatibility until replacement behavior is validated;
- focused regression coverage proves the replacement owner;
- stop after one seam and record a checkpoint.

Preferred first seam: eliminate/delegate a remaining historical Stage-C generation/persistence owner already superseded by the final v1.0.22/v1.0.23 acceptance owner. If inspection shows that seam is fully inert, choose the smallest equivalent duplicate authority in transform/selection/persistence.

### P3 — After Stage-C acceptance

Absent new evidence:

1. finish MS-020 only for real-job lifecycle gaps exposed by acceptance;
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
8. **MS-020 — High / IN PROGRESS:** bounded job authority/restart seam established; do not broaden speculatively.
9. **MS-019 — High architectural risk / IN PROGRESS:** next fallback is one authority-retirement seam.
10. **MS-025 — Medium / FIXED - NEEDS USER VERIFICATION:** starter scene removal.
11. **MS-027 — Medium:** planned bounded modernization sequence substantially implemented; no further opportunistic UI expansion currently ordered.

## 6. v1.0.24 release-chunk decision

**KEEP ACCUMULATING. Do not freeze v1.0.24 yet.**

Rationale:
- v1.0.23 has only just become the published testable increment;
- v1.0.24 currently contains a coherent but narrow MS-020 reliability seam;
- one additional coherent authority-retirement increment, or meaningful target-machine fixes, would make a better Coordinator-curated next release chunk;
- checkpoint `e3dfba9...` is evidence, not a freeze.

## 7. Architecture guards

- Do not create parallel state owners while retiring legacy paths.
- Migration-before-removal remains mandatory.
- New UI/presentation work should prefer stable version-neutral components, not additional `Main.V10xx...` layers.
- Job durability stays compact; do not turn the current lifecycle journal into a generalized persistent queue without evidence.
- AI results remain revision-bound candidates/conflicts; never silently overwrite newer durable state.
- User-observed GUI/GPU/runtime issues remain unresolved until real-machine evidence passes.
- Release identity must be coherent across every user/tool-visible surface before publication.

## 8. Continuous-development release discipline

- Dev records validated checkpoints and continues development.
- Checkpoints are evidence, not freezes.
- Coordinator decides release chunk size from current branch HEAD.
- A Coordinator release request freezes only the source version branch.
- The already-created next semantic-version branch remains writable while publication runs.
- Failed publication keeps the source frozen until the concrete failure is diagnosed.
- Never rewind a moving branch to publish an older checkpoint.
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

1. Consume new v1.0.23 reference-machine evidence first.
2. Work only on writable v1.0.24; v1.0.23 is immutable.
3. Do not broaden MS-020 after the completed restart-reconciliation seam.
4. If acceptance evidence remains unavailable, retire exactly one proven duplicate Stage-C authority under MS-019 and add focused regression coverage.
5. Stop after that bounded seam, validate strongly and record a useful checkpoint.
6. Keep completion at 57% until acceptance evidence justifies change.
7. Preserve local-first/storage/data/revision/transaction boundaries.

## 11. User dependency

No new product-level decision is required.

Test released v1.0.23, especially:

`Generate 3D → candidate visible → Apply → save → close/reopen → same 3D object/revision`

plus right-panel/whole-window resizing, no starter sphere/opaque floor, transform/sculpt, cleanup/export, storage containment, and cancellation/resource behavior.

Autonomous development can continue on v1.0.24.
