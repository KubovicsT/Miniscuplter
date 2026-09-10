# Miniscuplter Handoff

> Operational baton for the next development run. Inspect actual Git/release/CI state first; repository state wins over this document if they differ.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest application/code commit:** `cfd0352223cada181ea75843f16b25b9b5ceb541`
- **Overall completion estimate:** 56% acceptance-weighted.

The release/version reconciliation on this run was clean: v1.0.19 remains the immutable published release; v1.0.20 is a distinct forward-only development branch. Documentation commits after the application commit advance branch HEAD; resolve exact HEAD and current CI from Git before editing.

## What this run accomplished

### Production Stage-C baseline → 3D candidate integration

The Core-only Stage-C generation contract from the prior run is now connected to the actual legacy-compatible editor path rather than remaining test infrastructure.

New/changed code:

- `Core/StageCAssetStore.cs`
  - Copies the accepted source image into project-owned `images/` storage before it can become authoritative.
  - Uses a temporary file, durable flush, atomic rename and SHA-256.
  - Returns an immutable `ImageRevision`; deleting/moving the original external source no longer destroys the accepted project baseline.
- `Scripts/Main.V1020StageCBridge.cs`
  - Rewires the existing `Accept Current Image as Baseline` button away from the path-only legacy callback after UI composition.
  - Accept now writes a real durable `ImageRevision`, calls `StageCGeneration.AcceptBaseline()` on a real `ProjectSession`, saves through `ProjectStore`, then enables 3D generation.
  - Rewires the v1.0.9 production Generate-3D button away from the old behavior that immediately inserted returned STL as authoritative scene state.
  - Calls `StageCGeneration.BeginImageToMesh()` before inference and keeps the resulting stable binding for result registration.
  - Uses the existing `AIClient` request gate/cancellation/backend-reset path; do not bypass this in the next step.
  - Verifies returned STL, converts it to Core `MeshData`, creates a durable `MeshRevision` with the binding's reserved `ObjectId`, and calls `StageCGeneration.RegisterResult()`.
  - Ready output stays a candidate until explicit **Apply 3D Candidate**; Conflict output is preserved and cannot be applied; Discard is explicit.
  - Apply uses the Core transactional command, saves, then materializes the object into the legacy Godot viewport and maps its stable Core object ID.
  - Accepted baseline and pending Ready/Conflict candidate restore from the compatibility `.msculpt2` project after restart.
- `Scripts/Main.V1020StageCRestore.cs`
  - Self-review found that a previously Applied candidate would survive in Core but would not reappear in the legacy viewport after restart. The new restore pass re-materializes persisted Applied Stage-C objects and reattaches their stable ObjectIds.
- `Scripts/ExtrasInstaller.cs`
  - Installs the Stage-C bridge after the v1.0.19 viewport pipeline and then restores applied Stage-C objects.
- `Core.Tests/StageCGenerationTests.cs`
  - Uses the real `StageCAssetStore` rather than synthetic ImageRevision descriptors.
  - Verifies durable image survival after source deletion, plus existing stale-result conflict, explicit apply, undo/redo and save/reload semantics.

The bridge stores its compatibility project at `AppDataRoot.Resolve("projects/stagec_working.msculpt2")`. This is intentional while the new vertical slice is proven: do not overwrite legacy `.msculpt` projects or remove migration compatibility yet.

Relevant commits from this run:

- `8bf703024753448e10dc77404f3ab7c946b3ce90` — durable Stage-C image revision storage
- `386956ed8ee2ee80a0e52082a7ad886ed8c048a8` — initial production Stage-C bridge
- `6f438cf7d0b400e67752acc2dfcec797bb6df945` — wire bridge into editor startup
- `3aec484d7e125e2e393b239562fb5c3c9f5a2da7` — fix bridge wiring/binding access found in self-review
- `04fd660296e6d9657328de6b41b03c58fde334aa` — test real durable image revisions
- `dba4e4254717312e16b4aaf035d715b72c9b76cb` / `cfd0352223cada181ea75843f16b25b9b5ceb541` — restore applied Stage-C objects visibly after restart

## Validation state

Build run `34490666005` for `04fd660296e6d9657328de6b41b03c58fde334aa` completed **SUCCESS** and passed:

- editor / launcher / updater / Core C# restore and builds;
- Stage-B/Core regression suite, including the Stage-C durable baseline/candidate tests;
- Python compile and dependency resolution;
- Python core logic tests;
- job-progress/provider qualification tests;
- real geometry regression tests;
- release audit;
- portable package layout and ZIP SHA verification;
- Inno Setup installer-definition compilation.

Fresh build run `34490945593` for the later applied-object restore commit `cfd0352223cada181ea75843f16b25b9b5ceb541` had already passed the complete C# job and complete Python/core/job/geometry/release-audit job when this handoff was written; packaging had passed portable build/layout/hash and was finishing installer-definition compilation. Reconcile its final conclusion before more code or any release decision.

No real target-GPU inference was performed by CI and no v1.0.20 release was made.

## Self-review / known remaining gaps

### MS-020 transport identity is still incomplete

The production editor now creates and retains `GenerationJobBinding`, but `AIClient.Generate3DRoutedAsync()` still sends the historical request body based on image/output paths, prompt, role and provider. Therefore `GenerationJobId`, `ProjectId`, input project revision number, exact accepted `ImageRevisionId`, and reserved `OutputObjectId` are not yet present in the backend's authoritative job record.

Do **not** work around this by creating a second ad-hoc HttpClient in the bridge; that would bypass the existing AIClient job semaphore, cancellation ownership and backend-restart recovery. Extend the existing AIClient/backend contract instead.

### Save-failure rollback should be hardened

The compatibility session is saved after accepted-baseline/candidate/apply transactions. If a ProjectStore save fails after the in-memory session changed, reload the last durable state (or otherwise roll back the session) so memory cannot remain ahead of disk. Orphaned immutable asset files are acceptable and can later be garbage-collected; lying about durable project state is not.

### Real acceptance remains pending

MS-009 viewport and MS-013 storage containment are still FIXED - NEEDS USER VERIFICATION on released v1.0.19. MS-022 still needs one real GTX1080 lightweight/default 3D inference qualification. Do not translate CI into those acceptance claims.

## Exact next task

Continue the same Stage-C vertical slice; do not broaden into unrelated cleanup.

1. Reconcile exact `v1.0.20` HEAD and the final result of build `34490945593` plus the latest `core-foundation` run.
2. Extend the existing `AIClient.Generate3DRoutedAsync()` and backend `Generate3DRequest`/job-progress context so the Stage-C request carries at least:
   - `GenerationJobId`;
   - `ProjectId`;
   - input project revision number;
   - accepted `ImageRevisionId`;
   - reserved output `ObjectId`.
   Preserve existing AIClient serialization/job semaphore/cancellation/backend restart behavior. Have backend job status retain this context and return/echo enough data for the editor to reject a mismatched response before `RegisterResult()`.
3. Add Python/job-progress and C# regression tests for context preservation/mismatch rejection without requiring real GPU inference.
4. Harden `V1020SaveSessionAsync()` so a failed save cannot leave the in-memory compatibility session ahead of the durable ProjectStore state.
5. Then continue the same persisted Stage-C object through basic cleanup and exact export-scope/validated STL integration.
6. Separately, when real hardware is available, run the released v1.0.19 viewport/storage checks and one lightweight/default 3D provider qualification on the GTX1080/16GB reference machine.

## Release policy

Do not modify v1.0.19. Do not publish v1.0.20 yet: production Stage-C identity is not fully propagated through backend job context, the full cleanup/export slice is incomplete, and target-machine acceptance has not occurred. Before publication require all normal C#/Python/Core/job/geometry/audit gates, a real Godot Windows export, artifact/hash verification and installer smoke test. After publication verify GitHub latest-release state and create/use v1.0.21 before further application development.

## User input

No product/design decision currently blocks autonomous work. Real-machine test evidence is useful when available but independent engineering can continue without it.