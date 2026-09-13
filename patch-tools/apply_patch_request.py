from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path, PurePosixPath

MAX_PATCH_BYTES = 262144
MAX_CONTENT_BYTES = 262144
MAX_CHANGED_LINES = 5000
SEMVER_BRANCH = re.compile(r"^v\d+\.\d+\.\d+$")
FULL_SHA = re.compile(r"^[0-9a-f]{40}$")
DENIED_PREFIXES = (".github/", "patch-requests/", "release-requests/", ".automation-locks/")


def run(*args: str) -> str:
    result = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if result.returncode != 0:
        raise RuntimeError(f"command failed: {' '.join(args)}\n{result.stdout}")
    return result.stdout.strip()


def validate_common(request: dict) -> tuple[str, str, str, str]:
    branch = str(request["target_branch"])
    head = str(request["expected_head_sha"]).lower()
    target = str(request["target_path"])
    message = str(request["commit_message"]).strip()

    if not SEMVER_BRANCH.fullmatch(branch):
        raise RuntimeError("target_branch must be a semantic-version branch")
    if not FULL_SHA.fullmatch(head):
        raise RuntimeError("expected_head_sha must be a full 40-character hexadecimal value")
    p = PurePosixPath(target)
    if p.is_absolute() or ".." in p.parts or "." in p.parts or "\\" in target:
        raise RuntimeError("unsafe target_path")
    if any(target == x.rstrip("/") or target.startswith(x) for x in DENIED_PREFIXES):
        raise RuntimeError("control paths cannot be patched")
    if not message or len(message) > 120 or "\n" in message or "\r" in message:
        raise RuntimeError("invalid commit_message")
    return branch, head, target, message


def prepare_target(branch: str, head: str) -> None:
    run("git", "fetch", "origin", f"refs/heads/{branch}:refs/remotes/origin/{branch}", "--force")
    actual_head = run("git", "rev-parse", f"refs/remotes/origin/{branch}").lower()
    if actual_head != head:
        raise RuntimeError(f"stale branch head: {actual_head} != {head}")
    if run("git", "ls-remote", "--tags", "origin", f"refs/tags/{branch}"):
        raise RuntimeError("published/tagged version branches are immutable")
    run("git", "switch", "--detach", head)


def validate_result(target: str) -> None:
    path = Path(target)
    if not path.is_file() or path.is_symlink():
        raise RuntimeError("target must be a regular file")
    changed = run("git", "diff", "--name-only").splitlines()
    if changed != [target]:
        raise RuntimeError(f"request changed unexpected paths: {changed}")
    if run("git", "diff", "--summary"):
        raise RuntimeError("renames and file-mode changes are forbidden")
    numstat = run("git", "diff", "--numstat", "--", target).split("\t")
    if len(numstat) != 3 or "-" in numstat[:2]:
        raise RuntimeError("binary change is forbidden")
    if int(numstat[0]) + int(numstat[1]) > MAX_CHANGED_LINES:
        raise RuntimeError("request changes too many lines")
    run("git", "diff", "--check", "--", target)
    if target.endswith(".py"):
        run(sys.executable, "-m", "py_compile", target)


def recheck_head(branch: str, head: str) -> None:
    run("git", "fetch", "origin", f"refs/heads/{branch}:refs/remotes/origin/{branch}", "--force")
    if run("git", "rev-parse", f"refs/remotes/origin/{branch}").lower() != head:
        raise RuntimeError("target branch moved during validation")


def commit_and_push(branch: str, target: str, message: str, result_name: str) -> None:
    run("git", "config", "user.name", "github-actions[bot]")
    run("git", "config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com")
    run("git", "add", "--", target)
    run("git", "commit", "-m", message)
    commit = run("git", "rev-parse", "HEAD")
    result_blob = run("git", "hash-object", target)
    run("git", "push", "origin", f"HEAD:refs/heads/{branch}")
    print(f"{result_name} commit_sha={commit} result_blob_sha={result_blob}")


def apply_existing_patch(request: dict) -> None:
    branch, head, target, message = validate_common(request)
    blob = str(request["expected_blob_sha"]).lower()
    patch = request["patch"]
    if not FULL_SHA.fullmatch(blob):
        raise RuntimeError("expected_blob_sha must be a full 40-character hexadecimal value")
    if not isinstance(patch, str) or not patch.strip() or len(patch.encode()) > MAX_PATCH_BYTES:
        raise RuntimeError("invalid or oversized patch")

    prepare_target(branch, head)
    actual_blob = run("git", "rev-parse", f"{head}:{target}").lower()
    if actual_blob != blob:
        raise RuntimeError(f"stale target blob: {actual_blob} != {blob}")
    path = Path(target)
    if not path.is_file() or path.is_symlink():
        raise RuntimeError("target must be an existing regular file")

    fd, patch_path = tempfile.mkstemp(suffix=".patch")
    try:
        with os.fdopen(fd, "wb") as f:
            f.write(patch.encode("utf-8"))
        run("git", "apply", "--unidiff-zero", "--check", patch_path)
        run("git", "apply", "--unidiff-zero", patch_path)
        validate_result(target)
        recheck_head(branch, head)
        commit_and_push(branch, target, message, "PATCH_APPLIED")
    finally:
        try:
            os.unlink(patch_path)
        except OSError:
            pass


def create_new_text_file(request: dict) -> None:
    branch, head, target, message = validate_common(request)
    content = request.get("content")
    if not isinstance(content, str) or not content:
        raise RuntimeError("content must be a non-empty UTF-8 text string")
    encoded = content.encode("utf-8")
    if len(encoded) > MAX_CONTENT_BYTES:
        raise RuntimeError("new file content is oversized")
    if "\x00" in content:
        raise RuntimeError("binary/NUL content is forbidden")
    if content.count("\n") + 1 > MAX_CHANGED_LINES:
        raise RuntimeError("new file has too many lines")

    prepare_target(branch, head)
    exists = subprocess.run(
        ["git", "cat-file", "-e", f"{head}:{target}"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    ).returncode == 0
    if exists:
        raise RuntimeError("target_path already exists at expected_head_sha")

    path = Path(target)
    if path.exists() or path.is_symlink():
        raise RuntimeError("target_path unexpectedly exists in checkout")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="")
    if path.is_symlink() or not path.is_file():
        raise RuntimeError("created target must be a regular file")

    validate_result(target)
    recheck_head(branch, head)
    commit_and_push(branch, target, message, "FILE_CREATED")


def main() -> None:
    request = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    schema = request.get("schema")
    if schema == 1:
        apply_existing_patch(request)
        return
    if schema == 2 and request.get("operation") == "create_text_file":
        create_new_text_file(request)
        return
    raise RuntimeError("unsupported request schema/operation")


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"PATCH_REJECTED: {exc}", file=sys.stderr)
        raise SystemExit(1)
