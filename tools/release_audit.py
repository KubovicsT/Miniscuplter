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
launcher=text("Launcher/Miniscuplter.Launcher.csproj");uproj=text("Updater/Miniscuplter.Updater.csproj");installer=text("installer/Miniscuplter.iss");backend=text("ai_backend/app.py");workflow=text(".github/workflows/build.yml");commands=text("Scripts/Main.V096Commands.cs");core=text("Scripts/Main.cs");extras=text("Scripts/ExtrasInstaller.cs");geometry=text("ai_backend/geometry_ops.py");manager=text("ai_backend/model_manager.py");ext=text("ai_backend/model_manager_v105.py");router=text("ai_backend/model_router.py");caps=text("ai_backend/model_capabilities.py");special=text("ai_backend/specialist_3d_v105.py");modern=text("ai_backend/modern_image.py");partcrafter=text("ai_backend/partcrafter_shape.py");updater=text("Updater/Program.cs");updates=text("Launcher/ApplicationUpdateService.cs")
require(f"<Version>{EXPECTED}</Version>" in launcher,"launcher version mismatch");require(f"<Version>{EXPECTED}</Version>" in uproj,"updater version mismatch");require(f'#define MyAppVersion "{EXPECTED}"' in installer,"installer version mismatch");require('version="1.0.5"' in backend and '"version":"1.0.5"' in backend,"backend version mismatch");require("v1.0.5" in workflow,"v1.0.5 missing from CI")
require("/remesh selection" not in commands,"unfinished remesh-selection command exposed");require("pitch < .04 || pitch > 5.0" in commands,"remesh range mismatch");require("min(int(max_samples), 100000)" in geometry,"thickness cap regression");require("structurally_valid" in geometry,"canonical geometry validity missing");require("np.searchsorted" in geometry,"scalable intersection sweep missing")
for cid in ("z-image-turbo","qwen-image-2512","qwen-image-edit","sf3d","spar3d","hunyuan2mini","trellis2","partpacker"):
    require(cid in caps and cid in ext,f"v1.0.5 provider not registered/installable: {cid}")
for provider in ("zimage","qwen","qwen-edit","sf3d","spar3d","hunyuan-mini","trellis2","partpacker"):
    require(provider in router,f"router missing provider {provider}")
require("role_options" in router and "hardware-aware auto route" in router,"hardware-aware routing missing");require("enable_model_cpu_offload" in modern,"modern image providers lack offload");require("--low-vram-mode" in special,"SPAR3D low-VRAM route missing");require("MINISCULPTER_TRELLIS2_COMMAND" in special,"TRELLIS.2 Linux/WSL bridge missing");require("process_image" in special and "process_3d" in special,"PartPacker official inference adapter missing");require("_swap_staged" in manager and "_swap_staged" in ext,"transactional model staging missing");require('"pip", "check"' in ext,"isolated specialist environments are not dependency-checked")
require('"manifest.json"' in partcrafter and 'data.get("parts")' in partcrafter,"PartCrafter manifest contract missing");require("mesh.area" in partcrafter and "mesh.extents" in partcrafter,"PartCrafter degenerate output guard missing")
require("PreserveNested" in updater and '".venv"' in updater and "RestorePreservedNested" in updater,"application updater AI-runtime preservation missing");require("IncrementalHash.CreateHash(HashAlgorithmName.SHA256)" in updates and "Miniscuplter-win-x64.zip" in updates,"application update integrity/exact-asset guard missing")
for p in [p for p in ROOT.rglob("*") if p.is_file() and p.resolve()!=SELF and p.suffix.lower() in {".cs",".py",".ps1",".bat",".iss"} and ".git" not in p.parts]:
    for n,line in enumerate(p.read_text(encoding="utf-8",errors="replace").splitlines(),1):
        if re.search(r"\b(TODO|FIXME|HACK|PLACEHOLDER)\b",line,re.I):errors.append(f"unfinished marker {p.relative_to(ROOT)}:{n}: {line.strip()[:100]}")
if errors:
    print("v1.0.5 release audit FAILED:");[print(" -",x) for x in errors];sys.exit(1)
print("v1.0.5 release audit passed")
