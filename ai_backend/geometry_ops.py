from __future__ import annotations

import math
import os
from pathlib import Path
from typing import Iterable

import trimesh

MAX_VOXEL_CELLS = int(os.getenv("MINISCULPTER_MAX_VOXEL_CELLS", "100000000"))


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
