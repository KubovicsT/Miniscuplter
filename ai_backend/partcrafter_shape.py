from __future__ import annotations

import json
import subprocess
import sys
import time
from pathlib import Path

import trimesh

from model_manager import component_path, TOOLS_ROOT


def generate_parts(image_path: str, output_dir: str, num_parts: int = 4, tag: str = "miniscuplter") -> dict:
    """Invoke the official PartCrafter inference script and normalize its outputs.

    PartCrafter intentionally remains a separate process: its official dependency stack is
    much heavier than the editor backend and this prevents its CUDA allocations/imports
    from contaminating the long-running Miniscuplter process.
    """
    installed = component_path("partcrafter")
    code_dir = TOOLS_ROOT / "PartCrafter"
    if installed is None or not code_dir.exists(): raise RuntimeError("PartCrafter is not installed")
    script = code_dir / "scripts" / "inference_partcrafter.py"
    if not script.exists(): raise RuntimeError("PartCrafter inference script is missing")
    out = Path(output_dir).resolve(); out.mkdir(parents=True, exist_ok=True)
    clean_tag = "".join(c if c.isalnum() or c in "-_" else "_" for c in tag)[:64] or "miniscuplter"
    started = time.time()
    cmd = [sys.executable, str(script), "--image_path", str(Path(image_path).resolve()), "--num_parts", str(max(1,min(16,int(num_parts)))), "--tag", clean_tag, "--rmbg"]
    p = subprocess.run(cmd, cwd=str(code_dir), capture_output=True, text=True, timeout=3600)
    if p.returncode != 0: raise RuntimeError("PartCrafter failed: " + (p.stderr or p.stdout)[-5000:])

    # Official releases have changed exact result subfolder names. Discover only fresh
    # geometry created by this invocation rather than baking a brittle filename assumption.
    candidates=[]
    for ext in ("*.glb","*.gltf","*.obj","*.ply","*.stl"):
        for f in (code_dir / "results").rglob(ext):
            try:
                if f.stat().st_mtime >= started - 2: candidates.append(f)
            except OSError: pass
    if not candidates: raise RuntimeError("PartCrafter completed but no generated geometry was found")

    normalized=[]
    for i, source in enumerate(sorted(set(candidates))):
        try:
            loaded=trimesh.load(source, force="scene", process=False)
            geoms = list(loaded.geometry.values()) if isinstance(loaded,trimesh.Scene) else [loaded]
            for j, mesh in enumerate(geoms):
                if not isinstance(mesh,trimesh.Trimesh) or mesh.is_empty: continue
                path=out / f"part_{len(normalized)+1:02d}.stl"; mesh.export(path,file_type="stl"); normalized.append(str(path))
        except Exception:
            continue
    if not normalized: raise RuntimeError("PartCrafter outputs could not be converted to STL parts")
    manifest={"provider":"partcrafter","parts":normalized,"count":len(normalized),"requested_parts":int(num_parts)}
    (out/"parts_manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
    return manifest
