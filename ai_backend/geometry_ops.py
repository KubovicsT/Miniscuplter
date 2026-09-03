from __future__ import annotations

from pathlib import Path
from typing import Iterable

import trimesh


def _load_mesh(path: str) -> trimesh.Trimesh:
    mesh = trimesh.load_mesh(path, force="mesh", process=False)
    if isinstance(mesh, trimesh.Scene):
        mesh = trimesh.util.concatenate(tuple(mesh.geometry.values()))
    if mesh.is_empty:
        raise ValueError(f"Mesh is empty: {path}")
    return mesh


def voxel_remesh(input_paths: Iterable[str], output_path: str, voxel_size: float = 0.35) -> str:
    paths = [str(Path(p).resolve()) for p in input_paths]
    if not paths:
        raise ValueError("At least one input mesh is required")
    if voxel_size <= 0:
        raise ValueError("voxel_size must be greater than zero")

    meshes = [_load_mesh(p) for p in paths]
    combined = meshes[0] if len(meshes) == 1 else trimesh.util.concatenate(meshes)

    # Voxelization turns intersecting or merely overlapping shells into one occupancy field.
    # fill() closes interior voxels and marching_cubes reconstructs a fresh triangle surface.
    grid = combined.voxelized(pitch=float(voxel_size)).fill()
    result = grid.marching_cubes
    if result.is_empty:
        raise RuntimeError("Voxel reconstruction produced an empty mesh")

    # marching_cubes is expressed in voxel-index units. Reapply the voxel grid transform
    # so the reconstructed model returns to the original world-space millimetre coordinates.
    result.apply_transform(grid.transform)
    result.remove_unreferenced_vertices()
    result.merge_vertices()

    out = Path(output_path).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    result.export(out, file_type="stl")
    return str(out)
