from __future__ import annotations
import tempfile,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/"ai_backend"))
import model_manager,model_router,model_capabilities,quality_runtime
def check(c,m):
    if not c:raise AssertionError(m)
def test_quality_clamps():
    c=quality_runtime.normalize({"image_size":99999,"image_steps":-1,"image_guidance":999,"image_edit_strength":-2,"max_input_px":1,"shape_steps":999,"remesh_voxel_mm":0,"repair_voxel_mm":99,"max_voxel_cells":999999999999,"thickness_samples":1,"smart_select_views":99,"smart_select_render_size":1});check(c["image_size"]==1536,"image clamp");check(c["remesh_voxel_mm"]==.04,"remesh clamp");check(c["smart_select_views"]==12,"views clamp")
def test_model_routing():
    oi,oh=model_router.installed,model_router.hardware_info
    try:
        present={"sdxl-base","sd21","triposr","hunyuan21-shape","partcrafter","hunyuan2mini","sf3d"};model_router.installed=lambda x:x in present;model_router.hardware_info=lambda:{"vram_mb":8192}
        check(model_router.choose_image_provider("generate").provider=="sdxl","8GB concept should prefer SDXL");check(model_router.choose_3d_provider("fast").provider=="sf3d","8GB fast route should prefer SF3D");check(model_router.choose_3d_provider("quality").provider=="hunyuan-mini","8GB quality route should prefer Hunyuan mini");check(model_router.choose_3d_provider("structured").provider=="partcrafter","parts route")
        present.update({"flux2-klein-4b","qwen-image-edit","qwen-image-2512","z-image-turbo","spar3d","partpacker","trellis2"});model_router.hardware_info=lambda:{"vram_mb":24576};check(model_router.choose_image_provider("generate").provider=="qwen","24GB concept should prefer Qwen");check(model_router.choose_image_provider("detail").provider=="qwen-edit","24GB edit should prefer Qwen Edit");check(model_router.choose_3d_provider("quality").provider=="trellis2","24GB quality should prefer TRELLIS.2");check(model_router.choose_3d_provider("structured").provider=="partpacker","24GB parts should prefer PartPacker")
        try:model_router.choose_image_provider("generate","does-not-exist");raise AssertionError("unknown explicit provider accepted")
        except RuntimeError:pass
    finally:model_router.installed,model_router.hardware_info=oi,oh
def test_capabilities():
    rows={x["id"]:x for x in model_capabilities.recommendations(8192,"win32")};check(rows["hunyuan2mini"]["hardware_fit"] in {"recommended","possible"},"Hunyuan mini 8GB fit");check(rows["trellis2"]["wsl_possible"],"TRELLIS Windows should expose WSL route");check(rows["trellis2"]["hardware_fit"]=="not-recommended","TRELLIS must not be recommended at 8GB")
def test_component_file_validation():
    old=model_manager.TOOLS_ROOT
    try:
        with tempfile.TemporaryDirectory() as t:
            r=Path(t);model_manager.TOOLS_ROOT=r/"tools";model_manager.TOOLS_ROOT.mkdir();clip=r/"clip";clip.mkdir();check(not model_manager._component_files_valid("clipseg-smart-select",clip),"empty clip");(clip/"config.json").write_text("{}");(clip/"model.safetensors").write_bytes(b"x");check(model_manager._component_files_valid("clipseg-smart-select",clip),"clip markers")
    finally:model_manager.TOOLS_ROOT=old
def test_uninstall_path_guard():
    try:model_manager._managed_path(ROOT);raise AssertionError("path guard accepted repo")
    except RuntimeError:pass
if __name__=="__main__":test_quality_clamps();test_model_routing();test_capabilities();test_component_file_validation();test_uninstall_path_guard();print("v1.0.5 core logic tests passed")
