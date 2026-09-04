from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import numpy as np
import trimesh

from model_manager import component_path, TOOLS_ROOT


def _load_part_mesh(source: Path) -> list[trimesh.Trimesh]:
    loaded = trimesh.load(source, force="scene", process=False)
    geoms = list(loaded.geometry.values()) if isinstance(loaded, trimesh.Scene) else [loaded]
    result: list[trimesh.Trimesh] = []
    for mesh in geoms:
        if not isinstance(mesh, trimesh.Trimesh) or mesh.is_empty:
            continue
        if len(mesh.faces) == 0 or len(mesh.vertices) == 0:
            continue
        if not np.isfinite(mesh.vertices).all():
            continue
        # PartCrafter's official script substitutes a single zero-area triangle when its
        # decoder fails for a requested part. Reject that sentinel instead of importing it
        # as a successful STL part.
        if not np.isfinite(mesh.area) or float(mesh.area) <= 1e-10 or float(np.max(mesh.extents)) <= 1e-6:
            continue
        result.append(mesh)
    return result


def generate_parts(image_path: str, output_dir: str, num_parts: int = 4, tag: str = "miniscuplter") -> dict:
    """Invoke official PartCrafter inference and import only manifest-declared parts.

    PartCrafter remains a separate process so its model allocations are released when the
    process exits. The official manifest is authoritative; the merged object output is not
    re-imported as a duplicate part.
    """
    installed = component_path("partcrafter")
    code_dir = TOOLS_ROOT / "PartCrafter"
    if installed is None or not code_dir.exists():
        raise RuntimeError("PartCrafter is not installed")
    script = code_dir / "scripts" / "inference_partcrafter.py"
    if not script.exists():
        raise RuntimeError("PartCrafter inference script is missing")
    image = Path(image_path).resolve()
    if not image.is_file() or image.stat().st_size == 0:
        raise RuntimeError(f"PartCrafter input image does not exist or is empty: {image}")

    out = Path(output_dir).resolve()
    out.mkdir(parents=True, exist_ok=True)
    clean_tag = "".join(c if c.isalnum() or c in "-_" else "_" for c in tag)[:64] or "miniscuplter"
    requested = max(1, min(16, int(num_parts)))
    cmd = [
        sys.executable, str(script),
        "--image_path", str(image),
        "--num_parts", str(requested),
        "--output_dir", str(out),
        "--tag", clean_tag,
        "--rmbg",
    ]
    p = subprocess.run(cmd, cwd=str(code_dir), capture_output=True, text=True, timeout=3600)
    if p.returncode != 0:
        raise RuntimeError("PartCrafter failed: " + (p.stderr or p.stdout or "unknown error")[-5000:])

    official_manifest = out / clean_tag / "manifest.json"
    if not official_manifest.is_file():
        raise RuntimeError(f"PartCrafter completed but did not write its expected manifest: {official_manifest}")
    try:
        data = json.loads(official_manifest.read_text(encoding="utf-8"))
    except Exception as exc:
        raise RuntimeError("PartCrafter wrote an unreadable result manifest") from exc
    declared = data.get("parts")
    if not isinstance(declared, list) or not declared:
        raise RuntimeError("PartCrafter manifest contains no generated parts")

    normalized: list[str] = []
    result_root = official_manifest.parent.resolve()
    for item in declared:
        if not isinstance(item, dict) or not isinstance(item.get("file"), str):
            raise RuntimeError("PartCrafter manifest contains an invalid part entry")
        source = (result_root / item["file"]).resolve()
        if result_root not in source.parents or not source.is_file():
            raise RuntimeError(f"PartCrafter manifest references an invalid part file: {item['file']}")
        meshes = _load_part_mesh(source)
        if not meshes:
            raise RuntimeError(f"PartCrafter returned an empty or degenerate part: {source.name}")
        for mesh in meshes:
            path = out / f"part_{len(normalized)+1:02d}.stl"
            mesh.remove_unreferenced_vertices()
            if not mesh.vertices.shape[0] or not mesh.faces.shape[0] or not np.isfinite(mesh.vertices).all():
                raise RuntimeError(f"PartCrafter part became invalid during cleanup: {source.name}")
            mesh.export(path, file_type="stl")
            if not path.is_file() or path.stat().st_size == 0:
                raise RuntimeError(f"Failed to normalize PartCrafter part to STL: {source.name}")
            normalized.append(str(path))

    if not normalized:
        raise RuntimeError("PartCrafter outputs could not be converted to STL parts")
    if len(normalized) < requested:
        raise RuntimeError(f"PartCrafter returned only {len(normalized)} usable parts after requesting {requested}")

    manifest = {
        "provider": "partcrafter",
        "parts": normalized,
        "count": len(normalized),
        "requested_parts": requested,
        "official_manifest": str(official_manifest),
    }
    (out / "parts_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return manifest
