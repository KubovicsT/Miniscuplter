# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. The Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, and version scope. `HANDOFF.md` remains the immediate Dev Cycle baton.

Last coordinator review: 2026-09-10
Current stable release: `v1.0.20`
Stable release target: `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
Current development branch: `v1.0.21`
Latest v1.0.21 HEAD reviewed: `214b890b3c46be4ef7451c5c9a8fd60f03eee10f`
Acceptance-weighted completion: **58%**

## 1. Current technical objective

Qualify the shipped v1.0.20 Stage-C foundation on the reference machine, use v1.0.21 strictly for evidence-driven forward fixes, and only then broaden architecture work.

The shipped slice is:

`2D source → accepted immutable image baseline → revision-bound local 3D generation → explicit candidate Apply → visible/editable Core-owned object → transform/sculpt → save/reload → immutable cleanup revision → exact validated STL export`

The application and release pipeline have now proven this slice structurally and through automated Windows release gates. The remaining milestone risk is real-machine behavior, not missing speculative architecture.

## 2. Technical-direction assessment

**Direction: PRESERVE.**

The selective-refactor strategy remains correct. Recent Dev Cycles converged on one vertical slice, kept Core/Godot/Python ownership boundaries intact, avoided broad migration, repaired release orchestration without weakening safety gates, and successfully published v1.0.20.

The autonomous `release-control` path is now proven end to end and remains the preferred release mechanism. The historical explicit-tag path stays as fallback.

Do not interpret release/CI success as target-machine acceptance. Do not broaden migration merely because v1.0.20 shipped.

## 3. Current milestone — reference-machine Stage-C acceptance

Use released v1.0.20 on the GTX 1080 / 8 GB VRAM / 16 GB RAM reference PC.

Acceptance requires evidence for:

1. **Complete Stage-C flow (`MS-018`)**
   - 2D input/import succeeds.
   - Baseline acceptance persists.
   - One intended lightweight/default 3D route produces a visible candidate.
   - Apply produces a visible/editable project-owned object.
   - Transform and the bounded sculpt/edit path visibly work.
   - Save/reload reproduces the committed model state.
   - Cleanup succeeds on the intended revision lineage.
   - Exported STL matches the exact durable edited state and validates.

2. **Viewport (`MS-009`)**
   - grid, model, selection and gizmo are visible and usable in the released build.
   - Any reproduced blank viewport becomes immediate P0.

3. **Storage containment (`MS-013`)**
   - runtime/model caches, temp/intermediate files and process environment remain within intended Miniscuplter-controlled locations except explicitly approved external paths.

4. **Provider qualification (`MS-022`)**
   - at least one intended lightweight/default 3D provider completes on GTX 1080 / 16 GB.
   - record elapsed time and practical RAM/VRAM behavior.

5. **Cancellation/recovery (`MS-004`)**
   - exercise cancellation/recovery during a real job where practical.
   - no corrupt project state or stale-result application.

## 4. Ordered critical path

### P0 — Obtain target-machine evidence

The next meaningful dependency is the reference-machine acceptance pass. Each Dev Cycle must inspect new user/runtime evidence before choosing work.

If no target evidence is available, do not invent a broad refactor to fill time. Keep v1.0.21 ready for forward fixes and only add low-risk acceptance instrumentation when it directly improves diagnosis of the required tests.

### P1 — Fix reproduced target regressions forward on v1.0.21

Priority within observed failures:

1. blank/unusable viewport, data loss or storage-safety regression;
2. Stage-C flow blocker;
3. provider failure/VRAM-RAM incompatibility on the reference machine;
4. cancellation/recovery defect;
5. lower-severity UX defects that materially obstruct acceptance.

Every fix must preserve v1.0.20 immutability and receive the strongest relevant automated validation on v1.0.21.

### P2 — Close Stage-C acceptance

When the full acceptance path and companion checks are green, record exact evidence in `PROJECT_STATUS.md` / `ISSUES.md`. Only then should the Coordinator mark the Stage-C milestone accepted and set the next broader architectural scope.

### P3 — Post-acceptance structural work

Unless target evidence changes priorities, sequence post-acceptance work as:

1. `MS-020` — authoritative durable job ownership, queue/resource lifecycle and crash recovery;
2. `MS-019` — continue legacy-authority migration outward from the proven Stage-C seam;
3. `MS-022` — define default/supported/experimental provider tiers from measured evidence;
4. advance broader Stage-D practical editing work.

Do not execute this P3 work ahead of the acceptance dependency merely to maintain activity volume.

## 5. v1.0.21 intended scope

Until a later Coordinator review expands it, v1.0.21 is primarily the **forward-fix and acceptance-support branch** for findings from released v1.0.20.

In scope:
- concrete target-machine regression fixes;
- diagnostics/acceptance instrumentation directly required to reproduce or prove those fixes;
- regression tests for observed failures;
- canonical documentation of real acceptance evidence.

Not yet in scope:
- full sculpt migration;
- broad `Main.V*.cs` removal;
- complete Job Broker rewrite absent a concrete acceptance blocker;
- Rig & Pose modernization;
- kitbash expansion;
- provider proliferation;
- full UI rewrite;
- optional cleanup feature breadth.

If target-machine acceptance is entirely green, the next Coordinator review may redefine v1.0.21 or a subsequent version around the next structural milestone.

## 6. Issue priority

1. **MS-018 — Critical:** complete reference-machine Stage-C qualification.
2. **MS-009 — Critical verification risk:** reproduced blank viewport becomes immediate P0.
3. **MS-013 — High:** verify representative storage containment; severe leak/safety failure becomes P0.
4. **MS-022 — High:** qualify one intended lightweight/default route on reference hardware before provider breadth.
5. **MS-004 — High acceptance companion:** verify cancellation/recovery where practical.
6. **MS-019 — High architectural risk:** broader migration paused until acceptance evidence unless a concrete defect requires it.
7. **MS-020 — High:** broader durable broker work follows acceptance unless a lifecycle defect blocks testing.

## 7. Architecture and dependency boundaries

- **Core C#:** durable project identity, objects, immutable image/mesh revisions, transforms, transactions/history, persistence, provenance and stale-result rules.
- **Godot:** viewport, input and interactive projection of Core-owned migrated state; it must not become an independent durable authority.
- **Python:** inference/geometry/provider/runtime execution, not project-state authority.
- **Launcher/updater:** delivery, runtime and data-preservation responsibilities.
- **release-control:** gated exact-SHA release orchestration only, not an application-development branch.
- **STL:** interchange/export artifact only.

Reference-machine acceptance depends on the released immutable build; broader architecture sequencing depends on the evidence from that acceptance pass.

## 8. Technical risks to watch

1. **False acceptance:** CI and a successful Windows release do not prove GTX 1080 rendering/CUDA/storage behavior.
2. **Idle-work drift:** scheduled Dev Cycles must not start generalized infrastructure simply because user acceptance evidence has not arrived yet.
3. **Forward-fix scope creep:** v1.0.21 should not quietly become another broad migration branch before acceptance.
4. **Compatibility bridge permanence:** transitional `Main.V*` bridges must not become new long-term business-logic authority.
5. **Provider overgeneralization:** policy should follow measured reference-hardware evidence, not adapter count.
6. **Release immutability:** all post-release defects are fixed forward; v1.0.20 is never rewritten.

## 9. Next Coordinator-level objectives for Dev Cycle

Until superseded by a later Coordinator review:

1. inspect for new v1.0.20 target-machine evidence before coding;
2. help obtain/reproduce the complete Stage-C acceptance path and MS-009/MS-013/MS-022/MS-004 evidence;
3. if a defect is reproduced, document it and fix it forward on v1.0.21 with targeted regression coverage;
4. if no evidence exists yet, avoid broad architecture work and keep the branch diagnostically ready;
5. once acceptance is green, record the evidence and defer broader MS-019/MS-020 sequencing to the next Coordinator review.

## 10. User dependency

No product-level decision is currently required. The important current dependency is **reference-machine testing of released v1.0.20**. The user should report concrete pass/fail observations from that build; difficult-to-reverse product questions should only be raised if those findings expose one.