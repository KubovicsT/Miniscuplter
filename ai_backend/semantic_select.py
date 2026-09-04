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
from PIL import Image

from model_manager import component_path

SMART_SELECT_COMMAND = os.getenv("MINISCULPTER_SMART_SELECT_COMMAND", "").strip()
_MODEL = None
_PROCESSOR = None
_DEVICE = None


def _load_mesh(path: str) -> trimesh.Trimesh:
    loaded = trimesh.load(path, force="mesh", process=False)
    if isinstance(loaded, trimesh.Scene):
        if not loaded.geometry:
            raise ValueError("Mesh scene contains no geometry")
        loaded = trimesh.util.concatenate(tuple(loaded.geometry.values()))
    if not isinstance(loaded, trimesh.Trimesh) or len(loaded.vertices) == 0 or len(loaded.faces) == 0:
        raise ValueError("Mesh contains no usable triangles")
    if not np.isfinite(loaded.vertices).all():
        raise ValueError("Mesh contains non-finite coordinates")
    return loaded


def _load_clipseg():
    global _MODEL, _PROCESSOR, _DEVICE
    model_dir = component_path("clipseg-smart-select")
    if model_dir is None:
        return None
    if _MODEL is not None and _PROCESSOR is not None:
        return _MODEL, _PROCESSOR, _DEVICE
    try:
        import torch
        from transformers import CLIPSegForImageSegmentation, CLIPSegProcessor
    except Exception as exc:
        raise RuntimeError("CLIPSeg dependencies are unavailable. Run setup_ai_backend.bat again.") from exc
    _DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
    _PROCESSOR = CLIPSegProcessor.from_pretrained(str(model_dir), local_files_only=True)
    dtype = torch.float16 if _DEVICE == "cuda" else torch.float32
    _MODEL = CLIPSegForImageSegmentation.from_pretrained(str(model_dir), local_files_only=True, torch_dtype=dtype)
    _MODEL.to(_DEVICE)
    _MODEL.eval()
    return _MODEL, _PROCESSOR, _DEVICE


def release_model() -> None:
    global _MODEL, _PROCESSOR, _DEVICE
    _MODEL = None; _PROCESSOR = None; _DEVICE = None
    try:
        import torch
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
    except Exception:
        pass


def _view_basis(direction: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    forward = direction / max(np.linalg.norm(direction), 1e-9)
    up_hint = np.array([0.0, 1.0, 0.0], dtype=np.float64)
    if abs(np.dot(forward, up_hint)) > 0.92:
        up_hint = np.array([0.0, 0.0, 1.0], dtype=np.float64)
    right = np.cross(up_hint, forward); right /= max(np.linalg.norm(right), 1e-9)
    up = np.cross(forward, right); up /= max(np.linalg.norm(up), 1e-9)
    return right, up, forward


def _render_view(mesh: trimesh.Trimesh, direction: np.ndarray, size: int = 352) -> tuple[Image.Image, np.ndarray]:
    vertices = np.asarray(mesh.vertices, dtype=np.float64)
    faces = np.asarray(mesh.faces, dtype=np.int64)
    right, up, forward = _view_basis(direction)
    center = (vertices.min(axis=0) + vertices.max(axis=0)) * 0.5
    local = vertices - center
    px = local @ right; py = local @ up; pz = local @ forward
    extent = max(float(np.ptp(px)), float(np.ptp(py)), 1e-6) * 0.56
    sx = (px / extent * 0.5 + 0.5) * (size - 1)
    sy = (0.5 - py / extent * 0.5) * (size - 1)
    depth = pz

    zbuf = np.full((size, size), -np.inf, dtype=np.float32)
    face_ids = np.full((size, size), -1, dtype=np.int32)
    image = np.full((size, size), 24, dtype=np.uint8)
    normals = np.asarray(mesh.face_normals, dtype=np.float64)
    light = (-forward + up * 0.45 + right * 0.25)
    light /= max(np.linalg.norm(light), 1e-9)

    for fi, (ia, ib, ic) in enumerate(faces):
        xs = np.array([sx[ia], sx[ib], sx[ic]], dtype=np.float64)
        ys = np.array([sy[ia], sy[ib], sy[ic]], dtype=np.float64)
        minx = max(0, int(math.floor(xs.min()))); maxx = min(size - 1, int(math.ceil(xs.max())))
        miny = max(0, int(math.floor(ys.min()))); maxy = min(size - 1, int(math.ceil(ys.max())))
        if minx > maxx or miny > maxy:
            continue
        ax, ay = xs[0], ys[0]; bx, by = xs[1], ys[1]; cx, cy = xs[2], ys[2]
        denom = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(denom) < 1e-8:
            continue
        shade = int(np.clip(95 + 145 * max(0.0, float(np.dot(normals[fi], light))), 45, 240))
        for y in range(miny, maxy + 1):
            for x in range(minx, maxx + 1):
                fx, fy = x + 0.5, y + 0.5
                w1 = ((by - cy) * (fx - cx) + (cx - bx) * (fy - cy)) / denom
                w2 = ((cy - ay) * (fx - cx) + (ax - cx) * (fy - cy)) / denom
                w3 = 1.0 - w1 - w2
                if w1 < -1e-5 or w2 < -1e-5 or w3 < -1e-5:
                    continue
                z = w1 * depth[ia] + w2 * depth[ib] + w3 * depth[ic]
                if z > zbuf[y, x]:
                    zbuf[y, x] = z
                    face_ids[y, x] = fi
                    image[y, x] = shade
    rgb = np.stack([image, image, image], axis=-1)
    return Image.fromarray(rgb, mode="RGB"), face_ids


def _multi_view_clipseg(mesh: trimesh.Trimesh, query: str) -> np.ndarray:
    loaded = _load_clipseg()
    if loaded is None:
        raise RuntimeError("CLIPSeg Smart Select is not installed")
    model, processor, device = loaded
    import torch

    directions = [
        np.array([0, 0, 1.0]), np.array([0, 0, -1.0]),
        np.array([1.0, 0, 0]), np.array([-1.0, 0, 0]),
        np.array([0, 1.0, 0]), np.array([0, -1.0, 0]),
    ]
    renders, face_maps = zip(*[_render_view(mesh, d) for d in directions])
    inputs = processor(text=[query] * len(renders), images=list(renders), padding=True, return_tensors="pt")
    inputs = {k: v.to(device) for k, v in inputs.items()}
    with torch.inference_mode():
        logits = model(**inputs).logits
        probs = torch.sigmoid(logits).float().cpu().numpy()

    face_scores = np.zeros(len(mesh.faces), dtype=np.float64)
    face_hits = np.zeros(len(mesh.faces), dtype=np.int32)
    for prob, fmap in zip(probs, face_maps):
        if prob.shape != fmap.shape:
            prob_img = Image.fromarray(np.uint8(np.clip(prob, 0, 1) * 255)).resize((fmap.shape[1], fmap.shape[0]), Image.Resampling.BILINEAR)
            prob = np.asarray(prob_img, dtype=np.float32) / 255.0
        valid = fmap >= 0
        ids = fmap[valid]
        vals = prob[valid]
        for fi in np.unique(ids):
            score = float(np.percentile(vals[ids == fi], 70))
            face_scores[fi] += score
            face_hits[fi] += 1
    nz = face_hits > 0
    face_scores[nz] /= face_hits[nz]

    vertex_scores = np.zeros(len(mesh.vertices), dtype=np.float64)
    counts = np.zeros(len(mesh.vertices), dtype=np.int32)
    for fi, face in enumerate(mesh.faces):
        for vi in face:
            vertex_scores[vi] += face_scores[fi]
            counts[vi] += 1
    good = counts > 0
    vertex_scores[good] /= counts[good]
    if vertex_scores.max(initial=0.0) > 0:
        peak = float(np.percentile(vertex_scores[vertex_scores > 0], 95))
        if peak > 1e-6:
            vertex_scores = np.clip(vertex_scores / peak, 0.0, 1.0)
    return vertex_scores


def _heuristic(vertices: np.ndarray, query: str) -> np.ndarray:
    q = query.lower().strip(); lo = vertices.min(axis=0); hi = vertices.max(axis=0)
    span = np.maximum(hi - lo, 1e-8); n = (vertices - lo) / span
    x, y, z = n[:, 0], n[:, 1], n[:, 2]; center_x = 1.0 - np.minimum(1.0, np.abs(x - 0.5) * 2.0)
    if any(k in q for k in ("head", "face", "hair", "helmet", "skull", "horn")):
        w = np.clip((y - 0.68) / 0.20, 0, 1) * np.clip(center_x * 1.4, 0, 1)
        if "face" in q: w *= np.clip((z - 0.42) / 0.35, 0, 1)
    elif any(k in q for k in ("torso", "body", "chest", "waist", "abdomen")):
        w = np.clip(1 - np.abs(y - 0.55) / 0.32, 0, 1) * np.clip(center_x * 1.5, 0, 1)
    elif "base" in q or "ground" in q: w = np.clip((0.18 - y) / 0.18, 0, 1)
    else: w = np.zeros(len(vertices), dtype=np.float64)
    return np.asarray(w, dtype=np.float64)


def _samples(vertices: np.ndarray, weights: np.ndarray) -> tuple[list[list[float]], list[float]]:
    merged: dict[tuple[float, float, float], float] = {}
    for p, w in zip(vertices, weights):
        if not np.isfinite(w) or w <= 0.01: continue
        key = (round(float(p[0]), 6), round(float(p[1]), 6), round(float(p[2]), 6))
        merged[key] = max(merged.get(key, 0.0), float(np.clip(w, 0.0, 1.0)))
    return [list(k) for k in merged.keys()], list(merged.values())


def _normalize_provider_result(data: dict, vertices: np.ndarray) -> dict:
    method = data.get("method") or "external AI semantic provider"
    if isinstance(data.get("sample_positions_mm"), list) and isinstance(data.get("sample_weights"), list):
        return {"method": method, "sample_positions_mm": data["sample_positions_mm"], "sample_weights": data["sample_weights"], "ai_provider_available": True}
    weights = np.zeros(len(vertices), dtype=np.float64)
    if isinstance(data.get("weights"), list):
        raw = np.asarray(data["weights"], dtype=np.float64); n = min(len(raw), len(weights)); weights[:n] = np.clip(raw[:n], 0.0, 1.0)
    elif isinstance(data.get("indices"), list):
        for i in data["indices"]:
            if isinstance(i, int) and 0 <= i < len(weights): weights[i] = 1.0
    else:
        raise RuntimeError("AI Smart Select provider must return sample_positions_mm+sample_weights, weights, or indices")
    positions, sample_weights = _samples(vertices, weights)
    return {"method": method, "sample_positions_mm": positions, "sample_weights": sample_weights, "ai_provider_available": True}


def semantic_select(input_path: str, query: str) -> dict:
    if not query or not query.strip(): raise ValueError("Selection query is empty")
    path = str(Path(input_path).resolve())
    if not Path(path).exists(): raise FileNotFoundError(path)
    mesh = _load_mesh(path); vertices = np.asarray(mesh.vertices, dtype=np.float64)

    if SMART_SELECT_COMMAND:
        with tempfile.TemporaryDirectory(prefix="miniscuplter_select_") as td:
            output = str(Path(td) / "selection.json")
            command = SMART_SELECT_COMMAND.format(input=shlex.quote(path), output=shlex.quote(output), query=shlex.quote(query))
            completed = subprocess.run(command, shell=True, capture_output=True, text=True, timeout=900)
            if completed.returncode != 0: raise RuntimeError("External AI Smart Select provider failed: " + completed.stderr[-3000:])
            if not Path(output).exists(): raise RuntimeError("External AI Smart Select provider did not create its JSON output")
            data = json.loads(Path(output).read_text(encoding="utf-8"))
            if not isinstance(data, dict): raise RuntimeError("External AI Smart Select provider returned invalid JSON")
            result = _normalize_provider_result(data, vertices); result["query"] = query; return result

    if component_path("clipseg-smart-select") is not None:
        weights = _multi_view_clipseg(mesh, query)
        positions, sample_weights = _samples(vertices, weights)
        return {
            "method": "local CLIPSeg multi-view semantic segmentation",
            "query": query,
            "sample_positions_mm": positions,
            "sample_weights": sample_weights,
            "selected_samples": int(sum(1 for w in sample_weights if w >= 0.45)),
            "ai_provider_available": True,
            "views": 6,
        }

    weights = _heuristic(vertices, query); positions, sample_weights = _samples(vertices, weights)
    return {
        "method": "geometry semantic fallback (install CLIPSeg Smart Select for arbitrary semantic parts)",
        "query": query,
        "sample_positions_mm": positions,
        "sample_weights": sample_weights,
        "selected_samples": int(sum(1 for w in sample_weights if w >= 0.35)),
        "ai_provider_available": False,
    }
