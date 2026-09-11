# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22`.
- **Stable release target:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Frozen v1.0.23 release boundary:** `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Current writable development branch:** `v1.0.24`, created from that exact release boundary.
- **Latest validated v1.0.24 implementation/test checkpoint:** `7255bb75f0ea18409fe5ad63734cbd99c17ba18d`.
- **Overall completion:** **57% acceptance-weighted**.

## v1.0.23 release state

Coordinator froze v1.0.23 and initiated publication at the exact boundary above. The first autonomous release attempt reached the real Windows/Godot build but failed output-version verification because the frozen candidate still produced v1.0.22 package metadata/names while the release request expected v1.0.23.

A Coordinator-owned corrected-candidate retry was later submitted from `release-control`; run `34562412676` also failed, this time during the combined geometry/release-audit step before Windows export/package publication. The v1.0.23 source branch remains frozen and Dev has not modified it or release-control. Latest published stable remains v1.0.22.

## Current v1.0.24 progress — MS-020

With Stage-C acceptance still externally blocked, v1.0.24 advanced the Coordinator-approved Job Broker/resource-ownership work in bounded slices.

### Slice 1 — heavyweight inference owner

- one explicit process-local heavyweight-runtime lease;
- `3d-generate` acquires it with the existing transport/job ID;
- progress exposes the owner;
- completion/failure release ownership deterministically;
- regressions prevent a parallel owner from stealing the lease.

### Slice 2 — component/runtime mutation shares ownership

Validated checkpoint `6f6c1ed9784af8aabb5835413cd5f1dbeb6598e5` extended the same lease across install/update/repair/remove, while preserving audited/resumable model installation and storage containment. Foreign model-release and component mutation fail closed while inference owns the runtime.

### Slice 3 — truthful cancellation lifecycle

Validated checkpoint `f5ea1378c4ff1ba177262a275469ef8d310d7cc2` established request-only cancellation, terminal acknowledgement only after owned execution stops, late-success rejection, no cancelled-job qualification, and heavyweight ownership retention until the truthful terminal boundary.

### Slice 4 — minimum durable restart reconciliation

Validated checkpoint `c8451493b31306d12db7ed360a836e3e3b941fb2` added the bounded durability seam requested by the Coordinator:

- only heavyweight `3d-generate` lifecycle state is journaled; no generalized persistent queue was introduced;
- journal storage is contained under the existing backend data root through `storage.resolve()`;
- writes use same-directory atomic replacement with flush/fsync before replace;
- the record is compact and bounded: identity/kind/state/stage/progress/provider/cancel flag/timestamps plus whitelisted Stage-C identity context; no event history, model weights, output blobs or arbitrary context;
- a prior-process `running` record becomes inactive terminal `interrupted` on startup, explicitly indicating no output was accepted;
- a prior-process cancelling record becomes inactive terminal `cancelled`;
- restart recovery never reacquires the process-local GPU/runtime lease and never restores/auto-applies an interrupted candidate;
- corrupt or unsupported persisted state fails closed as inactive `recovery-error` without claiming heavyweight ownership;
- regressions cover clean completion, active crash/restart, cancelling restart, corrupt journal, storage containment and temp-file cleanup.

This closes the currently explicit bounded restart-reconciliation baton but does **not** claim isolated provider workers, a generalized persistent queue, multi-job durable history or complete broker architecture.

## Release-version identity hardening

The restart slice exposed a real release-audit inconsistency: v1.0.24 launcher/installer metadata already identified 1.0.24 while updater/editor/backend/export/display/audit metadata still identified 1.0.22. Those surfaces were corrected forward on writable v1.0.24 only; frozen v1.0.23 was untouched.

Because the same class of drift already blocked v1.0.23 publication, checkpoint `7255bb75f0ea18409fe5ad63734cbd99c17ba18d` adds an early branch-derived CI guard. On every `v1.*` branch push, CI derives the expected version from the branch name and verifies launcher, updater, editor assembly, installer, Windows file/product metadata, backend API, displayed editor version and `release_audit.py` agree. A newly created semantic-version branch that inherits stale prior-version identity now fails immediately instead of remaining apparently green until release-control.

## Validation

Exact implementation/test checkpoint `7255bb75f0ea18409fe5ad63734cbd99c17ba18d` is green:

- `core-foundation` run `34568222471`: **PASS**;
- broader `build` run `34568222539`: C#/Core **PASS**, branch-derived semantic-version identity guard **PASS**, Python/runtime **PASS**, execution/job regressions **PASS**, geometry **PASS**, release audit **PASS**, portable package/layout/hash **PASS**, installer-definition compilation **PASS**;
- tag-only full Windows release/publication jobs were correctly skipped for the development-branch push.

This is a useful implementation/release-worthy checkpoint, not a release freeze. Dev did not initiate publication.

## Critical path

Stage-C reference-machine acceptance remains P0. Required end-to-end evidence:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence preempts further architectural work immediately.

## Release ownership

Release publication is Coordinator-owned. v1.0.23 remains frozen while its failed retry/source correction is reconciled. Dev continues only on v1.0.24 and records implementation checkpoints without initiating publication.

## User dependency

No product/design decision is required. Reference-machine verification remains the external dependency. v1.0.22 is still the latest stable build for Stage-C testing. The new v1.0.24 restart journal and release-identity guard cannot be target-machine qualified until development reaches a published build.
