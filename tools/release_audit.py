from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SELF = Path(__file__).resolve()
EXPECTED = "1.0.0"
errors: list[str] = []


def text(path: str) -> str:
    p = ROOT / path
    if not p.is_file():
        errors.append(f"missing required file: {path}")
        return ""
    return p.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


launcher_proj = text("Launcher/Miniscuplter.Launcher.csproj")
updater_proj = text("Updater/Miniscuplter.Updater.csproj")
installer = text("installer/Miniscuplter.iss")
backend = text("ai_backend/app.py")
workflow = text(".github/workflows/build.yml")
commands = text("Scripts/Main.V096Commands.cs")
core = text("Scripts/Main.cs")
extras = text("Scripts/ExtrasInstaller.cs")
geometry = text("ai_backend/geometry_ops.py")
model_manager = text("ai_backend/model_manager.py")
partcrafter = text("ai_backend/partcrafter_shape.py")
updater = text("Updater/Program.cs")
app_updates = text("Launcher/ApplicationUpdateService.cs")
readme = text("README.md")

require(f"<Version>{EXPECTED}</Version>" in launcher_proj, "launcher version is not 1.0.0")
require(f"<Version>{EXPECTED}</Version>" in updater_proj, "updater version is not 1.0.0")
require(f'#define MyAppVersion "{EXPECTED}"' in installer, "installer version is not 1.0.0")
require('version="1.0.0"' in backend, "FastAPI application version is not 1.0.0")
require('"version": "1.0.0"' in backend, "backend /health version is not 1.0.0")
require("v1.0" in workflow, "v1.0 is not included in CI workflow")
require("core_logic_tests.py" in workflow, "v1.0 core logic tests are not wired into CI")
require("/remesh selection" not in commands, "unfinished /remesh selection command is still exposed")
require("pitch < .04 || pitch > 5.0" in commands, "command remesh range is not aligned to 0.04-5.0 mm")
require("min(int(max_samples), 100000)" in geometry, "thickness backend still caps below 100,000 samples")
require("structurally_valid" in geometry, "geometry backend lacks canonical structurally_valid result")
require("native geometry backend is not installed in this source build" not in core, "dead native-backend placeholder remains in core UI")
require("Bake/union requires the native remesh backend" not in core, "dead bake/union placeholder remains in core UI")
require("Ready — Miniscuplter v1.0" in core, "core editor is not identified as v1.0")
require(readme.lstrip().startswith("# Miniscuplter v1.0"), "README is not current for v1.0")

# Cross-version installers must run release polish last so legacy controls cannot overwrite v1.0 behavior.
require("InstallV099Locations()" in extras and "InstallV100ReleasePolish()" in extras, "release installer chain is incomplete")
if "InstallV099Locations()" in extras and "InstallV100ReleasePolish()" in extras:
    require(extras.index("InstallV100ReleasePolish()") > extras.index("InstallV099Locations()"), "v1.0 release polish is not installed last")

# Specialist models share one managed Python runtime. Do not blindly apply upstream requirement
# files that pin incompatible core packages; each route must use inference-specific dependencies
# and prove the actual inference module imports before it is marked installed.
require("_install_hunyuan_dependencies(code_dir)" in model_manager, "Hunyuan installer does not verify shape inference dependencies")
require("_install_triposr_dependencies(code_dir)" in model_manager, "TripoSR installer does not use isolated inference dependencies")
require("_install_partcrafter_dependencies(code_dir)" in model_manager, "PartCrafter installer does not use isolated inference dependencies")
require("_verify_tool_import" in model_manager and "pip", "specialist model import verification is missing")
require('code_dir / "requirements.txt"' not in model_manager, "shared runtime can still be modified by an unreviewed specialist requirements.txt")
require("pip check" in model_manager, "specialist dependency installation does not verify environment consistency")

# PartCrafter emits both individual parts and a merged object. The wrapper must follow its
# manifest instead of time-scanning every fresh mesh, otherwise the merged object is imported
# as a duplicate pseudo-part.
require('"--output_dir"' in partcrafter, "PartCrafter wrapper does not isolate each job output directory")
require('"manifest.json"' in partcrafter and 'data.get("parts")' in partcrafter, "PartCrafter wrapper does not use the official part manifest")
require("rglob(" not in partcrafter, "PartCrafter wrapper still scans arbitrary fresh geometry")

# App updates replace backend source but must preserve the installed AI virtual environment.
require("PreserveNested" in updater and '".venv"' in updater, "application updater does not preserve the installed AI runtime")
require("RestorePreservedNested(backup, target)" in updater, "application updater never restores the preserved AI runtime")
require("BackupManagedTree" in updater and "RestoreBackup" in updater, "transactional application-update rollback is missing")
require("IncrementalHash.CreateHash(HashAlgorithmName.SHA256)" in app_updates, "application update download lacks SHA-256 verification support")
require("Miniscuplter-win-x64.zip" in app_updates, "launcher can select an unrelated release ZIP")

# Reject common unfinished-work markers in active source. The audit file itself contains the
# marker vocabulary by definition and is excluded from its own scan.
for path in [p for p in ROOT.rglob("*") if p.is_file() and p.resolve() != SELF and p.suffix.lower() in {".cs", ".py", ".ps1", ".bat", ".iss"} and ".git" not in p.parts]:
    body = path.read_text(encoding="utf-8", errors="replace")
    for line_no, line in enumerate(body.splitlines(), 1):
        if re.search(r"\b(TODO|FIXME|HACK|PLACEHOLDER)\b", line, re.IGNORECASE):
            errors.append(f"unfinished marker {path.relative_to(ROOT)}:{line_no}: {line.strip()[:120]}")

if errors:
    print("v1.0 release audit FAILED:")
    for item in errors:
        print(" -", item)
    sys.exit(1)

print("v1.0 release audit passed")
