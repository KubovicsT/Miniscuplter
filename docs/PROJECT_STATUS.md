# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above.
- **Latest validated application/release-candidate code commit:** `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`.
- **Latest pure editing implementation commit:** `e71d55ff90d8eec3b03a8f3073e9ffefdc0d1228`.
- **Overall completion estimate:** **58% acceptance-weighted**.

Version/release reconciliation is clean: v1.0.19 is immutable and published; v1.0.20 is a distinct forward-only development branch. Documentation/process commits after the validated application code are intentional and must be included in the exact release candidate SHA.

## Current phase

The Coordinator-defined bounded v1.0.20 Stage-C code scope is implemented: accepted 2D baseline → identity-bound 3D candidate → explicit apply → Core-authoritative transforms → one bounded immutable sculpt commit → save/reload → revision-bound cleanup → exact validated STL export. Real target-machine acceptance remains outstanding and is intentionally sequenced after publication of the testable build.

The estimate remains 58%. No acceptance-weighted product capability changed during release-process repair.

## v1.0.20 Stage-C editing authority

- `Core/StageCEditing.cs` owns the narrow editing contract for this release.
- Move/rotate/scale/ground commands and native gizmo commits for mapped Stage-C objects update the exact durable `ProjectObject.Transform` through `ProjectSession` transactions.
- Save/reload and export therefore reproduce the committed transform instead of trusting transient Godot/widget state.
- One bounded sculpt-stroke path converts the committed visible mesh to `MeshData`, creates an immutable child `MeshRevision`, transactionally advances the same `ObjectId`, and persists through the recovery-safe save boundary.
- Stale sculpt output is rejected if the active input revision changed before commit.
- Stage-C-aware Undo/Redo replays Core transactions and projects the resulting durable state back into the Godot scene.
- Applied-object restore reapplies the durable `ProjectObject.Transform` after restart.

## Validation

For release-candidate application commit `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`:

- `core-foundation` run `34503132325`: **SUCCESS**.
- broader build run `34503132420`: **SUCCESS** across Python compilation/dependency resolution, Core/execution/job regressions, real geometry regressions, v1.0.20 release audit, C# restore/build, portable package/layout/SHA verification, and installer-definition compilation.

The real Godot 4.7.2 Windows release export and installer smoke-install are intentionally performed by the autonomous release workflow immediately before publication.

## Autonomous release state

The permanent `release-control` branch now owns `.github/workflows/autonomous_release.yml` and `release-requests/`. A v1.0.20 request was submitted for branch HEAD `dc72d4a528b07d5855066d5fcaf2d4ef2c4d1be0`.

The first autonomous-release run (`34506052015`) failed in the request-parser step before any application build gate. Root cause: request discovery used `git diff-tree` on only the triggering commit SHA; the release-control update arrived as a merge commit, so that command did not reliably expose the request path. This was an orchestration defect, not a v1.0.20 application failure.

`release-control` commit `06763c99a0206bef0bf48a66a980c6d6f1095305` fixes request discovery to compare the complete GitHub push range (`github.event.before` → `github.sha`), with a root-push fallback, while retaining the exactly-one-request, branch/SHA immutability, build/export/hash/smoke and publication gates.

Because canonical release-state documentation is being reconciled after that failed request, the v1.0.20 branch will receive a new documentation-only HEAD. The same `release-requests/v1.0.20.json` must then be updated to that exact final 40-character SHA to retrigger autonomous validation. While that request is running, v1.0.20 is frozen.

## Current highest-priority issues

1. **MS-018:** publish and target-qualify the coherent Stage-C thin slice; the release-orchestration failure above is contained and does not change the Stage-C application scope.
2. **MS-009:** viewport/grid/model/gizmo still needs target-machine verification on released v1.0.20.
3. **MS-013:** storage containment still needs representative target-machine verification.
4. **MS-022:** qualify one intended lightweight/default 3D provider on GTX 1080 / 16 GB.
5. **MS-019:** broader legacy architecture remains outside the migrated Stage-C slice; do not broaden before release.
6. **MS-020:** broader durable Job Broker work remains sequenced behind Stage-C release/acceptance unless a concrete lifecycle defect blocks it.

## Immediate engineering priority

Do not accumulate unrelated application work on v1.0.20. Finish the documentation reconciliation, update the existing v1.0.20 autonomous release request to the exact final branch HEAD, and allow `autonomous_release.yml` to independently rebuild and validate that SHA. Publication is permitted only if C#/Core/Python/geometry/release-audit gates, the real Godot 4.7.2 Windows export, release outputs/hash verification and silent installer smoke-install all pass and the source branch still points to the requested candidate.

After successful publication, verify GitHub latest release is v1.0.20, reconcile canonical docs to published state, and create/use forward-only `v1.0.21` before any application change.

## User input currently required

No product/design or manual release action is currently required. Target-machine acceptance will require the released v1.0.20 build once publication succeeds.
