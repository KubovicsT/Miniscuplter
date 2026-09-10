# Miniscuplter Handoff

> Operational baton for the next development run. Inspect actual Git/release/CI state first; repository state wins over this document if they differ.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest application/code commit:** `fe9079e718ad055c0ef5b43c7205e505ce54e217`
- **Overall completion estimate:** 56% acceptance-weighted.

The mandatory release/version reconciliation was clean this run: v1.0.19 remains the immutable published release and v1.0.20 is a distinct forward-only development branch. Documentation commits advance branch HEAD after the application commit; always resolve exact HEAD and latest release/CI before editing.

## What this run accomplished

### MS-020 — save-failure consistency for the migrated Stage-C seam

The previous run had completed end-to-end generation identity, but `V1020SaveSessionAsync()` could still leave `ProjectSession.Current` ahead of durable storage when `ProjectStore.SaveAsync()` failed after an in-memory transaction.

This run fixed that at the Core session boundary rather than adding a UI-only workaround:

- `Core/ProjectHistory.cs`
  - added `ProjectSession.SaveRecoveringAsync(saveAsync, loadLastDurableAsync)`;
  - a successful save marks the current revision saved;
  - a failed save reloads the last durable/recoverable state and uses `ReplaceFromLoad()` before surfacing failure;
  - rollback clears invalid undo/redo history and aligns `SavedRevisionNumber` to the recovered durable revision;
  - recovery from a different `ProjectId` is rejected fail-closed;
  - if both save and recovery fail, an `AggregateException` retains both failures and unresolved in-memory state is not falsely marked durable.
- `Scripts/Main.V1020StageCBridge.cs`
  - `V1020SaveSessionAsync()` now saves through `SaveRecoveringAsync()` and reloads via `ProjectStore.LoadWithRecoveryAsync()` on failure;
  - existing operation ordering already defers baseline UI publication, candidate UI publication, Apply scene insertion, and Discard UI clearing until after save succeeds, so failed saves cannot present attempted state as committed;
  - immutable asset payloads written before a manifest failure may remain orphaned, which is acceptable because they are not referenced by authoritative durable project state.
- `Core.Tests/StageCGenerationTests.cs`
  - deterministic save-failure regression simulates persistence failure without disk-permission/disk-full dependencies;
  - verifies rollback to durable revision, removal of unsaved metadata, clean saved/dirty state, invalid-history clearing, and error propagation;
  - verifies foreign-project recovery is rejected and does not replace or falsely save the unresolved session.

Relevant application commits:

- `80b1c68fac12bfa6d9bb6c53f99c74fe3ddb4ba3` — Add save failure rollback for project sessions
- `4afdbd75ce6ae72a76fd9569eae40235918d7dae` — Recover Stage C session after save failure
- `fe9079e718ad055c0ef5b43c7205e505ce54e217` — Test Stage C save failure recovery

No new durable product decision was required; this implements the existing transactional full-state/persistence rules rather than changing product scope or compatibility.

## Validation state

Application commit `fe9079e718ad055c0ef5b43c7205e505ce54e217` is fully green:

- `core-foundation` run `34497651489`: **SUCCESS**;
- broader Windows build run `34497651608`: **SUCCESS**;
- editor/launcher/updater/Core C# restore/build: **PASS**;
- deterministic Stage-B/Core/Stage-C tests including save recovery: **PASS**;
- Python compilation and dependency resolution: **PASS**;
- core logic tests: **PASS**;
- job-progress tests: **PASS**;
- real geometry regression tests: **PASS**;
- release audit: **PASS**;
- portable package layout and SHA verification: **PASS**;
- installer-definition compilation: **PASS**.

A real Godot Windows export and installer smoke-install were intentionally not run because v1.0.20 is not yet release-ready. No real CUDA inference or target-machine GUI acceptance occurred. No v1.0.20 release was made.

## Current unresolved priorities

1. **MS-009 — viewport/grid/model/gizmo:** v1.0.19 fix is published but still needs target-PC verification; any reported blank viewport immediately outranks planned work.
2. **MS-018 — Stage-C thin slice:** accepted baseline → identity-bound qualified-generation path → durable candidate → explicit apply → restart visibility is materially integrated and now persistence-failure safe. Cleanup/export and target-machine qualification remain.
3. **MS-013 — storage containment:** v1.0.19 hardening still needs representative target-machine verification.
4. **MS-020 — final Job Broker:** stale/replay transport identity and Stage-C save-failure consistency are hardened; durable queue/resource ownership and broader crash recovery remain incomplete.
5. **MS-022 — provider qualification:** readiness and real-success recording exist; GTX 1080 lightweight/default-provider inference evidence is still missing.
6. **MS-019 — legacy `Main.V*.cs`:** continue migrating only this vertical slice before broadening.

## Exact next task

Continue the same Stage-C object into **Cleanup & Export** without reverting to STL/widget authority:

1. Reconcile exact branch/release/CI state first. Stable should still be v1.0.19 unless a newer release was actually published.
2. Locate the minimum existing cleanup operation used by the normal workflow (repair/remesh is preferred if it is the canonical path). Bind it to the currently applied Stage-C `ObjectId` and its exact active `MeshRevisionId` rather than only the selected Godot node or temporary STL path.
3. Materialize the active immutable `MeshRevision` to the existing geometry backend only as job input. After successful cleanup, import the result into `MeshData`, create a **new immutable `MeshRevision`** with parent/provenance linking it to the pre-cleanup revision, and transactionally advance the same project object's active revision. Never overwrite the generated mesh revision.
4. Save through the new recovery-safe session boundary before changing visible authoritative UI/scene state. If save fails, recovered durable state remains authoritative.
5. Wire validated STL export to an explicit Stage-C object/revision scope. Export may materialize STL from the selected active revision, but STL remains output/interchange rather than project storage.
6. Add Core/regression coverage proving cleanup creates a new revision, preserves the generated parent revision, supports save/reload, and does not export an unintended object/revision.
7. Add the transport-response mismatch regression at the lowest-cost layer if practical without introducing a parallel client path or GPU dependency.
8. Preserve existing AIClient/backend cancellation/resource ownership. Do not create another HTTP/job mechanism.
9. When target hardware is available, collect v1.0.19 viewport/storage verification and one lightweight/default 3D inference qualification on GTX 1080/16 GB.

Do not broaden into an all-at-once Cleanup UI rewrite. The goal is one reliable `2D → accepted baseline → qualified 3D → visible/editable persisted object → cleanup revision → validated STL` vertical slice.

## Release policy

Do not modify v1.0.19. Do not publish v1.0.20 yet: the intended Stage-C increment still lacks cleanup/export integration and target-machine acceptance. Release only when the v1.0.20 scope is coherent, no release-blocking regression remains, all normal automated gates pass, a real Godot Windows export succeeds, artifacts/hashes verify, installer smoke-install succeeds, canonical docs describe the release, and the increment is meaningfully testable. After publication verify GitHub latest-release state and create/use v1.0.21 before further application development.

## User input

No product/design decision currently blocks autonomous engineering. Real-machine verification is useful when available but independent engineering can continue without it.
