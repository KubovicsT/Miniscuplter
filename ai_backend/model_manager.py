from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import uuid
from pathlib import Path
from typing import Any, Callable

ROOT = Path(__file__).resolve().parent
DATA_ROOT = Path(os.getenv("MINISCULPTER_DATA", ROOT / "data")).resolve()
MODELS_ROOT = DATA_ROOT / "models"
TOOLS_ROOT = DATA_ROOT / "tools"
STAGING_ROOT = DATA_ROOT / ".staging"
STATE_FILE = DATA_ROOT / "components.json"

COMPONENTS: dict[str, dict[str, Any]] = {
    "sd21": {"name":"Stable Diffusion 2.1 Base","kind":"image","source":"huggingface","repo_id":"stabilityai/stable-diffusion-2-1-base","description":"Legacy low-memory 2D fallback.","estimated_gb":5.5},
    "sdxl-base": {"name":"Stable Diffusion XL Base 1.0","kind":"image","source":"huggingface","repo_id":"stabilityai/stable-diffusion-xl-base-1.0","description":"Primary modern 2D generator/editor for 8GB-class hardware using CPU offload.","estimated_gb":7.0},
    "flux2-klein-4b": {"name":"FLUX.2 Klein 4B","kind":"image","source":"huggingface","repo_id":"black-forest-labs/FLUX.2-klein-4B","description":"Optional high-quality generation/editing specialist.","estimated_gb":13.0},
    "hunyuan21-shape": {"name":"Hunyuan3D 2.1 Shape","kind":"3d","source":"hunyuan","repo_id":"tencent/Hunyuan3D-2.1","code_url":"https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1.git","description":"Quality whole-object and selected-detail image-to-shape model.","estimated_gb":10.0},
    "triposr": {"name":"TripoSR","kind":"3d","source":"huggingface","repo_id":"stabilityai/TripoSR","code_url":"https://github.com/VAST-AI-Research/TripoSR.git","description":"Fast rough single-image 3D reconstruction route.","estimated_gb":2.5},
    "partcrafter": {"name":"PartCrafter","kind":"3d-parts","source":"github+huggingface","repo_id":"wgsxm/PartCrafter","code_url":"https://github.com/wgsxm/PartCrafter.git","description":"Structured part-aware 3D generation specialist.","estimated_gb":12.0},
    "clipseg-smart-select": {"name":"CLIPSeg Smart Select","kind":"segmentation","source":"huggingface","repo_id":"CIDAS/clipseg-rd64-refined","description":"Local text-guided semantic segmentation used by Smart Select.","estimated_gb":0.7},
}

def _run(command:list[str],cwd:Path|None=None,timeout:int|None=None): return subprocess.run(command,cwd=cwd,check=True,text=True,capture_output=True,timeout=timeout)
def _disk_info():
    DATA_ROOT.mkdir(parents=True,exist_ok=True); u=shutil.disk_usage(DATA_ROOT); g=1024**3; return {"free_gb":round(u.free/g,2),"total_gb":round(u.total/g,2)}
def hardware_info():
    info={"platform":sys.platform,"python":sys.version.split()[0],"gpu":None,"vram_mb":0,"cuda_available":False,"recommended_profile":"cpu",**_disk_info()}; n=shutil.which("nvidia-smi")
    if n:
        try:
            p=_run([n,"--query-gpu=name,memory.total","--format=csv,noheader,nounits"],timeout=10); line=p.stdout.strip().splitlines()[0]; name,mem=[x.strip() for x in line.rsplit(",",1)]; info.update(gpu=name,vram_mb=int(mem),cuda_available=True); v=int(mem); info["recommended_profile"]="ultra" if v>12288 else "high" if v>8192 else "medium" if v>4096 else "low"
        except Exception: pass
    return info
def load_state():
    if not STATE_FILE.exists(): return {"installed":{},"settings":{}}
    try:
        d=json.loads(STATE_FILE.read_text(encoding="utf-8")); return d if isinstance(d,dict) else {"installed":{},"settings":{}}
    except Exception:return {"installed":{},"settings":{}}
def save_state(state):
    DATA_ROOT.mkdir(parents=True,exist_ok=True); t=STATE_FILE.with_suffix(".json.tmp"); t.write_text(json.dumps(state,indent=2),encoding="utf-8"); t.replace(STATE_FILE)
def _hf_revision(repo_id):
    from huggingface_hub import HfApi
    i=HfApi().model_info(repo_id,revision="main");
    if not i.sha: raise RuntimeError(f"Hugging Face returned no revision for {repo_id}")
    return str(i.sha)
def _git_local_revision(path):
    if not path.exists() or not (path/".git").exists(): return None
    try:return _run(["git","rev-parse","HEAD"],cwd=path,timeout=10).stdout.strip() or None
    except Exception:return None
def _git_remote_revision(url):
    g=shutil.which("git");
    if not g: raise RuntimeError("Git is required")
    o=_run([g,"ls-remote",url,"HEAD"],timeout=30).stdout.strip(); return o.split()[0]
def _tool_dir(cid,tools_root=None):
    r=tools_root or TOOLS_ROOT; return {"hunyuan21-shape":r/"Hunyuan3D-2.1","triposr":r/"TripoSR","partcrafter":r/"PartCrafter","hunyuan2mini":r/"Hunyuan3D-2","sf3d":r/"stable-fast-3d","spar3d":r/"stable-point-aware-3d","partpacker":r/"PartPacker","trellis2":r/"TRELLIS.2"}.get(cid)
def _combined_revision(hf,tool): return f"hf:{hf[:12]} git:{tool[:12]}" if hf and tool else hf or tool
def _remote_revisions(cid,spec): return (_hf_revision(spec["repo_id"]) if spec.get("repo_id") else None,_git_remote_revision(spec["code_url"]) if spec.get("code_url") else None)
def _directory_has_files(path):
    try:return path.is_dir() and any(p.is_file() for p in path.rglob("*"))
    except OSError:return False
def _component_files_valid(cid,path,tools_root=None):
    tools=tools_root or TOOLS_ROOT
    if not path.exists():return False
    if cid in {"sd21","sdxl-base","flux2-klein-4b","z-image-turbo","qwen-image-2512","qwen-image-edit"}:return (path/"model_index.json").is_file() and _directory_has_files(path)
    if cid=="hunyuan21-shape":return _directory_has_files(path/"hunyuan3d-dit-v2-1") and _directory_has_files(path/"hunyuan3d-vae-v2-1") and _directory_has_files(tools/"Hunyuan3D-2.1")
    if cid=="triposr":return (path/"model.ckpt").is_file() and (path/"config.yaml").is_file() and _directory_has_files(tools/"TripoSR")
    if cid=="partcrafter":return _directory_has_files(path/"pretrained_weights"/"PartCrafter") and _directory_has_files(path/"pretrained_weights"/"RMBG-1.4") and (path/".git").is_dir()
    if cid=="clipseg-smart-select":return (path/"config.json").is_file() and (path/"model.safetensors").is_file()
    return _directory_has_files(path)
def component_path(cid):
    e=load_state().get("installed",{}).get(cid)
    if not isinstance(e,dict) or not e.get("installed") or not e.get("path"):return None
    p=Path(e["path"]).resolve(); return p if _component_files_valid(cid,p) else None
def status(check_updates=False):
    state=load_state(); result=[]; hw=hardware_info()
    try:
        from model_capabilities import recommendations
        fits={x["id"]:x for x in recommendations(int(hw.get("vram_mb",0)),sys.platform)}
    except Exception:fits={}
    for cid,spec in COMPONENTS.items():
        s=state.get("installed",{}).get(cid,{}); p=component_path(cid); e=dict(spec); e.update(id=cid,installed=p is not None,path=str(p) if p else s.get("path"),installed_revision=_combined_revision(s.get("hf_revision"),s.get("tool_revision")),remote_revision=None,update_available=False,update_error=None); e.update({k:v for k,v in fits.get(cid,{}).items() if k not in {"id","name"}})
        if isinstance(s,dict) and s.get("installed") and p is None:e["update_error"]="Installation is incomplete. Reinstall this component."
        if p and check_updates:
            try:
                rh,rt=_remote_revisions(cid,spec); e["remote_revision"]=_combined_revision(rh,rt); e["update_available"]=(rh is not None and s.get("hf_revision")!=rh) or (rt is not None and s.get("tool_revision")!=rt)
            except Exception as ex:e["update_error"]=str(ex)
        result.append(e)
    return {"hardware":hw,"components":result,"data_root":str(DATA_ROOT),"disk":_disk_info()}
def _clone_fresh(url,target):
    g=shutil.which("git");
    if not g:raise RuntimeError("Git is required")
    target.parent.mkdir(parents=True,exist_ok=True);_run([g,"clone","--depth","1",url,str(target)])
def _pip_install(packages,extra_args=None):
    if not packages:return
    c=[sys.executable,"-m","pip","install",*packages]; c.extend(extra_args or []);_run(c);_run([sys.executable,"-m","pip","check"])
def _verify_tool_import(code_dir,statement,label,extra_path=None):
    ps=[str(code_dir)];
    if extra_path is not None:ps.insert(0,str(extra_path))
    probe="import sys; "+"".join(f"sys.path.insert(0, {p!r}); " for p in reversed(ps))+statement;_run([sys.executable,"-c",probe],cwd=code_dir,timeout=120)
def _install_hunyuan_dependencies(code_dir):_pip_install(["PyYAML>=6.0","tqdm>=4.66"]);_verify_tool_import(code_dir,"from hy3dshape.pipelines import Hunyuan3DDiTFlowMatchingPipeline","Hunyuan3D 2.1",code_dir/"hy3dshape")
def _install_triposr_dependencies(code_dir):_pip_install(["git+https://github.com/tatsy/torchmcubes.git","imageio[ffmpeg]","xatlas==0.0.9","moderngl==5.10.0"]);_verify_tool_import(code_dir,"from tsr.system import TSR","TripoSR")
def _install_partcrafter_dependencies(code_dir):
    _pip_install(["numpy==1.26.4","scikit-learn","opencv-python","peft","jaxtyping","typeguard","matplotlib","imageio-ffmpeg","pyrender","colormaps"]);_pip_install(["torch-cluster"],["-f","https://data.pyg.org/whl/torch-2.5.1+cu124.html"]);_verify_tool_import(code_dir,"from src.pipelines.pipeline_partcrafter import PartCrafterPipeline","PartCrafter")
def _download_hf(snapshot_download,repo_id,target,revision,allow_patterns=None):
    kw={"repo_id":repo_id,"revision":revision,"local_dir":target};
    if allow_patterns is not None:kw["allow_patterns"]=allow_patterns
    snapshot_download(**kw)
def _swap_staged(replacements,finalize):
    b=STAGING_ROOT/("backup-"+uuid.uuid4().hex);b.mkdir(parents=True,exist_ok=True);m=[]
    try:
        for i,(s,f) in enumerate(replacements):
            f.parent.mkdir(parents=True,exist_ok=True);old=None
            if f.exists():old=b/f"old-{i:02d}";f.rename(old)
            try:s.rename(f)
            except Exception:
                if old and old.exists() and not f.exists():old.rename(f)
                raise
            m.append((f,old))
        finalize()
    except Exception:
        for f,old in reversed(m):
            if f.exists():shutil.rmtree(f)
            if old and old.exists():old.rename(f)
        raise
    else:shutil.rmtree(b,ignore_errors=True)

def install_component(cid,update=False):
    if cid not in COMPONENTS:raise ValueError(f"Unknown AI component: {cid}")
    spec=COMPONENTS[cid];MODELS_ROOT.mkdir(parents=True,exist_ok=True);TOOLS_ROOT.mkdir(parents=True,exist_ok=True);STAGING_ROOT.mkdir(parents=True,exist_ok=True);need=float(spec.get("estimated_gb",0))*1.25+1;disk=_disk_info()
    if disk["free_gb"]<need:raise RuntimeError(f"Not enough free disk space for {spec['name']}")
    from huggingface_hub import snapshot_download
    stage=STAGING_ROOT/f"{cid}-{uuid.uuid4().hex}";sm=stage/"models";st=stage/"tools";sm.mkdir(parents=True);st.mkdir(parents=True);repl=[];hf=_hf_revision(spec["repo_id"]) if spec.get("repo_id") else None;tool=None
    try:
        if cid=="sd21":target=sm/"stable-diffusion-2-1-base";final=MODELS_ROOT/"stable-diffusion-2-1-base";_download_hf(snapshot_download,spec["repo_id"],target,hf);repl.append((target,final))
        elif cid=="sdxl-base":target=sm/"stable-diffusion-xl-base-1.0";final=MODELS_ROOT/"stable-diffusion-xl-base-1.0";_download_hf(snapshot_download,spec["repo_id"],target,hf,["model_index.json","scheduler/**","text_encoder/**","text_encoder_2/**","tokenizer/**","tokenizer_2/**","unet/**","vae/**","*.safetensors"]);repl.append((target,final))
        elif cid=="flux2-klein-4b":target=sm/"FLUX.2-klein-4B";final=MODELS_ROOT/"FLUX.2-klein-4B";_download_hf(snapshot_download,spec["repo_id"],target,hf);repl.append((target,final))
        elif cid=="hunyuan21-shape":code=st/"Hunyuan3D-2.1";fc=TOOLS_ROOT/"Hunyuan3D-2.1";_clone_fresh(spec["code_url"],code);_install_hunyuan_dependencies(code);target=sm/"Hunyuan3D-2.1";final=MODELS_ROOT/"Hunyuan3D-2.1";_download_hf(snapshot_download,spec["repo_id"],target,hf,["hunyuan3d-dit-v2-1/**","hunyuan3d-vae-v2-1/**"]);tool=_git_local_revision(code);repl.extend([(code,fc),(target,final)])
        elif cid=="triposr":code=st/"TripoSR";fc=TOOLS_ROOT/"TripoSR";_clone_fresh(spec["code_url"],code);_install_triposr_dependencies(code);target=sm/"TripoSR";final=MODELS_ROOT/"TripoSR";_download_hf(snapshot_download,spec["repo_id"],target,hf,["config.yaml","model.ckpt"]);tool=_git_local_revision(code);repl.extend([(code,fc),(target,final)])
        elif cid=="partcrafter":target=st/"PartCrafter";final=TOOLS_ROOT/"PartCrafter";_clone_fresh(spec["code_url"],target);_install_partcrafter_dependencies(target);_download_hf(snapshot_download,spec["repo_id"],target/"pretrained_weights"/"PartCrafter",hf);r=_hf_revision("briaai/RMBG-1.4");_download_hf(snapshot_download,"briaai/RMBG-1.4",target/"pretrained_weights"/"RMBG-1.4",r);tool=_git_local_revision(target);repl.append((target,final))
        elif cid=="clipseg-smart-select":target=sm/"clipseg-rd64-refined";final=MODELS_ROOT/"clipseg-rd64-refined";_download_hf(snapshot_download,spec["repo_id"],target,hf,["config.json","preprocessor_config.json","tokenizer_config.json","special_tokens_map.json","vocab.json","merges.txt","model.safetensors"]);repl.append((target,final))
        else:raise RuntimeError(f"No legacy installer implemented for {cid}")
        if not _component_files_valid(cid,target,st):raise RuntimeError(f"{spec['name']} staged files are incomplete")
        def fin():
            s=load_state();s.setdefault("installed",{})[cid]={"installed":True,"path":str(final),"hf_revision":hf,"tool_revision":tool};save_state(s)
        _swap_staged(repl,fin);return {"id":cid,"installed":True,"path":str(final),"hardware":hardware_info()}
    finally:shutil.rmtree(stage,ignore_errors=True)
def update_component(cid):
    if component_path(cid) is None:raise RuntimeError("The model is not fully installed. Reinstall it instead.")
    return install_component(cid,True)
def _managed_path(path):
    r=path.resolve();
    if r==DATA_ROOT or DATA_ROOT not in r.parents:raise RuntimeError(f"Refusing to remove path outside AI data: {r}")
    return r
def uninstall_component(cid):
    if cid not in COMPONENTS:raise ValueError(f"Unknown AI component: {cid}")
    state=load_state();e=state.get("installed",{}).get(cid);paths=[]
    if isinstance(e,dict) and e.get("path"):paths.append(Path(e["path"]))
    t=_tool_dir(cid)
    if t is not None and all(t.resolve()!=p.resolve() for p in paths):paths.append(t)
    for p in paths:
        r=_managed_path(p)
        if r.exists():shutil.rmtree(r)
    state.setdefault("installed",{}).pop(cid,None);save_state(state);return {"id":cid,"installed":False}

# v1.0.5 extends, rather than forks, the hardened transactional model manager above.
from model_manager_v105 import install_component as install_component, update_component as update_component  # noqa:E402,F401
