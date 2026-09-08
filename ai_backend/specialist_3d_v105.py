from __future__ import annotations
import importlib,os,shlex,shutil,subprocess,sys
from pathlib import Path
from model_manager import component_path,TOOLS_ROOT
from job_progress import report

def _run(args,cwd,timeout=7200):
    p=subprocess.run(args,cwd=cwd,capture_output=True,text=True,timeout=timeout)
    if p.returncode!=0:raise RuntimeError((p.stderr or p.stdout or "specialist provider failed")[-5000:])
    return p

def _py(code):
    p=code/".venv"/("Scripts/python.exe" if os.name=="nt" else "bin/python")
    if not p.exists():raise RuntimeError(f"Isolated provider runtime is missing: {p}. Reinstall the component.")
    return str(p)

def _to_stl(source,output):
    import trimesh
    out=Path(output).resolve();out.parent.mkdir(parents=True,exist_ok=True);loaded=trimesh.load(source,force="scene");mesh=loaded.dump(concatenate=True) if isinstance(loaded,trimesh.Scene) else loaded
    if mesh is None or len(mesh.faces)==0:raise RuntimeError("Provider returned an empty mesh")
    mesh.export(out);return str(out)

def generate_sf3d(image,output):
    report("loading_model","Preparing Stable Fast 3D runtime and local weights.",22,"sf3d")
    code=TOOLS_ROOT/"stable-fast-3d";component_path("sf3d") or (_ for _ in ()).throw(RuntimeError("Stable Fast 3D is not installed"));model=code/"miniscuplter-model"
    if not (model/"model.safetensors").is_file():raise RuntimeError("Stable Fast 3D local weights are incomplete. Resume/reinstall the component.")
    work=Path(output).resolve().parent/".sf3d-output";shutil.rmtree(work,ignore_errors=True);work.mkdir(parents=True)
    report("running_inference","Stable Fast 3D reconstruction subprocess is running.",45,"sf3d")
    _run([_py(code),"run.py",str(Path(image).resolve()),"--output-dir",str(work),"--pretrained-model",str(model)],code);c=sorted(work.rglob("*.glb"),key=lambda p:p.stat().st_mtime,reverse=True)
    if not c:raise RuntimeError("Stable Fast 3D returned no GLB")
    report("decoding_output","Stable Fast 3D returned GLB geometry; converting it to STL.",90,"sf3d")
    return _to_stl(c[0],output)

def generate_spar3d(image,output,low_vram=False):
    report("loading_model","Preparing SPAR3D runtime and local weights.",22,"spar3d")
    code=TOOLS_ROOT/"stable-point-aware-3d";component_path("spar3d") or (_ for _ in ()).throw(RuntimeError("SPAR3D is not installed"));model=code/"miniscuplter-model"
    if not (model/"model.safetensors").is_file():raise RuntimeError("SPAR3D local weights are incomplete. Resume/reinstall the component.")
    work=Path(output).resolve().parent/".spar3d-output";shutil.rmtree(work,ignore_errors=True);work.mkdir(parents=True);args=[_py(code),"run.py",str(Path(image).resolve()),"--output-dir",str(work),"--pretrained-model",str(model)];args+=(["--low-vram-mode"] if low_vram else [])
    report("running_inference","SPAR3D reconstruction subprocess is running"+(" in low-VRAM mode." if low_vram else "."),45,"spar3d")
    _run(args,code);c=sorted(work.rglob("*.glb"),key=lambda p:p.stat().st_mtime,reverse=True)
    if not c:raise RuntimeError("SPAR3D returned no GLB")
    report("decoding_output","SPAR3D returned GLB geometry; converting it to STL.",90,"spar3d")
    return _to_stl(c[0],output)

def _require_hunyuan_mini_runtime():
    required=("cv2","pymeshlab","pygltflib","xatlas")
    missing=[]
    for name in required:
        try:importlib.import_module(name)
        except Exception:missing.append(name)
    if missing:
        raise RuntimeError("Hunyuan3D 2mini runtime is incomplete (missing: "+", ".join(missing)+"). Open Miniscuplter Launcher and run Repair AI Runtime; model weights will be preserved.")

def generate_hunyuan_mini(image,output):
    report("loading_model","Checking Hunyuan3D 2mini runtime dependencies and local weights.",20,"hunyuan-mini")
    code=TOOLS_ROOT/"Hunyuan3D-2";model=component_path("hunyuan2mini")
    if model is None or not code.exists():raise RuntimeError("Hunyuan3D 2mini is not installed")
    _require_hunyuan_mini_runtime()
    sys.path.insert(0,str(code))
    try:
        report("loading_model","Loading Hunyuan3D 2mini shape pipeline from local storage.",24,"hunyuan-mini")
        from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
        pipe=Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(model),subfolder="hunyuan3d-dit-v2-mini",use_safetensors=True)
        report("preparing_inputs","Hunyuan3D 2mini is loaded; preparing the accepted 2D baseline for reconstruction.",38,"hunyuan-mini")
        report("running_inference","Hunyuan3D 2mini flow-matching reconstruction is running. This is the long GPU/CPU stage.",45,"hunyuan-mini")
        mesh=pipe(image=str(Path(image).resolve()))[0]
        report("decoding_output","Hunyuan3D 2mini inference finished; converting generated geometry for export.",90,"hunyuan-mini")
        out=Path(output).resolve();out.parent.mkdir(parents=True,exist_ok=True)
        report("saving_result",f"Exporting generated mesh to {out.name}.",95,"hunyuan-mini")
        mesh.export(out);return str(out)
    finally:
        try:sys.path.remove(str(code))
        except ValueError:pass

def generate_trellis2(image,output):
    report("loading_model","Preparing configured TRELLIS.2 bridge/runtime.",20,"trellis2")
    template=os.getenv("MINISCULPTER_TRELLIS2_COMMAND","").strip()
    if not template:raise RuntimeError("TRELLIS.2 uses its official Linux runtime. Configure MINISCULPTER_TRELLIS2_COMMAND for native Linux or WSL2 after installation.")
    out=Path(output).resolve();out.parent.mkdir(parents=True,exist_ok=True)
    report("running_inference","TRELLIS.2 external reconstruction is running.",45,"trellis2")
    p=subprocess.run(template.format(image=shlex.quote(str(Path(image).resolve())),output=shlex.quote(str(out))),shell=True,capture_output=True,text=True,timeout=10800)
    if p.returncode!=0 or not out.exists():raise RuntimeError((p.stderr or p.stdout or "TRELLIS.2 produced no output")[-5000:])
    report("saving_result","TRELLIS.2 output file is ready.",95,"trellis2")
    return str(out)

def generate_partpacker(image,output_dir,tag="miniscuplter"):
    code=TOOLS_ROOT/"PartPacker"
    if component_path("partpacker") is None or not code.exists():raise RuntimeError("PartPacker is not installed")
    out=Path(output_dir).resolve();out.mkdir(parents=True,exist_ok=True);probe="import sys; sys.argv=['app.py']; import app; "+f"img=app.process_image({str(Path(image).resolve())!r}); "+"p=app.process_3d(img); print('MINISCULPTER_OUTPUT='+str(p))";completed=_run([_py(code),"-c",probe],code,10800);marker=[x.split("=",1)[1].strip() for x in completed.stdout.splitlines() if x.startswith("MINISCULPTER_OUTPUT=")]
    if not marker:raise RuntimeError("PartPacker did not report its output GLB")
    glb=Path(marker[-1]);glb=glb if glb.is_absolute() else code/glb
    import trimesh
    scene=trimesh.load(glb,force="scene")
    if not isinstance(scene,trimesh.Scene) or not scene.geometry:raise RuntimeError("PartPacker output contains no part geometry")
    parts=[]
    for i,mesh in enumerate(scene.geometry.values()):
        if len(mesh.faces)<=10:continue
        dst=out/f"{tag}_part_{i:02d}.stl";mesh.export(dst);parts.append(str(dst))
    if not parts:raise RuntimeError("PartPacker returned no usable part meshes")
    return {"provider":"partpacker","parts":parts,"count":len(parts)}
