# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest release-candidate application commit:** `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`
- **Latest pure Stage-C editing implementation commit:** `e71d55ff90d8eec3b03a8f3073e9ffefdc0d1228`
- **Overall completion:** 58% acceptance-weighted
- **Coordinator roadmap:** `docs/TECHNICAL_ROADMAP.md`
- **Coordinator history:** `docs/COORDINATOR_LOG.md`

Version/release reconciliation remained clean throughout this run: v1.0.19 is immutable/published and v1.0.20 is the distinct forward development branch.

## What this run completed

The Coordinator-defined bounded v1.0.20 Stage-C code target is now implemented.

### Authoritative mapped-object transforms

`Core/StageCEditing.cs` adds the narrow editing contract for the current vertical slice. `StageCEditing.SetTransform()` updates the exact durable `ProjectObject.Transform` through `ProjectSession.Execute()` while preserving `ObjectId` and active `MeshRevisionId`.

`Scripts/Main.V1020StageCEditing.cs` keeps the existing UX but makes committed state durable:

- toolbar move/rotate/scale/ground actions commit their resulting Godot transform to the mapped Stage-C object;
- native viewport gizmo transforms commit at gesture completion;
- transform-only commits do not reload mesh data unnecessarily;
- failed saves/commits project the last durable transform back into the scene;
- Stage-C-aware Undo/Redo replays the Core transaction for the exact selected object and saves it.

`Main.V1020StageCRestore.cs` now restores the durable transform when applied Stage-C objects are recreated after restart.

### One bounded authoritative sculpt/edit path

The native sculpt-stroke path is the one bounded mesh edit migrated for v1.0.20:

- stroke start captures the exact mapped `ObjectId` and active input `MeshRevisionId`;
- committed mesh becomes project-native `MeshData`;
- ProjectStore writes a new immutable child `MeshRevision`;
- `StageCEditing.CommitMeshRevision()` requires the exact active parent and rejects stale output;
- one ProjectSession transaction registers the revision and advances that same object;
- save failure restores durable state;
- Undo/Redo switches complete project state rather than replaying an isolated widget mesh.

Self-review found and fixed two integration hazards:

1. v1.0.18 installs its authoritative viewport handler lazily; the Stage-C observer now waits until that viewport is live before attaching.
2. legacy sculpt also pushed the same stroke into its mesh-only undo stack; the bridge now removes only the exact duplicated legacy entry after the Stage-C commit/revert, preventing a later fallback Undo from replaying the same edit outside Core.

Core regressions cover transform persistence, save/reload, Undo/Redo, exact export interaction, immutable sculpt lineage, stale sculpt rejection, sculpt Undo/Redo, and final exact active revision export scope.

Key commits:

- `e67a2d3a524a3cb429b29919f7805c7bb7b51483` — Add Stage C editing state contract
- `01db848b481ceee8bdc051aea6d528108d23eb39` — Bind Stage C editing to project state
- `68725d0a1c3f7faf7f5affd165953b277d6525b3` — Attach Stage C editing after viewport tool install
- `b5566027a3df2c9851a8cca15345b1efc4137d77` — Install Stage C editing authority bridge
- `df8730bcd29875993e3d346c227bc1031ff4d4ae` — Restore Stage C object transforms from project state
- `eb2aefc259910152965ab189c79c6fb11985fa95` — Test Stage C transform and sculpt state authority
- `0ecdbc95559794505156ae72b0bb8dc598659026` — Run Stage C editing authority regressions
- `e71d55ff90d8eec3b03a8f3073e9ffefdc0d1228` — Prevent duplicate legacy history for Stage C edits

## Release-candidate preparation

All user/tool-visible release identity surfaces were advanced from 1.0.19 to 1.0.20: editor/launcher/updater assemblies, backend APP_VERSION, Windows export file/product versions, installer, editor ready label, and release audit expectation.

### Failed release-prep attempt retained

Build run `34502642479` for `a588afa5b173a5a4af590cd6e8df1ceba7bd052d` failed at the release-audit step after Python/core/execution/geometry tests had passed. Cause: version reconciliation was incomplete; the audit/export/editor status still expected 1.0.19. The remaining identity surfaces were then corrected.

During self-review, the first backend-version `update_file` payload was found to have accidentally truncated part of `ai_backend/app.py`. This never reached a release. The defect was detected with an explicit Git compare against `e71d55f...`, then commit `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a` restored the exact prior backend and changed only `APP_VERSION`. A compare from `e71d55f...` to `bc3106d...` shows `app.py` at exactly +1/-1; every other release-prep file is also version-only.

## Validation

For release-candidate application commit `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`:

- `core-foundation` run `34503132325`: **SUCCESS**.
- build run `34503132420` at final inspection:
  - Python compile/dependency resolution: **PASS**
  - core logic/execution/job tests: **PASS**
  - real geometry regressions: **PASS**
  - v1.0.20 release audit: **PASS**
  - editor/launcher/updater/Core C# restore/build: **PASS**
  - portable package/layout/SHA: **PASS**
  - installer-definition compilation was still in progress at the final poll; reconcile the run conclusion before tagging.

No real tag-gated Godot Windows release export or installer smoke-install has run yet. No v1.0.20 release has been published.

Documentation commits after `bc3106d...` advance branch HEAD; inspect exact branch HEAD and rerun/reconcile CI before tagging.

## Coordinator alignment

This run stayed within the Coordinator’s P0 and bounded v1.0.20 milestone. It did not broaden into full sculpt migration, full Job Broker durability, provider expansion, Rig/Pose migration, kitbash migration, global `Main.V*.cs` removal, or UI rewrite.

The code target described in `TECHNICAL_ROADMAP.md` is now coherent. Do not accumulate unrelated feature work on v1.0.20 while release is pending.

## Exact next task

1. Reconcile actual latest release, branch HEAD, and all CI since this handoff.
2. Confirm build `34503132420` completed green, especially installer-definition packaging.
3. Confirm the documentation-only HEAD remains green and that no release-blocking regression was introduced.
4. If green, **publish v1.0.20 rather than doing unrelated implementation**:
   - create/push immutable tag `v1.0.20` at the final release-candidate/documentation HEAD;
   - monitor the tag-gated `full-windows-release` job;
   - require verified Godot 4.7.2 Windows export, release manifest/tag match, ZIP/hash verification, installer creation and silent installer smoke-install;
   - allow the existing workflow to publish the immutable GitHub Release only after those gates pass;
   - verify GitHub latest release really becomes v1.0.20.
5. After successful publication, update canonical docs to stable v1.0.20 and create/use forward-only `v1.0.21` before any new application change.
6. Use released v1.0.20 for target-machine acceptance: full Stage-C flow, MS-009 viewport/grid/model/gizmo, MS-013 storage containment, and one intended lightweight/default 3D provider on GTX 1080 / 16 GB.

## Operational tooling blocker

The GitHub connection available in this run exposes branch/file writes and Actions inspection/reruns, but no tag-creation or GitHub-release creation action. The container network also cannot provide a fallback Git push. The established workflow deliberately releases only from a `v1.x` tag, so **do not bypass the release gates by changing workflow behavior just to compensate for this tool limitation**.

If the next Dev Cycle has the same restricted connector, user action is operationally required: create/push tag `v1.0.20` at the final release-candidate commit documented after all branch CI is green. Once the tag exists, the existing GitHub Actions workflow owns real export/smoke/publication.

## Current issue priority

1. **MS-018:** publish and target-qualify the now coherent Stage-C thin slice.
2. **MS-009:** target-PC viewport verification on v1.0.20; any reproduced blank viewport immediately becomes P0.
3. **MS-013:** target-PC storage containment verification.
4. **MS-022:** one intended lightweight/default 3D route target qualification.
5. **MS-019:** remaining legacy authority outside Stage-C; Coordinator should sequence post-release migration.
6. **MS-020:** resume broader durable queue/resource ownership/crash recovery after Stage-C release/acceptance unless a concrete lifecycle blocker appears.

## User input

No product/design decision is required. Only the operational tag creation may require the user if release-capable GitHub tooling remains unavailable.
