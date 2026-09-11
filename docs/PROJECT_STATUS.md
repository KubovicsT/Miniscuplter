# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22`.
- **Stable release target:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Frozen v1.0.23 release boundary:** `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Current writable development branch:** `v1.0.24`, created from that exact release boundary.
- **Latest validated v1.0.24 MS-020 implementation/test checkpoint:** `6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5`.
- **Overall completion:** **57% acceptance-weighted**.

## v1.0.23 release state

Coordinator froze v1.0.23 and initiated publication at the exact boundary above. The autonomous release workflow reached the real Windows/Godot build successfully, but failed its output-version verification because the frozen candidate's release metadata/build script still produced **v1.0.22** package names (`Miniscuplter-Setup-1.0.22.exe`) while the release request expected `Miniscuplter-Setup-1.0.23.exe`.

The source branch remains frozen. Dev has not modified v1.0.23 or release-control; release diagnosis/source-fix direction remains Coordinator-owned. Forward development continues only on v1.0.24.

## v1.0.23 accumulated scope

The frozen version contains the complete planned bounded MS-027 acceptance-support/UI-modernization sequence:

1. workspace preferences, UI scaling and tooltips;
2. direct viewport Select/Move/Rotate/Scale/Sculpt controls;
3. synchronized collapsible scene hierarchy;
4. camera-linked view cube and selected-object orbit pivot;
5. unified AI command/history surface delegating to authoritative AI action owners;
6. MS-026 local resource telemetry for GPU/VRAM/RAM/temperature/job context;
7. density/spacing cleanup and retirement of superseded always-visible instructional copy;
8. MS-028 compile-regression repair.

Publication does not close any user-observed issue that still requires reference-machine verification.

## Current v1.0.24 progress — MS-020

With Stage-C acceptance still externally blocked, v1.0.24 is advancing the Coordinator-approved Job Broker durability/resource-ownership work in bounded slices.

### Slice 1 — heavyweight inference owner

- `ai_backend/resource_ownership.py` defines one explicit process-local heavyweight-runtime lease;
- `3d-generate` jobs acquire that lease using their existing transport/job ID before heavyweight work begins;
- progress snapshots expose the current resource owner;
- completion and failure release ownership deterministically;
- focused regressions verify lifecycle release and prevent a second owner from stealing an active lease.

### Slice 2 — component/runtime mutation shares that owner

Validated checkpoint `6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5` extends the same lease across install/update/repair/remove:

- component mutation now acquires the authoritative heavyweight lease with an explicit operation id and kind such as `component-install` or `component-remove`;
- component operations fail closed rather than racing an active inference owner or stealing/clearing its lease;
- cached-model release refuses to tear down models while a foreign heavyweight owner is active;
- once a component operation owns the lease, cached models may be released safely before transactional/resumable mutation begins;
- the v1.0.5 audited/resumable installer remains the underlying implementation, so deterministic staging and storage containment are preserved;
- component status exposes the current resource owner, and successful operations return their operation id/kind;
- failure paths release component ownership deterministically;
- focused regressions cover inference-versus-install exclusion, foreign-owner model-release refusal, release-on-error, and install/update/repair/remove owner kinds.

Validation for `6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5` is green:

- Stage-B/Core (`core-foundation` run `34557452791`): **PASS**;
- C# editor/launcher/updater/Core builds (`build` run `34557452815`): **PASS**;
- Python compile and runtime dependency resolution: **PASS**;
- core logic, execution and job regressions: **PASS**;
- real geometry regressions and release audit: **PASS**;
- portable package layout/hash and installer-definition compilation: **PASS**.

This intentionally does **not** claim the final durable Job Broker. Persistent queue state, isolated worker/process ownership, truthful cancellation acknowledgement after worker stop, and crash recovery remain future bounded MS-020 slices.

## Critical path

Stage-C reference-machine acceptance remains P0. The required end-to-end evidence is:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery.

New serious target-machine evidence preempts MS-020 immediately.

## Release ownership

Release publication is Coordinator-owned. v1.0.23 remains frozen while its release outcome is being reconciled. Dev continues only on v1.0.24 and records implementation checkpoints without initiating publication.

## User dependency

No product/design decision is required. Reference-machine verification remains the external dependency. Until the Coordinator successfully publishes a newer build, v1.0.22 remains the latest stable build for Stage-C testing.
