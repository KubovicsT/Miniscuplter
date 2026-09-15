from __future__ import annotations

from pydantic import BaseModel

from app import app
from detail_pipeline import detail_2d, detail_3d


class Detail2DContract(BaseModel):
    image_path: str
    mask_path: str
    prompt: str
    output_path: str
    image_provider: str = "auto"


class Detail3DContract(BaseModel):
    source_mesh: str
    image_path: str
    mask_path: str
    prompt: str
    bounds_min: list[float]
    bounds_max: list[float]
    output_patch: str
    output_image: str
    output_crop: str
    image_provider: str = "auto"
    three_d_provider: str = "auto"


def _retire_legacy_detail_routes() -> None:
    # app.py still carries the pre-Stage-C request shapes. Retire only those two routes while the
    # rest of the canonical FastAPI surface remains unchanged; this keeps the migration bounded
    # and avoids silently accepting editor fields that the old Pydantic models discarded.
    retired = {"/detail-2d", "/detail-3d"}
    app.router.routes[:] = [route for route in app.router.routes if getattr(route, "path", None) not in retired]


_retire_legacy_detail_routes()


@app.post("/detail-2d")
def detail_2d_contract(req: Detail2DContract):
    return detail_2d(req.image_path, req.mask_path, req.prompt, req.output_path, req.image_provider)


@app.post("/detail-3d")
def detail_3d_contract(req: Detail3DContract):
    return detail_3d(
        req.source_mesh,
        req.image_path,
        req.mask_path,
        req.prompt,
        req.bounds_min,
        req.bounds_max,
        req.output_patch,
        req.output_image,
        req.output_crop,
        req.image_provider,
        req.three_d_provider,
    )
