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
geometry = text("ai_backend/geometry_ops.py")
readme = text("README.md")

require(f"<Version>{EXPECTED}</Version>" in launcher_proj, "launcher version is not 1.0.0")
require(f"<Version>{EXPECTED}</Version>" in updater_proj, "updater version is not 1.0.0")
require(f'#define MyAppVersion "{EXPECTED}"' in installer, "installer version is not 1.0.0")
require('version="1.0.0"' in backend, "FastAPI application version is not 1.0.0")
require('"version": "1.0.0"' in backend, "backend /health version is not 1.0.0")
require("v1.0" in workflow, "v1.0 is not included in CI workflow")
require("/remesh selection" not in commands, "unfinished /remesh selection command is still exposed")
require("pitch < .04 || pitch > 5.0" in commands, "command remesh range is not aligned to 0.04-5.0 mm")
require("min(int(max_samples), 100000)" in geometry, "thickness backend still caps below 100,000 samples")
require("structurally_valid" in geometry, "geometry backend lacks canonical structurally_valid result")
require("native geometry backend is not installed in this source build" not in core, "dead native-backend placeholder remains in core UI")
require("Bake/union requires the native remesh backend" not in core, "dead bake/union placeholder remains in core UI")
require("Ready — Miniscuplter v1.0" in core, "core editor is not identified as v1.0")
require(readme.lstrip().startswith("# Miniscuplter v1.0"), "README is not current for v1.0")

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
