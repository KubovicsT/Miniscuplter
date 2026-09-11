# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22`.
- **Stable release target:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Frozen v1.0.23 release boundary:** `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Current writable development branch:** `v1.0.24`, created from that exact release boundary.
- **Latest validated v1.0.24 MS-020 implementation/test checkpoint:** `f5ea1378c4ff1ba177262a275469ef8d310d7cc2`.
- **Overall completion:** **57% acceptance-weighted**.

## v1.0.23 release state

Coordinator froze v1.0.23 and initiated publication at the exact boundary above. The autonomous release workflow reached the real Windows/Godot build successfully, but failed output-version verification because the frozen candidate still produced **v1.0.22** package metadata/names while the release request expected v1.0.23.

The source branch remains frozen. Dev has not modified v1.0.23 or release-control; release diagnosis/source-fix direction remains Coordinator-owned. Forward development continues only on v1.0.24.

## v1.0.23 accumulated scope

The frozen version contains the complete planned bounded MS-027 acceptance-support/UI-modernization sequence: workspace preferences/scaling/tooltips; direct viewport tools; synchronized scene hierarchy; view cube and selected-object orbit; unified AI command/history surface; MS-026 local resource telemetry; density cleanup; and MS-028 compile-regression repair.

Publication does not close any user-observed issue that still requires reference-machine verification.

## Current v1.0.24 progress — MS-020

With Stage-C acceptance still externally blocked, v1.0.24 is advancing the Coordinator-approved Job Broker durability/resource-ownership work in bounded slices.

### Slice 1 — heavyweight inference owner

- one explicit process-local heavyweight-runtime lease;
- `3d-generate` acquires it with the existing transport/job ID;
- progress exposes the owner;
- completion/failure release ownership deterministically;
- regressions prevent a parallel owner from stealing the lease.

### Slice 2 — component/runtime mutation shares ownership

Validated checkpoint `6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5` extended the same lease across install/update/repair/remove, while preserving the audited/resumable model installer and storage containment. Foreign model-release and component mutation now fail closed while inference owns the runtime.

### Slice 3 — truthful cancellation lifecycle at the migrated backend seam

Validated checkpoint `f5ea1378c4ff1ba177262a275469ef8d310d7cc2` establishes:

- `request_cancel()` is request-only: an active job remains `cancelling` and retains heavyweight ownership;
- explicit terminal `acknowledge_cancel()` exists for the point where owned execution is known to have stopped;
- provider failure after a cancel request terminates as `cancelled`, not generic `failed`;
- a late provider success after cancellation is discarded rather than becoming a successful job;
- cancelled late completion cannot record provider qualification;
- heavyweight ownership is released only when terminal cancellation is acknowledged/completed after execution stops;
- terminal cancellation acknowledgement is idempotent, so the FastAPI error wrapper cannot create a second false terminal transition;
- focused regressions cover cancel-request → still owned, terminal acknowledgement → released, cancelled failure, and late completion rejection.

The current editor hard-cancel boundary still restarts the owned backend process because synchronous provider adapters are not individually killable. Process termination therefore remains the external hard-stop boundary for those requests. Persistent job state, isolated provider worker processes and crash recovery are **not** claimed complete.

## Validation

Exact implementation/test checkpoint `f5ea1378c4ff1ba177262a275469ef8d310d7cc2` is green:

- `core-foundation` run `34560559516`: **PASS**;
- broader `build` run `34560559542`: **PASS**;
- C# editor/launcher/updater/Core builds: **PASS**;
- Python compile and runtime dependency resolution: **PASS**;
- core logic, execution and job regressions, including the new cancellation lifecycle cases: **PASS**;
- real geometry regressions and release audit: **PASS**;
- portable package layout/hash and installer-definition compilation: **PASS**.

This is a useful implementation checkpoint, not a release freeze.

## Critical path

Stage-C reference-machine acceptance remains P0. Required end-to-end evidence:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence preempts MS-020 immediately.

## Release ownership

Release publication is Coordinator-owned. v1.0.23 remains frozen while its release outcome is reconciled. Dev continues only on v1.0.24 and records implementation checkpoints without initiating publication.

## User dependency

No product/design decision is required. Reference-machine verification remains the external dependency. Until the Coordinator successfully publishes a newer build, v1.0.22 remains the latest stable build for Stage-C testing.
