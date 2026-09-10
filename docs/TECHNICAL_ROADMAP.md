# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. The Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, and version scope. `HANDOFF.md` remains the immediate Dev Cycle baton.

Last coordinator review: 2026-09-10
Current stable release: `v1.0.19`
Current development branch: `v1.0.20`
Latest validated application/release-candidate code commit: `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`
Latest live branch HEAD reviewed before this roadmap update: `6ead6ae2f3c2823b7b3b93f2fae805e6808f6f57`
Acceptance-weighted completion: **58%**

## 1. Current technical objective

Publish the already-bounded v1.0.20 Stage-C foundation slice without reopening application scope, then use the released build to obtain the target-machine evidence required for Stage-C acceptance.

The implemented slice is:

`2D source → accepted immutable image baseline → revision-bound local 3D generation → explicit candidate Apply → visible project-owned object → save/reload → Core-authoritative transform + one committed immutable mesh edit → immutable cleanup revision → exact validated STL export`

The application milestone is now structurally complete enough to be a release candidate. The immediate blocker is release orchestration, not missing product architecture.

## 2. Technical-direction assessment

**Direction: PRESERVE.**

The selective-refactor strategy remains correct and recent Dev Cycle work followed the intended convergence path. The bounded v1.0.20 scope was completed without broadening into full sculpt migration, Job Broker reconstruction, Rig/Pose, kitbash, provider expansion, global `Main.V*.cs` removal, or UI rewrite.

Do not start another architectural migration on v1.0.20. Do not reopen already-green Stage-C application work merely because the autonomous release controller is failing. Fix the release-control mechanism at its own boundary, publish the candidate when all gates pass, then move application work forward to v1.0.21.

## 3. Current milestone — publish v1.0.20

### Application scope already satisfied

- mapped Stage-C transforms commit through `ProjectSession` and persist through save/reload;
- Stage-C-aware undo/redo restores durable project state;
- one bounded sculpt/edit path creates immutable child `MeshRevision` state with stale-parent protection;
- cleanup advances immutable revision lineage;
- export resolves durable object/revision state and validated STL output scope;
- prior release-candidate Core/build/Python/geometry/release-audit/package validation is green.

### Remaining release gate

The permanent `release-control` workflow must successfully:

1. parse one exact-SHA semantic-version release request;
2. prove the requested source branch still equals that candidate SHA;
3. rebuild/retest the exact candidate;
4. perform real Godot Windows export;
5. verify release outputs and hashes;
6. create and smoke-install the installer;
7. recheck branch immutability before publication;
8. create the version tag and GitHub Release only after all gates pass.

The latest retry, autonomous-release run `34506900865`, failed **after successfully validating the v1.0.20 request** but before build/export. The PowerShell validation step intentionally probes `gh release view` to confirm that the release does not already exist; the expected nonzero native exit code remains in `$LASTEXITCODE`, so the otherwise-successful step terminates with exit code 1. This is a release-orchestration defect, not evidence against the v1.0.20 source candidate.

## 4. Ordered critical path

### P0 — Repair autonomous release orchestration

Dev Cycle should modify only the `release-control` workflow as needed to make the expected absent-release probe return a successful workflow step while preserving the immutable-release check. Do not weaken request validation, exact-SHA binding, branch-freeze behavior, build/export/hash/smoke gates, or published-release immutability.

After the workflow repair:

- update the existing `release-requests/v1.0.20.json` to the exact final v1.0.20 branch HEAD;
- freeze v1.0.20 while that request is pending;
- inspect the resulting workflow through completion;
- if a genuine source/build gate fails, fix forward and resubmit a new exact candidate;
- if the workflow succeeds, verify tag/release v1.0.20 and latest-release state.

### P1 — Transition to v1.0.21 after publication

Immediately after v1.0.20 publication:

- reconcile canonical docs to stable v1.0.20;
- create/use forward-only `v1.0.21` before any further application change;
- never modify the published v1.0.20 tag/release.

### P2 — Reference-machine acceptance

Use released v1.0.20 on the GTX 1080 / 8 GB VRAM / 16 GB RAM reference PC to collect evidence for:

1. **MS-009** — viewport/grid/model/gizmo visibility and interaction;
2. **MS-013** — representative storage containment including caches/temp/intermediates;
3. **MS-018** — the complete Stage-C path through save/reload/edit/cleanup/export;
4. **MS-022** — one intended lightweight/default 3D route with elapsed time and practical RAM/VRAM behavior;
5. cancellation/recovery where practical in the same test session.

Any reproduced target-machine failure immediately outranks planned post-release refactor work and must be fixed forward in v1.0.21.

### P3 — Post-acceptance structural work

Only after target-machine findings are addressed or acceptance evidence is collected:

1. resume `MS-020` authoritative durable job ownership/queue/crash recovery;
2. continue `MS-019` outward from the proven Stage-C editing seam;
3. use measured `MS-022` evidence to define default/supported/experimental provider tiers;
4. then advance broader Stage-D practical editing priorities.

## 5. Issue priority

1. **MS-018 — Critical milestone:** code slice is release-candidate complete; publication + target qualification remain.
2. **Release-control orchestration defect — release-critical:** latest workflow validates the request but exits incorrectly before application gates. Treat as immediate P0 operational work supporting MS-018, not as product-scope expansion.
3. **MS-009 — Critical verification risk:** becomes immediate P0 if the released viewport remains blank on the target machine.
4. **MS-013 — High verification risk:** verify before further speculative storage work unless a concrete new leak appears.
5. **MS-022 — High:** qualify one intended route before adding provider breadth.
6. **MS-019 — broader migration:** bounded v1.0.20 requirement is met; remaining migration waits behind release/acceptance.
7. **MS-020 — High but sequenced later:** resume after Stage-C release/acceptance unless a concrete lifecycle defect blocks testing.

## 6. Architecture and ownership boundaries

- **Core C# domain/state:** durable project identity, immutable revisions, transforms, transactions/history, persistence, stale-result rules.
- **Godot presentation/editor:** projection and interaction for Core-owned Stage-C state; no independent durable truth for migrated objects.
- **Python backend:** inference/geometry execution and provider/runtime management, not project-state authority.
- **Launcher/updater:** delivery/runtime/data-preservation responsibilities.
- **release-control:** release-request validation and gated delivery orchestration only; it must not become an application-development branch.
- **STL:** interchange/export artifact only.

## 7. Explicit non-priorities while v1.0.20 is pending

Do not spend v1.0.20 cycles on:

- full sculpt migration;
- broad legacy `Main.V*.cs` removal;
- full durable Job Broker architecture;
- Rig & Pose modernization;
- kitbash/parts expansion;
- provider proliferation;
- full UI rewrite;
- optional cleanup feature breadth.

## 8. Technical risks to watch

1. Release orchestration churn must not cause application-scope churn.
2. Repeated release-control fixes must preserve all safety gates rather than progressively bypass them.
3. v1.0.20 must remain frozen during an active release request so request SHA and branch state cannot diverge.
4. Target-machine acceptance still cannot be inferred from CI, even after a successful Windows release workflow.
5. Published versions remain immutable; failures discovered after release are fixed in v1.0.21+.
6. Compatibility bridges remain transitional and should only be broadened after Stage-C evidence justifies it.

## 9. Next Coordinator-level objectives for Dev Cycle

Until superseded by a later Coordinator review:

1. repair the release-control PowerShell success/exit handling without weakening any gate;
2. resubmit the exact final v1.0.20 HEAD through the existing release request and freeze the branch;
3. drive the autonomous release to a definitive success or genuine failing gate;
4. on success, verify v1.0.20 publication and move all further application development to v1.0.21;
5. prioritize real reference-machine findings over speculative architecture work;
6. only after acceptance evidence, resume broader MS-019/MS-020 work.

## 10. User input

No product-level decision or manual release action currently blocks autonomous engineering. User involvement becomes necessary after v1.0.20 publication for reference-machine acceptance evidence.