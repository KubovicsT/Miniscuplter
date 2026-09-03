from __future__ import annotations

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field

from geometry_ops import voxel_remesh

router = APIRouter(prefix="/geometry", tags=["geometry"])


class VoxelRequest(BaseModel):
    input_paths: list[str] = Field(min_length=1)
    output_path: str
    voxel_size: float = Field(default=0.35, gt=0.0, le=5.0)


@router.post("/voxel-remesh")
def remesh(req: VoxelRequest):
    try:
        path = voxel_remesh(req.input_paths, req.output_path, req.voxel_size)
        return {"path": path, "voxel_size": req.voxel_size, "inputs": len(req.input_paths)}
    except Exception as exc:
        raise HTTPException(500, f"Voxel remesh failed: {exc}") from exc
