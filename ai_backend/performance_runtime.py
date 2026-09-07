from __future__ import annotations

import json
import os
from pathlib import Path

_DATA_ROOT = Path(os.getenv("MINISCULPTER_DATA", Path(os.getenv("MINISCULPTER_ROOT", Path(__file__).resolve().parent.parent)) / "AIData")).resolve()
_STATE_FILE = _DATA_ROOT / "performance_runtime.json"
_DEFAULT = {"mode": "auto", "vram_target_fraction": 0.85}
_VALID_MODES = {"auto", "fast", "balanced", "safe"}


def normalize(data: dict | None) -> dict:
    src = data or {}
    mode = str(src.get("mode", _DEFAULT["mode"])).strip().lower()
    if mode not in _VALID_MODES:
        mode = _DEFAULT["mode"]
    try:
        target = float(src.get("vram_target_fraction", _DEFAULT["vram_target_fraction"]))
    except (TypeError, ValueError):
        target = _DEFAULT["vram_target_fraction"]
    return {"mode": mode, "vram_target_fraction": max(0.50, min(0.95, target))}


def get_config() -> dict:
    try:
        if _STATE_FILE.is_file():
            data = json.loads(_STATE_FILE.read_text(encoding="utf-8"))
            if isinstance(data, dict):
                return normalize(data)
    except Exception:
        pass
    return dict(_DEFAULT)


def set_config(data: dict) -> dict:
    cfg = normalize(data)
    _DATA_ROOT.mkdir(parents=True, exist_ok=True)
    tmp = _STATE_FILE.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(cfg, indent=2), encoding="utf-8")
    tmp.replace(_STATE_FILE)
    return cfg
