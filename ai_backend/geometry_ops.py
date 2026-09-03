from __future__ import annotations

import math
import os
from pathlib import Path
from typing import Iterable

import numpy as np
import trimesh

MAX_VOXEL_CELLS = int(os.getenv("MINISCULPTER_MAX_VOXEL_CELLS", "100000000"))
MAX_INTERSECTION_PAIRS = int(os.getenv("MINISCULPTER_MAX_INTERSECTION_PAIRS", "200000"))


def _load_mesh(path: str) -> trimesh.Trimesh:
    p = Path(path).resolve()
    if not p.exists() or not p.is_file():
        raise FileNotFoundError(f"Input mesh does not exist: {p}")
    if p.stat().st_size == 0:
        raise ValueError(f"Input mesh is empty on disk: {p}")
    mesh = trimesh.load_mesh(p, force="mesh", process=False)
    if isinstance(mesh, trimesh.Scene):
        if not mesh.geometry:
            raise ValueError(f"Mesh scene contains no geometry: {p}")
        mesh = trimesh.util.concatenate(tuple(mesh.geometry.values()))
    if mesh.is_empty or len(mesh.vertices) < 3 or len(mesh.faces) < 1:
        raise ValueError(f"Mesh contains no usable triangles: {p}")
    if not mesh.is_finite:
        raise ValueError(f"Mesh contains non-finite coordinates: {p}")
    return mesh


def estimate_voxel_cells(mesh: trimesh.Trimesh, voxel_size: float) -> int:
    if not math.isfinite(voxel_size) or voxel_size <= 0:
        raise ValueError("voxel_size must be a finite value greater than zero")
    extents = mesh.extents
    if len(extents) != 3 or any(not math.isfinite(float(v)) for v in extents):
        raise ValueError("Mesh bounds are invalid")
    dims = [max(1, int(math.ceil(float(v) / voxel_size)) + 3) for v in extents]
    return dims[0] * dims[1] * dims[2]


def _edge_incidence(mesh: trimesh.Trimesh) -> tuple[int, int]:
    edges = np.sort(mesh.edges, axis=1)
    if len(edges) == 0:
        return 0, 0
    _, counts = np.unique(edges, axis=0, return_counts=True)
    return int(np.count_nonzero(counts == 1)), int(np.count_nonzero(counts > 2))


def _degenerate_faces(mesh: trimesh.Trimesh) -> int:
    tri = mesh.triangles
    if len(tri) == 0:
        return 0
    cross = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    area2 = np.linalg.norm(cross, axis=1)
    scale = max(float(np.max(mesh.extents)), 1.0)
    eps = max(scale * scale * 1e-12, 1e-12)
    return int(np.count_nonzero(area2 <= eps))


def _feature_size_heuristic(mesh: trimesh.Trimesh, threshold_mm: float) -> dict:
    tri = mesh.triangles
    if len(tri) == 0:
        return {"threshold_mm": threshold_mm, "flagged_triangles": 0, "sampled_triangles": 0, "minimum_altitude_mm": None}
    a = np.linalg.norm(tri[:, 1] - tri[:, 0], axis=1)
    b = np.linalg.norm(tri[:, 2] - tri[:, 1], axis=1)
    c = np.linalg.norm(tri[:, 0] - tri[:, 2], axis=1)
    area2 = np.linalg.norm(np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0]), axis=1)
    longest = np.maximum(np.maximum(a, b), c)
    alt = np.divide(area2, longest, out=np.zeros_like(area2), where=longest > 1e-12)
    positive = alt[alt > 1e-9]
    minimum = float(np.min(positive)) if len(positive) else None
    flagged = int(np.count_nonzero((alt > 0) & (alt < threshold_mm)))
    return {
        "threshold_mm": float(threshold_mm),
        "flagged_triangles": flagged,
        "sampled_triangles": int(len(tri)),
        "minimum_altitude_mm": minimum,
        "meaning": "triangle-altitude feature-size heuristic; this is not a true wall-thickness measurement",
    }


def _self_intersection_heuristic(mesh: trimesh.Trimesh) -> dict:
    # Broad-phase triangle AABB overlap count. Adjacent triangles are excluded.
    # It intentionally does not claim exact triangle/triangle intersection.
    tri = mesh.triangles
    n = len(tri)
    if n < 2:
        return {"candidate_pairs": 0, "tested_pairs": 0, "truncated": False}
    mins = tri.min(axis=1)
    maxs = tri.max(axis=1)
    faces = mesh.faces
    candidates = 0
    tested = 0
    truncated = False
    for i in range(n):
        overlap = np.where(np.all(maxs[i + 1:] >= mins[i], axis=1) & np.all(mins[i + 1:] <= maxs[i], axis=1))[0]
        for rel in overlap:
            j = i + 1 + int(rel)
            if len(set(faces[i].tolist()).intersection(faces[j].tolist())) > 0:
                continue
            candidates += 1
            tested += 1
            if tested >= MAX_INTERSECTION_PAIRS:
                truncated = True
                return {"candidate_pairs": candidates, "tested_pairs": tested, "truncated": truncated,
                        "meaning": "non-adjacent triangle AABB-overlap heuristic; candidates are not guaranteed intersections"}
    return {"candidate_pairs": candidates, "tested_pairs": tested, "truncated": truncated,
            "meaning": "non-adjacent triangle AABB-overlap heuristic; candidates are not guaranteed intersections"}


def analyze_mesh(input_path: str, feature_threshold_mm: float = 0.6) -> dict:
    mesh = _load_mesh(input_path)
    open_edges, nonmanifold_edges = _edge_incidence(mesh)
    components = mesh.split(only_watertight=False)
    ext = [float(v) for v in mesh.extents]
    result = {
        "vertices": int(len(mesh.vertices)),
        "triangles": int(len(mesh.faces)),
        "bounds_mm": ext,
        "watertight": bool(mesh.is_watertight),
        "winding_consistent": bool(mesh.is_winding_consistent),
        "open_edges": open_edges,
        "nonmanifold_edges": nonmanifold_edges,
        "connected_shells": int(len(components)),
        "degenerate_faces": _degenerate_faces(mesh),
        "volume_mm3": float(abs(mesh.volume)) if mesh.is_watertight and math.isfinite(float(mesh.volume)) else None,
        "surface_area_mm2": float(mesh.area) if math.isfinite(float(mesh.area)) else None,
        "feature_size": _feature_size_heuristic(mesh, feature_threshold_mm),
        "self_intersection": _self_intersection_heuristic(mesh),
    }
    result["structurally_printable"] = bool(
        result["watertight"] and result["winding_consistent"] and result["open_edges"] == 0
        and result["nonmanifold_edges"] == 0 and result["degenerate_faces"] == 0
    )
    return result


def voxel_remesh(input_paths: Iterable[str], output_path: str, voxel_size: float = 0.35) -> str:
    paths = [str(Path(p).resolve()) for p in input_paths]
    if not paths:
        raise ValueError("At least one input mesh is required")
    if not math.isfinite(voxel_size) or voxel_size <= 0:
        raise ValueError("voxel_size must be a finite value greater than zero")

    meshes = [_load_mesh(p) for p in paths]
    combined = meshes[0] if len(meshes) == 1 else trimesh.util.concatenate(meshes)
    cells = estimate_voxel_cells(combined, float(voxel_size))
    if cells > MAX_VOXEL_CELLS:
        ext = combined.extents
        recommended = max(float(ext.max()) / 450.0, voxel_size)
        raise MemoryError(
            f"Requested voxel grid is approximately {cells:,} cells, above the safety limit of {MAX_VOXEL_CELLS:,}. "
            f"Increase voxel size (try about {recommended:.2f} mm or larger), reduce model size, or raise MINISCULPTER_MAX_VOXEL_CELLS if the machine has enough RAM."
        )

    grid = combined.voxelized(pitch=float(voxel_size)).fill()
    if grid.shape is None or any(int(v) <= 0 for v in grid.shape):
        raise RuntimeError("Voxelization produced an invalid occupancy grid")
    result = grid.marching_cubes
    if result.is_empty:
        raise RuntimeError("Voxel reconstruction produced an empty mesh")

    result.apply_transform(grid.transform)
    result.remove_unreferenced_vertices()
    result.merge_vertices()
    if not result.is_finite:
        raise RuntimeError("Voxel reconstruction produced invalid coordinates")

    out = Path(output_path).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    result.export(out, file_type="stl")
    if not out.exists() or out.stat().st_size == 0:
        raise RuntimeError("Voxel reconstruction finished but no STL was written")
    return str(out)


def repair_mesh(input_path: str, output_path: str, voxel_size: float = 0.30) -> dict:
    before = analyze_mesh(input_path)
    path = voxel_remesh([input_path], output_path, voxel_size)
    after = analyze_mesh(path)
    return {"path": path, "voxel_size": float(voxel_size), "before": before, "after": after,
            "method": "filled voxel reconstruction; destructive and may soften details below the selected voxel pitch"}
