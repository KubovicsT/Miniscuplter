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
    return {"threshold_mm": float(threshold_mm), "flagged_triangles": flagged, "sampled_triangles": int(len(tri)), "minimum_altitude_mm": minimum,
            "meaning": "triangle-altitude feature-size heuristic; this is not a true wall-thickness measurement and is advisory only"}


def _self_intersection_heuristic(mesh: trimesh.Trimesh) -> dict:
    """Count non-adjacent triangle AABB overlaps with a sweep broad phase.

    This deliberately remains an advisory candidate count rather than claiming exact triangle
    intersections. Sorting by minimum X avoids the previous quadratic full-tail comparison on
    every triangle, which was impractical for detailed meshes.
    """
    tri = mesh.triangles
    n = len(tri)
    if n < 2:
        return {"candidate_pairs": 0, "tested_pairs": 0, "truncated": False}
    mins = tri.min(axis=1)
    maxs = tri.max(axis=1)
    faces = np.asarray(mesh.faces)
    order = np.argsort(mins[:, 0], kind="stable")
    sorted_min_x = mins[order, 0]
    candidates = 0
    tested = 0

    for pos, face_i in enumerate(order):
        # Only later triangles whose minimum X lies before this triangle's maximum X can overlap.
        end = int(np.searchsorted(sorted_min_x, maxs[face_i, 0], side="right"))
        if end <= pos + 1:
            continue
        later = order[pos + 1:end]
        if len(later) == 0:
            continue
        yz = (
            (maxs[later, 1] >= mins[face_i, 1]) &
            (mins[later, 1] <= maxs[face_i, 1]) &
            (maxs[later, 2] >= mins[face_i, 2]) &
            (mins[later, 2] <= maxs[face_i, 2])
        )
        overlaps = later[yz]
        if len(overlaps) == 0:
            continue
        vertices_i = set(faces[face_i].tolist())
        for face_j in overlaps:
            # Adjacent triangles legitimately share an AABB boundary and are not candidates.
            if vertices_i.intersection(faces[face_j].tolist()):
                continue
            candidates += 1
            tested += 1
            if tested >= MAX_INTERSECTION_PAIRS:
                return {"candidate_pairs": candidates, "tested_pairs": tested, "truncated": True,
                        "meaning": "non-adjacent triangle AABB-overlap heuristic using an axis-sorted sweep; candidates are not guaranteed intersections"}
    return {"candidate_pairs": candidates, "tested_pairs": tested, "truncated": False,
            "meaning": "non-adjacent triangle AABB-overlap heuristic using an axis-sorted sweep; candidates are not guaranteed intersections"}


def thickness_map(input_path: str, target_mm: float = 0.8, max_samples: int = 12000) -> dict:
    """Estimate local wall thickness using inward multi-direction ray casting.

    Sample positions and values are returned in the same world-space coordinate frame as the exported STL.
    The editor spatially transfers those samples to its live mesh, so STL vertex re-indexing does not matter.
    """
    mesh = _load_mesh(input_path)
    if not math.isfinite(target_mm) or target_mm <= 0:
        raise ValueError("target_mm must be greater than zero")
    max_samples = max(100, min(int(max_samples), 100000))
    vertices = np.asarray(mesh.vertices, dtype=float)
    normals = np.asarray(mesh.vertex_normals, dtype=float)
    count = len(vertices)
    if count == 0:
        raise ValueError("Mesh contains no vertices")
    sample_idx = np.arange(count, dtype=int) if count <= max_samples else np.unique(np.linspace(0, count - 1, max_samples).astype(int))
    origins = vertices[sample_idx]
    n = normals[sample_idx]
    eps = max(float(np.max(mesh.extents)) * 1e-6, 1e-5)

    dirs_all = []
    for normal in n:
        inward = -normal / max(np.linalg.norm(normal), 1e-12)
        axis = np.array([1.0, 0.0, 0.0]) if abs(inward[0]) < 0.8 else np.array([0.0, 1.0, 0.0])
        t1 = np.cross(inward, axis); t1 /= max(np.linalg.norm(t1), 1e-12)
        t2 = np.cross(inward, t1); t2 /= max(np.linalg.norm(t2), 1e-12)
        dirs_all.append([inward,
                         (inward + .18 * t1).astype(float), (inward - .18 * t1).astype(float),
                         (inward + .18 * t2).astype(float), (inward - .18 * t2).astype(float)])
    dirs_all = np.asarray(dirs_all, dtype=float)
    dirs_all /= np.maximum(np.linalg.norm(dirs_all, axis=2, keepdims=True), 1e-12)
    best = np.full(len(sample_idx), np.nan, dtype=float)
    intersector = trimesh.ray.ray_triangle.RayMeshIntersector(mesh)
    for k in range(dirs_all.shape[1]):
        d = dirs_all[:, k, :]
        o = origins + d * eps
        locations, ray_ids, _ = intersector.intersects_location(o, d, multiple_hits=False)
        if len(locations):
            dist = np.linalg.norm(locations - o[ray_ids], axis=1) + eps
            valid = dist > eps * 2
            for rid, value in zip(ray_ids[valid], dist[valid]):
                if not math.isfinite(best[rid]) or value < best[rid]:
                    best[rid] = float(value)

    finite_mask = np.isfinite(best)
    finite_values = best[finite_mask]
    finite_positions = origins[finite_mask]
    below = int(np.count_nonzero(finite_values < target_mm))
    return {
        "target_mm": float(target_mm), "source_vertex_count": int(count), "ray_sampled_vertices": int(len(sample_idx)),
        "resolved_samples": int(len(finite_values)), "below_target_samples": below,
        "minimum_mm": float(np.min(finite_values)) if len(finite_values) else None,
        "maximum_mm": float(np.max(finite_values)) if len(finite_values) else None,
        "sample_positions_mm": [[float(x), float(y), float(z)] for x, y, z in finite_positions],
        "sample_values_mm": [float(v) for v in finite_values],
        "method": "multi-direction inward ray distance to opposite surface; viewport values are spatially interpolated from resolved samples",
    }


def analyze_mesh(input_path: str, feature_threshold_mm: float = 0.6) -> dict:
    mesh = _load_mesh(input_path)
    open_edges, nonmanifold_edges = _edge_incidence(mesh)
    components = mesh.split(only_watertight=False)
    result = {"vertices": int(len(mesh.vertices)), "triangles": int(len(mesh.faces)), "bounds_mm": [float(v) for v in mesh.extents],
              "watertight": bool(mesh.is_watertight), "winding_consistent": bool(mesh.is_winding_consistent), "open_edges": open_edges,
              "nonmanifold_edges": nonmanifold_edges, "connected_shells": int(len(components)), "degenerate_faces": _degenerate_faces(mesh),
              "volume_mm3": float(abs(mesh.volume)) if mesh.is_watertight and math.isfinite(float(mesh.volume)) else None,
              "surface_area_mm2": float(mesh.area) if math.isfinite(float(mesh.area)) else None,
              "feature_size": _feature_size_heuristic(mesh, feature_threshold_mm), "self_intersection": _self_intersection_heuristic(mesh)}
    structurally_valid = bool(result["watertight"] and result["winding_consistent"] and result["open_edges"] == 0 and result["nonmanifold_edges"] == 0 and result["degenerate_faces"] == 0)
    result["structurally_valid"] = structurally_valid
    # Compatibility alias for project/backend clients from pre-v1.0 builds. It is not used as a printability requirement in v1.0.
    result["structurally_printable"] = structurally_valid
    return result


def voxel_remesh(input_paths: Iterable[str], output_path: str, voxel_size: float = 0.35) -> str:
    paths = [str(Path(p).resolve()) for p in input_paths]
    if not paths: raise ValueError("At least one input mesh is required")
    if not math.isfinite(voxel_size) or voxel_size <= 0: raise ValueError("voxel_size must be a finite value greater than zero")
    meshes = [_load_mesh(p) for p in paths]
    combined = meshes[0] if len(meshes) == 1 else trimesh.util.concatenate(meshes)
    cells = estimate_voxel_cells(combined, float(voxel_size))
    if cells > MAX_VOXEL_CELLS:
        recommended = max(float(combined.extents.max()) / 450.0, voxel_size)
        raise MemoryError(f"Requested voxel grid is approximately {cells:,} cells, above the safety limit of {MAX_VOXEL_CELLS:,}. Increase voxel size (try about {recommended:.2f} mm or larger), reduce model size, or raise the configured voxel safety budget if the machine has enough RAM.")
    grid = combined.voxelized(pitch=float(voxel_size)).fill()
    if grid.shape is None or any(int(v) <= 0 for v in grid.shape): raise RuntimeError("Voxelization produced an invalid occupancy grid")
    result = grid.marching_cubes
    if result.is_empty: raise RuntimeError("Voxel reconstruction produced an empty mesh")
    # VoxelGrid.marching_cubes returns matrix-index geometry; the grid transform maps it
    # back to the original world-space pitch/origin exactly once.
    result.apply_transform(grid.transform); result.remove_unreferenced_vertices(); result.merge_vertices()
    if not result.is_finite: raise RuntimeError("Voxel reconstruction produced invalid coordinates")
    out = Path(output_path).resolve(); out.parent.mkdir(parents=True, exist_ok=True); result.export(out, file_type="stl")
    if not out.exists() or out.stat().st_size == 0: raise RuntimeError("Voxel reconstruction finished but no STL was written")
    return str(out)


def repair_mesh(input_path: str, output_path: str, voxel_size: float = 0.30) -> dict:
    before = analyze_mesh(input_path); path = voxel_remesh([input_path], output_path, voxel_size); after = analyze_mesh(path)
    return {"path": path, "voxel_size": float(voxel_size), "before": before, "after": after,
            "method": "filled voxel reconstruction; destructive and may soften details below the selected voxel pitch"}
