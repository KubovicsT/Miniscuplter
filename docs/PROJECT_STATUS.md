# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above.
- **Latest application/release-candidate commit:** `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`.
- **Latest pure editing implementation commit:** `e71d55ff90d8eec3b03a8f3073e9ffefdc0d1228`.
- **Overall completion estimate:** **58% acceptance-weighted**.

Version/release reconciliation remains clean: v1.0.19 is immutable and published; v1.0.20 is a distinct forward-only development branch.

## Current phase

The Coordinator-defined bounded v1.0.20 Stage-C code scope is now implemented: accepted 2D baseline → identity-bound 3D candidate → explicit apply → Core-authoritative transforms → one bounded immutable sculpt commit → save/reload → revision-bound cleanup → exact validated STL export. Real target-machine acceptance remains outstanding and is intentionally sequenced after publication of the testable build.

The estimate rises from 57% to 58% because mapped Stage-C transform and one committed mesh-edit path are now represented in the same durable ProjectSession/revision model and covered by deterministic regression tests. Stage-C does not receive full acceptance credit until the released build is exercised on the GTX 1080 / 16 GB target machine.

## v1.0.20 Stage-C editing authority

- `Core/StageCEditing.cs` owns the narrow editing contract for this release.
- Move/rotate/scale/ground commands and native gizmo commits for mapped Stage-C objects update the exact durable `ProjectObject.Transform` through `ProjectSession` transactions.
- Save/reload and export therefore reproduce the committed transform instead of trusting transient Godot/widget state.
- One bounded sculpt-stroke path converts the committed visible mesh to `MeshData`, creates an immutable child `MeshRevision`, transactionally advances the same `ObjectId`, and persists through the recovery-safe save boundary.
- Stale sculpt output is rejected if the active input revision changed before commit.
- Stage-C-aware Undo/Redo replays Core transactions and projects the resulting durable state back into the Godot scene.
- Self-review fixed two integration hazards before release: the viewport observer attaches only after the authoritative v1.0.18 viewport tool is installed, and the duplicated legacy sculpt undo entry is removed so the same Stage-C stroke cannot later be replayed outside Core history.
- Applied-object restore reapplies the durable `ProjectObject.Transform` after restart.

Relevant implementation commits:

- `e67a2d3a524a3cb429b29919f7805c7bb7b51483` — add Stage-C editing state contract.
- `01db848b481ceee8bdc051aea6d528108d23eb39` — bind production editing to Core state.
- `68725d0a1c3f7faf7f5affd165953b277d6525b3` — attach editing observer after viewport installation.
- `b5566027a3df2c9851a8cca15345b1efc4137d77` — install Stage-C editing authority bridge.
- `df8730bcd29875993e3d346c227bc1031ff4d4ae` — restore durable Stage-C transforms.
- `eb2aefc259910152965ab189c79c6fb11985fa95` / `0ecdbc95559794505156ae72b0bb8dc598659026` — transform/sculpt authority regressions.
- `e71d55ff90d8eec3b03a8f3073e9ffefdc0d1228` — remove duplicate legacy sculpt history and tighten object-specific Undo/Redo.

## Release-candidate preparation and failed-attempt history

All release-visible version surfaces are aligned to `1.0.20`: editor assembly, launcher, updater, backend, Windows export metadata, installer, editor status label, and release audit expectation.

A first release-prep validation at `a588afa5b173a5a4af590cd6e8df1ceba7bd052d` failed only at `tools/release_audit.py` because several release identity surfaces still expected 1.0.19. Python compilation, core logic, execution tests and geometry regressions had already passed. The metadata mismatch was corrected rather than ignored.

During self-review, an `update_file` payload for `ai_backend/app.py` was discovered to have unintentionally truncated part of the backend. It was caught before release by comparing against `e71d55f...`; `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a` restores the exact prior backend content and changes only `APP_VERSION`. Git compare from `e71d55f...` to `bc3106d...` confirms `app.py` is exactly +1/-1 and every other release-prep file is version-only.

## Validation

For release-candidate application commit `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`:

- `core-foundation` run `34503132325`: **SUCCESS**.
- broader build run `34503132420`: **SUCCESS** across all branch gates:
  - Python compilation/dependency resolution: **PASS**;
  - core logic/execution/job regressions: **PASS**;
  - real geometry regressions: **PASS**;
  - v1.0.20 release audit: **PASS**;
  - editor/launcher/updater/Core C# restore/build: **PASS**;
  - portable package build/layout/SHA verification: **PASS**;
  - installer-definition compilation: **PASS**.

The branch workflow correctly skipped `full-windows-release` and `publish-release`, because those are intentionally enabled only for explicit `v1.x` tags.

No real Godot Windows release export or installer smoke-install has run yet. No v1.0.20 release has been published.

## Current highest-priority issues

1. **MS-018:** bounded Stage-C code path is coherent; publish/test v1.0.20, then collect full target-machine acceptance.
2. **MS-009:** viewport/grid/model/gizmo still needs target-machine verification on the released test build.
3. **MS-013:** storage containment still needs representative target-machine verification.
4. **MS-022:** qualify one intended lightweight/default 3D provider on GTX 1080 / 16 GB.
5. **MS-019:** legacy architecture remains outside the migrated Stage-C slice; do not broaden before v1.0.20 release.
6. **MS-020:** broader durable Job Broker work remains sequenced behind Stage-C release/acceptance unless a concrete lifecycle defect blocks it.

## Immediate engineering priority

Do not accumulate unrelated application work on v1.0.20. All branch validation gates are green. The exact next action is release publication through the existing tag-gated path: create/push immutable tag `v1.0.20` at the final release-candidate/documentation HEAD, require the real Godot 4.7.2 Windows export, release manifest/tag match, ZIP/hash verification and silent installer smoke-install, then allow the existing workflow to publish the immutable GitHub Release. After successful publication, verify GitHub latest-release state and create/use `v1.0.21` before further application changes.

The GitHub connection available in this run can edit branches and inspect/rerun Actions but exposes no tag-creation or release-publication action. The container network cannot provide a fallback Git push. Do not weaken or bypass the established tag-gated release workflow to compensate for this tooling limitation.

## User input currently required

No product/design decision blocks engineering. With the current connector, one operational user action is required to start release gates: create/push tag `v1.0.20` at the final documented branch HEAD. The existing workflow can then perform the real export/smoke/publication automatically.
