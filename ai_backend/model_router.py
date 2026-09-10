from __future__ import annotations
from dataclasses import dataclass, asdict
from typing import Callable, Any
from model_manager import component_path, hardware_info
from model_capabilities import role_options
from provider_readiness import check_3d_provider

@dataclass(frozen=True)
class RouteDecision:
    role:str; provider:str; reason:str; fallback:str|None=None

def installed(cid:str)->bool:return component_path(cid) is not None

IMAGE_IDS={"sd21":"sd21","sdxl":"sdxl-base","flux":"flux2-klein-4b","zimage":"z-image-turbo","qwen":"qwen-image-2512","qwen-edit":"qwen-image-edit"}
THREED_IDS={"triposr":"triposr","sf3d":"sf3d","spar3d":"spar3d","hunyuan-mini":"hunyuan2mini","hunyuan":"hunyuan21-shape","trellis2":"trellis2","partcrafter":"partcrafter","partpacker":"partpacker"}

def _first(candidates:list[str])->str|None:
    return next((x for x in candidates if installed(x)),None)
def _provider(cid:str)->str:
    for p,c in {**IMAGE_IDS,**THREED_IDS}.items():
        if c==cid:return p
    return cid

def _first_ready_3d(component_ids:list[str])->str|None:
    for cid in component_ids:
        if not installed(cid):
            continue
        provider=_provider(cid)
        try:
            if check_3d_provider(provider).ready:
                return cid
        except Exception:
            continue
    return None

def _readiness_error(provider:str)->str:
    try:
        result=check_3d_provider(provider)
        return "; ".join(result.errors) or "provider readiness check failed"
    except Exception as exc:
        return str(exc)

def choose_image_provider(role="generate",mode="auto"):
    role=(role or "generate").lower();mode=(mode or "auto").lower();v=int(hardware_info().get("vram_mb",0) or 0)
    if mode!="auto":
        cid=IMAGE_IDS.get(mode)
        if not cid:raise RuntimeError(f"Unknown image provider '{mode}'")
        if not installed(cid):raise RuntimeError(f"Requested image provider '{mode}' is not installed")
        return RouteDecision(role,mode,"explicit user/provider selection")
    if role in {"edit","detail"}:
        order=["qwen-image-edit","flux2-klein-4b","sdxl-base","sd21"] if v>=16000 else ["flux2-klein-4b","sdxl-base","qwen-image-edit","sd21"] if v>=12000 else ["sdxl-base","sd21","flux2-klein-4b","qwen-image-edit"]
    else:
        order=["qwen-image-2512","z-image-turbo","flux2-klein-4b","sdxl-base","sd21"] if v>=24000 else ["z-image-turbo","flux2-klein-4b","sdxl-base","sd21","qwen-image-2512"] if v>=12000 else ["sdxl-base","sd21","z-image-turbo","flux2-klein-4b","qwen-image-2512"]
    cid=_first(order)
    if not cid:raise RuntimeError("No local image model is installed")
    fb=_first([x for x in order if x!=cid]);return RouteDecision(role,_provider(cid),f"hardware-aware auto route ({v//1024}GB VRAM class)",_provider(fb) if fb else None)

def choose_3d_provider(role="quality",mode="auto"):
    role=(role or "quality").lower();mode=(mode or "auto").lower();v=int(hardware_info().get("vram_mb",0) or 0)
    if mode!="auto":
        cid=THREED_IDS.get(mode)
        if not cid:raise RuntimeError(f"Unknown 3D provider '{mode}'")
        if not installed(cid):raise RuntimeError(f"Requested 3D provider '{mode}' is not installed")
        readiness=check_3d_provider(mode)
        if not readiness.ready:
            raise RuntimeError(f"Requested 3D provider '{mode}' is installed but not ready: {_readiness_error(mode)}")
        return RouteDecision(role,mode,"explicit user/provider selection; readiness preflight passed")
    if role in {"parts","structured"}:order=["partpacker","partcrafter"] if v>=16000 else ["partcrafter","partpacker"]
    elif role in {"fast","draft","rough"}:order=["spar3d","sf3d","triposr"] if v>=12000 else ["sf3d","triposr","spar3d"]
    elif v>=24000:order=["trellis2","hunyuan21-shape","spar3d","hunyuan2mini","sf3d","triposr"]
    elif v>=10000:order=["hunyuan21-shape","spar3d","hunyuan2mini","sf3d","triposr","trellis2"]
    else:order=["hunyuan2mini","sf3d","triposr","spar3d","hunyuan21-shape"]
    cid=_first_ready_3d(order)
    if not cid:
        installed_candidates=[_provider(x) for x in order if installed(x)]
        if installed_candidates:
            details=" | ".join(f"{p}: {_readiness_error(p)}" for p in installed_candidates)
            raise RuntimeError(f"Installed local 3D providers are not runtime-ready. {details}")
        raise RuntimeError("No local 3D model is installed")
    provider=_provider(cid)
    fb=_first_ready_3d([x for x in order if x!=cid])
    return RouteDecision(role,provider,f"hardware-aware auto route ({v//1024}GB VRAM class); readiness preflight passed",_provider(fb) if fb else None)

def routing_status()->dict[str,Any]:
    hw=hardware_info();r={"image":{},"three_d":{},"capabilities":{},"provider_readiness":{}}
    for role in ("generate","edit","detail"):
        try:r["image"][role]=asdict(choose_image_provider(role))
        except Exception as e:r["image"][role]={"error":str(e)}
    for role in ("fast","quality","detail","structured"):
        try:r["three_d"][role]=asdict(choose_3d_provider(role))
        except Exception as e:r["three_d"][role]={"error":str(e)}
    for provider in THREED_IDS:
        try:r["provider_readiness"][provider]=check_3d_provider(provider).to_dict()
        except Exception as e:r["provider_readiness"][provider]={"provider":provider,"ready":False,"errors":[str(e)]}
    rolemap={"generate":"concept","edit":"edit","detail-image":"detail","fast":"fast3d","quality":"quality3d","structured":"parts","select":"select"}
    for label,role in rolemap.items():r["capabilities"][label]=role_options(role,int(hw.get("vram_mb",0) or 0))
    return r

def release_all_models():
    for module_name,fn_name in (("local_image","release_models"),("sdxl_image","release_models"),("flux_klein","release_models"),("modern_image","release_models"),("hunyuan_shape","release_model"),("triposr_shape","release_model"),("semantic_select","release_model")):
        try:
            m=__import__(module_name,fromlist=[fn_name]);fn=getattr(m,fn_name,None)
            if callable(fn):fn()
        except Exception:pass
