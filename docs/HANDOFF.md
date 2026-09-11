# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Current writable development branch:** `v1.0.24`.
- **Frozen release source:** `v1.0.23` at exact boundary `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Latest v1.0.24 implementation/test head:** `2cd8a6c85ac9cfe838c7ac14144b75dd9510a959`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts MS-020.

## v1.0.23 release outcome — frozen source remains untouched

Coordinator froze v1.0.23 and initiated publication at `a6532003bc6ecfcf79fc15afa3ee772cb117b982`. The autonomous release workflow validated the request and exact SHA, passed C#, Core, Python/runtime, job regressions, geometry/release audit, verified Godot download and the full Windows build, then failed **versioned-output verification**.

Concrete failure: the frozen candidate still built itself as **v1.0.22**. `build_release.ps1` logged `Building Miniscuplter v1.0.22 release package...` and Inno Setup emitted `Miniscuplter-Setup-1.0.22.exe`, while the v1.0.23 release gate correctly required `Miniscuplter-Setup-1.0.23.exe`.

Do not modify v1.0.23 or release-control from Dev. Coordinator owns release diagnosis and any directed frozen-source correction. Propagate any eventual release-source fix forward to v1.0.24 without rewriting published history.

## This Dev Cycle — first bounded MS-020 slice

With no new reference-machine evidence available, work followed the Coordinator-approved v1.0.24 structural baton.

### Heavyweight local-runtime ownership seam

Commits:

- `be2b10c8099ce6977ea0ff217e5f93d1af2f8e5f` — adds `ai_backend/resource_ownership.py`, one explicit process-local heavyweight-runtime lease with owner identity and safe release;
- `6030be36ceb7b8a5f2841fc0010816b7e80a2820` — binds `3d-generate` lifecycle to that lease using the existing transport/job ID and exposes the active owner in progress snapshots;
- `2cd8a6c85ac9cfe838c7ac14144b75dd9510a959` — adds focused lifecycle/parallel-owner regressions.

Behavior now established:

1. a 3D generation job acquires the one heavyweight-runtime owner before heavyweight provider execution;
2. the owner identity is the existing generation job ID rather than a parallel identity system;
3. progress snapshots expose current resource ownership;
4. successful completion and failure release ownership deterministically;
5. a second nonblocking owner cannot steal an active lease;
6. existing provider-qualification semantics remain unchanged.

This is deliberately **not** the completed Job Broker. The first seam is process-local and serializes current 3D heavyweight work. It does not yet provide durable queued state, worker/process isolation, crash recovery or true cancellation acknowledgement.

## Validation

Exact implementation/test head `2cd8a6c85ac9cfe838c7ac14144b75dd9510a959`:

- `core-foundation` run `34553049740`: **PASS**;
- broader `build` run `34553049801`: **still running at handoff write** — inspect its final conclusion before calling this a fully validated release-worthy checkpoint.

Do not treat later documentation-only commits as replacing the code/test checkpoint. If the broader run fails, inspect the actual failing step and fix forward on v1.0.24.

## Exact next task

First consume any new reference-machine Stage-C evidence. If none exists and exact-head implementation validation is green, continue **one bounded MS-020 slice**:

**Make component install/update/remove/repair share the same authoritative heavyweight resource owner as inference.**

Requirements:

- do not allow model/runtime mutation to race active heavyweight inference;
- do not steal or silently clear an active inference owner;
- use explicit operation identity/kind rather than a second unrelated lock system;
- account for `model_manager.py` delegating install/update to `model_manager_v105`;
- preserve transactional/resumable model-install behavior and storage containment;
- expose a useful busy/ownership state to callers;
- add focused regressions for inference-vs-install/remove exclusion and release-on-error;
- stop after this ownership slice; durable queue persistence, isolated workers, cancellation acknowledgement and crash recovery remain later MS-020 work.

## Release/checkpoint rule

The MS-020 code/test head may be recorded as an implementation checkpoint after its full CI is green, but that does not freeze v1.0.24 and does not authorize publication. Release readiness/chunk size/publication remain Coordinator-owned.

## User verification dependency

Reference-machine testing remains required. Until a newer build is successfully published, v1.0.22 remains the latest stable build. Exercise Generate 3D → candidate → Apply → save/close/reopen → same object/revision; resize/presentation; no starter sphere/opaque floor; transform/sculpt; cleanup/export; storage containment; cancellation/recovery; and resource behavior during a long AI job.
