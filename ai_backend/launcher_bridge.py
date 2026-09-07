from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from pathlib import Path


# Model/provider installation can invoke pip build isolation for native extensions such as
# torchmcubes. pip normally puts those multi-gigabyte temporary build environments under the
# Windows user TEMP folder (usually C:), even when Miniscuplter and AIData live on another
# drive. Keep all model-manager subprocess temp/cache activity beside AIData instead.
def _configure_model_install_storage() -> None:
    data_root = Path(os.getenv("MINISCULPTER_DATA", Path(os.getenv("MINISCULPTER_ROOT", Path(__file__).resolve().parent.parent)) / "AIData")).resolve()
    runtime_cache = data_root / "runtime-cache"
    temp_root = runtime_cache / "model-install-temp"
    pip_cache = runtime_cache / "pip-cache"
    temp_root.mkdir(parents=True, exist_ok=True)
    pip_cache.mkdir(parents=True, exist_ok=True)
    os.environ["TEMP"] = str(temp_root)
    os.environ["TMP"] = str(temp_root)
    os.environ["PIP_CACHE_DIR"] = str(pip_cache)


_configure_model_install_storage()

# Import only after TEMP/TMP/PIP_CACHE_DIR are redirected. model_manager subprocesses inherit
# this environment, including pip's isolated build environments and Git source checkouts.
from model_manager import install_component, uninstall_component, update_component
from model_manager_v105 import status


def _exception_detail(exc: Exception) -> str:
    # subprocess.run(..., capture_output=True, check=True) otherwise collapses a useful compiler
    # or pip error into only "returned non-zero exit status 1". Preserve the actual diagnostics
    # for the launcher's operation log.
    if isinstance(exc, subprocess.CalledProcessError):
        parts: list[str] = []
        if exc.stdout and str(exc.stdout).strip():
            parts.append(str(exc.stdout).strip())
        if exc.stderr and str(exc.stderr).strip():
            parts.append(str(exc.stderr).strip())
        if parts:
            return "\n".join(parts)
    return str(exc)


def main() -> int:
    parser = argparse.ArgumentParser(description="Miniscuplter launcher/model-manager bridge")
    sub = parser.add_subparsers(dest="command", required=True)

    p_status = sub.add_parser("status")
    group = p_status.add_mutually_exclusive_group()
    group.add_argument("--updates", action="store_true")
    group.add_argument("--no-updates", action="store_true")

    for name in ("install", "remove", "update"):
        p = sub.add_parser(name)
        p.add_argument("id")

    args = parser.parse_args()
    try:
        if args.command == "status": result = status(check_updates=bool(args.updates and not args.no_updates))
        elif args.command == "install": result = install_component(args.id)
        elif args.command == "remove": result = uninstall_component(args.id)
        elif args.command == "update": result = update_component(args.id)
        else: raise RuntimeError("Unknown launcher command")
        print(json.dumps(result, ensure_ascii=False))
        return 0
    except Exception as exc:
        print(_exception_detail(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
