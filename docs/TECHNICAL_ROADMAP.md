# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. The Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, and version scope. `HANDOFF.md` owns the immediate Dev Cycle baton.

Last coordinator review: 2026-09-10
Current stable release: `v1.0.22`
Stable release target: `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
Current development branch: `v1.0.23`
Repository HEAD reviewed before this roadmap update: `549d1018866e242a3b0d5344f6840903a36fa6f6`
Acceptance-weighted completion: **57%**

## 1. Current technical objective

Complete **Stage-C reference-machine acceptance** on released v1.0.22 while keeping v1.0.23 as a narrow forward-fix / acceptance-support branch.

The accepted thin slice remains:

`2D source → accepted durable image baseline → local 3D generation → identity-bound candidate → explicit Apply → durable editable object → Move/Rotate/Scale + bounded sculpt/edit → save/reload → cleanup → exact validated STL export`

Released v1.0.22 contains bounded fixes for the concrete v1.0.21 findings:

- **MS-023** — one authoritative Stage-C Generate-3D owner so successful output cannot bypass candidate/Apply persistence;
- **MS-009** — final viewport presentation guard and non-occluding grid behavior;
- **MS-024** — whole-window client-fill correction;
- **MS-025** — automatic starter sphere removal.

All remain **FIXED - NEEDS USER VERIFICATION** until retested on the reference Windows / GTX 1080 / 8 GB VRAM / 16 GB RAM machine.

When acceptance is externally blocked and no higher-priority correctness work exists, exactly one bounded MS-027 modernization slice may advance.

## 2. Direction assessment

**Direction: PRESERVE, with a narrow execution correction.**

The selective-refactor architecture remains correct:

- Core owns durable project/object/revision/history state.
- Godot owns viewport/input/presentation.
- Python owns inference/geometry execution.
- Stable IDs, immutable revisions and transactional history remain the state model.
- STL remains export/interchange, not project authority.
- Local Job Broker remains the intended long-running execution architecture.
- Storage remains inside Miniscuplter-controlled roots.
- Published releases are immutable and development moves forward by semantic version.

Recent Dev work generally followed the Coordinator fallback: first workspace-preference/UI-scale/tooltips, then one direct viewport-tool strip slice.

However, the latest tool-strip composition introduced a **compile regression** at exact branch HEAD: broader build CI fails with `CS0122` because `ExtrasInstaller` calls `Main.InstallV1023ViewportToolStrip()` while that method is inaccessible. Core/Python/geometry/release-audit/packaging legs pass. This is a completed worker state and a real release-blocking regression.

The correct response is a narrow implementation fix and exact-head revalidation, not a roadmap redesign.

## 3. Current milestone — Stage-C reference-machine acceptance

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
   - one practical local 3D route works on the reference hardware;
   - preserve elapsed time and resource evidence.

6. **Cancellation/recovery — MS-004**
   - cancel/restart does not poison the next job or corrupt project state.

## 4. Ordered critical path

### P0 — Restore green v1.0.23 exact-head build

Before any additional UI feature work:

1. fix the `InstallV1023ViewportToolStrip()` accessibility/composition compile regression;
2. run exact-head C# build plus existing Core/Python/geometry/release-audit/packaging gates;
3. update HANDOFF/STATUS to the actual validated SHA;
4. do not start scene hierarchy, view cube, AI console, telemetry or unrelated work while the branch is red.

This is implementation work owned by Dev Cycle.

### P1 — Consume v1.0.22 reference-machine evidence

At every Dev Cycle start, check for new released-v1.0.22 user evidence.

Any reproduced persistence, data-loss, viewport, storage, cancellation or Stage-C blocker immediately preempts MS-027.

### P2 — Complete Stage-C acceptance

Once reference-machine testing is available, finish MS-018 through persistence, transforms/sculpt, cleanup/export, storage, provider evidence and cancellation/recovery.

### P3 — Opportunistic MS-027 only while P1/P2 are externally blocked and P0 is green

Completed/started bounded slices:

1. workspace splitter persistence + UI/font scale + tooltip infrastructure — code present, target/UI verification pending;
2. direct Select/Move/Rotate/Scale/Sculpt tool strip — implemented but currently **not validated because exact-head C# build is red**.

After slice 2 is green, the next allowed fallback slice is:

3. **synchronized collapsible scene hierarchy**.

Requirements:
- reflect existing durable project/object identity;
- synchronize tree and viewport selection through one selection owner;
- do not create a parallel scene-state model;
- stop after this slice.

Subsequent order:
4. view cube + selected-object orbit pivot;
5. unified AI command console/history through one authoritative dispatcher;
6. MS-026 resource telemetry;
7. density/spacing cleanup and retirement of superseded explanatory UI.

## 5. MS-027 architecture guard

The modernization must reduce, not extend, version-derived UI composition.

The current `Main.V1023...` files are accepted only as bounded migration bridges. Do **not** make `Main.V1024...`, `Main.V1025...`, etc. the permanent UI architecture.

For future slices, prefer stable version-neutral presentation components/services where practical. Reuse one authoritative underlying owner:

- viewport tool state/input: existing V1018 path until deliberately migrated;
- scene/object identity: Core/project authority;
- selection: one synchronized owner;
- AI actions: one dispatcher;
- UI preferences: presentation-only state outside ProjectStore.

Migration rule: move authority, prove behavior, then retire superseded legacy presentation paths. Do not stack another compatibility owner.

## 6. Issue priority

1. **MS-028 — High / OPEN:** v1.0.23 exact-head C# compile regression; immediate release blocker and feature-work gate.
2. **MS-023 — Critical / FIXED - NEEDS USER VERIFICATION.**
3. **MS-018 — Critical / IN PROGRESS.**
4. **MS-009 — Critical / FIXED - NEEDS USER VERIFICATION.**
5. **MS-024 — High / FIXED - NEEDS USER VERIFICATION.**
6. **MS-013 — High / FIXED - NEEDS USER VERIFICATION.**
7. **MS-022 — High / IN PROGRESS.**
8. **MS-004 — High / FIXED - NEEDS USER VERIFICATION.**
9. **MS-025 — Medium / FIXED - NEEDS USER VERIFICATION.**
10. **MS-027 — Medium / OPPORTUNISTIC.**
11. **MS-026 — Medium / PLANNED inside MS-027.**

MS-028 is above all new feature work but does not supersede the product-level Stage-C milestone once fixed.

## 7. v1.0.23 intended scope

v1.0.23 remains a **forward acceptance-fix + bounded opportunistic UI branch**.

In scope:
- narrow fixes for v1.0.22 reference-machine failures;
- acceptance diagnostics/regressions;
- MS-027 preference/layout slice;
- MS-027 direct tool-strip slice after build repair;
- at most one further bounded fallback slice at a time while acceptance is externally blocked;
- accurate docs/release preparation.

Not in scope without new evidence or Coordinator change:
- monolithic UI rewrite;
- broad Main.V* removal;
- full Job Broker reconstruction;
- full sculpt migration;
- Rig & Pose modernization;
- kitbash expansion;
- provider proliferation;
- unrelated feature breadth.

Do not release v1.0.23 while exact-head C# CI is red. Do not publish it merely because CI later becomes green; release only when scope is coherent and exact-SHA Windows/export/hash/installer gates pass.

## 8. Target hardware / resource constraints

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

MS-026 should remain local, low overhead, roughly 1 Hz, omit unavailable sensors gracefully, and associate resource samples with provider/job stage/elapsed time.

## 9. Technical risks

1. **False acceptance:** CI cannot close target-only Godot/CUDA/UI issues.
2. **Red-branch drift:** new features must not accumulate while exact-head build is broken.
3. **UI overlay accretion:** MS-027 must not become another endless `Main.V*.cs` stack.
4. **Duplicate authority:** tool strip, hierarchy and AI console are high-risk seams.
5. **Acceptance displacement:** UI fallback remains subordinate to Stage-C.
6. **Storage regression:** preference/telemetry/job data must stay inside controlled roots.
7. **Provider overclaim:** one successful run is useful evidence, not a support matrix.
8. **Release/version drift:** published releases remain immutable; release-request branches freeze while active.

## 10. Next Coordinator-level objectives for Dev Cycle

1. Fix MS-028 first and restore green exact-head build.
2. Reconcile HANDOFF/PROJECT_STATUS to the actual validated head after the fix.
3. Before selecting any further fallback work, consume new v1.0.22 reference-machine evidence.
4. If target evidence exposes a serious blocker, fix it forward narrowly on v1.0.23.
5. If acceptance remains externally blocked and CI is green, take only the synchronized scene-hierarchy slice next.
6. Keep one selection/project identity owner; no duplicate scene state.
7. Prefer stable version-neutral UI components for new MS-027 presentation code.
8. Stop after one bounded fallback slice per cycle.
9. Keep completion at 57% until acceptance evidence changes it.
10. Do not submit a v1.0.23 release request from a red or merely convenient scheduler state.

## 11. User input / verification dependency

No new product-level decision is required.

The principal user dependency remains reference-machine testing of released v1.0.22, especially:

`Generate 3D → candidate visible → Apply → save → close/reopen → same 3D object/revision`

plus right-panel resize, whole-window resize, no starter sphere/opaque floor, transform/sculpt, cleanup/export and storage behavior.

Autonomous engineering can continue after the current compile regression is repaired.
