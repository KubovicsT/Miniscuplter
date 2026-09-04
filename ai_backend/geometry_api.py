from __future__ import annotations

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field

import geometry_ops
from geometry_ops import analyze_mesh, repair_mesh, thickness_map, voxel_remesh
from quality_runtime import get_config, set_config

router = APIRouter(prefix="/geometry", tags=["geometry"])


class VoxelRequest(BaseModel):
    input_paths: list[str] = Field(min_length=1)
    output_path: str
    voxel_size: float = Field(default=0.35, ge=0.04, le=5.0)

class AnalyzeRequest(BaseModel):
    input_path: str
    feature_threshold_mm: float = Field(default=0.60, ge=0.05, le=10.0)

class RepairRequest(BaseModel):
    input_path: str
    output_path: str
    voxel_size: float = Field(default=0.30, ge=0.04, le=5.0)

class ThicknessRequest(BaseModel):
    input_path: str
    target_mm: float = Field(default=0.80, ge=0.01, le=100.0)
    max_samples: int = Field(default=12000, ge=100, le=100000)

class QualityConfigRequest(BaseModel):
    image_size: int = Field(default=512, ge=256, le=1536)
    image_steps: int = Field(default=24, ge=4, le=100)
    image_guidance: float = Field(default=7.0, ge=1.0, le=20.0)
    image_edit_strength: float = Field(default=0.58, ge=0.05, le=0.95)
    max_input_px: int = Field(default=2048, ge=512, le=8192)
    shape_steps: int = Field(default=30, ge=8, le=100)
    remesh_voxel_mm: float = Field(default=0.28, ge=0.04, le=5.0)
    repair_voxel_mm: float = Field(default=0.30, ge=0.04, le=5.0)
    max_voxel_cells: int = Field(default=100000000, ge=1000000, le=2000000000)
    thickness_samples: int = Field(default=12000, ge=100, le=100000)
    smart_select_views: int = Field(default=6, ge=2, le=12)
    smart_select_render_size: int = Field(default=352, ge=128, le=1024)

@router.get("/quality-config")
def quality_config_get():
    return get_config()

@router.post("/quality-config")
def quality_config_set(req: QualityConfigRequest):
    cfg = set_config(req.model_dump())
    geometry_ops.MAX_VOXEL_CELLS = int(cfg["max_voxel_cells"])
    return {"ok": True, "config": cfg}

@router.post("/voxel-remesh")
def remesh(req: VoxelRequest):
    try:
        path = voxel_remesh(req.input_paths, req.output_path, req.voxel_size)
        return {"path": path, "voxel_size": req.voxel_size, "inputs": len(req.input_paths), "max_voxel_cells": geometry_ops.MAX_VOXEL_CELLS}
    except (ValueError, FileNotFoundError) as exc: raise HTTPException(400, f"Voxel remesh input rejected: {exc}") from exc
    except MemoryError as exc: raise HTTPException(413, f"Voxel remesh memory safety check stopped the job: {exc}") from exc
    except Exception as exc: raise HTTPException(500, f"Voxel remesh failed: {exc}") from exc

@router.post("/analyze")
def analyze(req: AnalyzeRequest):
    try: return analyze_mesh(req.input_path, req.feature_threshold_mm)
    except (ValueError, FileNotFoundError) as exc: raise HTTPException(400, f"Mesh analysis input rejected: {exc}") from exc
    except Exception as exc: raise HTTPException(500, f"Mesh analysis failed: {exc}") from exc

@router.post("/thickness-map")
def thickness(req: ThicknessRequest):
    try: return thickness_map(req.input_path, req.target_mm, req.max_samples)
    except (ValueError, FileNotFoundError) as exc: raise HTTPException(400, f"Thickness analysis input rejected: {exc}") from exc
    except Exception as exc: raise HTTPException(500, f"Thickness analysis failed: {exc}") from exc

@router.post("/repair")
def repair(req: RepairRequest):
    try: return repair_mesh(req.input_path, req.output_path, req.voxel_size)
    except (ValueError, FileNotFoundError) as exc: raise HTTPException(400, f"Mesh repair input rejected: {exc}") from exc
    except MemoryError as exc: raise HTTPException(413, f"Mesh repair memory safety check stopped the job: {exc}") from exc
    except Exception as exc: raise HTTPException(500, f"Mesh repair failed: {exc}") from exc
