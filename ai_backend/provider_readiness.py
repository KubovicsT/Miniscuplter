from __future__ import annotations

import json
import os
import subprocess
import sys
import time
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import model_manager

_CACHE_TTL_SECONDS = 300.0
_CACHE: dict[str, tuple[float, dict[str, Any]]] = {}


@dataclass(frozen=True)
class ProviderReadiness:
    id: str
    downloaded: bool
    installed: bool
    importable: bool | None
    device_tested: bool | None
    inference_tested: bool
    route_eligible: bool
    checked_at: str | None
    runtime_version: str | None
    provider_revision: str | None
    device: dict[str, Any] | None
    failure: str | None
    benchmark: dict[str, Any] | None


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _qualification_state(component_id: str) -> dict[str, Any]:
    state = model_manager.load_state()
    rows = state.get("provider_qualification", {})
    row = rows.get(component_id, {}) if isinstance(rows, dict) else {}
    return row if isinstance(row, dict) else {}


def _provider_revision(component_id: str) -> str | None:
    entry = model_manager.load_state().get("installed", {}).get(component_id, {})
    if not isinstance(entry, dict):
        return None
    return model_manager._combined_revision(entry.get("hf_revision"), entry.get("tool_revision"))


def _isolated_python(code_root: Path) -> Path:
    return code_root / ".venv" / ("Scripts/python.exe" if os.name == "nt" else "bin/python")


def _probe_spec(component_id: str) -> tuple[Path | None, Path | None, str] | None:
    tools = model_manager.TOOLS_ROOT
    if component_id == "triposr":
        code = tools / "TripoSR"
        return Path(sys.executable), code, "from tsr.system import TSR"
    if component_id == "hunyuan2mini":
        code = tools / "Hunyuan3D-2"
        return Path(sys.executable), code, "import cv2, pymeshlab, pygltflib, xatlas; from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline"
    if component_id == "hunyuan21-shape":
        code = tools / "Hunyuan3D-2.1"
        return Path(sys.executable), code, "from hy3dshape.pipelines import Hunyuan3DDiTFlowMatchingPipeline"
    if component_id == "sf3d":
        code = tools / "stable-fast-3d"
        return _isolated_python(code), code, "from sf3d.system import SF3D"
    if component_id == "spar3d":
        code = tools / "stable-point-aware-3d"
        return _isolated_python(code), code, "from spar3d.system import SPAR3D"
    return None


def _probe_python(component_id: str) -> dict[str, Any]:
    spec = _probe_spec(component_id)
    if spec is None:
        return {"supported": False, "importable": None, "device_tested": None, "runtime_version": None, "device": None, "failure": None}
    python_exe, code_root, import_statement = spec
    if python_exe is None or not python_exe.is_file():
        return {"supported": True, "importable": False, "device_tested": False, "runtime_version": None, "device": None, "failure": f"Provider Python runtime is missing: {python_exe}"}
    if code_root is None or not code_root.is_dir():
        return {"supported": True, "importable": False, "device_tested": False, "runtime_version": None, "device": None, "failure": f"Provider source directory is missing: {code_root}"}

    probe = (
        "import json,sys; "
        + f"sys.path.insert(0, {str(code_root)!r}); "
        + import_statement
        + "; import torch; "
        + "cuda=bool(torch.cuda.is_available()); "
        + "d={'python':sys.version.split()[0],'torch':getattr(torch,'__version__',None),'cuda_available':cuda,'device_name':torch.cuda.get_device_name(0) if cuda else None,'vram_bytes':torch.cuda.get_device_properties(0).total_memory if cuda else 0}; "
        + "print('MINISCULPTER_PROVIDER_PROBE='+json.dumps(d,separators=(',',':')))"
    )
    try:
        completed = subprocess.run(
            [str(python_exe), "-c", probe],
            cwd=code_root,
            capture_output=True,
            text=True,
            timeout=45,
            check=False,
        )
    except Exception as exc:
        return {"supported": True, "importable": False, "device_tested": False, "runtime_version": None, "device": None, "failure": f"Provider preflight could not start: {exc}"}
    if completed.returncode != 0:
        detail = (completed.stderr or completed.stdout or "provider import probe failed").strip()[-3000:]
        return {"supported": True, "importable": False, "device_tested": False, "runtime_version": None, "device": None, "failure": detail}
    marker = next((line.split("=", 1)[1] for line in completed.stdout.splitlines() if line.startswith("MINISCULPTER_PROVIDER_PROBE=")), None)
    if not marker:
        return {"supported": True, "importable": False, "device_tested": False, "runtime_version": None, "device": None, "failure": "Provider preflight returned no structured result."}
    try:
        data = json.loads(marker)
    except json.JSONDecodeError as exc:
        return {"supported": True, "importable": False, "device_tested": False, "runtime_version": None, "device": None, "failure": f"Provider preflight returned invalid JSON: {exc}"}
    cuda = bool(data.get("cuda_available"))
    runtime_version = f"python {data.get('python') or '?'} / torch {data.get('torch') or '?'}"
    return {
        "supported": True,
        "importable": True,
        "device_tested": cuda,
        "runtime_version": runtime_version,
        "device": {
            "cuda_available": cuda,
            "name": data.get("device_name"),
            "vram_bytes": int(data.get("vram_bytes") or 0),
        },
        "failure": None if cuda else "CUDA is unavailable to this provider runtime. A GPU-backed 3D route is required for the normal Miniscuplter workflow.",
    }


def record_inference_success(component_id: str, elapsed_seconds: float | None = None, benchmark: dict[str, Any] | None = None) -> None:
    state = model_manager.load_state()
    rows = state.setdefault("provider_qualification", {})
    row = rows.setdefault(component_id, {})
    row.update({
        "inference_tested": True,
        "inference_ok": True,
        "inference_checked_at": _utc_now(),
        "provider_revision": _provider_revision(component_id),
    })
    merged_benchmark = dict(benchmark or {})
    if elapsed_seconds is not None:
        merged_benchmark["elapsed_seconds"] = round(float(elapsed_seconds), 3)
    if merged_benchmark:
        row["benchmark"] = merged_benchmark
    model_manager.save_state(state)
    _CACHE.pop(component_id, None)


def clear_qualification(component_id: str) -> None:
    state = model_manager.load_state()
    rows = state.get("provider_qualification", {})
    if isinstance(rows, dict):
        rows.pop(component_id, None)
        model_manager.save_state(state)
    _CACHE.pop(component_id, None)


def inspect_provider(component_id: str, probe: bool = False, persist: bool = False) -> dict[str, Any]:
    path = model_manager.component_path(component_id)
    installed = path is not None
    entry = model_manager.load_state().get("installed", {}).get(component_id, {})
    downloaded = bool(isinstance(entry, dict) and entry.get("path") and Path(str(entry.get("path"))).exists())
    qualification = _qualification_state(component_id)
    checked_at = qualification.get("checked_at")
    importable = qualification.get("importable") if isinstance(qualification.get("importable"), bool) else None
    device_tested = qualification.get("device_tested") if isinstance(qualification.get("device_tested"), bool) else None
    runtime_version = qualification.get("runtime_version")
    device = qualification.get("device") if isinstance(qualification.get("device"), dict) else None
    failure = qualification.get("failure") or None

    if not installed:
        result = ProviderReadiness(component_id, downloaded, False, False, False, bool(qualification.get("inference_tested") and qualification.get("inference_ok")), False, checked_at, runtime_version, _provider_revision(component_id), device, failure or "Provider files are not fully installed.", qualification.get("benchmark") if isinstance(qualification.get("benchmark"), dict) else None)
        return asdict(result)

    if probe:
        cached = _CACHE.get(component_id)
        now = time.monotonic()
        if cached is not None and now - cached[0] < _CACHE_TTL_SECONDS:
            return dict(cached[1])
        probed = _probe_python(component_id)
        if probed["supported"]:
            checked_at = _utc_now()
            importable = probed["importable"]
            device_tested = probed["device_tested"]
            runtime_version = probed["runtime_version"]
            device = probed["device"]
            failure = probed["failure"]
            if persist:
                state = model_manager.load_state()
                row = state.setdefault("provider_qualification", {}).setdefault(component_id, {})
                row.update({
                    "checked_at": checked_at,
                    "importable": importable,
                    "device_tested": device_tested,
                    "runtime_version": runtime_version,
                    "device": device,
                    "failure": failure,
                    "provider_revision": _provider_revision(component_id),
                })
                model_manager.save_state(state)
                qualification = row

    inference_tested = bool(qualification.get("inference_tested") and qualification.get("inference_ok"))
    known_failed_inference = qualification.get("inference_tested") is True and qualification.get("inference_ok") is False
    route_eligible = installed and importable is not False and device_tested is not False and not known_failed_inference
    result = ProviderReadiness(
        component_id,
        downloaded,
        installed,
        importable,
        device_tested,
        inference_tested,
        route_eligible,
        checked_at,
        runtime_version,
        _provider_revision(component_id),
        device,
        failure,
        qualification.get("benchmark") if isinstance(qualification.get("benchmark"), dict) else None,
    )
    payload = asdict(result)
    if probe:
        _CACHE[component_id] = (time.monotonic(), payload)
    return payload


def route_eligible(component_id: str) -> tuple[bool, dict[str, Any]]:
    result = inspect_provider(component_id, probe=True, persist=True)
    return bool(result["route_eligible"]), result


def readiness_status(probe: bool = False) -> dict[str, Any]:
    ids = [cid for cid, spec in model_manager.COMPONENTS.items() if spec.get("kind") in {"3d", "3d-parts"}]
    return {
        "providers": [inspect_provider(cid, probe=probe, persist=probe) for cid in ids],
        "hardware": model_manager.hardware_info(),
        "note": "Import/device preflight is lightweight. inference_tested becomes true only after a real provider inference succeeds; CI does not synthesize GPU qualification.",
    }
