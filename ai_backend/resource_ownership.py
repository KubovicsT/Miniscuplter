from __future__ import annotations

from copy import deepcopy
from threading import Lock
from time import time


class ResourceBusyError(RuntimeError):
    pass


_gate = Lock()
_state_lock = Lock()
_owner: dict | None = None


def acquire(owner_id: str, kind: str, *, blocking: bool = True) -> dict:
    """Acquire the single heavyweight local-runtime lease.

    This is intentionally process-local for the first MS-020 migration slice. It gives the
    backend one explicit owner for heavyweight work without pretending crash recovery or
    cross-process isolation is complete yet.
    """
    owner_id = str(owner_id or "").strip()
    kind = str(kind or "").strip()
    if not owner_id:
        raise ValueError("A resource owner id is required.")
    if not kind:
        raise ValueError("A resource owner kind is required.")

    acquired = _gate.acquire(blocking=blocking)
    if not acquired:
        current = snapshot()
        raise ResourceBusyError(
            f"Heavyweight local runtime is already owned by {current.get('kind') or 'another job'} "
            f"({current.get('owner_id') or 'unknown owner'})."
        )

    now = time()
    with _state_lock:
        global _owner
        _owner = {
            "owner_id": owner_id,
            "kind": kind,
            "acquired_at": now,
        }
        return deepcopy(_owner)


def release(owner_id: str) -> bool:
    owner_id = str(owner_id or "").strip()
    if not owner_id:
        return False

    with _state_lock:
        global _owner
        if _owner is None or _owner.get("owner_id") != owner_id:
            return False
        _owner = None

    _gate.release()
    return True


def snapshot() -> dict:
    with _state_lock:
        if _owner is None:
            return {
                "active": False,
                "owner_id": None,
                "kind": None,
                "acquired_at": None,
            }
        result = deepcopy(_owner)
        result["active"] = True
        return result
