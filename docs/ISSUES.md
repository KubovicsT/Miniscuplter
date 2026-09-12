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
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** repeatedly through v1.0.11–v1.0.18; reopened by released v1.0.20 reference-machine testing and reproduced again on released v1.0.25
- **Expected:** a visible, stable, readable 3D workspace exists immediately at launch; grid/axes/model/gizmo remain visually consistent across resize; model form/details are easy to inspect; the grid does not occlude geometry.
- **Historical actual:** output STL could exist while the center 3D surface was completely blank.
- **v1.0.20 evidence:** grid/floor and starter model finally rendered, but the resting grid/model were very dark, appearance changed after splitter resize settled, and the opaque floor could hide generated geometry.
- **User UX direction:** Blender-like solid-workspace hierarchy: neutral dark gray background, visible neutral grid/major lines, colored axes, light/mid neutral-gray model, readable studio lighting, and non-occluding grid semantics.
- **v1.0.21 implementation:** commits `fa381d4...`, `4d064a4...`, `6b80e14...`, `88abb5c...`, `c00bfc8...` make `SubViewportContainer.Stretch` the normal size owner; make legacy size writers defer; stop the v1.0.17 timer; remove delayed full repair on ordinary resize; replace the old tab full-repair handler with lightweight refresh; consolidate owned World3D setup; and apply neutral gray studio presentation/diagnostics.
- **v1.0.21 target-machine result:** partial success. Initial 3D viewport appearance is good, but right-panel resize changed viewport color and opaque-grid behavior remained suspect.
- **v1.0.22 fix:** final acceptance guard runs after historical installers; host resize/recovery reasserts `V1019ConfigureStudioLighting`, the opaque `Grid ground` mesh is hidden while bars/axes remain, and no new manual `SubViewport.Size` owner is introduced. Release audit and core wiring tests guard these invariants.
- **Verification history:** implementation/Core/C#/Python/geometry/release-audit validation was green in the v1.0.22 candidate, but this is a real-renderer symptom and CI was never sufficient acceptance evidence.
- **v1.0.25 reference-machine result (2026-09-11): FAILED.** User screenshot after right-panel/splitter resize shows the 3D viewport/grid presentation still darkens materially after resize. Layout fill remains present, so this evidence is MS-009 rather than the separate MS-024 client-fill seam.
- **Current code observation:** `V1022InstallViewportPresentationGuard` reacts to `ViewportHost.Resized` by reapplying studio lighting, hiding the opaque grid ground, updating the gizmo and arming the render probe. The persisted failure proves that this lightweight reassertion is not sufficient on the reference renderer. Do not add another blind delayed repaint/reassert loop without identifying which render/world/environment state actually changes across resize.
- **v1.0.26 implementation:** historical resize/world-repair owners were retired so native `SubViewportContainer.Stretch` remains the normal render-target size owner; focused ownership regressions guard against restoring direct `SubViewport.Size` writes or resize-triggered world repair.
- **Next action:** verify on released v1.0.26 using repeated right-splitter and whole-window resize on the GTX 1080 reference machine. CI is not sufficient to resolve this renderer-sensitive issue.

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
- **Result:** containment hardening is published in v1.0.19 and carried forward, but real-machine verification is required to prove no important path remains on C:.
- **Next action:** run representative 2D edit, 3D generation, reference download, geometry operation, capture, save and cancellation; inspect emitted paths/process environments.

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
- **Actual:** the bounded production path is coherent in code/tests, but the complete workflow has not yet been qualified on the target machine.
- **Attempts/fixes:** v1.0.12–v1.0.19 hardened provider, viewport, storage, geometry and update seams. v1.0.20 moved accepted baseline, generated candidate, explicit Apply/Discard, transport identity, recovery-safe saves, cleanup and exact STL export onto Core/ProjectStore and added Core-authoritative transform plus one immutable sculpt path. v1.0.22 fixes the production Generate-3D ownership seam that real-machine testing showed could bypass this durable path entirely.
- **Result:** automated state/revision semantics remain strong, and v1.0.22 now routes the production button deterministically into them. Stage-C itself remains unaccepted pending the released-build reference-machine flow.
- **Next action:** after v1.0.26 publication run the complete thin slice on the reference machine, starting with update/reopen, Repair health and Generate 3D, then candidate Apply, save/reopen, transform/sculpt, cleanup/export, storage containment and cancellation evidence.

## MS-019 — Legacy `Main.V*.cs` architecture remains authoritative

- **Severity:** High architectural risk
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit
- **Expected target:** stable domain IDs/revisions, declarative UI, transactional commands/history, clean service boundaries.
- **Actual:** legacy architecture remains authoritative outside migrated vertical slices. MS-023 proved that additive historical event ownership can still bypass a migrated Core path even when the Core path itself is correct.
- **Attempts/fixes:** Stage-B Core introduced stable IDs, immutable revisions, history, project storage and migration. v1.0.20 migrated baseline → generation → review/apply → persistence → cleanup/export plus bounded transform/sculpt authority. v1.0.22 adds a last-composed production ownership guard for the current Stage-C Generate action rather than starting a broad rewrite.
- **Result:** the current critical production seam is contained without changing the Coordinator's broader migration plan.
- **Next action:** keep broad migration behind Stage-C acceptance unless the Coordinator reprioritizes from new evidence.

## MS-020 — Final authoritative AI Job Broker / stale-result protection not complete

- **Severity:** High
- **Status:** IN PROGRESS
- **First observed:** Astra takeover audit
- **Expected:** durable job IDs, immutable input revision, one heavy GPU owner, structured stages, real cancellation, isolated outputs, stale-result candidate semantics and crash recovery.
- **Actual:** structured progress/reset behavior and Stage-C identity exist, but request/backend lifecycle is not yet the full durable job architecture.
- **Attempts/fixes:** cancellation reset in v1.0.13; job progress in v1.0.17/v1.0.18; v1.0.20 added stable generation binding, end-to-end transport correlation, fail-closed save recovery, revision-bound cleanup and stale-safe committed Stage-C editing state.
- **Result:** durable queue/resource ownership and crash recovery beyond current ProjectStore/backend reset behavior remain incomplete.
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
- **Target observation:** successful Hunyuan-mini generation took about 402 s; a Task Manager snapshot showed about 97% GPU, 5.4/8 GB dedicated VRAM, 5.0/15.9 GB RAM and 73 °C. These remain observations, not proven peaks.
- **Next action:** after Stage-C/viewport acceptance, collect repeatable target-machine qualification and use measured evidence for default/support tiers.

## MS-023 — Generated 3D result bypasses Stage-C persistence and disappears after restart

- **Severity:** Critical
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** released v1.0.20 reference-machine test on 2026-09-10
- **User reproduction:** generate/accept a 2D reference → run Hunyuan-mini 3D generation → generated mesh appears in the 3D viewport → close Miniscuplter → reopen it.
- **Actual:** the accepted/generated 2D image was restored, but the generated 3D model was gone. During the successful generation screenshot the scene contained `AI 3D — hunyuan-mini` while the Stage-C panel simultaneously reported `3D candidate: none` and Apply/Discard were disabled.
- **Expected:** successful generation creates an identity-bound Ready candidate; explicit Apply transactionally creates/advances the durable project object/revision; save/reload restores the applied mesh and transform.
- **Root cause evidence:** v1.0.17 attached `V1017Generate3DAsync` to the same Generate button. v1.0.20 removed only the older v1.0.9 handler before attaching `V1020Generate3DAsync`. The legacy handler could acquire `_v1093DBusy` first, directly call `AddMeshObject`, and cause the Stage-C handler to return without candidate registration.
- **v1.0.22 fix:** `Main.V1022Acceptance.cs` installs last and explicitly removes `V109Generate3DAsync`, `V1017Generate3DAsync` and `V1020Generate3DAsync`, then attaches exactly one `V1020Generate3DAsync`. It does not add a new generation path. Existing Stage-C Ready/Conflict candidate and explicit transactional Apply semantics remain authoritative.
- **Regression coverage:** `tools/core_logic_tests.py` guards final installer order, removal of legacy owners, exactly-one Stage-C rebound and candidate/apply/restore wiring. `tools/release_audit.py` independently guards the same production ownership invariant. Existing Core tests continue covering candidate Apply, ObjectId/revision lineage and save/reload.
- **Failed attempt preserved:** build `34518253820` at `42dd143661a81185c110e7e761d12d41ead15cf7` failed the newly added static test because it matched the words `SubViewport.Size` inside a comment rather than an assignment. Commit `0e41b78d6a109bc09515a3d8877f385f91714527` corrected the assertion to assignment syntax; subsequent Core/C#/Python/execution/geometry/release-audit validation passed.
- **Next action:** on released v1.0.22 run generate → candidate visible/reviewable → Apply → save → close/reopen → same object/active revision restored → Move/Rotate/Scale → cleanup/export. Keep this issue at FIXED - NEEDS USER VERIFICATION until that real flow passes.
- **Links:** blocks MS-018 and documents a concrete symptom of the broader MS-019 duplicate-authority risk.

## MS-024 — Window resize leaves black seams/gaps around the application layout

- **Severity:** High UI regression
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** released v1.0.20 reference-machine screenshot on 2026-09-10
- **Actual:** resizing the application window left black seams/unpainted-looking strips along layout edges instead of the UI cleanly filling the resized client area.
- **Expected:** the full application layout continuously fills the client area without black seams, stale regions or exposed backing surface at supported window sizes.
- **v1.0.21 verification:** user retest confirmed the seams remained after SubViewport-owner changes, proving this was not solely a 3D render-target issue.
- **v1.0.22 fix:** final acceptance guard listens to the top-level viewport size change and deferredly reasserts the outer VBox `FullRect` anchors/zero offsets and ExpandFill flags, plus body split ExpandFill. It deliberately does not assign `SubViewport.Size`; the v1.0.21 native Stretch render owner remains intact.
- **Regression coverage:** core wiring and release audit require the FullRect client-fill guard and prohibit a new `SubViewport.Size =` assignment in the v1.0.22 acceptance layer.
- **Next action:** retest whole-window resizing on released v1.0.22 at normal/minimized/maximized and intermediate sizes; inspect for black seams and inaccessible controls before resolving.

## MS-025 — Starter sphere is an inappropriate default object and scale reference

- **Severity:** Medium UX / workflow friction
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** released v1.0.20 reference-machine test on 2026-09-10
- **Actual:** a starter sphere was automatically created with radius 15 / height 30 while the successful Hunyuan mesh reported bounds around 1–2 units, making the generated result easy to miss and providing a misleading implicit scale reference.
- **Expected:** a new/empty project opens as a clean modeling workspace; the first imported/generated object is framed automatically. Provider scale/unit normalization should be explicit rather than anchored to a giant starter primitive.
- **User direction:** remove the automatic starter sphere.
- **v1.0.22 fix:** the final acceptance layer removes historical `Starter sphere` objects after application composition, after New Scene and after the deferred legacy `Repair 3D Viewport` action. It preserves an empty viewport/grid and existing first-real-object selection/framing behavior. This containment avoids a broader legacy rewrite while covering the live paths exposed by current composition.
- **Regression coverage:** core wiring tests and release audit require the starter/recovery guard and keep the native viewport owner intact.
- **Next action:** on released v1.0.22 verify launch, New and manual viewport repair remain empty until a real object is imported/applied; verify first real object is visible/selected/framed.

## MS-026 — In-app resource telemetry/graphs for long AI jobs

- **Severity:** Medium product/observability enhancement
- **Status:** PLANNED
- **First requested:** 2026-09-10 during reference-machine Hunyuan test
- **User need:** the user currently watches Windows Task Manager during long AI runs to understand resource use and wants useful resource graphs inside Miniscuplter.
- **First real-machine datapoint:** during a successful Hunyuan-mini run (~402 s), the supplied Task Manager snapshot showed ~97% GPU utilization, ~5.4/8.0 GB dedicated VRAM, ~5.6/16.0 GB total GPU memory including shared memory, ~5.0/15.9 GB system RAM in use, and ~73 °C GPU temperature. Treat these as snapshot values, not proven peaks.
- **Planned UX:** lightweight rolling graphs/current values for GPU utilization, dedicated VRAM, system RAM and GPU temperature; CPU as secondary. Show job/provider/stage and elapsed time alongside telemetry, and retain a compact post-job summary including observed peak resource values where available.
- **Constraints:** local-only, low overhead, approximately 1 Hz sampling is sufficient, gracefully omit unavailable sensors, and do not invent unsupported metrics. Telemetry must not materially reduce inference performance.
- **Product value:** supports MS-022 provider qualification and future evidence-based Fast/Balanced/Quality routing instead of being decorative monitoring.
- **Priority:** implement after the current correctness/persistence and viewport blockers unless minimal sampling directly helps MS-022 acceptance.

## MS-027 — Modular resizable workspace UI overhaul

- **Severity:** High product/UX acceptance
- **Status:** IN PROGRESS — CURRENT v1.0.26 USER-DIRECTED TRANCHE IMPLEMENTED, NEEDS USER VERIFICATION
- **User direction:** 2026-09-10 annotated UI specification, reinforced by 2026-09-11 v1.0.25 reference-machine screenshot and explicit acceptance corrections.
- **Scheduling rule:** MS-030 and MS-009, plus any new serious correctness/persistence/data-safety/release/Stage-C blocker, preempt MS-027. Once those are code-fixed and validated, the 2026-09-11 user-directed UI acceptance tranche is the next product-facing v1.0.26 work before integrated release hardening.
- **Primary goal:** make Miniscuplter feel like a compact modeling application rather than a large form full of explanatory text, while preserving the existing backend/state semantics.
- **v1.0.23 slice 1 implementation:** commits `f169f6e3428a64804e8778c05353b9d07a87dfe3`, `5faa1e528e6a81fb8db15942c68b3da71391743d` and `74ec73a14645071bb2742fb68f778bedd656aabd` add a final editor-only preference layer after `InstallV1022Acceptance()`. It persists the outer/body and viewport/right-panel splitter positions under `AppDataRoot.Resolve("Settings/ui_preferences.json")`, clamps restored panel widths, defaults the main UI text scale to 90%, exposes a 75–135% Settings → Interface font-scale control plus Reset Workspace Layout, and seeds reusable hover tooltips for core toolbar actions. It does not reference `ProjectStore` or own Stage-C generation/actions.
- **v1.0.23 regression coverage:** `tools/core_logic_tests.py` now guards final installer ordering, controlled-root preference storage, both splitter fields, bounded font scale, Interface settings/tooltips, replacement-safe preference writes, and absence of project/Stage-C-generation ownership from the preference layer.
- **Validation:** implementation commit `5faa1e528e6a81fb8db15942c68b3da71391743d` passed Stage-B/Core, full C# builds, Python/core/execution/geometry/release-audit validation, portable package layout/hash and installer-definition compilation. Exact code/test commit `74ec73a14645071bb2742fb68f778bedd656aabd` passed Stage-B/Core, C#, Python/core/execution/geometry and release-audit legs; packaging was still finishing when this ledger entry was written and must be checked before claiming exact-head validation.
- **Result:** first bounded modernization slice is implemented without broadening into a UI rewrite or displacing the v1.0.22 acceptance dependency. Real restart/layout/scale behavior still needs target/UI verification before the slice can be considered accepted.
- **2026-09-11 v1.0.25 reference-machine evidence:** the current UI still presents the AI command entry as a single top strip with a separate full-width action row; the top-right orientation control is only a flat letter/button box rather than an actual 3D view cube; the viewport's top-left tool overlay still consumes substantial space with letter controls plus instructional text; and the right-side 3D panel still contains persistent explanatory paragraphs. The user explicitly wants detailed explanations moved behind small circular `i` affordances shown on hover.
- **v1.0.26 tranche result:** the four explicit v1.0.25 corrections are implemented and regression-covered: multi-line scrollable command console/history, rendered interactive orientation cube, compact viewport tools, and explanatory workflow prose moved behind compact info/tooltips. Existing command/camera/tool authority is reused.
- **Next action:** verify this tranche on released v1.0.26. Keep broader scene-tree/resource/performance workspace work open for later.

### Required workspace structure

1. **Viewport tool bar**
   - Move, Rotate, Scale, Sculpt and other real viewport tools are separate icon buttons, not a dropdown.
   - Active tool is visibly highlighted.
   - Long explanatory text moves to hover tooltips/help rather than permanently occupying viewport space.

2. **Interactive view selector cube**
   - viewport-corner orientation cube rotates with the camera;
   - clickable faces snap to Front/Back/Left/Right/Top/Bottom;
   - clickable edges/corners snap to diagonal/isometric views;
   - normal orbit pivots around the currently selected scene entity when one exists.

3. **Compact information density**
   - smaller default text than the current UI;
   - user-adjustable UI/font scale in Settings;
   - unnecessary always-visible instructional paragraphs removed from viewport overlays and right-side workflow panels;
   - use small circular `i` help affordances beside relevant section/action labels; hovering them exposes the detailed explanation;
   - ordinary tool/action hover tooltips remain available where an extra `i` is unnecessary;
   - important state, errors, progress and destructive-action warnings remain directly visible.

4. **Scene hierarchy**
   - left scene area becomes a collapsible tree/hierarchy for every scene entity;
   - parent/child structure is represented;
   - tree selection and viewport selection stay synchronized;
   - this is a presentation of durable project/object identity, not a second independent scene-state model.

5. **Performance/resource panel**
   - integrate the MS-026 rolling resource telemetry here;
   - GPU utilization, dedicated VRAM, system RAM, GPU temperature and useful CPU data where available;
   - provider/job stage/elapsed time and compact peak summary.

6. **Unified AI command console**
   - replace the single-line top command strip with a compact multi-line text console, visually closer to a command terminal than a one-line form field;
   - the text area must preserve line breaks and have its own vertical scrolling when content/history exceeds the visible height;
   - keep an explicit Run action; do not make multiline editing impossible just to support submission;
   - previous commands/history are visible and reusable;
   - keyboard history navigation and explicit previous/next controls remain available;
   - contextual AI action buttons stay compact and associated with the console rather than consuming a second full-width explanatory/action band across the top;
   - action buttons invoke only real contextual operations and all buttons/typed commands route through one authoritative command/action dispatch layer. Do not create duplicate AI handlers or parallel state ownership.

### Global requirements

- Major workspace regions must be resizable where sensible.
- Persist workspace layout across restarts: splitter positions, panel dimensions, collapsed/expanded states, UI/font scale and other layout choices.
- The viewport remains the dominant visual area.
- Do not implement the overhaul as one monolithic rewrite. Deliver small reversible slices that preserve existing Stage-C behavior.
- Prefer a single current UI composition owner rather than adding another versioned overlay on top of legacy UI.
- Maintain keyboard/mouse accessibility and sensible minimum sizes at normal Windows display scaling.

### Current user-directed acceptance tranche

After MS-030 and MS-009 are code-fixed, implement these visible v1.0.25 corrections as one coordinated MS-027 tranche, in bounded commits/checkpoints:

1. multi-line scrollable AI command console/history, replacing the single-line top strip and oversized top action band while preserving one dispatch owner;
2. real interactive 3D orientation cube in the viewport corner, not a flat box of letter buttons;
3. compact top-left viewport tool controls with active-state visibility and no permanent instructional paragraph;
4. remove persistent explanatory prose from the right-side workflow tabs and replace detailed help with small circular `i` hover affordances/tooltips.

Do not broaden this tranche into scene-tree redesign, resource-graph work, new modeling features, or another UI state owner.

### Acceptance

- restart reproduces saved workspace layout and UI scale;
- major panels can be resized without black seams or viewport-state changes;
- viewport tools are directly accessible as icons;
- scene-tree and viewport selection agree;
- view cube snaps correctly and tracks camera orientation;
- orbit uses the selected entity as pivot;
- AI console is visibly multi-line, vertically scrollable, preserves line breaks/history, and actions are routed once;
- small circular `i` hover help/tooltips replace nonessential persistent explanations in the viewport and right-side panels;
- resource panel is low-overhead and local-only per MS-026;
- no Stage-C persistence, generation, transform, cleanup/export or cancellation regression.

---

## Issue handling rules

1. When a new bug is found, search this file first and reuse an existing ID if it is the same root problem.
2. Append attempts/results; do not erase failed fixes.
3. A successful CI build can move a code defect to `FIXED - NEEDS USER VERIFICATION`, not automatically `RESOLVED`, when the failure was observed only in the real GUI/GPU environment.
4. Record relevant version/commit references whenever known.
5. If an issue blocks the current task but independent work exists, document the blocker and continue the highest-value unblocked work.
6. If a fix exposes a deeper architectural root cause, keep the symptom issue and link it to the architectural issue rather than hiding the history.


## MS-028 — v1.0.23 direct viewport tool strip breaks C# build

- **Severity:** High release-blocking development regression
- **Status:** RESOLVED
- **First observed:** exact-head v1.0.23 CI on 2026-09-10 after the second bounded MS-027 slice.
- **Expected:** the direct viewport tool strip composes after UI preferences and the full editor C# target builds.
- **Actual:** broader build fails with `CS0122` because `ExtrasInstaller` calls `Main.InstallV1023ViewportToolStrip()` while that method is inaccessible due to its protection level.
- **Evidence:** build runs `34524072795` and `34527580075`; Core-foundation, Python/runtime/geometry/release-audit and packaging legs pass.
- **Impact:** current v1.0.23 HEAD is not release-ready; no further MS-027 feature slice should begin while exact-head C# validation is red.
- **Resolution:** commit `75e4990e04e273c00bf3eecc66e0ae93924b5571` made the composition entry point public. Subsequent exact-head C#/Core/Python/geometry/release-audit/packaging validation passed, and later scene-hierarchy work remained green.
- **Next action:** none for MS-028; preserve the failed-build history as regression context.

## MS-029 — Successful self-update health probe closes the updated launcher

- **Severity:** Critical release-path regression
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** 2026-09-11 user/reference-machine update to v1.0.25.
- **Root cause:** `Updater/Program.cs::VerifyLauncherStartup` killed the updated launcher from `finally` even after a successful health token and the success path did not relaunch it normally.
- **Implementation:** `9f36f44f7b792c2bbdf51cbdaa81cf72b1bebf6b` preserves a launcher after confirmed health while retaining failure/timed-out-probe termination and rollback; `e6ae3d3541de6de4bec19838e71595e4b76dbedfa` adds health-protocol regression coverage.
- **Automated evidence:** exact-head branch build/Core validation is green through current v1.0.26 planning HEAD.
- **Immutable-transition limitation:** the updater that installs v1.0.26 from published v1.0.25 is the already-installed v1.0.25 updater, so that one transition may still show the old brief-open/close behavior and require a manual launcher reopen. The v1.0.26 fix protects subsequent transitions. Do not rewrite v1.0.25 or add risky detached-process compatibility machinery solely to hide this one-time limitation.
- **Verification:** on a future update driven by the fixed updater, confirm the healthy updated launcher remains open and failed health checks still roll back/restart safely.
- **Next action:** preserve the fix through v1.0.26 integrated release hardening; no further feature work is required for MS-029 now.

## MS-030 — Packaged backend health fails after successful Runtime Repair

- **Severity:** Critical
- **Status:** FIXED - NEEDS USER VERIFICATION
- **First observed:** released v1.0.25 reference-machine testing on 2026-09-11.
- **Reproduction:** 3D generation fails after ~2 seconds at local-backend health; Repair AI Runtime can report success yet retry still fails.
- **Impact:** blocks the primary Stage-C `2D → 3D` workflow completely.
- **Attempt 1 — runtime authority convergence:** `4efd840eaa774a87c7dd8ec8aaba98479e02e266` made the editor use the backend-local repaired `.venv` and removed embedded/PATH interpreter fallback. `6dcb754b552f6254d6ab6165ecf94725f354bc2d` added a Repair health smoke using the same interpreter. `1e6ed3541de6de4bec19838e71595e4b76dbedfa` added source regressions. CI is green.
- **Coordinator end-to-end finding (2026-09-11):** Attempt 1 is incomplete. `ai_backend/app.py` defines `app = FastAPI(...)` and endpoints, but has no executable server entry point. Both `BackendLauncher` and Repair invoke it as `python app.py`, which can exit without opening port 7868. Green source/build CI therefore does not prove backend startup.
- **Second hidden risk:** Repair currently probes production port 7868. If another backend is already listening, Repair could validate the wrong process. The smoke must use an isolated port or otherwise bind health identity to the process/runtime it launched and verify expected backend version/identity.
- **v1.0.26 implementation:** editor and Repair now use the same backend-local repaired `.venv`; packaged `ai_backend/serve.py` is the canonical Uvicorn/FastAPI entry point; Repair uses an isolated loopback smoke port plus an instance token and expected backend version; editor production startup uses the same server contract on port 7868; process-tree containment and bounded startup diagnostics remain intact.
- **Verification:** actual packaged startup/health lifecycle test plus focused regression coverage and full exact-head validation; reference-machine Repair must only succeed after its own backend is healthy, and Generate 3D must then reach provider resolution/inference.
- **Next action:** on released v1.0.26, Repair AI Runtime must complete only after its isolated backend is healthy; then Generate 3D must pass health and reach provider resolution/inference on the GTX 1080 reference machine.

