from __future__ import annotations

import os
import shutil
import subprocess
import sys
import uuid
from pathlib import Path
from typing import Any

import model_manager as mm
from model_capabilities import BY_ID

NEW_COMPONENTS: dict[str, dict[str, Any]] = {
    "z-image-turbo": {"name":"Z-Image Turbo", "kind":"image", "source":"huggingface", "repo_id":"Tongyi-MAI/Z-Image-Turbo", "description":"Fast modern concept generator; CPU offload supported.", "estimated_gb":33.0},
    "qwen-image-2512": {"name":"Qwen-Image-2512", "kind":"image", "source":"huggingface", "repo_id":"Qwen/Qwen-Image-2512", "description":"High-end concept image generator.", "estimated_gb":58.0},
    "qwen-image-edit": {"name":"Qwen-Image-Edit", "kind":"image-edit", "source":"huggingface", "repo_id":"Qwen/Qwen-Image-Edit", "description":"High-end semantic image/detail editor.", "estimated_gb":55.0},
    "hunyuan2mini": {"name":"Hunyuan3D 2mini", "kind":"3d", "source":"github+huggingface", "repo_id":"tencent/Hunyuan3D-2mini", "code_url":"https://github.com/Tencent-Hunyuan/Hunyuan3D-2.git", "description":"0.6B resource-efficient image-to-shape provider.", "estimated_gb":5.0},
    "sf3d": {"name":"Stable Fast 3D", "kind":"3d", "source":"github+huggingface", "repo_id":"stabilityai/stable-fast-3d", "code_url":"https://github.com/Stability-AI/stable-fast-3d.git", "description":"Fast mesh reconstruction; upstream Windows support is experimental.", "estimated_gb":8.0},
    "spar3d": {"name":"SPAR3D", "kind":"3d", "source":"github+huggingface", "repo_id":"stabilityai/stable-point-aware-3d", "code_url":"https://github.com/Stability-AI/stable-point-aware-3d.git", "description":"Point-aware reconstruction with ~7GB low-VRAM mode.", "estimated_gb":10.0},
    "partpacker": {"name":"PartPacker", "kind":"3d-parts", "source":"github+huggingface", "repo_id":"nvidia/PartPacker", "code_url":"https://github.com/NVlabs/PartPacker.git", "description":"NVIDIA part-level generation; official inference is ~10GB VRAM fp16.", "estimated_gb":12.0},
    "trellis2": {"name":"TRELLIS.2 4B", "kind":"3d", "source":"github+huggingface", "repo_id":"microsoft/TRELLIS.2-4B", "code_url":"https://github.com/microsoft/TRELLIS.2.git", "description":"24GB+ workstation quality route. Official runtime is Linux; Windows uses configured WSL2 command.", "estimated_gb":40.0},
}

mm.COMPONENTS.update(NEW_COMPONENTS)
_legacy_install = mm.install_component
_legacy_update = mm.update_component


def _venv_python(root: Path) -> Path:
    return root / ".venv" / ("Scripts/python.exe" if os.name == "nt" else "bin/python")


def _make_venv(root: Path) -> Path:
    subprocess.run([sys.executable, "-m", "venv", str(root / ".venv")], check=True)
    py = _venv_python(root)
    subprocess.run([str(py), "-m", "pip", "install", "-U", "pip", "wheel", "setuptools"], check=True)
    return py


def _install_requirements(py: Path, code: Path, extra: list[str] | None = None) -> None:
    req = code / "requirements.txt"
    if req.exists(): subprocess.run([str(py), "-m", "pip", "install", "-r", str(req)], cwd=code, check=True)
    if extra: subprocess.run([str(py), "-m", "pip", "install", *extra], cwd=code, check=True)
    subprocess.run([str(py), "-m", "pip", "check"], cwd=code, check=True)


def _commit_state(component_id: str, final: Path, hf_rev: str | None, tool_rev: str | None) -> None:
    state = mm.load_state(); state.setdefault("installed", {})[component_id] = {"installed":True, "path":str(final), "hf_revision":hf_rev, "tool_revision":tool_rev}; mm.save_state(state)


def install_component(component_id: str, update: bool = False) -> dict[str, Any]:
    if component_id not in NEW_COMPONENTS: return _legacy_install(component_id, update)
    spec = NEW_COMPONENTS[component_id]; mm.MODELS_ROOT.mkdir(parents=True, exist_ok=True); mm.TOOLS_ROOT.mkdir(parents=True, exist_ok=True); mm.STAGING_ROOT.mkdir(parents=True, exist_ok=True)
    need = float(spec["estimated_gb"]) * 1.25 + 1.0; disk = mm._disk_info()
    if disk["free_gb"] < need: raise RuntimeError(f"About {need:.1f} GB free is required for transactional installation of {spec['name']}.")
    from huggingface_hub import snapshot_download
    stage = mm.STAGING_ROOT / f"{component_id}-{uuid.uuid4().hex}"; stage.mkdir(parents=True)
    hf_rev = mm._hf_revision(spec["repo_id"]) if spec.get("repo_id") else None; tool_rev = None
    try:
        if component_id in {"z-image-turbo", "qwen-image-2512", "qwen-image-edit"}:
            staged = stage / component_id; final = mm.MODELS_ROOT / component_id
            mm._download_hf(snapshot_download, spec["repo_id"], staged, hf_rev)
            if not (staged / "model_index.json").exists(): raise RuntimeError("Diffusers model_index.json is missing")
            mm._swap_staged([(staged, final)], lambda: _commit_state(component_id, final, hf_rev, None))
        elif component_id == "hunyuan2mini":
            code = stage / "Hunyuan3D-2"; final_code = mm.TOOLS_ROOT / "Hunyuan3D-2"; mm._clone_fresh(spec["code_url"], code)
            # Use the compatible shared runtime rather than Tencent's full paint/training stack.
            mm._pip_install(["PyYAML>=6.0", "tqdm>=4.66"])
            staged = stage / "Hunyuan3D-2mini"; final = mm.MODELS_ROOT / "Hunyuan3D-2mini"
            mm._download_hf(snapshot_download, spec["repo_id"], staged, hf_rev)
            tool_rev = mm._git_local_revision(code)
            mm._swap_staged([(code, final_code), (staged, final)], lambda: _commit_state(component_id, final, hf_rev, tool_rev))
        elif component_id in {"sf3d", "spar3d", "partpacker"}:
            folder = {"sf3d":"stable-fast-3d", "spar3d":"stable-point-aware-3d", "partpacker":"PartPacker"}[component_id]
            code = stage / folder; final = mm.TOOLS_ROOT / folder; mm._clone_fresh(spec["code_url"], code); py = _make_venv(code)
            if component_id == "partpacker":
                # Upstream explicitly validates Python 3.10 + torch 2.5.1/cu124 on Windows.
                subprocess.run([str(py), "-m", "pip", "install", "torch==2.5.1", "torchvision==0.20.1", "torchaudio", "--index-url", "https://download.pytorch.org/whl/cu124"], check=True)
                _install_requirements(py, code)
                for name in ("vae.pt", "flow.pt"):
                    from huggingface_hub import hf_hub_download
                    src = Path(hf_hub_download("nvidia/PartPacker", name, revision=hf_rev)); shutil.copy2(src, code / name)
            else:
                _install_requirements(py, code)
                # Prime gated/model snapshots into the isolated tool tree when access is granted.
                model_dir = code / "miniscuplter-model"; mm._download_hf(snapshot_download, spec["repo_id"], model_dir, hf_rev)
            tool_rev = mm._git_local_revision(code)
            mm._swap_staged([(code, final)], lambda: _commit_state(component_id, final, hf_rev, tool_rev))
        elif component_id == "trellis2":
            code = stage / "TRELLIS.2"; final = mm.TOOLS_ROOT / "TRELLIS.2"; mm._clone_fresh(spec["code_url"], code); tool_rev = mm._git_local_revision(code)
            # Do not pretend the Windows shared venv can run this: upstream requires Linux,
            # CUDA compilation and >=24GB VRAM. Source is managed here; runtime is native Linux
            # or WSL2 and is invoked through MINISCULPTER_TRELLIS2_COMMAND.
            marker = code / "MINISCULPTER_RUNTIME.txt"; marker.write_text("Official runtime: Linux, NVIDIA >=24GB VRAM. Configure MINISCULPTER_TRELLIS2_COMMAND.\n", encoding="utf-8")
            mm._swap_staged([(code, final)], lambda: _commit_state(component_id, final, hf_rev, tool_rev))
        else: raise RuntimeError(f"No v1.0.5 installer for {component_id}")
        return {"id":component_id, "installed":True, "path":str(final), "hardware":mm.hardware_info()}
    finally: shutil.rmtree(stage, ignore_errors=True)


def update_component(component_id: str) -> dict[str, Any]:
    if component_id in NEW_COMPONENTS: return install_component(component_id, True)
    return _legacy_update(component_id)
