# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Current writable development branch:** `v1.0.24`.
- **Frozen release source:** `v1.0.23`; Dev must not mutate it or `release-control`.
- **Latest validated v1.0.24 implementation/test checkpoint:** `e3dfba9aa26d7045b4bf9602920a484c443789c7`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## Release state

`v1.0.22` remains the latest published release. `v1.0.23` remains Coordinator-owned/frozen after failed publication retries. This Dev cycle made no release-control, tag, release, or v1.0.23 mutation.

## Completed bounded MS-020 seam

The current v1.0.24 Job Broker work remains intentionally bounded:

1. heavyweight `3d-generate` work has one process-local runtime owner;
2. component install/update/repair/remove shares that owner;
3. cancellation remains `cancelling` and retains ownership until physical terminal acknowledgement; late success is discarded;
4. a compact lifecycle journal under Miniscuplter-controlled storage records only the minimum heavyweight job tombstone/context;
5. stale running/cancelling state reconciles after backend restart to inactive `interrupted`/`cancelled`, without reacquiring the GPU lease or auto-applying output;
6. corrupt/unsupported journal state fails closed.

Do **not** broaden this into a generalized persistent queue or isolated-worker architecture without new Coordinator sequencing.

## This Dev Cycle — bounded restart-journal safety hardening

A concrete fail-closed gap was found inside the already-approved restart seam: the journal intended to reject state above 256 KiB, but the old loader called `read_text()` before enforcing that limit. A corrupt/hostile oversized managed journal could therefore consume unbounded memory during backend startup before recovery rejected it.

Implementation/test commits:

- `d561665fc09d9eaa3394718310f5dd9719095180` — introduced an explicit 256 KiB journal byte limit and pre-read size rejection;
- `d93bf18b51cb1dbfbc33fc0a3d4df1d9fa0279b3` — added an oversized-journal regression proving recovery fails closed and does not claim heavyweight ownership;
- `e3dfba9aa26d7045b4bf9602920a484c443789c7` — senior-review hardening: actual file input is now capped to `limit + 1` bytes before UTF-8 decode/JSON parsing, closing the grow-between-stat-and-read allocation window.

This reuses **MS-020**; it does not introduce a new queue, worker, or authority owner.

## Validation

Exact implementation/test checkpoint `e3dfba9aa26d7045b4bf9602920a484c443789c7` is green:

- `core-foundation` exact-head workflow: **PASS**;
- broader `build` run `34572788267`: **PASS**;
- semantic-version branch identity guard: **PASS**;
- C# editor/launcher/updater/Core builds: **PASS**;
- Python compilation/runtime dependency resolution: **PASS**;
- core logic + execution/job regressions, including oversized lifecycle journal recovery: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package layout + ZIP SHA-256 verification: **PASS**;
- installer-definition compilation: **PASS**;
- full Windows publication jobs correctly skipped for an ordinary development-branch push.

Documentation commits after this checkpoint do not supersede the validated implementation/test checkpoint.

## Exact next task

1. Re-bootstrap actual release/branch/CI state and re-read current ROADMAP / COORDINATOR_LOG.
2. Consume any new reference-machine Stage-C/cancellation/storage evidence immediately; serious reproduced defects preempt everything else.
3. Consume any newer Coordinator-sequenced task. The explicit restart-reconciliation seam is complete; do **not** independently broaden MS-020.
4. If no new Coordinator task/evidence exists, restrict autonomous work to concrete correctness/regression hardening of already-approved Stage-C seams, not speculative infrastructure.
5. Preserve the semantic-version identity guard and immutable release history.

## Release/checkpoint rule

`e3dfba9aa26d7045b4bf9602920a484c443789c7` is a useful validated implementation checkpoint. It does not freeze v1.0.24 and does not authorize publication. Release readiness/chunk size/publication remain Coordinator-owned.

## User verification dependency

Reference-machine testing remains required on latest published `v1.0.22`:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job. Development-only v1.0.24 broker/journal changes cannot be target-machine qualified until they reach a published build.
