# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Current writable development branch:** `v1.0.24`.
- **Frozen release source:** `v1.0.23` at exact boundary `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Latest validated v1.0.24 implementation/test checkpoint:** `6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5`.
- **Overall completion:** **57% acceptance-weighted**.
- **Coordinator critical path:** Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts MS-020.

## v1.0.23 release outcome — frozen source remains untouched

Coordinator froze v1.0.23 and initiated publication at `a6532003bc6ecfcf79fc15afa3ee772cb117b982`. The autonomous release workflow validated the request and exact SHA, passed C#, Core, Python/runtime, job regressions, geometry/release audit, verified Godot download and the full Windows build, then failed **versioned-output verification**.

Concrete failure: the frozen candidate still built itself as **v1.0.22**. `build_release.ps1` logged `Building Miniscuplter v1.0.22 release package...` and Inno Setup emitted `Miniscuplter-Setup-1.0.22.exe`, while the v1.0.23 release gate correctly required `Miniscuplter-Setup-1.0.23.exe`.

Do not modify v1.0.23 or release-control from Dev. Coordinator owns release diagnosis and any directed frozen-source correction. Propagate any eventual release-source fix forward to v1.0.24 without rewriting published history.

## This Dev Cycle — second bounded MS-020 slice

With no new reference-machine evidence available, work followed the Coordinator-approved v1.0.24 structural baton.

### Component/runtime mutation now shares heavyweight ownership

Commits:

- `71bda33b66cf81a5245cd1e0b1f6585affcc3cee` — protects cached-model release from foreign active heavyweight owners;
- `01a928c0987ee93476a1592849ba052fa35c472a` — routes install/update/repair/remove through the same authoritative heavyweight lease used by inference and exposes component-operation identity/busy state;
- `6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5` — adds inference-vs-component, foreign-release, error-release and operation-kind regressions.

Behavior now established:

1. a component install/update/repair/remove creates an explicit operation identity and kind (`component-install`, `component-update`, `component-repair`, `component-remove`);
2. it acquires the same `resource_ownership` lease used by `3d-generate`, non-blocking, before any component mutation;
3. an active inference owner cannot be stolen, cleared or interrupted by component mutation;
4. `release_all_models()` independently refuses to tear down cached models while a foreign heavyweight owner exists;
5. once the component operation owns the lease, cached models can be released safely before mutation;
6. v1.0.5 audited/resumable install/update remains the underlying implementation, preserving deterministic staging, partial-download resume behavior and storage containment;
7. component status exposes the current resource owner and successful operations return their operation id/kind;
8. failure paths release ownership deterministically.

This remains deliberately process-local and is **not** the completed Job Broker. It does not yet provide persistent queued state, worker/process isolation, truthful terminal cancellation acknowledgement after actual worker stop, or crash recovery.

## Validation

Exact implementation/test checkpoint `6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5` is fully green:

- `core-foundation` run `34557452791`: **PASS**;
- broader `build` run `34557452815`: **PASS**;
- C# editor/launcher/updater/Core builds: **PASS**;
- Python compile and runtime dependency resolution: **PASS**;
- core logic, execution and job regressions: **PASS**;
- real geometry regressions and release audit: **PASS**;
- portable package layout/hash: **PASS**;
- installer-definition compilation: **PASS**.

Later commits in this run are documentation-only and do not supersede the validated code/test checkpoint.

## Exact next task

First consume any new reference-machine Stage-C evidence. If none exists, continue **one bounded MS-020 slice** focused on cancellation truthfulness for the currently migrated heavyweight 3D lifecycle.

**Make cancellation state distinguish “requested” from “worker actually stopped”, without pretending process isolation is already complete.**

Requirements:

- map the current editor/backend cancellation/reset path before changing semantics;
- keep `request_cancel` as a request/cancelling state only;
- add one explicit terminal cancellation acknowledgement path that is invoked only after the owned worker/provider execution is actually known to have stopped or the owned backend process has been terminated;
- never release the heavyweight lease merely because cancellation was requested;
- prevent a cancellation-requested job from later being recorded as a successful provider qualification or silently completing as if no cancellation occurred;
- preserve Stage-C stale-result/candidate protections and do not apply partial output;
- ensure the next heavyweight job cannot start until the prior owner is truly released;
- add focused regressions for cancel-request → still-owned, terminal acknowledgement → released, and attempted late completion after cancellation;
- stop after this cancellation-lifecycle slice. Persistent queue/journal state, isolated worker processes and crash recovery remain later MS-020 work unless the implementation evidence proves one is a prerequisite.

## Release/checkpoint rule

`6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5` is a useful validated implementation checkpoint, but it does not freeze v1.0.24 and does not authorize publication. Release readiness/chunk size/publication remain Coordinator-owned.

## User verification dependency

Reference-machine testing remains required. Until a newer build is successfully published, v1.0.22 remains the latest stable build. Exercise Generate 3D → candidate → Apply → save/close/reopen → same object/revision; resize/presentation; no starter sphere/opaque floor; transform/sculpt; cleanup/export; storage containment; cancellation/recovery; and resource behavior during a long AI job.
