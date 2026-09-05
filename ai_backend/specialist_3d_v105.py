from __future__ import annotations

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
    mesh.export(out); return str(out)


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
    code = TOOLS_ROOT / "Hunyuan3D-2"; model = component_path("hunyuan2mini")
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
    template = os.getenv("MINISCULPTER_TRELLIS2_COMMAND", "").strip()
    if not template:
        raise RuntimeError("TRELLIS.2 uses its official Linux runtime. Configure MINISCULPTER_TRELLIS2_COMMAND for native Linux or WSL2 after installation.")
    out = Path(output).resolve(); out.parent.mkdir(parents=True, exist_ok=True)
    cmd = template.format(image=str(Path(image).resolve()), output=str(out))
    p = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=10800)
    if p.returncode != 0 or not out.exists(): raise RuntimeError((p.stderr or p.stdout or "TRELLIS.2 produced no output")[-5000:])
    return str(out)


def generate_partpacker(image: str, output_dir: str, tag: str = "miniscuplter") -> dict:
    code = TOOLS_ROOT / "PartPacker"
    if component_path("partpacker") is None or not code.exists(): raise RuntimeError("PartPacker is not installed")
    out = Path(output_dir).resolve(); out.mkdir(parents=True, exist_ok=True)
    # Upstream exposes process_image/process_3d from app.py rather than a stable CLI. Run those
    # functions in an isolated child process so its argparse/global CUDA state cannot pollute the
    # Miniscuplter backend. The GLB is a Scene whose geometries are the generated parts.
    probe = (
        "import sys; sys.argv=['app.py']; import app; "
        f"img=app.process_image({str(Path(image).resolve())!r}); "
        "p=app.process_3d(img); print('MINISCULPTER_OUTPUT='+str(p))"
    )
    completed = _run([sys.executable, "-c", probe], code, timeout=10800)
    marker = [x.split("=",1)[1].strip() for x in completed.stdout.splitlines() if x.startswith("MINISCULPTER_OUTPUT=")]
    if not marker: raise RuntimeError("PartPacker did not report its output GLB")
    glb = Path(marker[-1]); glb = glb if glb.is_absolute() else code / glb
    import trimesh
    scene = trimesh.load(glb, force="scene")
    if not isinstance(scene, trimesh.Scene) or not scene.geometry: raise RuntimeError("PartPacker output contains no part geometry")
    parts = []
    for i, mesh in enumerate(scene.geometry.values()):
        if len(mesh.faces) <= 10: continue
        dst = out / f"{tag}_part_{i:02d}.stl"; mesh.export(dst); parts.append(str(dst))
    if not parts: raise RuntimeError("PartPacker returned no usable part meshes")
    return {"provider": "partpacker", "parts": parts, "count": len(parts)}
