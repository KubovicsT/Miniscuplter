from __future__ import annotations

import sys
import tempfile
from pathlib import Path

import numpy as np
import trimesh

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "ai_backend"))

import geometry_ops


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def test_stl_topology_is_welded_for_analysis() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        path = Path(tmp) / "closed_cube.stl"
        trimesh.creation.box(extents=(10.0, 12.0, 14.0)).export(path, file_type="stl")

        raw = trimesh.load_mesh(path, force="mesh", process=False)
        check(len(raw.vertices) > 8, "fixture must reproduce STL per-face vertex duplication")

        report = geometry_ops.analyze_mesh(str(path))
        check(report["watertight"], f"closed STL cube was not recognized as watertight: {report}")
        check(report["open_edges"] == 0, f"closed STL cube reported open edges: {report['open_edges']}")
        check(report["nonmanifold_edges"] == 0, f"closed STL cube reported non-manifold edges: {report['nonmanifold_edges']}")
        check(report["structurally_valid"], "closed STL cube should pass structural topology checks")
        check(report["source_vertices"] > report["vertices"], "analysis did not weld duplicated STL vertices")


def test_finite_coordinate_validation() -> None:
    mesh = trimesh.creation.box()
    check(geometry_ops._vertices_are_finite(mesh), "ordinary box should contain finite vertices")
    broken = mesh.copy()
    vertices = np.asarray(broken.vertices).copy()
    vertices[0, 0] = np.nan
    broken.vertices = vertices
    check(not geometry_ops._vertices_are_finite(broken), "NaN coordinate was not detected")


def test_voxel_remesh_round_trip() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        source = Path(tmp) / "source.stl"
        output = Path(tmp) / "remeshed.stl"
        trimesh.creation.box(extents=(8.0, 8.0, 8.0)).export(source, file_type="stl")
        result = geometry_ops.voxel_remesh([str(source)], str(output), voxel_size=1.0)
        check(Path(result).exists() and Path(result).stat().st_size > 0, "voxel remesh did not write output")
        report = geometry_ops.analyze_mesh(result)
        check(report["triangles"] > 0, "voxel remesh output contains no triangles")


def test_thickness_has_spatial_index_dependency() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        source = Path(tmp) / "thickness_cube.stl"
        trimesh.creation.box(extents=(10.0, 10.0, 10.0)).export(source, file_type="stl")
        report = geometry_ops.thickness_map(str(source), target_mm=2.0, max_samples=500)
        check(report["resolved_samples"] > 0, "thickness ray casting resolved no samples")
        check(report["minimum_mm"] is not None and report["minimum_mm"] > 0, "thickness result is invalid")


if __name__ == "__main__":
    test_stl_topology_is_welded_for_analysis()
    test_finite_coordinate_validation()
    test_voxel_remesh_round_trip()
    test_thickness_has_spatial_index_dependency()
    print("v1.0.12 geometry regression tests passed")
