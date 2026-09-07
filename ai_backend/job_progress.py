from __future__ import annotations

from threading import Lock, local
from time import time
from uuid import uuid4

_lock = Lock()
_context = local()
_jobs: dict[str, dict] = {}
_current_id: str | None = None


def _now() -> float:
    return time()


def begin(kind: str) -> str:
    global _current_id
    job_id = uuid4().hex
    now = _now()
    entry = {
        "active": True,
        "job_id": job_id,
        "kind": kind,
        "state": "running",
        "stage": "queued",
        "detail": "Request accepted by the local AI backend.",
        "progress": 1.0,
        "provider": None,
        "started_at": now,
        "updated_at": now,
    }
    with _lock:
        _jobs[job_id] = entry
        _current_id = job_id
        if len(_jobs) > 64:
            for old_id, _ in sorted(_jobs.items(), key=lambda kv: kv[1].get("updated_at", 0.0))[:-48]:
                _jobs.pop(old_id, None)
    _context.job_id = job_id
    return job_id


def bind(job_id: str | None) -> None:
    _context.job_id = job_id


def current_job_id() -> str | None:
    return getattr(_context, "job_id", None)


def report(stage: str, detail: str = "", progress: float | None = None, provider: str | None = None) -> None:
    job_id = current_job_id()
    if not job_id:
        return
    with _lock:
        entry = _jobs.get(job_id)
        if entry is None:
            return
        entry["stage"] = stage
        if detail:
            entry["detail"] = detail
        if progress is not None:
            entry["progress"] = max(0.0, min(100.0, float(progress)))
        if provider:
            entry["provider"] = provider
        entry["updated_at"] = _now()


def complete(detail: str = "Completed.", provider: str | None = None) -> None:
    job_id = current_job_id()
    if not job_id:
        return
    with _lock:
        entry = _jobs.get(job_id)
        if entry is None:
            return
        entry.update({
            "active": False,
            "state": "completed",
            "stage": "completed",
            "detail": detail,
            "progress": 100.0,
            "updated_at": _now(),
        })
        if provider:
            entry["provider"] = provider


def fail(detail: str) -> None:
    job_id = current_job_id()
    if not job_id:
        return
    with _lock:
        entry = _jobs.get(job_id)
        if entry is None:
            return
        entry.update({
            "active": False,
            "state": "failed",
            "stage": "failed",
            "detail": detail,
            "updated_at": _now(),
        })


def current() -> dict:
    with _lock:
        if not _current_id or _current_id not in _jobs:
            return {
                "active": False,
                "job_id": None,
                "kind": "",
                "state": "idle",
                "stage": "idle",
                "detail": "No AI job has run in this backend session yet.",
                "progress": 0.0,
                "provider": None,
                "started_at": None,
                "updated_at": _now(),
            }
        return dict(_jobs[_current_id])


def get(job_id: str) -> dict | None:
    with _lock:
        entry = _jobs.get(job_id)
        return dict(entry) if entry is not None else None
