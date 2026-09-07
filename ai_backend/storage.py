from __future__ import annotations

import os
from pathlib import Path


def data_root() -> Path:
    configured = os.getenv("MINISCULPTER_DATA", "").strip()
    if configured:
        root = Path(configured)
    else:
        install = Path(os.getenv("MINISCULPTER_ROOT", Path(__file__).resolve().parent.parent)).resolve()
        root = install / "AIData"
    root = root.expanduser().resolve()
    root.mkdir(parents=True, exist_ok=True)
    return root


def resolve(relative: str | Path) -> Path:
    root = data_root()
    candidate = Path(relative)
    if not candidate.is_absolute():
        candidate = root / candidate
    candidate = candidate.resolve()
    try:
        candidate.relative_to(root)
    except ValueError as exc:
        raise ValueError(f"Path escapes the Miniscuplter data root: {candidate}") from exc
    return candidate


def validate_output_path(value: str | Path) -> Path:
    if not value or not str(value).strip():
        raise ValueError("An output path is required.")
    candidate = resolve(value)
    candidate.parent.mkdir(parents=True, exist_ok=True)
    return candidate
