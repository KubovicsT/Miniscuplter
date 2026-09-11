from __future__ import annotations

from copy import deepcopy
from threading import Lock, local
from time import time
from uuid import uuid4

import job_journal
import resource_ownership

_lock = Lock()
_context = local()
_jobs: dict[str, dict] = {}
_current_id: str | None = None
_recovery_warning: str | None = None
_MAX_EVENTS = 96
_HEAVYWEIGHT_KINDS = {"3d-generate"}

_3D_PROVIDER_COMPONENTS = {
    "triposr": "triposr",
    "sf3d": "sf3d",
    "spar3d": "spar3d",
    "hunyuan-mini": "hunyuan2mini",
    "hunyuan": "hunyuan21-shape",
    "trellis2": "trellis2",
}


class JobCancellationAcknowledged(RuntimeError):
    """Raised when provider execution has ended after cancellation was requested."""


def _now() -> float:
    return time()


def _snapshot(entry: dict) -> dict:
    result = dict(entry)
    result["events"] = deepcopy(entry.get("events", []))
    result["context"] = deepcopy(entry.get("context", {}))
    result["resource_owner"] = resource_ownership.snapshot()
    return result


def _event(entry: dict, stage: str, detail: str, progress: float | None = None) -> None:
    entry["sequence"] = int(entry.get("sequence", 0)) + 1
    event = {
        "sequence": entry["sequence"],
        "stage": stage,
        "detail": detail,
        "progress": entry.get("progress", 0.0) if progress is None else progress,
        "timestamp": _now(),
    }
    events = entry.setdefault("events", [])
    events.append(event)
    if len(events) > _MAX_EVENTS:
        del events[:-_MAX_EVENTS]


def _persist_locked(entry: dict) -> None:
    if str(entry.get("kind") or "") in _HEAVYWEIGHT_KINDS:
        job_journal.save(entry)


def _record_completed_inference(kind: str, provider: str | None, elapsed_seconds: float) -> None:
    """Persist qualification only after a real, verified 3D job completed."""
    if kind != "3d-generate" or not provider:
        return
    component_id = _3D_PROVIDER_COMPONENTS.get(provider)
    if component_id is None:
        return
    from model_manager import hardware_info
    from provider_readiness import record_inference_success
    record_inference_success(
        component_id,
        elapsed_seconds=elapsed_seconds,
        benchmark={"hardware": hardware_info()},
    )


def _mark_cancelled_locked(entry: dict, detail: str) -> str:
    cancelled_at = _now()
    entry.update({
        "active": False,
        "state": "cancelled",
        "stage": "cancelled",
        "detail": detail,
        "updated_at": cancelled_at,
    })
    _event(entry, "cancelled", detail, entry.get("progress", 0.0))
    return str(entry.get("kind") or "")


def recover_persisted_state() -> dict | None:
    """Load one compact heavyweight tombstone and reconcile a dead prior backend process."""
    global _current_id, _recovery_warning
    try:
        recovered = job_journal.reconcile_startup()
    except job_journal.JournalCorruptError as exc:
        with _lock:
            _recovery_warning = f"Persisted AI job state was rejected as corrupt: {exc}"
        return None

    if recovered is None:
        return None

    with _lock:
        entry = dict(recovered)
        entry["context"] = deepcopy(recovered.get("context", {}))
        entry["events"] = []
        entry["sequence"] = 0
        detail = str(entry.get("detail") or "Recovered persisted AI job state.")
        _event(entry, str(entry.get("stage") or entry.get("state") or "recovered"), detail, entry.get("progress", 0.0))
        _jobs[str(entry["job_id"])] = entry
        _current_id = str(entry["job_id"])
        _recovery_warning = None
        return _snapshot(entry)


def begin(kind: str, client_job_id: str | None = None, context: dict | None = None) -> str:
    global _current_id, _recovery_warning
    requested = (client_job_id or "").strip()
    job_id = requested if requested and len(requested) <= 96 else uuid4().hex
    safe_context = deepcopy(context or {})

    with _lock:
        if job_id in _jobs:
            job_id = f"{job_id}-{uuid4().hex[:8]}"

    owns_resource = kind in _HEAVYWEIGHT_KINDS
    if owns_resource:
        resource_ownership.acquire(job_id, kind, blocking=True)

    now = _now()
    try:
        with _lock:
            entry = {
                "active": True,
                "job_id": job_id,
                "kind": kind,
                "state": "running",
                "stage": "queued",
                "detail": "Request accepted by the local AI backend.",
                "progress": 1.0,
                "provider": None,
                "cancel_requested": False,
                "sequence": 0,
                "started_at": now,
                "updated_at": now,
                "context": safe_context,
                "events": [],
            }
            _jobs[job_id] = entry
            _current_id = job_id
            if len(_jobs) > 96:
                for old_id, _ in sorted(_jobs.items(), key=lambda kv: kv[1].get("updated_at", 0.0))[:-72]:
                    _jobs.pop(old_id, None)
            _event(entry, "queued", entry["detail"], 1.0)
            _persist_locked(entry)
            _recovery_warning = None
    except Exception:
        with _lock:
            _jobs.pop(job_id, None)
            if _current_id == job_id:
                _current_id = None
        if owns_resource:
            resource_ownership.release(job_id)
        raise

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
        if entry is None or not entry.get("active"):
            return
        entry["stage"] = stage
        if detail:
            entry["detail"] = detail
        if progress is not None:
            entry["progress"] = max(0.0, min(100.0, float(progress)))
        if provider:
            entry["provider"] = provider
        entry["updated_at"] = _now()
        _event(entry, stage, entry.get("detail", ""), entry.get("progress", 0.0))
        _persist_locked(entry)


def acknowledge_cancel(detail: str = "Cancellation acknowledged after the owned worker stopped.", job_id: str | None = None) -> dict | None:
    target = job_id or current_job_id()
    if not target:
        return None
    kind = ""
    snapshot: dict | None = None
    persist_error: Exception | None = None
    with _lock:
        entry = _jobs.get(target)
        if entry is None or not entry.get("cancel_requested"):
            return None
        if entry.get("state") == "cancelled" and not entry.get("active"):
            return _snapshot(entry)
        kind = _mark_cancelled_locked(entry, detail)
        try:
            _persist_locked(entry)
        except Exception as exc:
            persist_error = exc
        snapshot = _snapshot(entry)

    if kind in _HEAVYWEIGHT_KINDS:
        resource_ownership.release(target)
    if persist_error is not None:
        raise persist_error
    return snapshot


def complete(detail: str = "Completed.", provider: str | None = None) -> None:
    job_id = current_job_id()
    if not job_id:
        return
    qualification: tuple[str, str | None, float] | None = None
    kind = ""
    cancelled = False
    persist_error: Exception | None = None
    with _lock:
        entry = _jobs.get(job_id)
        if entry is None:
            return
        if entry.get("cancel_requested"):
            if entry.get("state") == "cancelled" and not entry.get("active"):
                raise JobCancellationAcknowledged("Cancellation was already acknowledged after provider execution stopped.")
            kind = _mark_cancelled_locked(
                entry,
                "Cancellation acknowledged after provider execution stopped; late completion was discarded.",
            )
            cancelled = True
        else:
            completed_at = _now()
            entry.update({
                "active": False,
                "state": "completed",
                "stage": "completed",
                "detail": detail,
                "progress": 100.0,
                "updated_at": completed_at,
            })
            if provider:
                entry["provider"] = provider
            _event(entry, "completed", detail, 100.0)
            kind = str(entry.get("kind") or "")
            qualification = (
                kind,
                entry.get("provider"),
                max(0.0, completed_at - float(entry.get("started_at") or completed_at)),
            )
        try:
            _persist_locked(entry)
        except Exception as exc:
            persist_error = exc

    if kind in _HEAVYWEIGHT_KINDS:
        resource_ownership.release(job_id)

    if persist_error is not None:
        raise persist_error
    if cancelled:
        raise JobCancellationAcknowledged("Cancellation acknowledged after provider execution stopped.")

    if qualification is not None:
        try:
            _record_completed_inference(*qualification)
        except Exception:
            pass


def fail(detail: str) -> None:
    job_id = current_job_id()
    if not job_id:
        return
    kind = ""
    persist_error: Exception | None = None
    with _lock:
        entry = _jobs.get(job_id)
        if entry is None:
            return
        if not entry.get("active") and entry.get("state") in {"completed", "failed", "cancelled", "interrupted"}:
            return
        if entry.get("cancel_requested"):
            kind = _mark_cancelled_locked(
                entry,
                "Cancellation acknowledged after provider execution stopped with an error.",
            )
        else:
            kind = str(entry.get("kind") or "")
            entry.update({
                "active": False,
                "state": "failed",
                "stage": "failed",
                "detail": detail,
                "updated_at": _now(),
            })
            _event(entry, "failed", detail, entry.get("progress", 0.0))
        try:
            _persist_locked(entry)
        except Exception as exc:
            persist_error = exc

    if kind in _HEAVYWEIGHT_KINDS:
        resource_ownership.release(job_id)
    if persist_error is not None:
        raise persist_error


def request_cancel(job_id: str) -> dict | None:
    with _lock:
        entry = _jobs.get(job_id)
        if entry is None:
            return None
        if not entry.get("active"):
            return _snapshot(entry)
        entry["cancel_requested"] = True
        entry["state"] = "cancelling"
        entry["stage"] = "cancelling"
        entry["detail"] = "Cancellation requested; the owned worker must terminate before the next job starts."
        entry["updated_at"] = _now()
        _event(entry, "cancelling", entry["detail"], entry.get("progress", 0.0))
        _persist_locked(entry)
        return _snapshot(entry)


def is_cancel_requested(job_id: str | None = None) -> bool:
    target = job_id or current_job_id()
    if not target:
        return False
    with _lock:
        return bool(_jobs.get(target, {}).get("cancel_requested", False))


def current() -> dict:
    with _lock:
        if not _current_id or _current_id not in _jobs:
            if _recovery_warning:
                return {
                    "active": False,
                    "job_id": None,
                    "kind": "",
                    "state": "recovery-error",
                    "stage": "recovery-error",
                    "detail": _recovery_warning,
                    "progress": 0.0,
                    "provider": None,
                    "cancel_requested": False,
                    "sequence": 0,
                    "started_at": None,
                    "updated_at": _now(),
                    "context": {},
                    "events": [],
                    "resource_owner": resource_ownership.snapshot(),
                }
            return {
                "active": False,
                "job_id": None,
                "kind": "",
                "state": "idle",
                "stage": "idle",
                "detail": "No AI job has run in this backend session yet.",
                "progress": 0.0,
                "provider": None,
                "cancel_requested": False,
                "sequence": 0,
                "started_at": None,
                "updated_at": _now(),
                "context": {},
                "events": [],
                "resource_owner": resource_ownership.snapshot(),
            }
        return _snapshot(_jobs[_current_id])


def get(job_id: str) -> dict | None:
    with _lock:
        entry = _jobs.get(job_id)
        return _snapshot(entry) if entry is not None else None


def get_events(job_id: str, after: int = 0) -> list[dict] | None:
    with _lock:
        entry = _jobs.get(job_id)
        if entry is None:
            return None
        return [deepcopy(event) for event in entry.get("events", []) if int(event.get("sequence", 0)) > after]


recover_persisted_state()
