from __future__ import annotations

import argparse
import re
from pathlib import Path

ROOT = Path.cwd()
SEMVER = re.compile(r"^\d+\.\d+\.\d+$")

SURFACES = {
    "Launcher/Miniscuplter.Launcher.csproj": [
        (r"(<Version>)\d+\.\d+\.\d+(</Version>)", lambda v: rf"\g<1>{v}\g<2>"),
    ],
    "Updater/Miniscuplter.Updater.csproj": [
        (r"(<Version>)\d+\.\d+\.\d+(</Version>)", lambda v: rf"\g<1>{v}\g<2>"),
    ],
    "Miniscuplter.csproj": [
        (r"(<Version>)\d+\.\d+\.\d+(</Version>)", lambda v: rf"\g<1>{v}\g<2>"),
    ],
    "installer/Miniscuplter.iss": [
        (r'(#define MyAppVersion ")\d+\.\d+\.\d+(")', lambda v: rf"\g<1>{v}\g<2>"),
    ],
    "export_presets.cfg": [
        (r'(application/file_version=")\d+\.\d+\.\d+\.0(")', lambda v: rf"\g<1>{v}.0\g<2>"),
        (r'(application/product_version=")\d+\.\d+\.\d+\.0(")', lambda v: rf"\g<1>{v}.0\g<2>"),
    ],
    "ai_backend/app.py": [
        (r'(APP_VERSION = ")\d+\.\d+\.\d+(")', lambda v: rf"\g<1>{v}\g<2>"),
    ],
    "Scripts/Main.V100Release.cs": [
        (r'(Ready — Miniscuplter v)\d+\.\d+\.\d+', lambda v: rf"\g<1>{v}"),
    ],
    "tools/release_audit.py": [
        (r'(EXPECTED = ")\d+\.\d+\.\d+(")', lambda v: rf"\g<1>{v}\g<2>"),
    ],
    "tools/backend_lifecycle_tests.py": [
        (r'(EXPECTED_VERSION = ")\d+\.\d+\.\d+(")', lambda v: rf"\g<1>{v}\g<2>"),
    ],
}


def transform(path: Path, version: str) -> tuple[str, str]:
    before = path.read_text(encoding="utf-8")
    after = before
    for pattern, replacement in SURFACES[path.as_posix()]:
        after, count = re.subn(pattern, replacement(version), after, count=1)
        if count != 1:
            raise RuntimeError(f"{path}: expected exactly one match for {pattern!r}, got {count}")
    return before, after


def main() -> int:
    parser = argparse.ArgumentParser(description="Synchronize Miniscuplter derived version surfaces.")
    parser.add_argument("--version", help="Canonical x.y.z version. Defaults to VERSION file.")
    parser.add_argument("--check", action="store_true", help="Fail if any derived surface is stale.")
    args = parser.parse_args()

    version_file = ROOT / "VERSION"
    if args.version:
        version = args.version.strip()
    else:
        if not version_file.is_file():
            raise RuntimeError("VERSION file is missing")
        version = version_file.read_text(encoding="utf-8").strip()

    if not SEMVER.fullmatch(version):
        raise RuntimeError(f"invalid canonical version: {version!r}")

    stale: list[str] = []
    for rel in SURFACES:
        path = ROOT / rel
        if not path.is_file():
            raise RuntimeError(f"missing version surface: {rel}")
        before, after = transform(path, version)
        if before != after:
            stale.append(rel)
            if not args.check:
                path.write_text(after, encoding="utf-8")

    current_version = version_file.read_text(encoding="utf-8").strip() if version_file.is_file() else None
    if current_version != version:
        stale.insert(0, "VERSION")
        if not args.check:
            version_file.write_text(version + "\n", encoding="utf-8")

    if args.check and stale:
        raise RuntimeError("stale derived version surfaces: " + ", ".join(stale))

    if stale:
        print("Synchronized version " + version + ": " + ", ".join(stale))
    else:
        print("Version surfaces already synchronized at " + version)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
