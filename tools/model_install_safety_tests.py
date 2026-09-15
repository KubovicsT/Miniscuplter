from __future__ import annotations

import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "ai_backend"))

import model_install_safety as safety


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def test_long_path_clone_command() -> None:
    old_which, old_run = safety.shutil.which, safety.mm._run
    calls: list[tuple[list[str], Path | None]] = []
    try:
        safety.shutil.which = lambda name: "git.exe" if name == "git" else None
        safety.mm._run = lambda command, cwd=None, timeout=None: calls.append((list(command), cwd))
        with tempfile.TemporaryDirectory() as temp:
            target = Path(temp) / "tool"
            safety.clone_long_path_safe("https://example.invalid/tool.git", target)
        command = calls[0][0]
        check(command[:4] == ["git.exe", "-c", "core.longpaths=true", "clone"], "clone must enable long paths per invocation")
        check("--depth" in command and "1" in command, "clone must remain shallow")
    finally:
        safety.shutil.which, safety.mm._run = old_which, old_run


def test_partial_clone_is_repaired() -> None:
    old_usable, old_clone = safety._checkout_is_usable, safety.clone_long_path_safe
    cloned: list[Path] = []
    try:
        safety._checkout_is_usable = lambda target: bool(cloned)
        safety.clone_long_path_safe = lambda url, target: (target.mkdir(parents=True, exist_ok=True), cloned.append(target))
        with tempfile.TemporaryDirectory() as temp:
            target = Path(temp) / "tool"
            (target / ".git").mkdir(parents=True)
            (target / "stale.txt").write_text("partial", encoding="utf-8")
            safety.ensure_clone("https://example.invalid/tool.git", target)
            check(len(cloned) == 1, "an unusable .git directory must not be accepted as a completed checkout")
            check(not (target / "stale.txt").exists(), "only the incomplete tool checkout should be recreated")
    finally:
        safety._checkout_is_usable, safety.clone_long_path_safe = old_usable, old_clone


def test_torchmcubes_uses_contained_no_isolation() -> None:
    old_run, old_pip, old_verify = safety.mm._run, safety.mm._pip_install, safety.mm._verify_tool_import
    pip_calls: list[tuple[list[str], list[str] | None]] = []
    try:
        safety.mm._run = lambda *args, **kwargs: None
        safety.mm._pip_install = lambda packages, extra_args=None: pip_calls.append((list(packages), list(extra_args) if extra_args else None))
        safety.mm._verify_tool_import = lambda *args, **kwargs: None
        safety.install_triposr_dependencies(Path("TripoSR"))
        check(pip_calls[0][0] == ["git+https://github.com/tatsy/torchmcubes.git"], "torchmcubes must be isolated from unrelated dependency installation")
        check(pip_calls[0][1] == ["--no-build-isolation"], "only torchmcubes should disable build isolation")
        check(pip_calls[1][1] is None, "unrelated TripoSR dependencies must retain normal build isolation")
    finally:
        safety.mm._run, safety.mm._pip_install, safety.mm._verify_tool_import = old_run, old_pip, old_verify


if __name__ == "__main__":
    test_long_path_clone_command()
    test_partial_clone_is_repaired()
    test_torchmcubes_uses_contained_no_isolation()
    print("model install safety tests passed")
