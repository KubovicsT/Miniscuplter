from __future__ import annotations

import shutil
import subprocess
import sys
from pathlib import Path

import model_manager as mm
import model_manager_v105 as mm105


def _git() -> str:
    git = shutil.which("git")
    if not git:
        raise RuntimeError("Git is required")
    return git


def clone_long_path_safe(url: str, target: Path) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    mm._run([_git(), "-c", "core.longpaths=true", "clone", "--depth", "1", url, str(target)])


def _checkout_is_usable(target: Path) -> bool:
    if not (target / ".git").is_dir():
        return False
    try:
        mm._run([_git(), "-c", "core.longpaths=true", "rev-parse", "--verify", "HEAD"], cwd=target, timeout=15)
        mm._run([_git(), "-c", "core.longpaths=true", "checkout", "-f", "HEAD"], cwd=target, timeout=120)
        return any(p.name != ".git" for p in target.iterdir())
    except (OSError, subprocess.SubprocessError):
        return False


def ensure_clone(url: str, target: Path) -> None:
    if _checkout_is_usable(target):
        return
    if target.exists():
        shutil.rmtree(target, ignore_errors=True)
    clone_long_path_safe(url, target)
    if not _checkout_is_usable(target):
        raise RuntimeError(f"Git checkout is incomplete after clone: {target}")


def install_triposr_dependencies(code_dir: Path) -> None:
    # torchmcubes' dynamic build metadata imports torch. Verify that the packaged/host
    # interpreter can import torch, then disable build isolation only for torchmcubes.
    mm._run([sys.executable, "-c", "import torch; print(torch.__version__)"], timeout=30)
    mm._pip_install(["git+https://github.com/tatsy/torchmcubes.git"], ["--no-build-isolation"])
    mm._pip_install(["imageio[ffmpeg]", "xatlas==0.0.9", "moderngl==5.10.0"])
    mm._verify_tool_import(code_dir, "from tsr.system import TSR", "TripoSR")


# Keep the repair local to Miniscuplter's model-install entry point. The legacy manager and
# v1.0.5 resumable installer share these helpers, so both fresh and resumed installs use the
# same contained Windows behavior without changing global Git or pip configuration.
mm._clone_fresh = clone_long_path_safe
mm._install_triposr_dependencies = install_triposr_dependencies
mm105._ensure_clone = ensure_clone

install_component = mm105.install_component
update_component = mm105.update_component
status = mm105.status
uninstall_component = mm.uninstall_component
