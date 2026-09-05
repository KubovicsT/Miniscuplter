from __future__ import annotations
import os,shutil,subprocess,sys
from pathlib import Path
from model_manager import component_path,TOOLS_ROOT
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
    code=TOOLS_ROOT/"stable-fast-3d";component_path("sf3d") or (_ for _ in ()).throw(RuntimeError("Stable Fast 3D is not installed"));model=code/"miniscuplter-model"
    if not (model/"model.safetensors").is_file():raise RuntimeError("Stable Fast 3D local weights are incomplete. Resume/reinstall the component.")
    work=Path(output).resolve().parent/".sf3d-output";shutil.rmtree(work,ignore_errors=True);work.mkdir(parents=True)
    _run([_py(code),"run.py",str(Path(image).resolve()),"--output-dir",str(work),"--pretrained-model",str(model)],code);c=sorted(work.rglob("*.glb"),key=lambda p:p.stat().st_mtime,reverse=True)
    if not c:raise RuntimeError("Stable Fast 3D returned no GLB")
    return _to_stl(c[0],output)
def generate_spar3d(image,output,low_vram=False):
    code=TOOLS_ROOT/"stable-point-aware-3d";component_path("spar3d") or (_ for _ in ()).throw(RuntimeError("SPAR3D is not installed"));model=code/"miniscuplter-model"
    if not (model/"model.safetensors").is_file():raise RuntimeError("SPAR3D local weights are incomplete. Resume/reinstall the component.")
    work=Path(output).resolve().parent/".spar3d-output";shutil.rmtree(work,ignore_errors=True);work.mkdir(parents=True);args=[_py(code),"run.py",str(Path(image).resolve()),"--output-dir",str(work),"--pretrained-model",str(model)];args+=(["--low-vram-mode"] if low_vram else []);_run(args,code);c=sorted(work.rglob("*.glb"),key=lambda p:p.stat().st_mtime,reverse=True)
    if not c:raise RuntimeError("SPAR3D returned no GLB")
    return _to_stl(c[0],output)
def generate_hunyuan_mini(image,output):
    code=TOOLS_ROOT/"Hunyuan3D-2";model=component_path("hunyuan2mini")
    if model is None or not code.exists():raise RuntimeError("Hunyuan3D 2mini is not installed")
    sys.path.insert(0,str(code))
    try:
        from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
        pipe=Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(model),subfolder="hunyuan3d-dit-v2-mini",use_safetensors=True);mesh=pipe(image=str(Path(image).resolve()))[0];out=Path(output).resolve();out.parent.mkdir(parents=True,exist_ok=True);mesh.export(out);return str(out)
    finally:
        try:sys.path.remove(str(code))
        except ValueError:pass
def generate_trellis2(image,output):
    template=os.getenv("MINISCULPTER_TRELLIS2_COMMAND","").strip()
    if not template:raise RuntimeError("TRELLIS.2 uses its official Linux runtime. Configure MINISCULPTER_TRELLIS2_COMMAND for native Linux or WSL2 after installation.")
    out=Path(output).resolve();out.parent.mkdir(parents=True,exist_ok=True);p=subprocess.run(template.format(image=str(Path(image).resolve()),output=str(out)),shell=True,capture_output=True,text=True,timeout=10800)
    if p.returncode!=0 or not out.exists():raise RuntimeError((p.stderr or p.stdout or "TRELLIS.2 produced no output")[-5000:])
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
