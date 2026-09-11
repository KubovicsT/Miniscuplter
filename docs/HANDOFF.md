# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Current writable development branch:** `v1.0.24`.
- **Frozen release source:** `v1.0.23` at exact boundary `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Latest validated v1.0.24 implementation/test checkpoint:** `c8451493b31306d12db7ed360a836e3e3b941fb2`.
- **Overall completion:** **57% acceptance-weighted**.
- **Coordinator critical path:** Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts MS-020.

## Frozen v1.0.23 release source

Do not modify v1.0.23 or release-control from Dev. The latest published release remains v1.0.22. The prior v1.0.23 publication attempt failed output-version verification because the frozen source still produced v1.0.22 release metadata/package naming. Coordinator owns diagnosis and any directed frozen-source correction. This Dev cycle made no release-control, tag, release, or v1.0.23 mutation.

## This Dev Cycle — bounded MS-020 restart reconciliation

With no new target-machine evidence available, this run completed the exact Coordinator baton: persist only enough heavyweight 3D job lifecycle state to reconcile an owned-backend restart/crash truthfully.

### Implementation commits

- `e163488d13576d781df152a52715caf7668d1188` — adds the contained, compact, atomic heavyweight job lifecycle journal;
- `1d85da472a2793668a5f30eae62fd45ce8259c3d` — persists/reconciles the migrated 3D job lifecycle and restores only a terminal tombstone after restart;
- `d5c7dd6ccee6938b129c3361054ed340f6384c2f` — adds completion/restart/cancelling/corrupt-journal/containment regressions;
- `57624d3acfec99306ce29d0b208e2de97aaf00b1` — wires the new journal regressions into the existing execution/job CI leg.

### Restart/recovery semantics now established

1. Only the migrated heavyweight `3d-generate` lifecycle is journaled; this is not a generalized durable queue.
2. The journal lives under the existing Miniscuplter-controlled backend data root at `state/job-lifecycle.json` via `storage.resolve()`.
3. Writes are same-directory atomic replacements with flush/fsync before replace; temporary files are not authoritative.
4. Persisted data is intentionally compact: job identity/kind/state/stage/progress/provider/cancel flag/timestamps plus a whitelist of Stage-C identity fields. Event history, model weights, output blobs and arbitrary large context are not persisted.
5. On startup, a stale `running` record from the dead prior backend becomes inactive terminal `interrupted`, explicitly stating that no output was accepted.
6. A stale `cancelling`/cancel-requested record becomes inactive terminal `cancelled` after restart.
7. Startup recovery never reacquires the process-local heavyweight lease and never restores or auto-applies a prior generated candidate.
8. Corrupt/unsupported persisted state fails closed as inactive `recovery-error`; it does not claim the GPU/runtime owner. A later explicit new job may atomically replace that untrusted tombstone.
9. Existing truthful cancellation behavior remains intact: cancellation retains ownership until terminal acknowledgement while the process is alive; restart reconciles the dead-process case separately.

## Version-identity regression found and fixed forward on v1.0.24

The first CI pass of the new restart slice exposed a release-audit failure unrelated to the journal: v1.0.24 launcher/installer metadata already said 1.0.24 while updater/editor/backend/export/display/audit metadata still said 1.0.22. That would make a future v1.0.24 package internally inconsistent.

Forward-only fixes on the writable v1.0.24 branch:

- `260f6666bbc046d0bddb4bd6a378ef97b27f614f` — updater version 1.0.24;
- `55c2435dd307eade9d020ceaa424849b66932fc0` — editor assembly version 1.0.24;
- `c0eddca2c21fabc69bf45d3f6b08e7397b0dbdef` — backend API version 1.0.24;
- `e0cecfc54cc290a09b2a0e120dccc078272b91fb` — Windows export file/product version 1.0.24.0;
- `2021629f106fdcb49dc29ef767e4e635a392f985` — displayed editor version 1.0.24;
- `c8451493b31306d12db7ed360a836e3e3b941fb2` — release audit now validates 1.0.24 identity.

The frozen v1.0.23 source was not changed.

## Validation

Exact implementation/test checkpoint `c8451493b31306d12db7ed360a836e3e3b941fb2` is fully green:

- `core-foundation` run `34564634258`: **PASS**;
- broader `build` run `34564634248`: **PASS**;
- C# editor/launcher/updater/Core builds: **PASS**;
- Python compilation/runtime dependency resolution: **PASS**;
- core logic and execution/job regressions: **PASS**;
- new journal regressions cover clean completion persistence, active-job crash/restart → interrupted, cancelling restart → cancelled, corrupt-journal fail-closed behavior, controlled-root containment and no leftover temp authority;
- real geometry regressions: **PASS**;
- release audit after 1.0.24 identity reconciliation: **PASS**;
- portable package layout and ZIP SHA-256 verification: **PASS**;
- installer-definition compilation: **PASS**;
- branch push correctly skipped tag-only full Windows release/publication jobs.

Documentation commits after this checkpoint do not supersede the validated code/test checkpoint.

## Exact next task

First consume any new reference-machine Stage-C/cancellation/storage evidence. If new evidence exists, reproduce and prioritize any serious correctness/persistence/data-safety/viewport/provider regression immediately.

If no new evidence exists, **do not broaden MS-020 into a generalized persistent queue or isolated-worker rewrite on your own.** This run has completed the currently explicit bounded restart-reconciliation seam. Re-read TECHNICAL_ROADMAP / COORDINATOR_LOG / current HANDOFF for the next Coordinator-sequenced slice. If Coordinator still directs MS-020, take only the next narrowly bounded lifecycle/ownership seam it specifies.

## Release/checkpoint rule

`c8451493b31306d12db7ed360a836e3e3b941fb2` is a useful validated implementation checkpoint. It does not freeze v1.0.24 and does not authorize publication. Release readiness/chunk size/publication remain Coordinator-owned.

## User verification dependency

Reference-machine testing remains required. `v1.0.22` is still the latest successfully published build, so exercise:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job. The new durable restart journal is development-only in v1.0.24 and cannot be user-qualified until that work reaches a published build.
