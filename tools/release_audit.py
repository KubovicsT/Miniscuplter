from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SELF = Path(__file__).resolve()
EXPECTED = "1.0.16"
errors: list[str] = []


def text(path: str) -> str:
    file = ROOT / path
    if not file.is_file():
        errors.append(f"missing required file: {path}")
        return ""
    return file.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


launcher = text("Launcher/Miniscuplter.Launcher.csproj")
uproj = text("Updater/Miniscuplter.Updater.csproj")
app_project = text("Miniscuplter.csproj")
installer = text("installer/Miniscuplter.iss")
export_presets = text("export_presets.cfg")
backend = text("ai_backend/app.py")
workflow = text(".github/workflows/build.yml")
core_workflow = text(".github/workflows/core_foundation.yml")
build_release = text("build_release.ps1")
extras = text("Scripts/ExtrasInstaller.cs")
ai_feedback = text("Scripts/Main.V108AiFeedback.cs")
v109 = text("Scripts/Main.V109Experience.cs")
responsive = text("Scripts/Main.V109Responsive.cs")
workflow1011 = text("Scripts/Main.V1011Workflow.cs")
thin_slice1015 = text("Scripts/Main.V1015ThinSlice.cs")
references1016 = text("Scripts/Main.V1016References.cs")
safety1012 = text("Scripts/Main.V1012Safety.cs")
performance = text("ai_backend/performance_runtime.py")
commands = text("Scripts/Main.V096Commands.cs")
detail = text("ai_backend/detail_pipeline.py")
geometry = text("ai_backend/geometry_ops.py")
geometry_tests = text("tools/geometry_regression_tests.py")
manager = text("ai_backend/model_manager.py")
ext = text("ai_backend/model_manager_v105.py")
downloads = text("ai_backend/model_downloads.py")
requirements = text("ai_backend/requirements.txt")
router = text("ai_backend/model_router.py")
caps = text("ai_backend/model_capabilities.py")
special = text("ai_backend/specialist_3d_v105.py")
modern = text("ai_backend/modern_image.py")
sdxl = text("ai_backend/sdxl_image.py")
sd21 = text("ai_backend/local_image.py")
partcrafter = text("ai_backend/partcrafter_shape.py")
triposr = text("ai_backend/triposr_shape.py")
updater = text("Updater/Program.cs")
updates = text("Launcher/ApplicationUpdateService.cs")
backend_launcher = text("Scripts/BackendLauncher.cs")
launcher_job = text("Launcher/OwnedChildProcessJob.cs")
model_service = text("Launcher/ModelService.cs")
model_dialog = text("Launcher/ModelOperationDialog.cs")
runtime_setup_service = text("Launcher/RuntimeSetupService.cs")
runtime_dialog = text("Launcher/RuntimeSetupDialog.cs")
launcher_program = text("Launcher/Program.cs")
launcher_form = text("Launcher/LauncherForm.cs")
runtime_setup_script = text("setup_ai_backend.bat")
release_polish = text("Scripts/Main.V100Release.cs")
core_ids = text("Core/Ids.cs")
core_geometry = text("Core/GeometryTypes.cs")
core_models = text("Core/ProjectModels.cs")
core_history = text("Core/ProjectHistory.cs")
core_codec = text("Core/MeshBinaryCodec.cs")
core_store = text("Core/ProjectStore.cs")
core_migration = text("Core/LegacyProjectImporter.cs")
core_tests = text("Core.Tests/Program.cs")
core_logic_tests = text("tools/core_logic_tests.py")

# Release identity must agree everywhere users/tools can observe it.
require(f"<Version>{EXPECTED}</Version>" in launcher, "launcher version mismatch")
require(f"<Version>{EXPECTED}</Version>" in uproj, "updater version mismatch")
require(f"<Version>{EXPECTED}</Version>" in app_project, "Godot C# assembly version mismatch")
require(f'#define MyAppVersion "{EXPECTED}"' in installer, "installer version mismatch")
require(
    f'application/file_version="{EXPECTED}.0"' in export_presets and
    f'application/product_version="{EXPECTED}.0"' in export_presets,
    "Windows exported file version mismatch",
)
require(f'APP_VERSION="{EXPECTED}"' in backend and '"version":APP_VERSION' in backend, "backend version mismatch")
require(f"Ready — Miniscuplter v{EXPECTED}" in release_polish, "editor displayed version mismatch")
require("Assembly.GetExecutingAssembly()" in launcher_program and 'form.Text = $"Miniscuplter Launcher v{version}"' in launcher_program, "launcher title is not derived from assembly version")
require("<OutputType>WinExe</OutputType>" in uproj, "updater still opens a console window")

# v1.0.8: concept AI jobs must not look like unexplained hangs.
require("InstallV108AiFeedback" in extras, "v1.0.8 AI feedback installer missing")
for token in ("AI status:", "Generating Concept…", "Cancel AI Job", "elapsed", "V108ShowAiError", "GenerateConceptAsync"):
    require(token in ai_feedback, f"AI job feedback missing: {token}")
require("_ai.CancelCurrentRequest()" in ai_feedback, "AI job cancel does not reach AIClient")
require("File.Exists(_lastEditedImage)" in ai_feedback, "AI success does not verify produced image")
require("AI generation failed" in ai_feedback and "AcceptDialog" in ai_feedback, "AI failures are not surfaced visibly")

# v1.0.9: central settings, visible viewport, large preview, reference repair and 3D feedback.
require("InstallV109Experience" in extras, "v1.0.9 experience installer missing")
for token in ("Settings", "GPU / VRAM", "VRAM allocator ceiling", "QUALITY PRESET", "ACTIVE MODEL ROUTING"):
    require(token in v109, f"v1.0.9 settings surface missing: {token}")
require("Viewport Grid v1.0.9" in v109 and "ImmediateMesh" in v109 and "Minor grid" in v109 and "Major grid" in v109, "visible 3D grid missing")
require("OwnWorld3D = true" in v109 and "sub.Size" in v109, "viewport sizing/world visibility guard missing")
require("Open Large 2D Preview" in v109 and "FitV109Preview" in v109 and "Open Externally" in v109, "large zoomable 2D preview missing")
require("Miniscuplter/1.0.9" in v109 and "Wikimedia Commons" in v109 and "Reference search FAILED" in v109, "Wikimedia reference-search 403/visibility repair missing")
for token in ("3D status:", "Generating 3D Part…", "Cancel 3D Job", "Validating generated STL", "Importing mesh into scene", "elapsed"):
    require(token in v109, f"2D-to-3D feedback missing: {token}")
require("_ai.CancelCurrentRequest()" in v109 and "Generate3DRoutedAsync" in v109, "2D-to-3D job is not cancellable/routed")
require('"mode": "auto"' in performance and '"vram_target_fraction": 0.85' in performance, "GPU performance policy defaults missing")
require("set_per_process_memory_fraction" in sdxl and "enable_model_cpu_offload" in sdxl and "enable_sequential_cpu_offload" in sdxl, "VRAM-first SDXL tiered policy missing")
require('mode == "fast"' in sdxl and 'mode == "balanced"' in sdxl, "SDXL performance modes missing")

# v1.0.10: responsive editor layout keeps the viewport and lower AI controls reachable.
require("InstallV109ResponsiveLayout" in extras, "responsive v1.0.10 layout installer missing")
for token in ("WrapV109MainTabsForScrolling", "SyncV109ResponsiveSplit", "SyncV109SubViewportToHost", "Resized +=", "ScrollContainer"):
    require(token in responsive, f"responsive layout guard missing: {token}")
require("_aiPreview.CustomMinimumSize" in responsive and "_prompt.CustomMinimumSize" in responsive, "AI panel fixed-size pressure was not reduced")
require("_v109ResponsiveSubViewport.Size = target" in responsive, "SubViewport does not resize with its host")

# v1.0.11: the user sees one four-step workflow, not implementation/version archaeology.
require("InstallV1011Workflow" in extras, "v1.0.11 workflow consolidation installer missing")
for token in ('WorkflowPage("2D")', 'WorkflowPage("3D")', 'WorkflowPage("Rig & Pose")', 'WorkflowPage("Cleanup & Export")'):
    require(token in workflow1011, f"four-step workflow tab missing: {token}")
require("Accept Current Image as Baseline" in workflow1011 and "Accept a 2D Baseline First" in workflow1011, "explicit 2D baseline approval gate missing")
require("MoveV1011SculptControlsTo3D" in workflow1011 and "SCULPTING" in workflow1011, "sculpt controls were not moved into the 3D workflow")
require("AttachV1011AdvancedSettings" in workflow1011 and "Advanced Quality" in workflow1011 and "Advanced AI Models" in workflow1011 and "Files & Locations" in workflow1011, "legacy advanced controls are not preserved in Settings")
require("SemanticV1011Heading" in workflow1011 and "Regex.Replace" in workflow1011 and "AI PATCH WORKFLOW" in workflow1011, "version/milestone headings are not normalized away")
require("RepairV1011ViewportAtLaunch" in workflow1011 and "FinishV1011ViewportRepair" in workflow1011 and "AddV1011Grid" in workflow1011, "launch-time grid/viewport repair missing")
require("plane.Visible = false" in workflow1011 and 'Name = "Viewport Grid"' in workflow1011 and "_camera.Current = false" in workflow1011 and "_camera.Current = true" in workflow1011, "grid launch visibility hardening incomplete")
require("InstallV1011Workflow();" in extras and extras.index("InstallV1011Workflow();") < extras.index("InstallV109ResponsiveLayout();"), "workflow must be built before responsive tab wrapping")

# v1.0.12 Stage-A bridge: delivery, guarded persistence/export and real geometry correctness.
require("InstallV1012SafetyBridge" in extras and "InstallV1012SafetyBridge();" in extras, "v1.0.12 safety bridge installer missing")
require(extras.index("InstallV109ResponsiveLayout();") < extras.index("InstallV1012SafetyBridge();"), "v1.0.12 safety bridge must patch the final composed UI")
for token in ("OpenV099SafeExportDialog", "SafeV095SaveProject", "V099ProjectRoot", "_v055AutosaveTimer.Stop"):
    require(token in safety1012, f"guarded export/autosave bridge missing: {token}")
require("_vertices_are_finite" in geometry and "np.isfinite" in geometry and "_analysis_mesh" in geometry and "merge_vertices" in geometry, "geometry finite/topology normalization fix missing")
require("source_vertices" in geometry and "topology_representation" in geometry, "geometry analysis does not expose normalized-topology semantics")
require("rtree>=1.3.0,<2.0" in requirements, "ray/thickness spatial-index dependency is not packaged")
for token in ("closed_cube.stl", 'report["open_edges"] == 0', "voxel_remesh", "thickness_map"):
    require(token in geometry_tests, f"real geometry regression coverage missing: {token}")
require("_code_dir()" in triposr and "from tsr.utils" in triposr and "mesh = meshes[0]" in triposr and "meshes[0][0]" not in triposr, "TripoSR adapter source/output contract fix missing")
require("ManagedTopLevel" in updater and "UpdateJournal" in updater and "RecoverInterruptedTransactions" in updater, "updater ownership/journal recovery missing")
require("VerifyLauncherStartup" in updater and "launcher-healthy" in updater and "--update-health-token" in launcher_program, "post-update launcher health acknowledgement missing")
require("backupComplete" in updater and "HasManagedContent" in updater and "RollbackManagedUpdate" in updater, "partial-backup rollback distinction missing")
require("Windows cannot reliably hash" in updates and "await output.FlushAsync" in updates, "update download writer lifetime guard missing")
require("Miniscuplter-Launcher/{version}" in updates, "launcher update User-Agent is still hard-coded to an old release")

# v1.0.13 Stage-B foundation: stable identity, immutable revisions, transactional history and migration harness.
require('ProjectReference Include="Core\\Miniscuplter.Core.csproj"' in app_project, "editor does not reference the replacement core project")
for token in ("ProjectId", "ObjectId", "RevisionId", "CandidateId", "TransactionId"):
    require(token in core_ids, f"strong domain identity missing: {token}")
require("CurrentSchemaVersion = 7" in core_models and "ActiveMeshRevisionId" in core_models and "SelectionBinding" in core_models and "RestMeshRevisionId" in core_models, "revision-bound Stage-B domain graph incomplete")
require("ProjectTransaction" in core_history and "Before" in core_history and "After" in core_history and "ApplyCandidate" in core_history and "CandidateStatus.Conflict" in core_history, "full-state history/stale-result protection missing")
require('Encoding.ASCII.GetBytes("MSHM")' in core_codec and "Indices" in core_codec and "Flush(flushToDisk: true)" in core_codec, "indexed durable internal mesh codec missing")
require("ProjectExtension = \".msculpt2\"" in core_store and "RecoveryCheckpointLimit" in core_store and "VerifyDurableAssetsAsync" in core_store and "SHA-256" in core_store, "atomic versioned project store incomplete")
require("schema is < 1 or > 6" in core_migration and "legacy_manifest.json" in core_migration and "migration_log.json" in core_migration and "LegacyStlReader" in core_migration, "legacy schema 1–6 migration harness incomplete")
require("stale candidate" in core_tests.lower() and "legacy path traversal" in core_tests.lower() and "core foundation tests passed" in core_tests, "Stage-B core regression tests incomplete")
require("Core.Tests/Miniscuplter.Core.Tests.csproj" in core_workflow and "dotnet run" in core_workflow, "Stage-B foundation tests are not wired into CI")

# v1.0.14: modern 2D models must respect low-VRAM policy instead of relying on whole-model offload.
for token in ("_offload_strategy", "enable_sequential_cpu_offload", "set_per_process_memory_fraction", "low_cpu_mem_usage", "_generation_size", "force_safe", "retry=True"):
    require(token in modern, f"modern image low-VRAM guard missing: {token}")
require('component == "z-image-turbo"' in modern and "vram_mb <= 10240" in modern and 'return "sequential"' in modern, "Z-Image 8-10GB route is not forced to sequential offload")
require("max_sequence_length" in modern and "size = min(size, 768)" in modern, "Z-Image 8GB activation/RAM guard missing")
require("ran out of memory even in Miniscuplter low-VRAM mode" in modern, "modern image OOM retry diagnostics missing")
require("Z-Image 8GB must use sequential offload" in core_logic_tests and "Z-Image OOM retry canvas guard" in core_logic_tests, "Z-Image low-VRAM routing is not regression-tested")
require("sequential CPU offload" in caps and "16GB system RAM is tight" in caps, "Z-Image hardware limitation is not surfaced in capabilities")


# v1.0.15: the Stage-C thin slice must be usable rather than merely present as legacy controls.
require("InstallV1015ThinSlice();" in extras and extras.index("InstallV1013CancellationRecovery();") < extras.index("InstallV1015ThinSlice();"), "v1.0.15 thin-slice UX must install last")
for token in ("V1015ImageCanvas", "AI Edit Selected 2D Region", "AI Edit Whole 2D Image", "SelectionPixels", "CurrentV1015ImageSource", "SetStartingImage(cached)"):
    require(token in thin_slice1015, f"v1.0.15 2D canvas workflow missing: {token}")
require("CaptureView();" not in thin_slice1015, "v1.0.15 2D image editing still depends on a 3D viewport capture")
for token in ("REFERENCE IMAGES", "Search Reference Images", "Use as 2D Source", "thumburl", "descriptionurl"):
    require(token in thin_slice1015, f"v1.0.15 visible reference browser missing: {token}")
require("Mesh.PrimitiveType.Triangles" in thin_slice1015 and "AddV1015GridBars" in thin_slice1015 and "Grid ground" in thin_slice1015, "v1.0.15 driver-robust triangle floor grid missing")
for dep in ("opencv-python-headless", "pymeshlab", "pygltflib", "xatlas", "ninja", "pybind11"):
    require(dep in requirements, f"Hunyuan3D 2mini runtime dependency missing: {dep}")
require("_require_hunyuan_mini_runtime" in special and "Repair AI Runtime" in special, "Hunyuan 2mini runtime preflight/repair guidance missing")
require('(req.provider or "auto").lower()!="auto"' in backend and "automatic fallback to" in backend and "fallback_from" in backend, "Auto 3D provider fallback is missing or can hide explicit provider failures")


# v1.0.16: reference discovery must search more than Wikimedia while preserving the local 2D workflow.
require("InstallV1016ReferenceSearch();" in extras and extras.index("InstallV1015ThinSlice();") < extras.index("InstallV1016ReferenceSearch();"), "v1.0.16 multi-source reference browser must replace the v1.0.15 surface last")
for token in ("api.openverse.org/v1/images/", "All Sources (Openverse + Wikimedia)", "SearchV1016OpenverseAsync", "SearchV1016WikimediaAsync", "RunV1016ProviderAsync", "Task.WhenAll", "Partial failure"):
    require(token in references1016, f"v1.0.16 multi-source reference search missing: {token}")
for token in ("mature=false", "watermarked", "Source:", "License:", "Open Source", "Use as 2D Source"):
    require(token in references1016, f"v1.0.16 reference-result safety/attribution surface missing: {token}")
require("SetStartingImage(cached)" in references1016 and "SyncV1015CanvasSource(current)" in references1016, "v1.0.16 references do not feed the established 2D source workflow")
require("ContentLength" in references1016 and "maxBytes" in references1016 and "Image.LoadFromFile(path)" in references1016, "v1.0.16 remote-image size/decoding guards missing")

# SDXL/runtime repair must identify the phase, self-heal package corruption and never silently run on CPU when NVIDIA hardware exists.
require("_require_consistent_cuda" in sdxl and "torch.cuda.is_available()" in sdxl, "SDXL CUDA consistency guard missing")
require("SDXL model loading failed before inference" in sdxl, "SDXL model-load diagnostics missing")
require("SDXL inference failed after the model load stage" in sdxl, "SDXL inference diagnostics missing")
require("saving the PNG failed" in sdxl, "SDXL output-save diagnostics missing")
require("Verifying PyTorch CUDA access" in runtime_setup_script and "The AI runtime will not be marked ready" in runtime_setup_script, "runtime setup does not verify usable NVIDIA CUDA")
require('.venv\\Scripts\\python.exe -m pip --version' in runtime_setup_script and 'set "REBUILD_VENV=1"' in runtime_setup_script, "runtime Repair does not detect a broken pip/venv")
require("Rebuilding .venv only; AI models and persistent download caches are preserved" in runtime_setup_script and "rmdir /s /q .venv" in runtime_setup_script, "runtime Repair does not safely rebuild a corrupt disposable venv")
require("torch.utils.data.datapipes.iter.sharding" in runtime_setup_script and "--force-reinstall" in runtime_setup_script, "runtime Repair does not verify/reinstall structurally corrupt PyTorch")

# Cross-version editor / geometry integration seams.
require('FindChild("Model", true, false)' in extras and 'modelTab.Name = "Print"' in extras, "Model/Print compatibility repair missing")
require('case "/rig": await SafeV095GenerateRigAsync' in commands, "command-palette rig bypasses guarded rig generation")
require('voxel_remesh([source_path,patch_path],str(out),pitch)' in detail, "detail apply violates voxel_remesh path contract")
require("pitch < .04 || pitch > 5.0" in commands, "remesh range mismatch")
require("min(int(max_samples), 100000)" in geometry and "structurally_valid" in geometry and "np.searchsorted" in geometry, "geometry safety/scalability regression")

# Hardware-aware model matrix and deterministic, verified model downloads.
for cid in ("z-image-turbo", "qwen-image-2512", "qwen-image-edit", "sf3d", "spar3d", "hunyuan2mini", "trellis2", "partpacker"):
    require(cid in caps and cid in ext, f"provider not registered/installable: {cid}")
for provider in ("zimage", "qwen", "qwen-edit", "sf3d", "spar3d", "hunyuan-mini", "trellis2", "partpacker"):
    require(provider in router, f"router missing provider {provider}")
require("role_options" in router and "hardware-aware auto route" in router, "hardware-aware routing missing")
require("enable_model_cpu_offload" in modern and "enable_sequential_cpu_offload" in modern, "modern image providers lack tiered offload")
require("--low-vram-mode" in special and "MINISCULPTER_TRELLIS2_COMMAND" in special, "specialist low-VRAM/external-runtime integration missing")
require("_swap_staged" in manager and "_swap_staged" in ext, "transactional model staging missing")
require('hf_xet==1.6.0' in requirements, "Hugging Face Xet transport is not pinned")
require("selected_manifest" in downloads and "files_metadata=True" in downloads and "size mismatch" in downloads, "model downloads are not verified against upstream metadata")
require("prepare_stage" in downloads and 'f"{component_id}-partial"' in downloads and "_prune_unselected" in downloads and "_stage_rank" in downloads, "resumable/deduplicated model staging regression")
require('"text_encoder_2/model.fp16.safetensors"' in ext and '"unet/diffusion_pytorch_model.fp16.safetensors"' in ext and '"*.safetensors"' not in ext, "SDXL audited fp16 manifest regression")
require('variant="fp16"' in sdxl and "local_files_only=True" in sdxl, "SDXL does not enforce audited local fp16 payload")
require('variant="fp16"' in sd21 and "local_files_only=True" in sd21, "SD2.1 does not enforce audited local fp16 payload")
require("directory_size(stage, exclude_stage_meta=True)" in ext and "mm._component_files_valid = _component_files_valid_v105" in ext, "v1.x model resume/validation regression")

# Launcher model/runtime operations must be visible, cancellable and contained.
require("CancellationTokenSource" in model_dialog and "RequestCancel" in model_dialog and "Partial stage" in model_dialog, "model operation dialog is not safely cancellable/resumable")
require("ResumeAvailable" in model_service and "resume_available" in model_service and "Resume selected" in launcher_form, "launcher model resume UI missing")
require("RedirectStandardOutput = true" in runtime_setup_service and "RedirectStandardError = true" in runtime_setup_service and "Kill(entireProcessTree: true)" in runtime_setup_service, "runtime setup is not streamed/cancellable")
require("RichTextBox" in runtime_dialog and "RequestCancel" in runtime_dialog and "cached downloads" in runtime_dialog.lower(), "runtime setup progress dialog missing")
require("JobObjectLimitKillOnJobClose" in backend_launcher and "AssignProcessToJobObject" in backend_launcher, "editor backend process containment missing")
require("JobObjectLimitKillOnJobClose" in launcher_job and "AssignProcessToJobObject" in launcher_job and "OwnedChildProcessJob.Dispose" in launcher_program, "launcher child-process containment missing")

# Application self-update: verified, resumable, storage-aware and data preserving.
require("releases?per_page=100" in updates and "Installable" in updates, "launcher stable-release discovery missing")
require("RangeHeaderValue" in updates and "update-cache" in updates and ".partial" in updates, "application ZIP resume missing")
require("VerifyPackageFileAsync" in updates and "AssetSize" in updates, "application size/hash verification missing")
require("UpdateCacheCandidates" in updates and "DriveInfo" in updates and "ResolveUpdateCacheAsync" in updates, "launcher update cache is not storage-aware")
require("BuildPreserveSet" in updater and '"AIData"' in updater and '"Runtime"' in updater, "updater persistent-data preservation missing")
require("ParkPreservedNested" in updater and "RestoreParkedNested" in updater and '".venv"' in updater, "updater nested runtime preservation missing")
require("GetExpandedSize" in updater and "EnsureFreeSpace" in updater and "MoveManagedTreeToBackup" in updater and "InstallManagedTreeFromStage" in updater, "updater low-disk transactional flow regression")
require("VerifySha256" in updater and "ValidateReleaseManifest" in updater, "updater independent package verification missing")
require("release.json" in build_release and "Miniscuplter-win-x64.zip.sha256" in build_release, "release package metadata/SHA sidecar missing")

# Intermediate version-branch commits are validation-only. A final explicit v1.x tag is the sole release trigger.
require("tags: [ 'v*' ]" in workflow, "version-tag workflow trigger missing")
require("refs/tags/v1." in workflow and "full-windows-release" in workflow, "full Windows release is not v1.x tag-gated")
require("refs/tags/v1." in workflow and "publish-release" in workflow and "gh release create" in workflow, "GitHub Release publication is not tag-gated")
require("refs/heads/v1." not in workflow, "intermediate version-branch pushes can still publish releases")
require("geometry_regression_tests.py" in workflow, "real geometry regression tests are not in CI")

require('"manifest.json"' in partcrafter and 'data.get("parts")' in partcrafter and "mesh.area" in partcrafter, "PartCrafter output contract/guard missing")

# No unfinished implementation markers in release code.
for path in [
    p for p in ROOT.rglob("*")
    if p.is_file() and p.resolve() != SELF and p.suffix.lower() in {".cs", ".py", ".ps1", ".bat", ".iss"} and ".git" not in p.parts
]:
    for number, line in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
        if re.search(r"\b(TODO|FIXME|HACK|PLACEHOLDER)\b", line, re.I):
            errors.append(f"unfinished marker {path.relative_to(ROOT)}:{number}: {line.strip()[:100]}")

if errors:
    print(f"v{EXPECTED} release audit FAILED:")
    for error in errors:
        print(" -", error)
    sys.exit(1)
print(f"v{EXPECTED} release audit passed")
