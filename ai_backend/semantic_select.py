from __future__ import annotations

import json
import math
import os
import shlex
import subprocess
import tempfile
from pathlib import Path

import numpy as np
import trimesh

SMART_SELECT_COMMAND = os.getenv("MINISCULPTER_SMART_SELECT_COMMAND", "").strip()


def _load_vertices(path: str) -> np.ndarray:
    loaded = trimesh.load(path, force="mesh", process=False)
    if isinstance(loaded, trimesh.Scene):
        if not loaded.geometry:
            raise ValueError("Mesh scene contains no geometry")
        loaded = trimesh.util.concatenate(tuple(loaded.geometry.values()))
    vertices = np.asarray(loaded.vertices, dtype=np.float64)
    if vertices.ndim != 2 or vertices.shape[1] != 3 or len(vertices) == 0:
        raise ValueError("Mesh contains no usable vertices")
    if not np.isfinite(vertices).all():
        raise ValueError("Mesh contains non-finite coordinates")
    return vertices


def _heuristic(vertices: np.ndarray, query: str) -> np.ndarray:
    q = query.lower().strip()
    lo = vertices.min(axis=0)
    hi = vertices.max(axis=0)
    span = np.maximum(hi - lo, 1e-8)
    n = (vertices - lo) / span
    x, y, z = n[:, 0], n[:, 1], n[:, 2]
    center_x = 1.0 - np.minimum(1.0, np.abs(x - 0.5) * 2.0)
    center_z = 1.0 - np.minimum(1.0, np.abs(z - 0.5) * 2.0)

    if any(k in q for k in ("head", "face", "hair", "helmet", "skull", "horn")):
        w = np.clip((y - 0.68) / 0.20, 0, 1) * np.clip(center_x * 1.4, 0, 1)
        if "face" in q:
            w *= np.clip((z - 0.42) / 0.35, 0, 1)
    elif any(k in q for k in ("torso", "body", "chest", "waist", "abdomen")):
        w = np.clip(1 - np.abs(y - 0.55) / 0.32, 0, 1) * np.clip(center_x * 1.5, 0, 1)
    elif "left hand" in q or "left arm" in q:
        w = np.clip((0.40 - x) / 0.30, 0, 1) * np.clip(1 - np.abs(y - (0.45 if "hand" in q else 0.60)) / 0.40, 0, 1)
    elif "right hand" in q or "right arm" in q:
        w = np.clip((x - 0.60) / 0.30, 0, 1) * np.clip(1 - np.abs(y - (0.45 if "hand" in q else 0.60)) / 0.40, 0, 1)
    elif "left leg" in q or "left foot" in q:
        w = np.clip((0.52 - x) / 0.35, 0, 1) * np.clip((0.55 - y) / 0.45, 0, 1)
        if "foot" in q:
            w *= np.clip((0.22 - y) / 0.22, 0, 1)
    elif "right leg" in q or "right foot" in q:
        w = np.clip((x - 0.48) / 0.35, 0, 1) * np.clip((0.55 - y) / 0.45, 0, 1)
        if "foot" in q:
            w *= np.clip((0.22 - y) / 0.22, 0, 1)
    elif "base" in q or "ground" in q:
        w = np.clip((0.18 - y) / 0.18, 0, 1)
    elif "wing" in q:
        w = np.clip((np.abs(x - 0.5) - 0.20) / 0.30, 0, 1) * np.clip((y - 0.35) / 0.40, 0, 1)
    elif "tail" in q:
        w = np.clip((0.38 - y) / 0.35, 0, 1) * np.clip(np.abs(z - 0.5) * 2.0 - 0.20, 0, 1)
    else:
        w = np.zeros(len(vertices), dtype=np.float64)
    return np.asarray(w, dtype=np.float64)


def semantic_select(input_path: str, query: str) -> dict:
    if not query or not query.strip():
        raise ValueError("Selection query is empty")
    path = str(Path(input_path).resolve())
    if not Path(path).exists():
        raise FileNotFoundError(path)

    if SMART_SELECT_COMMAND:
        with tempfile.TemporaryDirectory(prefix="miniscuplter_select_") as td:
            output = str(Path(td) / "selection.json")
            command = SMART_SELECT_COMMAND.format(
                input=shlex.quote(path),
                output=shlex.quote(output),
                query=shlex.quote(query),
            )
            completed = subprocess.run(command, shell=True, capture_output=True, text=True, timeout=900)
            if completed.returncode != 0:
                raise RuntimeError("AI Smart Select provider failed: " + completed.stderr[-3000:])
            if not Path(output).exists():
                raise RuntimeError("AI Smart Select provider did not create its JSON output")
            data = json.loads(Path(output).read_text(encoding="utf-8"))
            if not isinstance(data, dict):
                raise RuntimeError("AI Smart Select provider returned invalid JSON")
            data.setdefault("method", "AI semantic provider")
            return data

    vertices = _load_vertices(path)
    weights = _heuristic(vertices, query)
    selected = int(np.count_nonzero(weights >= 0.35))
    return {
        "method": "geometry semantic heuristic (AI provider not configured)",
        "query": query,
        "vertex_count": int(len(vertices)),
        "selected_vertices": selected,
        "weights": [float(x) for x in weights.tolist()],
        "ai_provider_available": False,
    }
