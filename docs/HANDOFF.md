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

Version/release reconciliation remained clean throughout this run: v1.0.19 is immutable/published and v1.0.20 is the distinct forward development branch. Documentation commits after the release-candidate application commit advance branch HEAD; resolve exact live HEAD before tagging.

## What this run completed

The Coordinator-defined bounded v1.0.20 Stage-C code target is implemented.

### Authoritative mapped-object transforms

`Core/StageCEditing.cs` adds the narrow editing contract. `StageCEditing.SetTransform()` updates the exact durable `ProjectObject.Transform` through `ProjectSession.Execute()` while preserving `ObjectId` and active `MeshRevisionId`.

`Scripts/Main.V1020StageCEditing.cs` keeps the established UX while making committed state durable:

- toolbar move/rotate/scale/ground actions commit their resulting Godot transform to the mapped Stage-C object;
- native viewport gizmo transforms commit at gesture completion;
- transform-only commits do not reload mesh data unnecessarily;
- failed saves/commits project the last durable transform back into the scene;
- Stage-C-aware Undo/Redo replays the Core transaction for the exact selected object and saves it.

`Main.V1020StageCRestore.cs` restores the durable transform when applied Stage-C objects are recreated after restart.

### One bounded authoritative sculpt/edit path

The native sculpt-stroke path is the one bounded mesh edit migrated for v1.0.20:

- stroke start captures exact mapped `ObjectId` and active input `MeshRevisionId`;
- committed mesh becomes project-native `MeshData`;
- ProjectStore writes a new immutable child `MeshRevision`;
- `StageCEditing.CommitMeshRevision()` requires the exact active parent and rejects stale output;
- one ProjectSession transaction registers the revision and advances that same object;
- save failure restores durable state;
- Undo/Redo switches complete project state rather than replaying an isolated widget mesh.

Self-review found and fixed two integration hazards:

1. v1.0.18 installs its authoritative viewport handler lazily; the Stage-C observer now waits until that viewport is live before attaching.
2. legacy sculpt also pushed the same stroke into its mesh-only undo stack; the bridge removes only the exact duplicated legacy entry after the Stage-C commit/revert, preventing later fallback Undo from replaying the same edit outside Core.

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

## Release-candidate preparation and failed-attempt history

All user/tool-visible release identity surfaces are now 1.0.20: editor/launcher/updater assemblies, backend APP_VERSION, Windows export file/product versions, installer, editor ready label and release-audit expectation.

Build `34502642479` at `a588afa5b173a5a4af590cd6e8df1ceba7bd052d` failed at release audit after Python/core/execution/geometry tests passed because version reconciliation was still incomplete. The remaining identity surfaces were corrected.

During self-review, the first backend-version `update_file` payload was found to have accidentally truncated part of `ai_backend/app.py`. It never reached a release. Git compare against `e71d55f...` exposed the mistake; `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a` restores the exact prior backend content and changes only `APP_VERSION`. Compare from `e71d55f...` to `bc3106d...` shows app.py exactly +1/-1 and all other release-prep changes version-only. Preserve this failed-and-recovered history.

## Validation

For release-candidate application commit `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`:

- `core-foundation` run `34503132325`: **SUCCESS**.
- build run `34503132420`: **SUCCESS**.
- Python compile/dependency resolution: **PASS**.
- core logic/execution/job tests: **PASS**.
- real geometry regressions: **PASS**.
- v1.0.20 release audit: **PASS**.
- editor/launcher/updater/Core C# restore/build: **PASS**.
- portable package/layout/SHA verification: **PASS**.
- installer-definition compilation: **PASS**.

The branch workflow correctly skipped `full-windows-release` and `publish-release`; those are tag-gated by design. No real Godot Windows release export or installer smoke-install has run yet. No v1.0.20 release has been published.

Docs-only commits after the validated application commit trigger additional branch CI; reconcile exact HEAD/latest CI before tagging, but do not reopen unrelated application work if they remain green.

## Coordinator alignment

This run stayed within the Coordinator’s P0 and bounded v1.0.20 milestone. It did not broaden into full sculpt migration, full Job Broker durability, provider expansion, Rig/Pose migration, kitbash migration, global `Main.V*.cs` removal, or UI rewrite.

The code target in `TECHNICAL_ROADMAP.md` is coherent. Do not accumulate unrelated feature work on v1.0.20 while release is pending.

## Exact next task

1. Reconcile actual latest release, exact branch HEAD and all docs-only CI since this handoff.
2. If all branch validation remains green, **publish v1.0.20 rather than doing unrelated implementation**:
   - create/push immutable tag `v1.0.20` at the final release-candidate/documentation HEAD;
   - monitor tag-gated `full-windows-release`;
   - require verified Godot 4.7.2 Windows export, release-manifest/tag match, ZIP/hash verification, installer creation and silent installer smoke-install;
   - allow the existing workflow to publish immutable GitHub Release only after those gates pass;
   - verify GitHub latest release becomes v1.0.20.
3. After successful publication, update canonical docs to stable v1.0.20 and create/use forward-only `v1.0.21` before any application change.
4. Use released v1.0.20 for target-machine acceptance: complete Stage-C flow, MS-009 viewport/grid/model/gizmo, MS-013 storage containment, and one intended lightweight/default 3D provider on GTX 1080 / 16 GB.

## Operational tooling blocker

The GitHub connection in this run exposes branch/file writes and Actions inspection/reruns, but no tag-creation or GitHub-release creation action. Container networking also cannot provide a fallback Git push. The established workflow intentionally releases only from an explicit `v1.x` tag. **Do not change or bypass that release architecture solely to compensate for connector limitations.**

If the next Dev Cycle has the same restricted tools, the user must create/push tag `v1.0.20` at the final branch HEAD after branch CI is green. Once the tag exists, the existing Actions workflow performs the real export/smoke/publication gates.

## Current issue priority

1. **MS-018:** publish and target-qualify the coherent Stage-C thin slice.
2. **MS-009:** target-PC viewport verification on v1.0.20; any reproduced blank viewport immediately becomes P0.
3. **MS-013:** target-PC storage containment verification.
4. **MS-022:** one intended lightweight/default 3D route target qualification.
5. **MS-019:** remaining legacy authority outside Stage-C; Coordinator should sequence post-release migration.
6. **MS-020:** resume broader durable queue/resource ownership/crash recovery after Stage-C release/acceptance unless a concrete lifecycle blocker appears.

## User input

No product/design decision is required. Only operational tag creation may require user action if release-capable GitHub tooling remains unavailable.
