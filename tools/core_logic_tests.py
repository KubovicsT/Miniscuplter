from __future__ import annotations
import os,tempfile,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/"ai_backend"))
import model_manager,model_router,model_capabilities,quality_runtime,model_downloads,modern_image,provider_readiness
from storage import validate_input_path, validate_output_path
def check(c,m):
    if not c:raise AssertionError(m)
def test_quality_clamps():
    c=quality_runtime.normalize({"image_size":99999,"image_steps":-1,"image_guidance":999,"image_edit_strength":-2,"max_input_px":1,"shape_steps":999,"remesh_voxel_mm":0,"repair_voxel_mm":99,"max_voxel_cells":999999999999,"thickness_samples":1,"smart_select_views":99,"smart_select_render_size":1});check(c["image_size"]==1536,"image clamp");check(c["remesh_voxel_mm"]==.04,"remesh clamp");check(c["smart_select_views"]==12,"views clamp")
def test_model_routing():
    oi,oh,oroute=model_router.installed,model_router.hardware_info,model_router.route_eligible
    try:
        present={"sdxl-base","sd21","triposr","hunyuan21-shape","partcrafter","hunyuan2mini","sf3d"};model_router.installed=lambda x:x in present;model_router.hardware_info=lambda:{"vram_mb":8192};model_router.route_eligible=lambda cid:(True,{"route_eligible":True,"failure":None})
        check(model_router.choose_image_provider("generate").provider=="sdxl","8GB concept should prefer SDXL");check(model_router.choose_3d_provider("fast").provider=="sf3d","8GB fast route should prefer SF3D");check(model_router.choose_3d_provider("quality").provider=="hunyuan-mini","8GB quality route should prefer Hunyuan mini");check(model_router.choose_3d_provider("structured").provider=="partcrafter","parts route")
        present.update({"flux2-klein-4b","qwen-image-edit","qwen-image-2512","z-image-turbo","spar3d","partpacker","trellis2"});model_router.hardware_info=lambda:{"vram_mb":24576};check(model_router.choose_image_provider("generate").provider=="qwen","24GB concept should prefer Qwen");check(model_router.choose_image_provider("detail").provider=="qwen-edit","24GB edit should prefer Qwen Edit");check(model_router.choose_3d_provider("quality").provider=="trellis2","24GB quality should prefer TRELLIS.2");check(model_router.choose_3d_provider("structured").provider=="partpacker","24GB parts should prefer PartPacker")
        try:model_router.choose_image_provider("generate","does-not-exist");raise AssertionError("unknown explicit provider accepted")
        except RuntimeError:pass
    finally:model_router.installed,model_router.hardware_info,model_router.route_eligible=oi,oh,oroute
def test_readiness_aware_routing():
    oi,oh,oroute,oinspect=model_router.installed,model_router.hardware_info,model_router.route_eligible,model_router.inspect_provider
    try:
        present={"hunyuan2mini","sf3d","triposr"};model_router.installed=lambda x:x in present;model_router.hardware_info=lambda:{"vram_mb":8192}
        states={"hunyuan2mini":(False,{"route_eligible":False,"failure":"missing pymeshlab"}),"sf3d":(True,{"route_eligible":True,"failure":None}),"triposr":(True,{"route_eligible":True,"failure":None})}
        model_router.route_eligible=lambda cid:states[cid];model_router.inspect_provider=lambda cid,probe=False:states[cid][1]
        d=model_router.choose_3d_provider("quality");check(d.provider=="sf3d","Auto must skip a known-unready preferred provider");check(d.fallback=="triposr","fallback must also be readiness-qualified")
        try:model_router.choose_3d_provider("quality","hunyuan-mini");raise AssertionError("explicit broken provider accepted")
        except RuntimeError as exc:check("missing pymeshlab" in str(exc),"explicit readiness failure should explain the dependency problem")
    finally:model_router.installed,model_router.hardware_info,model_router.route_eligible,model_router.inspect_provider=oi,oh,oroute,oinspect
def test_provider_readiness_contract():
    old_load,old_save,old_component,old_probe,old_revision=provider_readiness.model_manager.load_state,provider_readiness.model_manager.save_state,provider_readiness.model_manager.component_path,provider_readiness._probe_python,provider_readiness._provider_revision
    state={"installed":{"triposr":{"installed":True,"path":"dummy"}},"provider_qualification":{}}
    try:
        provider_readiness.model_manager.load_state=lambda:state
        provider_readiness.model_manager.save_state=lambda value:(state.clear(),state.update(value))
        provider_readiness.model_manager.component_path=lambda cid:Path(".") if cid=="triposr" else None
        provider_readiness._provider_revision=lambda cid:"hf:abc git:def"
        provider_readiness._probe_python=lambda cid:{"supported":True,"importable":True,"device_tested":True,"runtime_version":"python 3.10 / torch test","device":{"cuda_available":True,"name":"GPU","vram_bytes":8},"failure":None}
        provider_readiness._CACHE.clear();result=provider_readiness.inspect_provider("triposr",probe=True,persist=True)
        check(result["downloaded"] and result["installed"],"installed/downloaded states missing");check(result["importable"] is True and result["device_tested"] is True,"preflight states missing");check(result["inference_tested"] is False,"preflight must not pretend inference ran");check(result["route_eligible"],"healthy preflight should be routable")
        provider_readiness.record_inference_success("triposr",12.5,{"peak_vram_mb":7000});provider_readiness._CACHE.clear();qualified=provider_readiness.inspect_provider("triposr",probe=False)
        check(qualified["inference_tested"],"real inference success should promote inference-tested state");check(qualified["benchmark"]["elapsed_seconds"]==12.5,"benchmark elapsed time not retained");check(qualified["benchmark"]["peak_vram_mb"]==7000,"benchmark metadata not retained")
    finally:
        provider_readiness.model_manager.load_state,provider_readiness.model_manager.save_state,provider_readiness.model_manager.component_path=old_load,old_save,old_component
        provider_readiness._probe_python,provider_readiness._provider_revision=old_probe,old_revision;provider_readiness._CACHE.clear()
def test_capabilities():
    rows={x["id"]:x for x in model_capabilities.recommendations(8192,"win32")};check(rows["hunyuan2mini"]["hardware_fit"] in {"recommended","possible"},"Hunyuan mini 8GB fit");check(rows["trellis2"]["wsl_possible"],"TRELLIS Windows should expose WSL route");check(rows["trellis2"]["hardware_fit"]=="not-recommended","TRELLIS must not be recommended at 8GB")
def test_modern_image_low_vram_strategy():
    check(modern_image._offload_strategy("z-image-turbo",8192,"auto")=="sequential","Z-Image 8GB must use sequential offload")
    check(modern_image._offload_strategy("z-image-turbo",8192,"fast")=="sequential","Z-Image 8GB Fast must not try whole-model GPU placement")
    check(modern_image._offload_strategy("z-image-turbo",16384,"balanced")=="model","Z-Image 16GB balanced should use model offload")
    check(modern_image._offload_strategy("z-image-turbo",24576,"fast")=="cuda","Z-Image workstation Fast should permit full GPU")
    check(modern_image._generation_size("z-image-turbo",8192)<=768,"Z-Image 8GB canvas guard")
    check(modern_image._generation_size("z-image-turbo",8192,retry=True)<=640,"Z-Image OOM retry canvas guard")
def test_component_file_validation():
    old=model_manager.TOOLS_ROOT
    try:
        with tempfile.TemporaryDirectory() as t:
            r=Path(t);model_manager.TOOLS_ROOT=r/"tools";model_manager.TOOLS_ROOT.mkdir();clip=r/"clip";clip.mkdir();check(not model_manager._component_files_valid("clipseg-smart-select",clip),"empty clip");(clip/"config.json").write_text("{}");(clip/"model.safetensors").write_bytes(b"x");check(model_manager._component_files_valid("clipseg-smart-select",clip),"clip markers")
    finally:model_manager.TOOLS_ROOT=old
def test_storage_path_boundaries():
    old = os.environ.get("MINISCULPTER_DATA")
    try:
        with tempfile.TemporaryDirectory() as t:
            root = Path(t) / "AIData"
            root.mkdir()
            source = Path(t) / "source.stl"
            source.write_bytes(b"solid")
            os.environ["MINISCULPTER_DATA"] = str(root)
            safe = validate_input_path(source, (".stl",), max_bytes=1024)
            check(safe.resolve() == source.resolve(), "external read-only input was not accepted")
            output = validate_output_path("Workspace/result.stl", (".stl",))
            try:
                output.resolve().relative_to(root.resolve())
                contained = True
            except ValueError:
                contained = False
            check(output.parent.is_dir() and contained, "contained output was not created")
            try:
                validate_output_path(Path(t).parent / "escape.stl", (".stl",))
                raise AssertionError("output path escaped the configured data root")
            except ValueError:
                pass
    finally:
        if old is None:
            os.environ.pop("MINISCULPTER_DATA", None)
        else:
            os.environ["MINISCULPTER_DATA"] = old

def test_uninstall_path_guard():
    try:model_manager._managed_path(ROOT);raise AssertionError("path guard accepted repo")
    except RuntimeError:pass
def test_resumable_stage_recovery():
    with tempfile.TemporaryDirectory() as t:
        root=Path(t)
        small=root/"sdxl-base-small";(small/"models"/"stable-diffusion-xl-base-1.0").mkdir(parents=True);(small/"models"/"stable-diffusion-xl-base-1.0"/"small.bin").write_bytes(b"old")
        large=root/"sdxl-base-large";(large/"models"/"stable-diffusion-xl-base-1.0").mkdir(parents=True);(large/"models"/"stable-diffusion-xl-base-1.0"/"partial.bin").write_bytes(b"most-progress"*10)
        stage=model_downloads.prepare_stage(root,"sdxl-base","rev-a","manifest-a","install");check(stage.name=="sdxl-base-partial","deterministic stage name");check(not large.exists(),"largest legacy UUID stage not migrated");check(not small.exists(),"redundant legacy UUID stage not cleaned up");check((stage/"models"/"stable-diffusion-xl-base-1.0"/"partial.bin").exists(),"largest reusable partial was not retained")
        s=model_downloads.stage_status(root,"sdxl-base");check(s["resume_available"] and s["resume_action"]=="install","partial install not reported")
        same=model_downloads.prepare_stage(root,"sdxl-base","rev-a","manifest-a","install");check((same/"models"/"stable-diffusion-xl-base-1.0"/"partial.bin").exists(),"matching stage was not preserved")
        fresh=model_downloads.prepare_stage(root,"sdxl-base","rev-b","manifest-a","install");check(not (fresh/"models").exists(),"stale revision payload was reused");check(not model_downloads.stage_status(root,"sdxl-base")["resume_available"],"metadata-only stage incorrectly reported as resumable")
if __name__=="__main__":test_quality_clamps();test_model_routing();test_readiness_aware_routing();test_provider_readiness_contract();test_capabilities();test_modern_image_low_vram_strategy();test_component_file_validation();test_storage_path_boundaries();test_uninstall_path_guard();test_resumable_stage_recovery();print("v1.0.20 core logic tests passed")