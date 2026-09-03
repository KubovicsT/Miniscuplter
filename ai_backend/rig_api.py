from __future__ import annotations

import json
import os
import shlex
import subprocess
from pathlib import Path

import numpy as np
import trimesh
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field

router = APIRouter(prefix="/rig", tags=["rig"])

UNIVERSAL_RIG_COMMAND = os.getenv("MINISCULPTER_UNIVERSAL_RIG_COMMAND", "").strip()


class RigRequest(BaseModel):
    input_path: str
    output_path: str
    mode: str = "quick"
    seed: int = 0
    branch_threshold: float = Field(default=0.28, ge=0.05, le=0.8)


def _load_mesh(path: str) -> trimesh.Trimesh:
    mesh = trimesh.load_mesh(path, force="mesh", process=False)
    if isinstance(mesh, trimesh.Scene):
        mesh = trimesh.util.concatenate(tuple(mesh.geometry.values()))
    if mesh.is_empty or len(mesh.vertices) < 4:
        raise ValueError("Input mesh contains too little geometry to rig")
    return mesh


def _adaptive_quick_rig(input_path: str, output_path: str, branch_threshold: float) -> dict:
    mesh = _load_mesh(input_path)
    points = np.asarray(mesh.vertices, dtype=np.float64)
    center = points.mean(axis=0)
    centered = points - center
    cov = np.cov(centered.T)
    values, vectors = np.linalg.eigh(cov)
    order = np.argsort(values)[::-1]
    basis = vectors[:, order]
    local = centered @ basis
    mins = local.min(axis=0)
    maxs = local.max(axis=0)
    ext = np.maximum(maxs - mins, 1e-6)

    def world(local_point: np.ndarray) -> list[float]:
        p = center + local_point @ basis.T
        return [float(p[0]), float(p[1]), float(p[2])]

    joints: list[dict] = []

    # A neutral axial chain follows the dominant shape axis. This works for bipeds,
    # quadrupeds, serpentine bodies, machinery and many fantasy silhouettes without
    # assuming a named humanoid topology.
    axial_t = [0.0, 0.22, 0.5, 0.78, 1.0]
    for i, t in enumerate(axial_t):
        p = np.array([mins[0] + ext[0] * t, 0.0, 0.0])
        joints.append({"name": f"axis_{i}", "parent": i - 1, "position": world(p)})

    # Add symmetric branch chains where the cross-section is meaningful relative to
    # the primary extent. The branches are intentionally generic; the user can move,
    # add, remove or reparent joints before skinning/posing.
    branch_specs = [(1, 0.28), (3, 0.72)]
    next_id = len(joints)
    for parent_index, t in branch_specs:
        base = np.array([mins[0] + ext[0] * t, 0.0, 0.0])
        for axis in (1, 2):
            if ext[axis] / ext[0] < branch_threshold:
                continue
            for sign in (-1.0, 1.0):
                mid = base.copy(); mid[axis] = sign * ext[axis] * 0.28
                tip = base.copy(); tip[axis] = sign * ext[axis] * 0.5
                joints.append({"name": f"branch_{next_id}", "parent": parent_index, "position": world(mid)})
                mid_index = len(joints) - 1
                next_id += 1
                joints.append({"name": f"branch_{next_id}", "parent": mid_index, "position": world(tip)})
                next_id += 1

    payload = {
        "provider": "adaptive-quick",
        "mode": "quick",
        "input_path": str(Path(input_path).resolve()),
        "joints": joints,
        "notes": "Geometry-derived editable skeleton; generic topology by design.",
    }
    out = Path(output_path).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return payload


def _run_universal_command(req: RigRequest) -> dict:
    if not UNIVERSAL_RIG_COMMAND:
        raise RuntimeError(
            "Universal AI rig provider is not configured. Quick Rig remains available. "
            "Set MINISCULPTER_UNIVERSAL_RIG_COMMAND to a provider command that writes the Miniscuplter skeleton JSON format."
        )
    input_path = str(Path(req.input_path).resolve())
    output_path = str(Path(req.output_path).resolve())
    Path(output_path).parent.mkdir(parents=True, exist_ok=True)
    command = UNIVERSAL_RIG_COMMAND.format(
        input=shlex.quote(input_path), output=shlex.quote(output_path), seed=req.seed
    )
    completed = subprocess.run(command, shell=True, capture_output=True, text=True, timeout=3600)
    if completed.returncode != 0:
        raise RuntimeError(completed.stderr[-4000:] or completed.stdout[-4000:])
    if not Path(output_path).exists():
        raise RuntimeError("Universal rig provider completed without producing the requested JSON output")
    payload = json.loads(Path(output_path).read_text(encoding="utf-8"))
    joints = payload.get("joints")
    if not isinstance(joints, list) or not joints:
        raise RuntimeError("Universal rig provider returned no joints")
    payload.setdefault("provider", "external-universal")
    payload.setdefault("mode", "universal")
    return payload


@router.get("/status")
def rig_status():
    return {
        "quick_available": True,
        "universal_available": bool(UNIVERSAL_RIG_COMMAND),
        "universal_provider": "configured-command" if UNIVERSAL_RIG_COMMAND else "not-configured",
    }


@router.post("/predict-skeleton")
def predict_skeleton(req: RigRequest):
    try:
        mode = (req.mode or "quick").lower()
        if mode == "universal":
            payload = _run_universal_command(req)
        else:
            payload = _adaptive_quick_rig(req.input_path, req.output_path, req.branch_threshold)
        return {"path": str(Path(req.output_path).resolve()), "provider": payload.get("provider", "unknown"), "joints": len(payload.get("joints", []))}
    except Exception as exc:
        raise HTTPException(500, f"Rig prediction failed: {exc}") from exc
