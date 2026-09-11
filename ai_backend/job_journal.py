from __future__ import annotations

import json
import os
from copy import deepcopy
from pathlib import Path
from time import time
from uuid import uuid4

import storage

_SCHEMA_VERSION = 1
_JOURNAL_RELATIVE = Path("state") / "job-lifecycle.json"
_MAX_JOURNAL_BYTES = 256 * 1024
_ALLOWED_CONTEXT_KEYS = {
    "generation_job_id",
    "project_id",
    "project_revision",
    "input_image_revision_id",
    "output_object_id",
}
_ALLOWED_STATES = {"running", "cancelling", "completed", "failed", "cancelled", "interrupted"}


class JournalCorruptError(RuntimeError):
    """Raised when persisted lifecycle state cannot be trusted."""


def journal_path() -> Path:
    return storage.resolve(_JOURNAL_RELATIVE)


def _compact_context(value: object) -> dict:
    if not isinstance(value, dict):
        return {}
    result: dict = {}
    for key in _ALLOWED_CONTEXT_KEYS:
        if key not in value:
            continue
        item = value[key]
        if item is None or isinstance(item, (str, int, float, bool)):
            result[key] = item
    return result


def compact_record(entry: dict) -> dict:
    return {
        "schema_version": _SCHEMA_VERSION,
        "job_id": str(entry.get("job_id") or ""),
        "kind": str(entry.get("kind") or ""),
        "active": bool(entry.get("active")),
        "state": str(entry.get("state") or ""),
        "stage": str(entry.get("stage") or ""),
        "detail": str(entry.get("detail") or "")[:2000],
        "progress": max(0.0, min(100.0, float(entry.get("progress") or 0.0))),
        "provider": str(entry.get("provider")) if entry.get("provider") else None,
        "cancel_requested": bool(entry.get("cancel_requested")),
        "started_at": float(entry.get("started_at") or time()),
        "updated_at": float(entry.get("updated_at") or time()),
        "context": _compact_context(entry.get("context")),
    }


def _validate_record(value: object) -> dict:
    if not isinstance(value, dict):
        raise JournalCorruptError("Job lifecycle journal root is not a JSON object.")
    if int(value.get("schema_version") or 0) != _SCHEMA_VERSION:
        raise JournalCorruptError("Job lifecycle journal schema is unsupported.")
    job_id = value.get("job_id")
    kind = value.get("kind")
    state = value.get("state")
    if not isinstance(job_id, str) or not job_id or len(job_id) > 128:
        raise JournalCorruptError("Job lifecycle journal has an invalid job id.")
    if not isinstance(kind, str) or not kind or len(kind) > 64:
        raise JournalCorruptError("Job lifecycle journal has an invalid job kind.")
    if state not in _ALLOWED_STATES:
        raise JournalCorruptError("Job lifecycle journal has an invalid state.")
    try:
        return compact_record(value)
    except (TypeError, ValueError, OverflowError) as exc:
        raise JournalCorruptError(f"Job lifecycle journal contains invalid values: {exc}") from exc


def save(entry: dict) -> Path:
    path = journal_path()
    path.parent.mkdir(parents=True, exist_ok=True)
    payload = json.dumps(compact_record(entry), ensure_ascii=False, separators=(",", ":"))
    tmp = path.with_name(path.name + "." + uuid4().hex + ".tmp")
    try:
        with tmp.open("w", encoding="utf-8", newline="\n") as stream:
            stream.write(payload)
            stream.flush()
            os.fsync(stream.fileno())
        tmp.replace(path)
    finally:
        try:
            if tmp.exists():
                tmp.unlink()
        except OSError:
            pass
    return path


def load() -> dict | None:
    path = journal_path()
    if not path.exists():
        return None
    if path.is_symlink() or not path.is_file():
        raise JournalCorruptError("Job lifecycle journal path is not a regular managed file.")
    try:
        # Reject obviously oversized state before opening, then cap the actual
        # read as well so file growth between stat/open cannot cause an
        # unbounded allocation during backend recovery.
        if path.stat().st_size > _MAX_JOURNAL_BYTES:
            raise JournalCorruptError("Job lifecycle journal is unexpectedly large.")
        with path.open("rb") as stream:
            raw_bytes = stream.read(_MAX_JOURNAL_BYTES + 1)
        if len(raw_bytes) > _MAX_JOURNAL_BYTES:
            raise JournalCorruptError("Job lifecycle journal is unexpectedly large.")
        raw = raw_bytes.decode("utf-8")
        parsed = json.loads(raw)
    except JournalCorruptError:
        raise
    except Exception as exc:
        raise JournalCorruptError(f"Job lifecycle journal cannot be decoded: {exc}") from exc
    return _validate_record(parsed)


def reconcile_startup(now: float | None = None) -> dict | None:
    record = load()
    if record is None:
        return None
    if not record.get("active") and record.get("state") in {"completed", "failed", "cancelled", "interrupted"}:
        return deepcopy(record)

    recovered_at = float(now if now is not None else time())
    was_cancelling = bool(record.get("cancel_requested")) or record.get("state") == "cancelling"
    record.update({
        "active": False,
        "state": "cancelled" if was_cancelling else "interrupted",
        "stage": "cancelled" if was_cancelling else "interrupted",
        "detail": (
            "Cancellation was interrupted by a backend restart; the prior worker is no longer running."
            if was_cancelling
            else "The prior backend process ended before this job reached a terminal result; no output was accepted."
        ),
        "updated_at": recovered_at,
    })
    save(record)
    return deepcopy(record)
