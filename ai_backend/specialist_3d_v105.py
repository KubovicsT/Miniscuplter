from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

from model_manager import component_path, TOOLS_ROOT


def _run(args: list[str], cwd: Path, timeout: int = 7200) -> subprocess.CompletedProcess[str]:
    p = subprocess.run(args, cwd=cwd, capture_output=True, text=True, timeout=timeout)
    if p.returncode != 0: raise RuntimeError((p.stderr or p.stdout or "specialist provider failed")[-5000:])
    return p


def _to_stl(source: Path, output: str) -> str:
    import trimesh
    out = Path(output).resolve(); out.parent.mkdir(parents=True, exist_ok=True)
    loaded = trimesh.load(source, force="scene")
    mesh = loaded.dump(concatenate=True) if isinstance(loaded, trimesh.Scene) else loaded
    if mesh is None or len(mesh.faces) == 0: raise RuntimeError("Provider returned an empty mesh")
    mesh.export(out)
    return str(out)


def generate_sf3d(image: str, output: str) -> str:
    code = TOOLS_ROOT / "stable-fast-3d"
    if component_path("sf3d") is None or not code.exists(): raise RuntimeError("Stable Fast 3D is not installed")
    work = Path(output).resolve().parent / ".sf3d-output"; shutil.rmtree(work, ignore_errors=True); work.mkdir(parents=True)
    _run([sys.executable, "run.py", str(Path(image).resolve()), "--output-dir", str(work)], code)
    candidates = sorted(work.rglob("*.glb"), key=lambda p: p.stat().st_mtime, reverse=True)
    if not candidates: raise RuntimeError("Stable Fast 3D returned no GLB")
    return _to_stl(candidates[0], output)


def generate_spar3d(image: str, output: str, low_vram: bool = False) -> str:
    code = TOOLS_ROOT / "stable-point-aware-3d"
    if component_path("spar3d") is None or not code.exists(): raise RuntimeError("SPAR3D is not installed")
    work = Path(output).resolve().parent / ".spar3d-output"; shutil.rmtree(work, ignore_errors=True); work.mkdir(parents=True)
    args = [sys.executable, "run.py", str(Path(image).resolve()), "--output-dir", str(work)]
    if low_vram: args.append("--low-vram-mode")
    _run(args, code)
    candidates = sorted(work.rglob("*.glb"), key=lambda p: p.stat().st_mtime, reverse=True)
    if not candidates: raise RuntimeError("SPAR3D returned no GLB")
    return _to_stl(candidates[0], output)


def generate_hunyuan_mini(image: str, output: str) -> str:
    code = TOOLS_ROOT / "Hunyuan3D-2"
    model = component_path("hunyuan2mini")
    if model is None or not code.exists(): raise RuntimeError("Hunyuan3D 2mini is not installed")
    sys.path.insert(0, str(code))
    try:
        from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
        pipe = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(model), subfolder="hunyuan3d-dit-v2-mini")
        mesh = pipe(image=str(Path(image).resolve()))[0]
        out = Path(output).resolve(); out.parent.mkdir(parents=True, exist_ok=True); mesh.export(out); return str(out)
    finally:
        try: sys.path.remove(str(code))
        except ValueError: pass


def generate_trellis2(image: str, output: str) -> str:
    # Official TRELLIS.2 is Linux-only. On Linux run its isolated environment command; on
    # Windows use WSL2. The command is configurable because CUDA/Conda environment names vary.
    template = os.getenv("MINISCULPTER_TRELLIS2_COMMAND", "").strip()
    if not template:
        raise RuntimeError("TRELLIS.2 requires its official Linux runtime. Configure MINISCULPTER_TRELLIS2_COMMAND (native Linux or WSL2) after installing the component.")
    out = Path(output).resolve(); out.parent.mkdir(parents=True, exist_ok=True)
    cmd = template.format(image=str(Path(image).resolve()), output=str(out))
    p = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=10800)
    if p.returncode != 0 or not out.exists(): raise RuntimeError((p.stderr or p.stdout or "TRELLIS.2 produced no output")[-5000:])
    return str(out)


def generate_partpacker(image: str, output_dir: str, tag: str = "miniscuplter") -> dict:
    code = TOOLS_ROOT / "PartPacker"
    if component_path("partpacker") is None or not code.exists(): raise RuntimeError("PartPacker is not installed")
    out = Path(output_dir).resolve(); out.mkdir(parents=True, exist_ok=True)
    # Upstream CLI evolves; Miniscuplter calls the repository's inference entry point and then
    # normalizes every produced part mesh. No merged object is returned as a pseudo-part.
    script = code / "app.py"
    cli = code / "inference.py"
    if cli.exists(): _run([sys.executable, str(cli), "--image", str(Path(image).resolve()), "--output_dir", str(out)], code)
    else: raise RuntimeError("Installed PartPacker revision has no supported inference.py CLI; update the component when an upstream CLI-compatible revision is available")
    parts = []
    for i, src in enumerate(sorted([*out.rglob("*.obj"), *out.rglob("*.ply"), *out.rglob("*.glb")])):
        dst = out / f"{tag}_part_{i:02d}.stl"; _to_stl(src, str(dst)); parts.append(str(dst))
    if not parts: raise RuntimeError("PartPacker returned no part meshes")
    return {"provider": "partpacker", "parts": parts, "count": len(parts)}
