from __future__ import annotations
from pathlib import Path
import numpy as np, trimesh
from PIL import Image, ImageFilter
from model_router import choose_image_provider, choose_3d_provider, release_all_models
from geometry_ops import voxel_remesh

def _masked_crop(image_path,mask_path,output_path,pad_ratio=.38):
    image=Image.open(image_path).convert("RGB");mask=Image.open(mask_path).convert("L").resize(image.size,Image.Resampling.BILINEAR);bbox=mask.getbbox()
    if not bbox:raise ValueError("Detail mask is empty")
    l,t,r,b=bbox;pad=max(24,int(max(r-l,b-t)*pad_ratio));box=(max(0,l-pad),max(0,t-pad),min(image.width,r+pad),min(image.height,b+pad));crop=image.crop(box);local=mask.crop(box).filter(ImageFilter.GaussianBlur(radius=max(2,int(min(crop.size)*.01))));iso=Image.composite(crop,Image.new("RGB",crop.size,(245,245,245)),local);side=max(iso.size);canvas=Image.new("RGB",(side,side),(245,245,245));canvas.paste(iso,((side-iso.width)//2,(side-iso.height)//2));out=Path(output_path).resolve();out.parent.mkdir(parents=True,exist_ok=True);canvas.save(out);return str(out)
def _run_image_edit(provider,image,mask,prompt,output):
    if provider=="flux":return __import__("flux_klein",fromlist=["edit_image"]).edit_image(image,mask,prompt,output,detail=True)
    if provider=="sdxl":return __import__("sdxl_image",fromlist=["edit_image"]).edit_image(image,mask,prompt,output,detail=True)
    if provider=="sd21":return __import__("local_image",fromlist=["edit_image"]).edit_image(image,mask,prompt,output,"high")
    if provider=="qwen-edit":return __import__("modern_image",fromlist=["edit"]).edit("qwen-image-edit",image,mask,prompt,output)
    raise RuntimeError(f"Unsupported detail image provider: {provider}")
def _run_3d(provider,image,prompt,output):
    if provider=="hunyuan":return __import__("hunyuan_shape",fromlist=["generate_shape"]).generate_shape(image,output,prompt,"high")
    if provider=="triposr":return __import__("triposr_shape",fromlist=["generate_shape"]).generate_shape(image,output,mc_resolution=320)
    s=__import__("specialist_3d_v105",fromlist=["x"])
    if provider=="hunyuan-mini":return s.generate_hunyuan_mini(image,output)
    if provider=="sf3d":return s.generate_sf3d(image,output)
    if provider=="spar3d":return s.generate_spar3d(image,output,low_vram=True)
    raise RuntimeError(f"Provider {provider} does not support local detail reconstruction")
def _load_mesh(path):
    m=trimesh.load_mesh(path,force="mesh",process=False)
    if isinstance(m,trimesh.Scene):m=trimesh.util.concatenate(tuple(m.geometry.values()))
    if m.is_empty or len(m.faces)==0 or not np.isfinite(m.vertices).all():raise RuntimeError("Mesh is empty or invalid")
    return m
def _fit_patch_to_bounds(path,bounds_min,bounds_max,padding=1.04):
    m=_load_mesh(path);lo=np.asarray(bounds_min,float);hi=np.asarray(bounds_max,float)
    if lo.shape!=(3,) or hi.shape!=(3,) or np.any(hi<=lo):raise ValueError("Selection bounds are invalid")
    target=np.maximum(hi-lo,1e-3);center=(lo+hi)*.5;ext=np.maximum(np.asarray(m.extents,float),1e-6);scale=float(np.min((target*padding)/ext));m.apply_translation(-np.asarray(m.bounds).mean(axis=0));m.apply_scale(scale);m.apply_translation(center);m.remove_unreferenced_vertices();m.merge_vertices();out=Path(path).resolve();m.export(out,file_type="stl");return {"scale":scale,"target_center_mm":center.tolist(),"target_extents_mm":target.tolist(),"patch_extents_mm":m.extents.tolist(),"fit_method":"uniform all-axis bounding fit"}
def detail_2d(image_path,mask_path,prompt,output_path,image_provider="auto"):
    d=choose_image_provider("detail",image_provider);release_all_models()
    try:return {"path":_run_image_edit(d.provider,image_path,mask_path,prompt,output_path),"provider":d.provider,"routing_reason":d.reason}
    finally:release_all_models()
def detail_3d(source_mesh,image_path,mask_path,prompt,bounds_min,bounds_max,output_patch,output_image,output_crop,image_provider="auto",three_d_provider="auto"):
    i=choose_image_provider("detail",image_provider);release_all_models()
    try:enh=_run_image_edit(i.provider,image_path,mask_path,prompt,output_image)
    finally:release_all_models()
    crop=_masked_crop(enh,mask_path,output_crop);s=choose_3d_provider("detail",three_d_provider)
    if s.provider in {"partcrafter","partpacker","trellis2"}:s=choose_3d_provider("quality")
    try:patch=_run_3d(s.provider,crop,prompt,output_patch)
    finally:release_all_models()
    fit=_fit_patch_to_bounds(patch,bounds_min,bounds_max);return {"patch_path":patch,"enhanced_image":enh,"crop_image":crop,"image_provider":i.provider,"three_d_provider":s.provider,"image_reason":i.reason,"three_d_reason":s.reason,"fit":fit}
def apply_detail(source_mesh,patch_mesh,output_path,voxel_size=None):
    source=_load_mesh(source_mesh);patch=_load_mesh(patch_mesh);combined=trimesh.util.concatenate([source,patch]);pitch=float(voxel_size or max(float(np.max(combined.extents))/512.0,.08));result=voxel_remesh(combined,pitch);out=Path(output_path).resolve();out.parent.mkdir(parents=True,exist_ok=True);result.export(out);return {"path":str(out),"voxel_size":pitch}
