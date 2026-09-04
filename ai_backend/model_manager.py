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
        "description": "Legacy low-memory 2D fallback.", "estimated_gb": 5.5,
    },
    "sdxl-base": {
        "name": "Stable Diffusion XL Base 1.0", "kind": "image", "source": "huggingface",
        "repo_id": "stabilityai/stable-diffusion-xl-base-1.0",
        "description": "Primary modern 2D generator/editor for 8GB-class hardware using CPU offload.", "estimated_gb": 7.0,
    },
    "flux2-klein-4b": {
        "name": "FLUX.2 Klein 4B", "kind": "image", "source": "huggingface",
        "repo_id": "black-forest-labs/FLUX.2-klein-4B",
        "description": "Optional high-quality generation/editing specialist; heavier than the GTX1080 VRAM budget and therefore offloaded.", "estimated_gb": 13.0,
    },
    "hunyuan21-shape": {
        "name": "Hunyuan3D 2.1 Shape", "kind": "3d", "source": "hunyuan",
        "repo_id": "tencent/Hunyuan3D-2.1", "code_url": "https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1.git",
        "description": "Quality whole-object and selected-detail image-to-shape model.", "estimated_gb": 10.0,
    },
    "triposr": {
        "name": "TripoSR", "kind": "3d", "source": "huggingface",
        "repo_id": "stabilityai/TripoSR", "code_url": "https://github.com/VAST-AI-Research/TripoSR.git",
        "description": "Fast rough single-image 3D reconstruction route; about 6GB VRAM at official defaults.", "estimated_gb": 2.5,
    },
    "partcrafter": {
        "name": "PartCrafter", "kind": "3d-parts", "source": "github+huggingface",
        "repo_id": "wgsxm/PartCrafter", "code_url": "https://github.com/wgsxm/PartCrafter.git",
        "description": "Structured part-aware 3D generation specialist. Official requirement starts at 8GB CUDA VRAM.", "estimated_gb": 12.0,
    },
    "clipseg-smart-select": {
        "name": "CLIPSeg Smart Select", "kind": "segmentation", "source": "huggingface",
        "repo_id": "CIDAS/clipseg-rd64-refined",
        "description": "Local text-guided semantic segmentation used by Smart Select on multi-view renders.", "estimated_gb": 0.7,
    },
}


def _run(command: list[str], cwd: Path | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=cwd, check=True, text=True, capture_output=True)


def _disk_info() -> dict[str, float]:
    DATA_ROOT.mkdir(parents=True, exist_ok=True)
    usage = shutil.disk_usage(DATA_ROOT); gib = 1024 ** 3
    return {"free_gb": round(usage.free / gib, 2), "total_gb": round(usage.total / gib, 2)}


def hardware_info() -> dict[str, Any]:
    info: dict[str, Any] = {"platform": sys.platform, "python": sys.version.split()[0], "gpu": None, "vram_mb": 0,
        "cuda_available": False, "recommended_profile": "cpu", **_disk_info()}
    nvidia_smi = shutil.which("nvidia-smi")
    if nvidia_smi:
        try:
            p = _run([nvidia_smi, "--query-gpu=name,memory.total", "--format=csv,noheader,nounits"])
            first = p.stdout.strip().splitlines()[0]; name, mem = [x.strip() for x in first.rsplit(",", 1)]
            info["gpu"] = name; info["vram_mb"] = int(mem); info["cuda_available"] = True
            if info["vram_mb"] >= 16000: info["recommended_profile"] = "ultra"
            elif info["vram_mb"] > 8192: info["recommended_profile"] = "high"
            elif info["vram_mb"] >= 6144: info["recommended_profile"] = "medium"
            else: info["recommended_profile"] = "low"
        except Exception: pass
    return info


def load_state() -> dict[str, Any]:
    if not STATE_FILE.exists(): return {"installed": {}, "settings": {}}
    try: return json.loads(STATE_FILE.read_text(encoding="utf-8"))
    except Exception: return {"installed": {}, "settings": {}}


def save_state(state: dict[str, Any]) -> None:
    DATA_ROOT.mkdir(parents=True, exist_ok=True); STATE_FILE.write_text(json.dumps(state, indent=2), encoding="utf-8")


def status() -> dict[str, Any]:
    state = load_state(); result = []
    for component_id, spec in COMPONENTS.items():
        entry = dict(spec); entry["id"] = component_id
        entry["installed"] = bool(state.get("installed", {}).get(component_id, {}).get("installed"))
        entry["path"] = state.get("installed", {}).get(component_id, {}).get("path"); result.append(entry)
    return {"hardware": hardware_info(), "components": result, "data_root": str(DATA_ROOT), "disk": _disk_info()}


def _clone_if_missing(url: str, target: Path) -> None:
    if target.exists(): return
    git=shutil.which("git")
    if not git: raise RuntimeError("Git is required to install this component")
    _run([git,"clone","--depth","1",url,str(target)])


def _pip_requirements(path: Path) -> None:
    if path.exists():
        try: _run([sys.executable,"-m","pip","install","-r",str(path)])
        except subprocess.CalledProcessError as exc:
            raise RuntimeError("Python dependency installation failed: " + (exc.stderr or exc.stdout)[-4000:]) from exc


def install_component(component_id: str) -> dict[str, Any]:
    if component_id not in COMPONENTS: raise ValueError(f"Unknown AI component: {component_id}")
    spec=COMPONENTS[component_id]; MODELS_ROOT.mkdir(parents=True,exist_ok=True); TOOLS_ROOT.mkdir(parents=True,exist_ok=True)
    disk=_disk_info(); required=float(spec.get("estimated_gb",0))*1.25+1.0
    if disk["free_gb"] < required: raise RuntimeError(f"Not enough free disk space for {spec['name']}. About {required:.1f} GB free is recommended; only {disk['free_gb']:.1f} GB is available.")
    try: from huggingface_hub import snapshot_download
    except Exception as exc: raise RuntimeError("huggingface_hub is required. Re-run setup_ai_backend.bat.") from exc

    if component_id == "sd21":
        target=MODELS_ROOT/"stable-diffusion-2-1-base"; snapshot_download(repo_id=spec["repo_id"],local_dir=target,local_dir_use_symlinks=False)
    elif component_id == "sdxl-base":
        target=MODELS_ROOT/"stable-diffusion-xl-base-1.0"; snapshot_download(repo_id=spec["repo_id"],local_dir=target,local_dir_use_symlinks=False,
            allow_patterns=["model_index.json","scheduler/**","text_encoder/**","text_encoder_2/**","tokenizer/**","tokenizer_2/**","unet/**","vae/**","*.safetensors","LICENSE*","README.md"])
    elif component_id == "flux2-klein-4b":
        target=MODELS_ROOT/"FLUX.2-klein-4B"; snapshot_download(repo_id=spec["repo_id"],local_dir=target,local_dir_use_symlinks=False)
    elif component_id == "hunyuan21-shape":
        code_dir=TOOLS_ROOT/"Hunyuan3D-2.1"; _clone_if_missing(spec["code_url"],code_dir); target=MODELS_ROOT/"Hunyuan3D-2.1"
        snapshot_download(repo_id=spec["repo_id"],local_dir=target,local_dir_use_symlinks=False,
            allow_patterns=["hunyuan3d-dit-v2-1/**","hunyuan3d-vae-v2-1/**","README.md","LICENSE","Notice.txt"])
    elif component_id == "triposr":
        code_dir=TOOLS_ROOT/"TripoSR"; _clone_if_missing(spec["code_url"],code_dir); _pip_requirements(code_dir/"requirements.txt")
        target=MODELS_ROOT/"TripoSR"; snapshot_download(repo_id=spec["repo_id"],local_dir=target,local_dir_use_symlinks=False,
            allow_patterns=["config.yaml","model.ckpt","README.md","LICENSE*"])
    elif component_id == "partcrafter":
        code_dir=TOOLS_ROOT/"PartCrafter"; _clone_if_missing(spec["code_url"],code_dir)
        # Official setup scripts are Linux-oriented. Install any conventional requirements
        # here and stage the official checkpoints so Windows inference does not depend on a
        # first-run download. Provider runs out-of-process to isolate its dependency stack.
        _pip_requirements(code_dir/"requirements.txt")
        target=code_dir
        snapshot_download(repo_id=spec["repo_id"],local_dir=code_dir/"pretrained_weights"/"PartCrafter",local_dir_use_symlinks=False)
        try: snapshot_download(repo_id="briaai/RMBG-1.4",local_dir=code_dir/"pretrained_weights"/"RMBG-1.4",local_dir_use_symlinks=False)
        except Exception: pass
    elif component_id == "clipseg-smart-select":
        target=MODELS_ROOT/"clipseg-rd64-refined"; snapshot_download(repo_id=spec["repo_id"],local_dir=target,local_dir_use_symlinks=False,
            allow_patterns=["config.json","preprocessor_config.json","tokenizer_config.json","special_tokens_map.json","vocab.json","merges.txt","model.safetensors","README.md"])
    else: raise RuntimeError(f"No installer implemented for {component_id}")

    state=load_state(); state.setdefault("installed",{})[component_id]={"installed":True,"path":str(target)}
    state.setdefault("settings",{})["profile"]=hardware_info()["recommended_profile"]; save_state(state)
    return {"id":component_id,"installed":True,"path":str(target),"hardware":hardware_info()}


def uninstall_component(component_id: str) -> dict[str, Any]:
    state=load_state(); entry=state.get("installed",{}).get(component_id)
    if entry and entry.get("path"):
        path=Path(entry["path"])
        # Never recursively delete the shared tools root as a side effect of uninstalling
        # PartCrafter; delete only its named directory or a model directory.
        if path.exists() and DATA_ROOT in path.parents: shutil.rmtree(path,ignore_errors=True)
    state.setdefault("installed",{}).pop(component_id,None); save_state(state); return {"id":component_id,"installed":False}


def component_path(component_id: str) -> Path | None:
    state=load_state(); entry=state.get("installed",{}).get(component_id)
    if not entry or not entry.get("installed") or not entry.get("path"): return None
    p=Path(entry["path"]); return p if p.exists() else None
