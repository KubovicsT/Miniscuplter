# Miniscuplter Issues Ledger

> Canonical bug/problem ledger. Every meaningful defect, reliability concern, or architectural risk should have a stable `MS-xxx` ID. Do not delete history when an attempted fix fails; append the attempt/result. User-observed runtime/UI issues should not be marked `RESOLVED` merely because code compiled.

Allowed statuses:

- `OPEN`
- `INVESTIGATING`
- `IN PROGRESS`
- `FIXED - NEEDS USER VERIFICATION`
- `RESOLVED`
- `BLOCKED`
- `DEFERRED`
- `WONTFIX`

Last reconciled: 2026-09-10

---

## MS-001 — Updater exhausted C: / `%TEMP%`

- **Severity:** High
- **Status:** RESOLVED
- **First observed:** v1.0.5 → v1.0.6 update path
- **Affected versions:** primarily v1.0.5 updater
- **Reproduction:** update on a machine with limited system-drive free space while the old updater stages ZIP/extraction/backup under Windows TEMP.
- **Expected:** update work uses configured/installation storage and does not require large free space on C:.
- **Actual:** update failed with `System.IO.IOException: Nincs elég hely a lemezen` while copying large `.venv` content into a TEMP backup.
- **Root cause:** old update flow used `%TEMP%` for download/staging/extraction/large backup.
- **Attempts/fixes:** v1.0.7 introduced storage-aware update cache, same-volume extraction and move-based rollback; later updater work added stronger transaction handling.
- **Result:** repeated later releases have used the improved path successfully; the original TEMP-only architecture is gone.
- **Next action:** retain low-disk regression/fault testing as updater evolves.

## MS-002 — Updater rollback / partial backup transaction hazards

- **Severity:** Critical
- **Status:** RESOLVED
- **First observed:** takeover audit before v1.0.12
- **Affected versions:** pre-v1.0.12 transaction design
- **Expected:** an interrupted/partial backup can never cause deletion of old files that were not safely preserved.
- **Actual risk:** caller logic could treat incomplete backup state too much like a completed transaction; Windows stream lifetime also risked verifying/renaming a ZIP while the writer remained open.
- **Root cause:** insufficient explicit transaction state and ownership boundaries.
- **Attempts/fixes:** v1.0.12 added explicit managed application ownership, persistent transaction journal, partial-backup distinction, retained rollback until new-launcher health acknowledgement, and writer disposal before hash/rename.
- **Verification:** release/installer checks pass; many subsequent self-updates have been delivered.
- **Next action:** destructive interruption/fault-injection matrix remains desirable but is not blocking normal use.

## MS-003 — Corrupt AI `.venv`, pip, or PyTorch runtime

- **Severity:** High
- **Status:** RESOLVED
- **First observed:** v1.0.6/v1.0.7 testing
- **Symptoms:** missing `torch.utils.data.datapipes.iter.sharding`, later missing `pip._vendor.urllib3...`; concept generation appeared hung or failed.
- **Root cause:** incomplete/corrupt shared Python environment.
- **Attempts/fixes:** runtime repair now validates Python/pip, can rebuild only `.venv`, verifies cached torch wheels, force-reinstalls torch/torchvision when inconsistent, imports exact critical modules and validates CUDA/GPU visibility.
- **Result:** user verified `torch 2.5.1+cu124`, `torchvision 0.20.1+cu124`, CUDA available and GTX 1080 recognized; SDXL subsequently generated successfully.
- **Next action:** preserve repair tests and avoid tying model-weight deletion to runtime repair.

## MS-004 — Cancelling an AI job can poison the next job

- **Severity:** High
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** v1.0.12/v1.0.13-era testing
- **Reproduction:** start long AI job → cancel → quickly start another job; second job may fail within seconds.
- **Expected:** cancellation fully releases/isolates the abandoned work before a new job starts.
- **Actual:** editor request cancellation could leave synchronous Python/CUDA inference running in the backend process.
- **Root cause:** request-lifetime cancellation was not equivalent to stopping the owned worker/process/GPU work.
- **Attempts/fixes:** v1.0.13 cancellation path resets/kills the owned local backend process tree, restarts it and waits for health before accepting new work; later job infrastructure adds more structured progress/state.
- **Result:** code path improved, but durable Job Broker semantics remain incomplete.
- **Next action:** target-machine stress test repeated cancel/restart across 2D and 3D; fold final behavior into authoritative Job Broker (MS-020).

## MS-005 — TripoSR installation depends on fragile native tooling (`torchmcubes`)

- **Severity:** Medium
- **Status:** OPEN
- **First observed:** v1.0.7 testing
- **Symptoms:** required Git; then `pip install git+https://github.com/tatsy/torchmcubes.git ...` failed. Initial concrete failure was `No space left on device` in isolated build TEMP.
- **Root cause:** upstream/native-extension install path introduces Git/build-isolation/compiler/CUDA-toolkit dependencies and large temp usage.
- **Attempts/fixes:** model-install TEMP/PIP cache moved under Miniscuplter data storage; detailed subprocess stdout/stderr reporting added; TripoSR adapter import/output contract repaired in v1.0.12.
- **Result:** C: TEMP blocker addressed, but the product still should not depend on a developer toolchain for a default user provider if avoidable.
- **Next action:** investigate prebuilt/Gitless/CPU-fallback packaging or demote TripoSR to experimental if reliable end-user install cannot be achieved.

## MS-006 — Hunyuan/3D generation missing `pymeshlab`

- **Severity:** High
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** v1.0.13 user test
- **Reproduction:** press 3D generation; job fails almost immediately with `No module named 'pymeshlab'`.
- **Expected:** provider dependencies are installed/validated before generation begins.
- **Root cause:** provider/runtime dependency contract omitted required modules.
- **Attempts/fixes:** v1.0.15 added required Hunyuan-related dependencies/preflight and clearer Repair AI Runtime guidance; Auto routing can try another suitable installed provider when an auto-selected provider fails.
- **Result:** static/runtime contract improved; real target-machine inference still needs verification.
- **Next action:** include in provider self-test framework (MS-022) and Stage-C acceptance (MS-018).

## MS-007 — Z-Image OOM / system-memory pressure on 8 GB GPU

- **Severity:** Medium
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** v1.0.13 user test
- **Reproduction:** run Z-Image on GTX 1080 8 GB / 16 GB RAM; dedicated VRAM and shared memory saturate, followed by CUDA allocation failure.
- **Expected:** supported low-VRAM mode either runs within budget or fails early with a useful explanation.
- **Root cause:** offload strategy and model size exceeded practical memory limits.
- **Attempts/fixes:** v1.0.14 uses more conservative sequential CPU offload on 8–10 GB GPUs, applies VRAM ceiling, lowers initial/safe retry resolution, reduces prompt length and retries after pipeline/cache release.
- **Result:** mitigation exists, but 16 GB RAM remains tight and this model may not be a sensible default for the reference machine.
- **Next action:** benchmark/qualify; consider quantized execution or classify as non-default if it remains impractical.

## MS-008 — FLUX concept generation is too slow for default UX on GTX 1080

- **Severity:** Medium
- **Status:** OPEN
- **First observed:** user test after v1.0.14-era work
- **Observed:** approximately 7–8 minutes for one somewhat useful image.
- **Expected:** user understands expected cost and Auto/Balanced routing chooses a sensible model for the machine/task.
- **Root cause:** model/hardware combination is computationally heavy; current provider UX exposes technical model choices more than intent/cost.
- **Attempts/fixes:** hardware-aware routing and quality presets exist; stage feedback exists.
- **Next action:** collect per-provider target-machine benchmarks; expose estimated time/RAM/VRAM and choose default Fast/Balanced/Quality routes based on qualified data.

## MS-009 — 3D viewport, grid, model and gizmo can be completely blank

- **Severity:** Critical
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** repeatedly through v1.0.11–v1.0.18 testing
- **Reproduction:** launch editor or complete 3D generation; output STL may exist and status may claim success, but center 3D surface shows no model, grid, axes or gizmo.
- **Expected:** a visible 3D workspace exists at launch; generated/imported mesh is inserted, selected, framed and rendered with grid/axes/gizmo.
- **Actual:** user repeatedly observed a flat/empty gray viewport.
- **Attempts/fixes:** multiple grid implementations; explicit SubViewport sizing/world/camera work; v1.0.15 triangle-bar grid; v1.0.17 recovery/rebind/material/gizmo logic; v1.0.18 native viewport tool foundation; v1.0.19 adds `Main.V1019ViewportPipeline.cs` with native `SubViewportContainer`, explicit world/camera ownership, starter mesh, visible materials, grid rebuild, selection/gizmo refresh and render-frame diagnostics.
- **Result:** v1.0.19 is now published after full Windows export/hash/installer validation. The runtime/UI symptom remains unverified on the user's actual machine.
- **Next action:** test released v1.0.19 on the target PC; if still blank, capture viewport diagnostics/render-probe output and fix root scene/render ownership rather than adding overlays.

## MS-010 — 2D regional AI editing originally expected image selection in the 3D viewport

- **Severity:** High
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** v1.0.13 user test
- **Expected:** select/edit regions directly on the visible 2D image.
- **Actual:** old flow relied on a viewport/capture concept that was empty and unsuitable for image masking.
- **Attempts/fixes:** v1.0.15 introduced a real center 2D canvas, image-coordinate region selection and direct mask creation; edited outputs sync back to the center canvas.
- **Next action:** continue testing regional mask accuracy at zoom/resize and integrate revision-bound masks into the new core.

## MS-011 — AI image edit lacked meaningful progress feedback

- **Severity:** High
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** user test before v1.0.17
- **Expected:** edit/enhance shows stage, elapsed time, provider/progress when available and cancellation state.
- **Actual:** image edit appeared to do nothing while inference ran.
- **Attempts/fixes:** v1.0.17 added explicit image-edit status panel, activity/progress bar, cancellation and backend progress polling; v1.0.18 extends structured backend job-progress handling.
- **Result:** implementation exists; provider-specific true inference-step detail varies.
- **Next action:** verify on real edit jobs; expand real step reporting where provider APIs expose it, without inventing percentages.

## MS-012 — Internet reference search failed / was too narrow

- **Severity:** Medium
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** v1.0.8/v1.0.13-era testing
- **Symptoms:** HTTP 403/failing search, status visibility problems, later Wikimedia-only results and insufficient practical browsing.
- **Attempts/fixes:** Wikimedia API with explicit user agent; v1.0.15 visible thumbnails and adoption into 2D workflow; v1.0.16 adds Openverse + Wikimedia concurrent search, source/creator/license metadata, deduplication and partial-provider failure recovery.
- **Next action:** verify result quality/availability; add filters/ranking only if real use shows need.

## MS-013 — Miniscuplter working files leak into C:\AppData / system TEMP

- **Severity:** High
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** user screenshots before v1.0.18
- **Expected:** generated 2D images, masks, 3D outputs, job artifacts, caches, logs and temporary files use the Miniscuplter-controlled data root, normally beside the installation/data area unless user explicitly configures another root.
- **Actual:** paths under `C:\Users\...\AppData\Roaming\Godot\...` were visible.
- **Attempts/fixes:** update/model temp was progressively redirected from v1.0.7 onward; v1.0.17 added contained workspace paths; v1.0.18 added authoritative `AppDataRoot` and environment redirection for APPDATA/LOCALAPPDATA/TEMP/HF/Torch/Pip/Python caches; v1.0.19 adds backend storage and Windows canonical containment hardening/tests.
- **Result:** v1.0.19 containment hardening is now published after automated Windows validation, but real-machine verification is required to prove no important path remains on C:.
- **Next action:** on released v1.0.19 run representative 2D edit, 3D generation, reference download, geometry operation, capture, save and cancellation; inspect all emitted paths/process environments.

## MS-014 — Quality presets were opaque and not manageable

- **Severity:** Medium
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** user Settings screenshot before v1.0.17
- **Expected:** inspect what a preset changes and create/clone/rename/save/delete custom presets.
- **Actual:** old UI exposed selection without an understandable parameter breakdown or adequate custom management.
- **Attempts/fixes:** v1.0.17 rebuilt the Settings Quality panel to expose 2D resolution/steps/CFG/edit strength/input size, 3D shape steps, remesh/repair voxel pitch, voxel budget, thickness samples and Smart Select settings, plus Create/Clone/Rename/Save/Delete for custom presets and immutable built-ins.
- **Next action:** verify usability; later migrate from additive legacy UI into the declarative settings architecture.

## MS-015 — Context-aware AI Edit `Enhance` mode missing

- **Severity:** Medium
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** user request before v1.0.17
- **Use case:** select an obviously malformed gun/hand/armor/detail and ask the AI to fix it without explaining the exact correction.
- **Expected:** selected region is reconstructed using full-image subject/style/perspective/material context while preserving everything outside the mask.
- **Attempts/fixes:** v1.0.17 added `Enhance Selected Region` with a dedicated context-aware corrective prompt and regional detail pipeline.
- **Next action:** evaluate real outputs; if generic prompt inference is insufficient, add optional semantic/reference analysis while keeping the simple one-click UX.

## MS-016 — Fixed-size right-side UI caused controls to be pushed out of frame

- **Severity:** Medium
- **Status:** RESOLVED
- **First observed:** v1.0.9 screenshot
- **Expected:** resizing window or expanding feedback never makes required controls unreachable.
- **Actual:** fixed/minimum heights and non-scrolling right panel pushed 2D preview/actions below the window.
- **Attempts/fixes:** v1.0.10 added vertical scrolling, proportional bounded panel width and explicit SubViewport resize behavior; later UI continued using responsive containers.
- **Next action:** preserve responsive-layout checks in future redesign.

## MS-017 — Geometry analysis falsely reported open STL / called nonexistent `Trimesh.is_finite`

- **Severity:** High
- **Status:** RESOLVED
- **First observed:** Astra takeover audit
- **Expected:** closed STL analyzes as closed; finite-coordinate validation uses actual mesh data.
- **Actual:** STL per-face duplicate vertices confused index-based topology; `Trimesh.is_finite` property did not exist.
- **Attempts/fixes:** v1.0.12 validates coordinate arrays, welds coincident vertices only in an analysis copy, explicitly includes `rtree`, and adds real closed-box/STL/remesh/thickness regression tests.
- **Next action:** keep regression fixtures as geometry code evolves.

## MS-018 — No qualified end-to-end Stage-C thin slice yet

- **Severity:** Critical
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit / reinforced by user testing
- **Expected:** on GTX 1080/8 GB + 16 GB RAM, the user can complete `2D → accept baseline → qualified 3D → visible/editable mesh → save/reload → cleanup → validated STL`, with cancellation recovery and contained storage.
- **Actual:** the production path now reaches durable cleanup and explicit validated STL export in code, but normal mapped-object editing state is still partly widget-authoritative and the complete flow has not been qualified on the target machine.
- **Attempts/fixes:** v1.0.12–v1.0.19 hardened provider, viewport, storage, geometry and update seams. v1.0.20 moved accepted baseline, generated candidate, explicit Apply/Discard, transport identity, and save-failure recovery onto Core/ProjectStore. This run added `StageCCleanup` (`3f34cd0...`), the Stage-C-aware repair/export bridge (`35078ab...`, installed by `55db2a7...`), and cleanup lineage/export-scope tests (`52feda4...`). The first new Core run `34499552249` failed because Applied candidate validation incorrectly required the generated revision to remain current after cleanup. That failed attempt exposed the real lineage bug. Commit `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b` fixed validation so the generated revision must remain in the active descendant lineage instead.
- **Result:** an applied Stage-C object can now be repaired into a new immutable child `MeshRevision`, saved/reloaded with generation provenance intact, and exported from an explicit exact active object/revision scope. STL is only backend/export interchange. Target-machine qualification and authoritative normal transform/sculpt persistence remain incomplete.
- **Verification:** failed Core run `34499552249` is retained as evidence of the discovered lineage defect. Replacement `core-foundation` run `34499826871` passed. Broader Windows build `34499826741` passed Python/core/execution tests, real geometry regressions, release audit, C# builds, portable package/layout/SHA and installer-definition compilation at the latest check.
- **Next action:** migrate mapped Stage-C transforms and the minimum sculpt/edit mutation into ProjectSession/immutable revision transactions, then run the complete thin slice on the target machine.

## MS-019 — Legacy `Main.V*.cs` architecture remains authoritative

- **Severity:** High architectural risk
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit
- **Expected target:** stable domain IDs/revisions, declarative UI, transactional commands/history, clean service boundaries.
- **Actual:** many features still depend on partial `Main.V*.cs`, widget/scene state and compatibility reparenting. On mapped Stage-C objects, normal transform/sculpt mutations can still diverge from durable project state.
- **Attempts/fixes:** Stage-B `Core` introduced stable IDs, project models, immutable mesh revisions, history, indexed project storage and legacy importer; v1.0.19 extended these foundations. v1.0.20 migrated accepted baseline → generation → review/apply → persistence. This run extended the same vertical slice through cleanup and final STL export: repair is now revision-bound and transactional for Stage-C objects, while legacy objects retain fallback behavior.
- **Result:** the production Stage-C vertical slice now uses Core authority through cleanup/export, but editing between Apply and Cleanup still has legacy escape paths.
- **Next action:** bind mapped-object move/rotate/scale and minimum sculpt commit semantics to ProjectSession transactions/new immutable revisions before broadening to unrelated subsystems.

## MS-020 — Final authoritative AI Job Broker / stale-result protection not complete

- **Severity:** High
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit
- **Expected:** durable job IDs, immutable input revision, one heavy GPU owner, structured stages, real cancellation, isolated outputs, stale-result candidate semantics and crash recovery.
- **Actual:** structured progress and backend reset behavior exist, but request/backend lifecycle is not yet the full target durable job architecture.
- **Attempts/fixes:** cancellation reset in v1.0.13; job progress in v1.0.17/v1.0.18; v1.0.20 added stable generation binding, end-to-end transport correlation and fail-closed save recovery. Stage-C cleanup continues to reuse the existing AIClient/backend geometry path rather than creating a parallel request mechanism; its Core binding rejects stale object revisions before applying results.
- **Result:** generation stale/replay safety and project-save consistency are hardened, and cleanup is also revision-bound at the project boundary. Durable queue/resource ownership and crash recovery beyond ProjectStore recovery are still incomplete.
- **Verification:** deterministic Core tests cover generation/candidate identity, save rollback, cleanup stale-result rejection and revision lineage.
- **Next action:** finish the Stage-C editing-state migration, then return to durable queue/resource ownership/recovery.

## MS-021 — Canonical documentation was stale/incomplete

- **Severity:** Medium project-management risk
- **Status:** IN PROGRESS
- **First observed:** 2026-09-10 autonomous-work bootstrap
- **Evidence:** v1.0.18 repository README still identifies itself as v1.0.12; release history current-testing section also lags. Earlier handoff docs were not consistently present on release branches.
- **Expected:** a new session can recover product truth, status, issues, decisions and current baton from the repository.
- **Attempts/fixes:** canonical `PROJECT_CHARTER.md`, `PROJECT_STATUS.md`, `ISSUES.md`, `DECISIONS.md`, `HANDOFF.md` introduced on v1.0.19 and carried into v1.0.20. Recurring development runs are maintaining status/issues/handoff after coherent engineering work.
- **Next action:** continue maintaining canonical files; later reconcile/remove misleading stale top-level documentation rather than allowing duplicate truth sources.

## MS-022 — Provider readiness is not qualified strongly enough

- **Severity:** High
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit; reinforced by TripoSR/Hunyuan/Z-Image failures
- **Expected:** provider registry distinguishes downloaded, installed, importable, device-tested and inference-tested, with hardware/platform/resource expectations.
- **Actual:** provider presence/routing historically overstated practical readiness; some optional providers require fragile native tooling or exceed reference hardware.
- **Attempts/fixes:** hardware routing, explicit selection failure, isolated environments, dependency preflights, runtime repair and model manifests existed before v1.0.20. v1.0.20 added persisted readiness states, lightweight import/CUDA probes for main single-mesh routes, readiness-aware Auto/explicit routing, non-blocking health status, and verified successful-inference qualification for the actual final provider with elapsed time/hardware context. Failed/cancelled jobs do not qualify or blacklist a provider; CI does not fake inference-tested status.
- **Result:** the readiness contract is materially implemented, but target GTX1080 evidence and supported/default-vs-experimental provider policy remain incomplete.
- **Verification:** v1.0.20 branch validation through the Stage-C bridge continues to pass Python/provider regression coverage; real provider inference remains target-machine work.
- **Next action:** collect target-machine inference qualification for one intended lightweight/default 3D route and use measured evidence for default/support tiers.

---

## Issue handling rules

1. When a new bug is found, search this file first and reuse an existing ID if it is the same root problem.
2. Append attempts/results; do not erase failed fixes.
3. A successful CI build can move a code defect to `FIXED - NEEDS USER VERIFICATION`, not automatically `RESOLVED`, when the failure was observed only in the real GUI/GPU environment.
4. Record relevant version/commit references whenever known.
5. If an issue blocks the current task but independent work exists, document the blocker and continue the highest-value unblocked work.
6. If a fix exposes a deeper architectural root cause, keep the symptom issue and link it to the architectural issue rather than hiding the history.
