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

## MS-009 — 3D viewport/grid/model/gizmo rendering is unstable or unreadable

- **Severity:** Critical
- **Status:** IN PROGRESS — v1.0.21 PARTIAL TARGET PASS; RESIZE COLOR DRIFT REMAINS
- **First observed:** repeatedly through v1.0.11–v1.0.18; reopened by released v1.0.20 reference-machine testing
- **Expected:** a visible, stable, readable 3D workspace exists immediately at launch; grid/axes/model/gizmo remain visually consistent across resize; model form/details are easy to inspect; the grid does not occlude geometry.
- **Historical actual:** output STL could exist while the center 3D surface was completely blank.
- **v1.0.20 evidence:** grid/floor and starter model finally rendered, but the resting grid/model were very dark, appearance changed after splitter resize settled, and the opaque floor could hide generated geometry.
- **User UX direction:** Blender-like solid-workspace hierarchy: neutral dark gray background, visible neutral grid/major lines, colored axes, light/mid neutral-gray model, readable studio lighting, and non-occluding grid semantics.
- **v1.0.21 implementation:** commits `fa381d4...`, `4d064a4...`, `6b80e14...`, `88abb5c...`, `c00bfc8...` make `SubViewportContainer.Stretch` the normal size owner; make legacy size writers defer; stop the v1.0.17 timer; remove delayed full repair on ordinary resize; replace the old tab full-repair handler with lightweight refresh; consolidate owned World3D setup; and apply neutral gray studio presentation/diagnostics.
- **v1.0.21 target-machine result:** **partial success.** The user reports that the 3D viewport now looks good on initial presentation, confirming the palette/lighting/readability work materially improved the base state. However, resizing the right-side AI panel still changes the viewport color. Therefore the presentation is still state-dependent and MS-009 is not resolved.
- **Residual code evidence:** shipped v1.0.21 still contains legacy `V1017RepairViewport()` palette/world mutation code that sets the environment background to approximately `(.025,.030,.040)` and older ambient settings. It is still reachable from historical code paths (including deferred install and legacy generation completion) even though the v1.0.19+ pipeline owns presentation. This is a concrete duplicate-authority seam and must be neutralized/delegated when the native pipeline is active. The exact right-panel-resize trigger still needs implementation-level tracing; do not assume this legacy call is the only cause until verified.
- **Result:** initial viewport appearance is now usable, but resize invariance and non-occluding grid behavior remain acceptance blockers.
- **Next action:** fix forward on v1.0.22. Make every legacy viewport repair/presentation path defer to the native v1.0.19+ owner when installed; ensure splitter resize cannot change environment/material/grid state; remove opaque ground occlusion; add regression/audit coverage for presentation-state invariance; retest initial state vs during/after right-panel resize on the reference PC.


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
- **Result:** containment hardening is published in v1.0.19 and carried into v1.0.20, but real-machine verification is required to prove no important path remains on C:.
- **Next action:** on released v1.0.20 run representative 2D edit, 3D generation, reference download, geometry operation, capture, save and cancellation; inspect emitted paths/process environments.

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
- **Actual:** the bounded production path is now coherent in code/tests through committed transform and one bounded sculpt edit, but the complete workflow has not yet been qualified on the target machine.
- **Attempts/fixes:** v1.0.12–v1.0.19 hardened provider, viewport, storage, geometry and update seams. Earlier v1.0.20 work moved accepted baseline, generated candidate, explicit Apply/Discard, transport identity, recovery-safe saves, cleanup and exact STL export onto Core/ProjectStore. This run added `StageCEditing` and production hooks so toolbar/native-gizmo transforms become `ProjectObject.Transform` transactions and one native sculpt stroke becomes a new immutable child `MeshRevision`. Commits include `e67a2d3...`, `01db848...`, `68725d0...`, `b556602...`, `df8730b...`, `eb2aefc...`, `0ecdbc9...`, and `e71d55f...`.
- **Self-review results:** viewport commit observation is delayed until the authoritative v1.0.18 viewport handler is installed; duplicate legacy sculpt undo is removed after the Core commit/revert; transform-only commits avoid unnecessary mesh reloads; stale sculpt output is rejected.
- **Release-prep history:** build `34502642479` at `a588afa...` failed release audit because version metadata was only partially advanced to 1.0.20; this was corrected. An accidental connector-side truncation of `ai_backend/app.py` was caught before release by compare and fully restored in `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`; compare against `e71d55f...` confirms the backend delta is now only APP_VERSION +1/-1.
- **Result:** the Coordinator-defined bounded v1.0.20 Stage-C code scope is complete enough to publish a test build once release gates pass. Stage-C itself remains unaccepted pending real GTX1080/16 GB evidence.
- **Verification:** `core-foundation` run `34503132325` for `bc3106d...` passed. Build `34503132420` passed Python compilation/dependencies, core/execution/job tests, real geometry regressions, v1.0.20 release audit, C# builds, and portable package/layout/SHA; installer-definition completion must be reconciled before tagging.
- **Next action:** v1.0.20 is now published; resume complete Stage-C target-machine qualification after MS-009 viewport verification no longer blocks visual inspection.

## MS-019 — Legacy `Main.V*.cs` architecture remains authoritative

- **Severity:** High architectural risk
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit
- **Expected target:** stable domain IDs/revisions, declarative UI, transactional commands/history, clean service boundaries.
- **Actual:** legacy architecture remains authoritative outside migrated vertical slices, but the immediate Stage-C dual-authority gap targeted for v1.0.20 is now bounded.
- **Attempts/fixes:** Stage-B Core introduced stable IDs, immutable revisions, history, project storage and migration. v1.0.20 migrated baseline → generation → review/apply → persistence → cleanup/export. This run added `Core/StageCEditing.cs` plus `Main.V1020StageCEditing.cs`: mapped move/rotate/scale/native-gizmo commits now update durable transforms, and one bounded native sculpt stroke advances an immutable child revision transactionally. Applied-object restore projects the durable transform back into Godot after restart.
- **Result:** normal mapped Stage-C transform plus one committed mesh-edit path now use Core authority and full-state history. This does **not** resolve the broader legacy architecture issue or migrate all sculpt/tool paths.
- **Next action:** keep broad migration behind the current viewport/Stage-C acceptance work unless the Coordinator reprioritizes from new evidence.

## MS-020 — Final authoritative AI Job Broker / stale-result protection not complete

- **Severity:** High
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit
- **Expected:** durable job IDs, immutable input revision, one heavy GPU owner, structured stages, real cancellation, isolated outputs, stale-result candidate semantics and crash recovery.
- **Actual:** structured progress/reset behavior and Stage-C identity exist, but request/backend lifecycle is not yet the full durable job architecture.
- **Attempts/fixes:** cancellation reset in v1.0.13; job progress in v1.0.17/v1.0.18; v1.0.20 added stable generation binding, end-to-end transport correlation, fail-closed save recovery, revision-bound cleanup, and now stale-safe committed Stage-C editing state.
- **Result:** the current Stage-C vertical slice has substantially stronger identity/persistence safety without creating a parallel request mechanism. Durable queue/resource ownership and crash recovery beyond current ProjectStore/backend reset behavior remain incomplete.
- **Verification:** deterministic Core tests cover generation/candidate identity, save rollback, cleanup stale-result rejection, transform transactions, immutable edit lineage and stale sculpt rejection.
- **Next action:** resume broader durable queue/resource ownership/crash-recovery work after Stage-C acceptance unless a concrete lifecycle defect blocks testing.

## MS-021 — Canonical documentation was stale/incomplete

- **Severity:** Medium project-management risk
- **Status:** IN PROGRESS
- **First observed:** 2026-09-10 autonomous-work bootstrap
- **Evidence:** older top-level documentation lagged release state; earlier handoff docs were not consistently present on release branches.
- **Expected:** a new session can recover product truth, status, issues, decisions, Coordinator direction and current baton from the repository.
- **Attempts/fixes:** canonical `PROJECT_CHARTER.md`, `PROJECT_STATUS.md`, `ISSUES.md`, `DECISIONS.md`, `HANDOFF.md` introduced and maintained; Coordinator planning now lives in `TECHNICAL_ROADMAP.md` / `COORDINATOR_LOG.md` with stable role separation.
- **Next action:** continue maintaining canonical files; reconcile misleading duplicate top-level docs only when Coordinator priority permits.

## MS-022 — Provider readiness is not qualified strongly enough

- **Severity:** High
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit; reinforced by TripoSR/Hunyuan/Z-Image failures
- **Expected:** provider registry distinguishes downloaded, installed, importable, device-tested and inference-tested, with hardware/platform/resource expectations.
- **Actual:** provider presence/routing historically overstated practical readiness; some optional providers require fragile native tooling or exceed reference hardware.
- **Attempts/fixes:** hardware routing, explicit selection failure, isolated environments, dependency preflights, runtime repair and model manifests existed before v1.0.20. v1.0.20 added persisted readiness states, lightweight import/CUDA probes for main single-mesh routes, readiness-aware Auto/explicit routing, non-blocking health status, and verified successful-inference qualification for the actual final provider with elapsed time/hardware context. Failed/cancelled jobs do not qualify or blacklist a provider; CI does not fake inference-tested status.
- **Result:** the readiness contract is materially implemented, but target GTX1080 evidence and supported/default-vs-experimental provider policy remain incomplete.
- **Verification:** v1.0.20 branch validation passed Python/provider regression coverage; real provider inference remains target-machine work.
- **Next action:** after viewport acceptance, collect target-machine inference qualification for one intended lightweight/default 3D route and use measured evidence for default/support tiers.

## MS-023 — Generated 3D result bypasses Stage-C persistence and disappears after restart

- **Severity:** Critical
- **Status:** OPEN
- **First observed:** released v1.0.20 reference-machine test on 2026-09-10
- **User reproduction:** generate/accept a 2D reference → run Hunyuan-mini 3D generation → generated mesh appears in the 3D viewport → close Miniscuplter → reopen it.
- **Actual:** the accepted/generated 2D image is restored, but the generated 3D model is gone. During the successful generation screenshot the scene contained `AI 3D — hunyuan-mini` while the Stage-C panel simultaneously reported `3D candidate: none` and Apply/Discard were disabled.
- **Expected:** successful generation creates an identity-bound Ready candidate; explicit Apply transactionally creates/advances the durable project object/revision; save/reload restores the applied mesh and transform.
- **Code evidence:** the legacy v1.0.17 installer attaches `V1017Generate3DAsync` to the same Generate button. The v1.0.20 Stage-C installer removes only the older v1.0.9 handler before attaching `V1020Generate3DAsync`; it does not disconnect `V1017Generate3DAsync`. The legacy handler can therefore acquire the shared busy state first, directly call `AddMeshObject`, and cause the Stage-C handler to return immediately on `_v1093DBusy`.
- **Consequence:** a visually successful 3D inference can bypass candidate registration, immutable MeshRevision creation, ObjectId mapping and ProjectSession persistence. The live mesh can therefore disappear on restart and does not exercise the new durable transform/sculpt/export path.
- **Related user symptom:** the viewport label said the Hunyuan mesh was selected, yet Move could not be made to reposition it above the grid. Retest Move/Rotate/Scale only after the generated object is genuinely Stage-C-applied and mapped; do not assume this was user selection error.
- **Required fix on v1.0.22:** establish one authoritative production 3D-generation button handler when the Stage-C bridge is installed; explicitly disconnect legacy generation ownership. Generated output must enter Ready/Conflict candidate state rather than being silently committed as a transient legacy scene object. Apply must be the durable transition into the editable project object.
- **Regression/acceptance:** automated wiring/audit coverage plus real flow: generate → candidate visible/reviewable → Apply → save → close app → reopen → same mesh/object/revision restored → Move/Rotate/Scale persists → cleanup/export resolve the same active revision.
- **Links:** blocks MS-018 and exposes remaining duplicate-authority risk in MS-019.

## MS-024 — Window resize leaves black seams/gaps around the application layout

- **Severity:** High UI regression
- **Status:** IN PROGRESS — CONFIRMED ON v1.0.21
- **First observed:** released v1.0.20 reference-machine screenshot on 2026-09-10
- **Actual:** resizing the application window leaves black seams/unpainted-looking strips along layout edges instead of the UI cleanly filling the resized client area.
- **Expected:** the full application layout continuously fills the client area without black seams, stale regions or exposed backing surface at supported window sizes.
- **v1.0.21 verification:** user retest confirms the black seams are **still present** after the viewport-owner changes. This is therefore not resolved by the v1.0.21 SubViewport resize fix and should be treated as a whole-window/root-layout defect, not merely a 3D render-target artifact.
- **Code direction:** inspect the root `VBoxContainer` / outer split hierarchy and window/viewport resize propagation. The root Control is created directly under `Main : Node`; verify that its anchors/offsets actually track the window client rect after runtime resize and that no minimum-size/split offset leaves exposed clear-color regions.
- **Next action:** fix forward on v1.0.22 with a responsive-root/layout invariant test where practical. Preserve the now-improved native 3D viewport ownership rather than reintroducing manual SubViewport sizing.


## MS-025 — Starter sphere is an inappropriate default object and scale reference

- **Severity:** Medium UX / workflow friction
- **Status:** OPEN
- **First observed:** released v1.0.20 reference-machine test on 2026-09-10
- **Actual:** a starter sphere is automatically created with radius 15 / height 30 while the successful Hunyuan mesh reported bounds around 1–2 units, making the starter object enormously larger and making the generated result easy to miss.
- **Expected:** a new/empty project opens as a clean modeling workspace; the first imported/generated object is framed automatically. Provider scale/unit normalization should be explicit rather than implicitly anchored to a giant starter primitive.
- **User direction:** remove the automatic starter sphere.
- **Next action:** on v1.0.22 remove automatic starter-sphere creation from launch/New Scene/recovery paths, preserve an empty usable viewport/grid, and ensure the first real object is selected/framed. Audit any code that assumes at least one scene object.

## MS-026 — In-app resource telemetry/graphs for long AI jobs

- **Severity:** Medium product/observability enhancement
- **Status:** PLANNED
- **First requested:** 2026-09-10 during reference-machine Hunyuan test
- **User need:** the user currently watches Windows Task Manager during long AI runs to understand resource use and wants useful resource graphs inside Miniscuplter.
- **First real-machine datapoint:** during a successful Hunyuan-mini run (~402 s), the supplied Task Manager snapshot showed ~97% GPU utilization, ~5.4/8.0 GB dedicated VRAM, ~5.6/16.0 GB total GPU memory including shared memory, ~5.0/15.9 GB system RAM in use, and ~73 °C GPU temperature. Treat these as snapshot values, not proven peaks.
- **Planned UX:** lightweight rolling graphs/current values for GPU utilization, dedicated VRAM, system RAM and GPU temperature; CPU as secondary. Show job/provider/stage and elapsed time alongside telemetry, and retain a compact post-job summary including observed peak resource values where available.
- **Constraints:** local-only, low overhead, approximately 1 Hz sampling is sufficient, gracefully omit unavailable sensors, and do not invent unsupported metrics. Telemetry must not materially reduce inference performance.
- **Product value:** supports MS-022 provider qualification and future evidence-based Fast/Balanced/Quality routing instead of being decorative monitoring.
- **Priority:** plan now; implement after the current correctness/persistence and viewport blockers unless minimal sampling directly helps MS-022 acceptance.

---

## Issue handling rules

1. When a new bug is found, search this file first and reuse an existing ID if it is the same root problem.
2. Append attempts/results; do not erase failed fixes.
3. A successful CI build can move a code defect to `FIXED - NEEDS USER VERIFICATION`, not automatically `RESOLVED`, when the failure was observed only in the real GUI/GPU environment.
4. Record relevant version/commit references whenever known.
5. If an issue blocks the current task but independent work exists, document the blocker and continue the highest-value unblocked work.
6. If a fix exposes a deeper architectural root cause, keep the symptom issue and link it to the architectural issue rather than hiding the history.
