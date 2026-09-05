from __future__ import annotations

import fnmatch
import json
import shutil
from pathlib import Path
from typing import Iterable

STAGE_SCHEMA = 2
STAGE_META = ".miniscuplter-stage.json"


def directory_size(path: Path, *, exclude_cache: bool = False, exclude_stage_meta: bool = False) -> int:
    total = 0
    if not path.exists(): return 0
    try:
        for file in path.rglob("*"):
            if not file.is_file(): continue
            if exclude_cache and ".cache" in file.parts: continue
            if exclude_stage_meta and file.name in {STAGE_META, STAGE_META + ".tmp"}: continue
            try: total += file.stat().st_size
            except OSError: pass
    except OSError: pass
    return total


def _matches(name: str, patterns: Iterable[str] | None) -> bool:
    if patterns is None: return True
    return any(fnmatch.fnmatchcase(name, pattern) for pattern in patterns)


def selected_manifest(repo_id: str, revision: str, allow_patterns: list[str] | None = None) -> tuple[list[tuple[str, int | None]], int]:
    """Return exact upstream files selected by Miniscuplter plus their byte total."""
    from huggingface_hub import HfApi
    info = HfApi().model_info(repo_id, revision=revision, files_metadata=True)
    selected: list[tuple[str, int | None]] = []; total = 0
    for sibling in info.siblings or []:
        name = getattr(sibling, "rfilename", None)
        if not name or not _matches(name, allow_patterns): continue
        size = getattr(sibling, "size", None)
        if size is None:
            lfs = getattr(sibling, "lfs", None)
            size = lfs.get("size") if isinstance(lfs, dict) else getattr(lfs, "size", None) if lfs is not None else None
        parsed = int(size) if size is not None else None
        selected.append((str(name), parsed))
        if parsed is not None: total += parsed
    if not selected: raise RuntimeError(f"The selected Miniscuplter payload for {repo_id} contains no files at revision {revision}.")
    return selected, total


def _prune_unselected(target: Path, allowed_names: set[str]) -> None:
    """Remove files left by an older/broader installer while retaining resume metadata."""
    if not target.exists(): return
    for file in list(target.rglob("*")):
        if not file.is_file(): continue
        try: rel = file.relative_to(target).as_posix()
        except ValueError: continue
        if rel.startswith(".cache/"): continue
        if rel not in allowed_names:
            try: file.unlink()
            except OSError: pass


def download_verified(repo_id: str, target: Path, revision: str, allow_patterns: list[str] | None = None) -> int:
    """Resume/download a selected HF payload and verify every known upstream file size."""
    from huggingface_hub import snapshot_download
    manifest, expected_bytes = selected_manifest(repo_id, revision, allow_patterns); allowed = {name for name, _ in manifest}; target.mkdir(parents=True, exist_ok=True)
    _prune_unselected(target, allowed)
    gib = expected_bytes / 1024**3 if expected_bytes else 0.0
    print(f"Verified upstream payload: {repo_id} - {len(manifest)} files" + (f", {gib:.2f} GiB" if expected_bytes else ""), flush=True)
    if expected_bytes: print(f"MINISCULPTER_EXPECTED_BYTES_ADD={expected_bytes}", flush=True)
    kwargs = {"repo_id": repo_id, "revision": revision, "local_dir": target}
    if allow_patterns is not None: kwargs["allow_patterns"] = allow_patterns
    snapshot_download(**kwargs)
    failures: list[str] = []
    for name, expected_size in manifest:
        file = target / Path(name)
        if not file.is_file(): failures.append(f"missing {name}"); continue
        if expected_size is not None:
            try: actual = file.stat().st_size
            except OSError: failures.append(f"unreadable {name}"); continue
            if actual != expected_size: failures.append(f"size mismatch {name}: {actual} != {expected_size}")
    if failures: raise RuntimeError(f"Downloaded files for {repo_id} failed verification: {'; '.join(failures[:8])}")
    shutil.rmtree(target / ".cache", ignore_errors=True)
    print(f"Verified downloaded payload: {repo_id}", flush=True); return expected_bytes


def _read_meta(stage: Path) -> dict:
    try:
        data = json.loads((stage / STAGE_META).read_text(encoding="utf-8")); return data if isinstance(data, dict) else {}
    except Exception: return {}


def _write_meta(stage: Path, data: dict) -> None:
    stage.mkdir(parents=True, exist_ok=True); tmp = stage / (STAGE_META + ".tmp"); tmp.write_text(json.dumps(data, indent=2), encoding="utf-8"); tmp.replace(stage / STAGE_META)


def prepare_stage(staging_root: Path, component_id: str, revision: str | None, signature: str, action: str) -> Path:
    """Create/reuse a deterministic stage, including migration from v1.0.5 UUID stages."""
    staging_root.mkdir(parents=True, exist_ok=True); stage = staging_root / f"{component_id}-partial"
    if not stage.exists():
        legacy = [p for p in staging_root.glob(f"{component_id}-*") if p.is_dir() and p.name != stage.name]
        if legacy:
            candidate = max(legacy, key=lambda p: p.stat().st_mtime)
            try: candidate.rename(stage); print(f"Recovered interrupted v1.0.5 stage: {candidate.name}", flush=True)
            except OSError: pass
    if stage.exists():
        meta = _read_meta(stage)
        if meta and (int(meta.get("schema", 0)) != STAGE_SCHEMA or meta.get("revision") != revision or meta.get("signature") != signature):
            print("Discarding stale partial stage because the upstream revision or download manifest changed.", flush=True); shutil.rmtree(stage, ignore_errors=True)
    stage.mkdir(parents=True, exist_ok=True)
    _write_meta(stage, {"schema":STAGE_SCHEMA,"component_id":component_id,"revision":revision,"signature":signature,"action":action})
    return stage


def stage_status(staging_root: Path, component_id: str) -> dict:
    candidates = [p for p in staging_root.glob(f"{component_id}-*") if p.is_dir()]
    if not candidates: return {"resume_available":False,"resume_action":None,"staged_gb":0.0}
    stage = max(candidates, key=lambda p: p.stat().st_mtime); size = directory_size(stage, exclude_stage_meta=True)
    if size <= 0: return {"resume_available":False,"resume_action":None,"staged_gb":0.0}
    meta = _read_meta(stage); action = str(meta.get("action") or "install")
    return {"resume_available":True,"resume_action":"update" if action == "update" else "install","staged_gb":round(size / 1024**3, 2)}


def clear_stage(stage: Path) -> None:
    shutil.rmtree(stage, ignore_errors=True)
