# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Current writable development branch:** `v1.0.24`.
- **Frozen release source:** `v1.0.23` at exact boundary `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Latest validated v1.0.24 implementation/test checkpoint:** `f5ea1378c4ff1ba177262a275469ef8d310d7cc2`.
- **Overall completion:** **57% acceptance-weighted**.
- **Coordinator critical path:** Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts MS-020.

## Frozen v1.0.23 release source

Do not modify v1.0.23 or release-control from Dev. The publication attempt reached the full Windows/Godot build but failed versioned-output verification because the frozen source still produced v1.0.22 release metadata/package naming. Coordinator owns release diagnosis and any directed frozen-source correction. Propagate any eventual required fix forward to v1.0.24.

## This Dev Cycle — third bounded MS-020 slice

With no new target-machine evidence available, this run completed the cancellation-truthfulness baton for the migrated heavyweight 3D lifecycle.

### Commits

- `eefb73ad78ad1dd7dc2542f00d34536a9e5438f6` — adds explicit terminal cancellation acknowledgement and rejects late successful completion after cancellation;
- `e5511afdcedc8b8541fe92c5ab90fc972fedaed3` — adds focused cancellation ownership/lifecycle regressions;
- `f5ea1378c4ff1ba177262a275469ef8d310d7cc2` — makes terminal cancellation acknowledgement idempotent after self-review.

### Behavior now established

1. `request_cancel(job_id)` is request-only. An active job becomes `cancelling` but remains active and retains its heavyweight lease.
2. `acknowledge_cancel()` is the explicit terminal state transition for a cancellation-requested job once owned execution is known to have stopped.
3. Terminal acknowledgement sets `active=false`, `state/stage=cancelled`, then releases heavyweight ownership.
4. A provider that returns success after cancellation was requested cannot be recorded as a successful job; `complete()` converts that outcome to terminal cancellation and raises `JobCancellationAcknowledged`.
5. Late cancelled completion cannot persist provider qualification.
6. A provider error after cancellation is also terminal `cancelled`, not generic `failed`.
7. Terminal cancellation is idempotent, so the FastAPI wrapper's follow-up `fail()` cannot append a second terminal transition or release ownership twice.
8. Progress updates ignore already-terminal jobs.

### Important boundary that remains

Current provider adapters are still synchronous inside the owned backend process. The editor's existing hard-cancel path cancels its HTTP request and restarts the owned backend process; that process termination is still the reliable physical stop boundary for such providers. The newly explicit backend acknowledgement path is truthful for a still-alive backend after provider execution returns/stops, but this slice does **not** claim persistent progress across process restart, isolated provider worker processes, a durable queue, or crash recovery.

The process-local resource lease naturally dies with a terminated backend; durability across restart remains future MS-020 work.

## Validation

Exact implementation/test checkpoint `f5ea1378c4ff1ba177262a275469ef8d310d7cc2` is fully green:

- `core-foundation` run `34560559516`: **PASS**;
- broader `build` run `34560559542`: **PASS**;
- C# editor/launcher/updater/Core builds: **PASS**;
- Python compile and runtime dependency resolution: **PASS**;
- core logic and execution/job regressions: **PASS**;
- new cancellation tests cover request → still owned, explicit acknowledgement → released, cancelled provider failure, and attempted late success → cancelled/no qualification;
- real geometry regressions and release audit: **PASS**;
- portable package layout/hash and installer-definition compilation: **PASS**.

Documentation commits after this checkpoint do not supersede the validated code/test checkpoint.

## Exact next task

First consume any new reference-machine Stage-C evidence. If none exists, continue **one bounded MS-020 durability slice** addressing the restart gap exposed above, without jumping to a complete broker rewrite.

**Persist only the minimum job lifecycle/tombstone state needed to reconcile an owned-backend restart or crash truthfully.**

Requirements:

- inspect existing Miniscuplter-controlled storage helpers and use an existing contained data/state root; do not create arbitrary paths;
- persist a compact job record for the currently migrated heavyweight 3D lifecycle containing identity, kind, state, cancellation flag, timestamps and Stage-C context needed for safe reconciliation;
- use atomic/transactional file replacement; never leave a partially written journal as authoritative state;
- on backend startup/recovery, convert a previously active/cancelling record from the dead prior process into an explicit interrupted/cancelled terminal outcome rather than pretending it is still running or completed;
- do not restore or auto-apply generated candidates from an interrupted job;
- do not persist provider weights, large outputs or unbounded event history in the journal;
- preserve one-heavyweight-owner behavior and current editor hard-cancel recovery;
- add focused regressions for clean completion persistence, crash/restart reconciliation of active/cancelling state, corrupt-journal fail-closed behavior, and storage containment;
- stop after this bounded restart-reconciliation seam. Isolated provider worker processes and a generalized persistent queue remain later MS-020 work unless implementation evidence proves they are prerequisites.

If Coordinator changes this baton before the next Dev run, Coordinator direction wins.

## Release/checkpoint rule

`f5ea1378c4ff1ba177262a275469ef8d310d7cc2` is a useful validated implementation checkpoint. It does not freeze v1.0.24 and does not authorize publication. Release readiness/chunk size/publication remain Coordinator-owned.

## User verification dependency

Reference-machine testing remains required. Until a newer build is successfully published, v1.0.22 remains the latest stable build. Exercise Generate 3D → candidate → Apply → save/close/reopen → same object/revision; resize/presentation; no starter sphere/opaque floor; transform/sculpt; cleanup/export; storage containment; cancellation/recovery; and resource behavior during a long AI job.
