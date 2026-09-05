from __future__ import annotations
import re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];SELF=Path(__file__).resolve();EXPECTED="1.0.5";errors=[]
def text(p):
    q=ROOT/p
    if not q.is_file():errors.append(f"missing required file: {p}");return ""
    return q.read_text(encoding="utf-8")
def require(c,m):
    if not c:errors.append(m)
launcher=text("Launcher/Miniscuplter.Launcher.csproj");uproj=text("Updater/Miniscuplter.Updater.csproj");installer=text("installer/Miniscuplter.iss");backend=text("ai_backend/app.py");workflow=text(".github/workflows/build.yml");commands=text("Scripts/Main.V096Commands.cs");core=text("Scripts/Main.cs");extras=text("Scripts/ExtrasInstaller.cs");geometry=text("ai_backend/geometry_ops.py");manager=text("ai_backend/model_manager.py");ext=text("ai_backend/model_manager_v105.py");downloads=text("ai_backend/model_downloads.py");requirements=text("ai_backend/requirements.txt");router=text("ai_backend/model_router.py");caps=text("ai_backend/model_capabilities.py");special=text("ai_backend/specialist_3d_v105.py");modern=text("ai_backend/modern_image.py");sdxl=text("ai_backend/sdxl_image.py");sd21=text("ai_backend/local_image.py");partcrafter=text("ai_backend/partcrafter_shape.py");updater=text("Updater/Program.cs");updates=text("Launcher/ApplicationUpdateService.cs");backend_launcher=text("Scripts/BackendLauncher.cs");launcher_job=text("Launcher/OwnedChildProcessJob.cs");model_service=text("Launcher/ModelService.cs");model_dialog=text("Launcher/ModelOperationDialog.cs");runtime_setup=text("Launcher/RuntimeSetupService.cs");launcher_program=text("Launcher/Program.cs");launcher_form=text("Launcher/LauncherForm.cs")
require(f"<Version>{EXPECTED}</Version>" in launcher,"launcher version mismatch");require(f"<Version>{EXPECTED}</Version>" in uproj,"updater version mismatch");require(f'#define MyAppVersion "{EXPECTED}"' in installer,"installer version mismatch");require('version="1.0.5"' in backend and '"version":"1.0.5"' in backend,"backend version mismatch");require("v1.0.5" in workflow,"v1.0.5 missing from CI")
require("/remesh selection" not in commands,"unfinished remesh-selection command exposed");require("pitch < .04 || pitch > 5.0" in commands,"remesh range mismatch");require("min(int(max_samples), 100000)" in geometry,"thickness cap regression");require("structurally_valid" in geometry,"canonical geometry validity missing");require("np.searchsorted" in geometry,"scalable intersection sweep missing")
for cid in ("z-image-turbo","qwen-image-2512","qwen-image-edit","sf3d","spar3d","hunyuan2mini","trellis2","partpacker"):
    require(cid in caps and cid in ext,f"v1.0.5 provider not registered/installable: {cid}")
for provider in ("zimage","qwen","qwen-edit","sf3d","spar3d","hunyuan-mini","trellis2","partpacker"):
    require(provider in router,f"router missing provider {provider}")
require("role_options" in router and "hardware-aware auto route" in router,"hardware-aware routing missing");require("enable_model_cpu_offload" in modern,"modern image providers lack offload");require("--low-vram-mode" in special,"SPAR3D low-VRAM route missing");require("MINISCULPTER_TRELLIS2_COMMAND" in special,"TRELLIS.2 Linux/WSL bridge missing");require("process_image" in special and "process_3d" in special,"PartPacker official inference adapter missing");require("_swap_staged" in manager and "_swap_staged" in ext,"transactional model staging missing");require('"pip", "check"' in ext,"isolated specialist environments are not dependency-checked")
require('hf_xet==1.6.0' in requirements,"Hugging Face Xet transport is not pinned in managed runtime")
require("selected_manifest" in downloads and "files_metadata=True" in downloads and "size mismatch" in downloads,"model downloads are not verified against upstream file metadata")
require("prepare_stage" in downloads and 'f"{component_id}-partial"' in downloads and "Recovered interrupted v1.0.5 stage" in downloads,"deterministic resumable model staging missing")
require("_prune_unselected" in downloads,"old over-broad model payload cleanup missing")
require('"sdxl-base"' in ext and '"text_encoder_2/model.fp16.safetensors"' in ext and '"unet/diffusion_pytorch_model.fp16.safetensors"' in ext,"SDXL fp16 manifest missing")
require('"*.safetensors"' not in ext,"broad root safetensors model download pattern reintroduced")
require('variant="fp16"' in sdxl and "local_files_only=True" in sdxl,"SDXL does not enforce audited local fp16 payload")
require('variant="fp16"' in sd21 and "local_files_only=True" in sd21,"SD2.1 does not enforce audited local fp16 payload")
require('"hunyuan3d-dit-v2-mini/model.fp16.safetensors"' in ext and '"hunyuan3d-vae-v2-mini/model.fp16.safetensors"' in ext,"Hunyuan2mini subset manifest missing")
require(special.count('"--pretrained-model"') >= 2,"SF3D/SPAR3D runtime can bypass installed local weights")
require("use_safetensors=True" in special,"Hunyuan2mini does not enforce selected safetensors weights")
require("CancellationTokenSource" in model_dialog and "RequestCancel" in model_dialog and "Partial stage" in model_dialog,"model operation dialog is not safely cancellable/resumable")
require("ResumeAvailable" in model_service and "resume_available" in model_service and "resume_action" in model_service,"launcher does not expose interrupted model stages")
require("Resume selected" in launcher_form and "interrupted model operation" in launcher_form,"launcher does not offer interrupted download resume")
require("CreateNoWindow = false" in runtime_setup and "RedirectStandardOutput = false" in runtime_setup,"AI runtime setup progress is not visible")
require('"manifest.json"' in partcrafter and 'data.get("parts")' in partcrafter,"PartCrafter manifest contract missing");require("mesh.area" in partcrafter and "mesh.extents" in partcrafter,"PartCrafter degenerate output guard missing")
require("PreserveNested" in updater and '".venv"' in updater and "RestorePreservedNested" in updater,"application updater AI-runtime preservation missing");require("IncrementalHash.CreateHash(HashAlgorithmName.SHA256)" in updates and "Miniscuplter-win-x64.zip" in updates,"application update integrity/exact-asset guard missing")
require("JobObjectLimitKillOnJobClose" in backend_launcher and "AssignProcessToJobObject" in backend_launcher and "Kill(entireProcessTree: true)" in backend_launcher,"editor AI backend is not guaranteed process-tree cleanup on close")
require("JobObjectLimitKillOnJobClose" in launcher_job and "AssignProcessToJobObject" in launcher_job,"launcher-owned subprocess kill-on-close job missing")
require("OwnedChildProcessJob.Start" in model_service and "OwnedChildProcessJob.Start" in runtime_setup,"launcher child processes bypass lifetime job")
require("OwnedChildProcessJob.Dispose" in launcher_program,"launcher does not close child-process lifetime job")
for p in [p for p in ROOT.rglob("*") if p.is_file() and p.resolve()!=SELF and p.suffix.lower() in {".cs",".py",".ps1",".bat",".iss"} and ".git" not in p.parts]:
    for n,line in enumerate(p.read_text(encoding="utf-8",errors="replace").splitlines(),1):
        if re.search(r"\b(TODO|FIXME|HACK|PLACEHOLDER)\b",line,re.I):errors.append(f"unfinished marker {p.relative_to(ROOT)}:{n}: {line.strip()[:100]}")
if errors:
    print("v1.0.5 release audit FAILED:");[print(" -",x) for x in errors];sys.exit(1)
print("v1.0.5 release audit passed")
