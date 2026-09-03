from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parent
DATA_ROOT = Path(os.getenv("MINISCULPTER_DATA", ROOT / "data")).resolve()
MODELS_ROOT = DATA_ROOT / "models"
TOOLS_ROOT = DATA_ROOT / "tools"
STATE_FILE = DATA_ROOT / "components.json"

COMPONENTS: dict[str, dict[str, Any]] = {
    "sd21": {
        "name": "Stable Diffusion 2.1 Base", "kind": "image", "source": "huggingface",
        "repo_id": "stabilityai/stable-diffusion-2-1-base",
        "description": "Default local 2D concept and inpainting model for v0.2.", "estimated_gb": 5.5,
    },
    "hunyuan21-shape": {
        "name": "Hunyuan3D 2.1 Shape", "kind": "3d", "source": "hunyuan",
        "repo_id": "tencent/Hunyuan3D-2.1", "code_url": "https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1.git",
        "description": "Official Tencent image-to-shape model. Texture generation is intentionally omitted for miniature/STL use.", "estimated_gb": 10.0,
    },
}


def _run(command: list[str], cwd: Path | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=cwd, check=True, text=True, capture_output=True)


def _disk_info() -> dict[str, float]:
    DATA_ROOT.mkdir(parents=True, exist_ok=True)
    usage = shutil.disk_usage(DATA_ROOT)
    gib = 1024 ** 3
    return {"free_gb": round(usage.free / gib, 2), "total_gb": round(usage.total / gib, 2)}


def hardware_info() -> dict[str, Any]:
    info: dict[str, Any] = {
        "platform": sys.platform, "python": sys.version.split()[0], "gpu": None, "vram_mb": 0,
        "cuda_available": False, "recommended_profile": "cpu", **_disk_info(),
    }
    nvidia_smi = shutil.which("nvidia-smi")
    if nvidia_smi:
        try:
            p = _run([nvidia_smi, "--query-gpu=name,memory.total", "--format=csv,noheader,nounits"])
            first = p.stdout.strip().splitlines()[0]
            name, mem = [x.strip() for x in first.rsplit(",", 1)]
            info["gpu"] = name; info["vram_mb"] = int(mem); info["cuda_available"] = True
            if info["vram_mb"] >= 12000: info["recommended_profile"] = "quality"
            elif info["vram_mb"] >= 8000: info["recommended_profile"] = "low-vram"
            else: info["recommended_profile"] = "cpu-offload"
        except Exception:
            pass
    return info


def load_state() -> dict[str, Any]:
    if not STATE_FILE.exists(): return {"installed": {}, "settings": {}}
    try: return json.loads(STATE_FILE.read_text(encoding="utf-8"))
    except Exception: return {"installed": {}, "settings": {}}


def save_state(state: dict[str, Any]) -> None:
    DATA_ROOT.mkdir(parents=True, exist_ok=True)
    STATE_FILE.write_text(json.dumps(state, indent=2), encoding="utf-8")


def status() -> dict[str, Any]:
    state = load_state(); result = []
    for component_id, spec in COMPONENTS.items():
        entry = dict(spec); entry["id"] = component_id
        entry["installed"] = bool(state.get("installed", {}).get(component_id, {}).get("installed"))
        entry["path"] = state.get("installed", {}).get(component_id, {}).get("path")
        result.append(entry)
    return {"hardware": hardware_info(), "components": result, "data_root": str(DATA_ROOT), "disk": _disk_info()}


def install_component(component_id: str) -> dict[str, Any]:
    if component_id not in COMPONENTS: raise ValueError(f"Unknown AI component: {component_id}")
    spec = COMPONENTS[component_id]; MODELS_ROOT.mkdir(parents=True, exist_ok=True); TOOLS_ROOT.mkdir(parents=True, exist_ok=True)
    disk = _disk_info(); required = float(spec.get("estimated_gb", 0)) * 1.25 + 1.0
    if disk["free_gb"] < required:
        raise RuntimeError(
            f"Not enough free disk space for {spec['name']}. About {required:.1f} GB free is recommended "
            f"including download/cache overhead; only {disk['free_gb']:.1f} GB is available at {DATA_ROOT}."
        )
    try:
        from huggingface_hub import snapshot_download
    except Exception as exc:
        raise RuntimeError("huggingface_hub is required. Run setup_ai_backend.bat again for v0.5.5 dependencies.") from exc

    if component_id == "sd21":
        target = MODELS_ROOT / "stable-diffusion-2-1-base"
        snapshot_download(repo_id=spec["repo_id"], local_dir=target, local_dir_use_symlinks=False, resume_download=True)
    elif component_id == "hunyuan21-shape":
        code_dir = TOOLS_ROOT / "Hunyuan3D-2.1"
        if not code_dir.exists():
            git = shutil.which("git")
            if not git: raise RuntimeError("Git is required to install the official Hunyuan3D source code.")
            _run([git, "clone", "--depth", "1", spec["code_url"], str(code_dir)])
        target = MODELS_ROOT / "Hunyuan3D-2.1"
        snapshot_download(
            repo_id=spec["repo_id"], local_dir=target, local_dir_use_symlinks=False, resume_download=True,
            allow_patterns=["hunyuan3d-dit-v2-1/**", "hunyuan3d-vae-v2-1/**", "README.md", "LICENSE", "Notice.txt"],
        )
    else:
        raise RuntimeError(f"No installer implemented for {component_id}")

    state = load_state()
    state.setdefault("installed", {})[component_id] = {"installed": True, "path": str(target)}
    state.setdefault("settings", {})["profile"] = hardware_info()["recommended_profile"]
    save_state(state)
    return {"id": component_id, "installed": True, "path": str(target), "hardware": hardware_info()}


def uninstall_component(component_id: str) -> dict[str, Any]:
    state = load_state(); entry = state.get("installed", {}).get(component_id)
    if entry and entry.get("path"):
        path = Path(entry["path"])
        if path.exists() and DATA_ROOT in path.parents: shutil.rmtree(path, ignore_errors=True)
    state.setdefault("installed", {}).pop(component_id, None); save_state(state)
    return {"id": component_id, "installed": False}


def component_path(component_id: str) -> Path | None:
    state = load_state(); entry = state.get("installed", {}).get(component_id)
    if not entry or not entry.get("installed") or not entry.get("path"): return None
    p = Path(entry["path"]); return p if p.exists() else None
