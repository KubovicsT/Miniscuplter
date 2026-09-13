from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path, PurePosixPath

MAX_PATCH_BYTES = 262144
MAX_CHANGED_LINES = 5000
SEMVER_BRANCH = re.compile(r"^v\d+\.\d+\.\d+$")
FULL_SHA = re.compile(r"^[0-9a-f]{40}$")
DENIED_PREFIXES = (".github/", "patch-requests/", "release-requests/", ".automation-locks/")


def run(*args: str) -> str:
    result = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if result.returncode != 0:
        raise RuntimeError(f"command failed: {' '.join(args)}\n{result.stdout}")
    return result.stdout.strip()


def main() -> None:
    request = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    if request.get("schema") != 1:
        raise RuntimeError("schema must be 1")

    branch = str(request["target_branch"])
    head = str(request["expected_head_sha"]).lower()
    blob = str(request["expected_blob_sha"]).lower()
    target = str(request["target_path"])
    message = str(request["commit_message"]).strip()
    patch = request["patch"]

    if not SEMVER_BRANCH.fullmatch(branch):
        raise RuntimeError("target_branch must be a semantic-version branch")
    if not FULL_SHA.fullmatch(head) or not FULL_SHA.fullmatch(blob):
        raise RuntimeError("expected SHAs must be full 40-character hexadecimal values")
    p = PurePosixPath(target)
    if p.is_absolute() or ".." in p.parts or "." in p.parts or "\\" in target:
        raise RuntimeError("unsafe target_path")
    if any(target == x.rstrip("/") or target.startswith(x) for x in DENIED_PREFIXES):
        raise RuntimeError("control paths cannot be patched")
    if not message or len(message) > 120 or "\n" in message or "\r" in message:
        raise RuntimeError("invalid commit_message")
    if not isinstance(patch, str) or not patch.strip() or len(patch.encode()) > MAX_PATCH_BYTES:
        raise RuntimeError("invalid or oversized patch")

    run("git", "fetch", "origin", f"refs/heads/{branch}:refs/remotes/origin/{branch}", "--force")
    actual_head = run("git", "rev-parse", f"refs/remotes/origin/{branch}").lower()
    if actual_head != head:
        raise RuntimeError(f"stale branch head: {actual_head} != {head}")
    if run("git", "ls-remote", "--tags", "origin", f"refs/tags/{branch}"):
        raise RuntimeError("published/tagged version branches are immutable")
    actual_blob = run("git", "rev-parse", f"{head}:{target}").lower()
    if actual_blob != blob:
        raise RuntimeError(f"stale target blob: {actual_blob} != {blob}")

    fd, patch_path = tempfile.mkstemp(suffix=".patch")
    try:
        with os.fdopen(fd, "wb") as f:
            f.write(patch.encode("utf-8"))

        run("git", "switch", "--detach", head)
        path = Path(target)
        if not path.is_file() or path.is_symlink():
            raise RuntimeError("target must be an existing regular file")

        # Exact HEAD + blob guards already bind the request to one immutable file version.
        # Allow zero-context unified diffs so large-file edits do not depend on fragile
        # surrounding-context matching while still requiring removed lines to match exactly.
        run("git", "apply", "--unidiff-zero", "--check", patch_path)
        run("git", "apply", "--unidiff-zero", patch_path)

        changed = run("git", "diff", "--name-only").splitlines()
        if changed != [target]:
            raise RuntimeError(f"patch changed unexpected paths: {changed}")
        if not path.is_file() or path.is_symlink():
            raise RuntimeError("patch may not delete target or create a symlink")
        if run("git", "diff", "--summary"):
            raise RuntimeError("renames and file-mode changes are forbidden")

        numstat = run("git", "diff", "--numstat", "--", target).split("\t")
        if len(numstat) != 3 or "-" in numstat[:2]:
            raise RuntimeError("binary patch is forbidden")
        if int(numstat[0]) + int(numstat[1]) > MAX_CHANGED_LINES:
            raise RuntimeError("patch changes too many lines")
        run("git", "diff", "--check", "--", target)
        if target.endswith(".py"):
            run(sys.executable, "-m", "py_compile", target)

        run("git", "fetch", "origin", f"refs/heads/{branch}:refs/remotes/origin/{branch}", "--force")
        if run("git", "rev-parse", f"refs/remotes/origin/{branch}").lower() != head:
            raise RuntimeError("target branch moved during validation")

        run("git", "config", "user.name", "github-actions[bot]")
        run("git", "config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com")
        run("git", "add", "--", target)
        run("git", "commit", "-m", message)
        commit = run("git", "rev-parse", "HEAD")
        result_blob = run("git", "hash-object", target)
        run("git", "push", "origin", f"HEAD:refs/heads/{branch}")
        print(f"PATCH_APPLIED commit_sha={commit} result_blob_sha={result_blob}")
    finally:
        try:
            os.unlink(patch_path)
        except OSError:
            pass


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"PATCH_REJECTED: {exc}", file=sys.stderr)
        raise SystemExit(1)
