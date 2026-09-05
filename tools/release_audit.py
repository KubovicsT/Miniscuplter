from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SELF = Path(__file__).resolve()
EXPECTED = "1.0.6"
errors: list[str] = []


def text(path: str) -> str:
    file = ROOT / path
    if not file.is_file():
        errors.append(f"missing required file: {path}")
        return ""
    return file.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition: errors.append(message)


launcher = text("Launcher/Miniscuplter.Launcher.csproj")
uproj = text("Updater/Miniscuplter.Updater.csproj")
app_project = text("Miniscuplter.csproj")
installer = text("installer/Miniscuplter.iss")
export_presets = text("export_presets.cfg")
backend = text("ai_backend/app.py")
workflow = text(".github/workflows/build.yml")
build_release = text("build_release.ps1")
extras = text("Scripts/ExtrasInstaller.cs")
commands = text("Scripts/Main.V096Commands.cs")
detail = text("ai_backend/detail_pipeline.py")
geometry = text("ai_backend/geometry_ops.py")
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
updater = text("Updater/Program.cs")
updates = text("Launcher/ApplicationUpdateService.cs")
backend_launcher = text("Scripts/BackendLauncher.cs")
launcher_job = text("Launcher/OwnedChildProcessJob.cs")
model_service = text("Launcher/ModelService.cs")
model_dialog = text("Launcher/ModelOperationDialog.cs")
runtime_setup = text("Launcher/RuntimeSetupService.cs")
runtime_dialog = text("Launcher/RuntimeSetupDialog.cs")
launcher_program = text("Launcher/Program.cs")
launcher_form = text("Launcher/LauncherForm.cs")

require(f"<Version>{EXPECTED}</Version>" in launcher, "launcher version mismatch")
require(f"<Version>{EXPECTED}</Version>" in uproj, "updater version mismatch")
require(f"<Version>{EXPECTED}</Version>" in app_project, "Godot C# assembly version mismatch")
require(f'#define MyAppVersion "{EXPECTED}"' in installer, "installer version mismatch")
require('application/file_version="1.0.6.0"' in export_presets and 'application/product_version="1.0.6.0"' in export_presets, "Windows exported file version mismatch")
require('APP_VERSION="1.0.6"' in backend and '"version":APP_VERSION' in backend, "backend version mismatch")
require("v1.0.6" in launcher_form, "launcher UI version mismatch")

# Cross-version editor integration fixes discovered during the v1.0.5 audit.
require('FindChild("Model", true, false)' in extras and 'modelTab.Name = "Print"' in extras, "Model/Print compatibility repair missing")
require('case "/rig": await SafeV095GenerateRigAsync' in commands, "command-palette rig bypasses guarded rig generation")
require('voxel_remesh([source_path,patch_path],str(out),pitch)' in detail, "detail apply still violates voxel_remesh path contract")
require("result.export(out)" not in detail.split("def apply_detail", 1)[-1], "detail apply still treats voxel_remesh path as a mesh object")

require("/remesh selection" not in commands, "unfinished remesh-selection command exposed")
require("pitch < .04 || pitch > 5.0" in commands, "remesh range mismatch")
require("min(int(max_samples), 100000)" in geometry, "thickness cap regression")
require("structurally_valid" in geometry, "canonical geometry validity missing")
require("np.searchsorted" in geometry, "scalable intersection sweep missing")

for cid in ("z-image-turbo", "qwen-image-2512", "qwen-image-edit", "sf3d", "spar3d", "hunyuan2mini", "trellis2", "partpacker"):
    require(cid in caps and cid in ext, f"v1.x provider not registered/installable: {cid}")
for provider in ("zimage", "qwen", "qwen-edit", "sf3d", "spar3d", "hunyuan-mini", "trellis2", "partpacker"):
    require(provider in router, f"router missing provider {provider}")
require("role_options" in router and "hardware-aware auto route" in router, "hardware-aware routing missing")
require("enable_model_cpu_offload" in modern, "modern image providers lack offload")
require("--low-vram-mode" in special, "SPAR3D low-VRAM route missing")
require("MINISCULPTER_TRELLIS2_COMMAND" in special, "TRELLIS.2 Linux/WSL bridge missing")
require("process_image" in special and "process_3d" in special, "PartPacker official inference adapter missing")
require("_swap_staged" in manager and "_swap_staged" in ext, "transactional model staging missing")
require('"pip", "check"' in ext, "isolated specialist environments are not dependency-checked")

require('hf_xet==1.6.0' in requirements, "Hugging Face Xet transport is not pinned in managed runtime")
require("selected_manifest" in downloads and "files_metadata=True" in downloads and "size mismatch" in downloads, "model downloads are not verified against upstream file metadata")
require("prepare_stage" in downloads and 'f"{component_id}-partial"' in downloads and "Recovered interrupted v1.0.5 stage" in downloads, "deterministic resumable model staging missing")
require("_prune_unselected" in downloads, "old over-broad model payload cleanup missing")
require("_stage_rank" in downloads and "Removing redundant interrupted legacy stage" in downloads, "legacy retry stages are not deduplicated safely")
require('"sdxl-base"' in ext and '"text_encoder_2/model.fp16.safetensors"' in ext and '"unet/diffusion_pytorch_model.fp16.safetensors"' in ext, "SDXL fp16 manifest missing")
require('"*.safetensors"' not in ext, "broad root safetensors model download pattern reintroduced")
require('variant="fp16"' in sdxl and "local_files_only=True" in sdxl, "SDXL does not enforce audited local fp16 payload")
require('variant="fp16"' in sd21 and "local_files_only=True" in sd21, "SD2.1 does not enforce audited local fp16 payload")
require('"hunyuan3d-dit-v2-mini/model.fp16.safetensors"' in ext and '"hunyuan3d-vae-v2-mini' not in ext, "Hunyuan2mini manifest is not limited to its self-contained fp16 shape checkpoint")
require('"hunyuan3d-dit-v2-1/model.fp16.ckpt"' in ext and '"hunyuan3d-vae-v2-1' not in ext, "Hunyuan3D 2.1 manifest downloads a redundant standalone VAE")
require("directory_size(stage, exclude_stage_meta=True)" in ext, "resume disk preflight does not credit already staged data")
require("mm._component_files_valid = _component_files_valid_v105" in ext, "component validation is not aligned to audited v1.x layouts")
require('"sd21": 2.6' in ext and '"partcrafter": 4.8' in ext and '"clipseg-smart-select": 0.6' in ext, "audited model payload estimates drifted from selected manifests")
require(special.count('"--pretrained-model"') >= 2, "SF3D/SPAR3D runtime can bypass installed local weights")
require("use_safetensors=True" in special, "Hunyuan2mini does not enforce selected safetensors weights")

require("CancellationTokenSource" in model_dialog and "RequestCancel" in model_dialog and "Partial stage" in model_dialog, "model operation dialog is not safely cancellable/resumable")
require("ResumeAvailable" in model_service and "resume_available" in model_service and "resume_action" in model_service, "launcher does not expose interrupted model stages")
require("Resume selected" in launcher_form and "interrupted model operation" in launcher_form, "launcher does not offer interrupted download resume")
require("RedirectStandardOutput = true" in runtime_setup and "RedirectStandardError = true" in runtime_setup and "CancellationToken" in runtime_setup and "Kill(entireProcessTree: true)" in runtime_setup, "AI runtime setup service is not streamed/cancellable")
require("RichTextBox" in runtime_dialog and "RequestCancel" in runtime_dialog and "RepairAsync" in runtime_dialog and "cached downloads" in runtime_dialog.lower(), "AI runtime setup progress dialog missing")
require("RuntimeSetupDialog" in launcher_form, "launcher does not surface the AI runtime setup progress dialog")

require('"manifest.json"' in partcrafter and 'data.get("parts")' in partcrafter, "PartCrafter manifest contract missing")
require("mesh.area" in partcrafter and "mesh.extents" in partcrafter, "PartCrafter degenerate output guard missing")

# Application self-update must be installable from a public release and preserve expensive data.
require("releases?per_page=100" in updates and "Installable" in updates, "launcher does not discover the newest stable release safely")
require("RangeHeaderValue" in updates and "update-cache" in updates and ".partial" in updates, "application ZIP downloads are not resumable")
require("VerifyPackageFileAsync" in updates and "SHA-256" in updates and "AssetSize" in updates, "launcher does not enforce update size/hash integrity")
require('psi.ArgumentList.Add("--data-root")' in updates and 'psi.ArgumentList.Add("--sha256")' in updates and 'psi.ArgumentList.Add("--version")' in updates, "staged updater is not given preservation/integrity metadata")
require("_updatePromptShown" in launcher_form and "await ApplyApplicationUpdateAsync()" in launcher_form, "launcher does not automatically offer a discovered update on open")
require("BuildPreserveSet" in updater and '"AIData"' in updater and '"Runtime"' in updater, "updater does not preserve persistent top-level data")
require("ParkPreservedNested" in updater and "RestoreParkedNested" in updater and '".venv"' in updater and '".runtime-cache"' in updater, "updater does not preserve expensive nested AI runtime data")
require("VerifySha256" in updater and "ValidateReleaseManifest" in updater and '"release.json"' in updater, "updater does not independently verify package digest/version")
require("release.json" in build_release and "Miniscuplter-win-x64.zip.sha256" in build_release, "release package metadata/SHA sidecar missing")
require("publish-release" in workflow and "gh release create" in workflow and "Miniscuplter-win-x64.zip.sha256" in workflow, "CI does not publish stable self-update assets")

require("JobObjectLimitKillOnJobClose" in backend_launcher and "AssignProcessToJobObject" in backend_launcher and "Kill(entireProcessTree: true)" in backend_launcher, "editor AI backend is not guaranteed process-tree cleanup on close")
require("JobObjectLimitKillOnJobClose" in launcher_job and "AssignProcessToJobObject" in launcher_job, "launcher-owned subprocess kill-on-close job missing")
require("OwnedChildProcessJob.Start" in model_service and "OwnedChildProcessJob.Start" in runtime_setup, "launcher child processes bypass lifetime job")
require("OwnedChildProcessJob.Dispose" in launcher_program, "launcher does not close child-process lifetime job")

for path in [p for p in ROOT.rglob("*") if p.is_file() and p.resolve() != SELF and p.suffix.lower() in {".cs", ".py", ".ps1", ".bat", ".iss"} and ".git" not in p.parts]:
    for number, line in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
        if re.search(r"\b(TODO|FIXME|HACK|PLACEHOLDER)\b", line, re.I):
            errors.append(f"unfinished marker {path.relative_to(ROOT)}:{number}: {line.strip()[:100]}")

if errors:
    print(f"v{EXPECTED} release audit FAILED:")
    for error in errors: print(" -", error)
    sys.exit(1)
print(f"v{EXPECTED} release audit passed")
