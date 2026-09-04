using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    sealed class V098RouteState
    {
        public string ImageGenerate { get; set; } = "auto";
        public string ImageEdit { get; set; } = "auto";
        public string ImageDetail { get; set; } = "auto";
        public string Fast3D { get; set; } = "auto";
        public string Quality3D { get; set; } = "auto";
        public string Detail3D { get; set; } = "auto";
        public string Structured3D { get; set; } = "auto";
    }

    V098RouteState _v098Routes = new();
    Label? _v098Hardware, _v098Routing, _v098DetailStatus;
    VBoxContainer? _v098Components;
    SpinBox? _v098PartCount;
    MeshInstance3D? _v098DetailSource, _v098DetailPreview;
    string _v098DetailPatchPath = "";

    string V098RoutePath() => ProjectSettings.GlobalizePath("user://ai_routing_v098.json");
    string V098WorkDir()
    {
        string p = ProjectSettings.GlobalizePath("user://ai_detail_v098"); Directory.CreateDirectory(p); return p;
    }

    public void InstallV098MultiModelAI()
    {
        LoadV098Routes(); ApplyV098Routes();
        var tabs = FindChild("TabContainer", true, false) as TabContainer; if (tabs == null) return;
        var scroll = new ScrollContainer { Name = "AI Models v0.9.8", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(box); tabs.AddChild(scroll);
        box.AddChild(new Label { Text = "MULTI-MODEL AI + LOCAL DETAIL — v0.9.8", ThemeTypeVariation = "HeaderSmall" });
        box.AddChild(new Label { Text = "Specialist models are installed side-by-side but loaded sequentially. Automatic routing chooses a suitable installed model for each job; explicit selections always win.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        _v098Hardware = new Label { Text = "Hardware: detecting…", AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(_v098Hardware);
        _v098Routing = new Label { Text = "Routing: detecting…", AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(_v098Routing);

        box.AddChild(new HSeparator()); box.AddChild(new Label { Text = "ROLE ROUTING", ThemeTypeVariation = "HeaderSmall" });
        AddV098Route(box, "2D generation", new[] {"auto","sdxl","flux","sd21"}, _v098Routes.ImageGenerate, v => _v098Routes.ImageGenerate=v);
        AddV098Route(box, "2D edit", new[] {"auto","sdxl","flux","sd21"}, _v098Routes.ImageEdit, v => _v098Routes.ImageEdit=v);
        AddV098Route(box, "2D selected detail", new[] {"auto","sdxl","flux","sd21"}, _v098Routes.ImageDetail, v => _v098Routes.ImageDetail=v);
        AddV098Route(box, "Fast / rough 3D", new[] {"auto","triposr","hunyuan"}, _v098Routes.Fast3D, v => _v098Routes.Fast3D=v);
        AddV098Route(box, "Quality whole 3D", new[] {"auto","hunyuan","triposr"}, _v098Routes.Quality3D, v => _v098Routes.Quality3D=v);
        AddV098Route(box, "Selected-detail 3D", new[] {"auto","hunyuan","triposr"}, _v098Routes.Detail3D, v => _v098Routes.Detail3D=v);
        AddV098Route(box, "Structured parts 3D", new[] {"auto","partcrafter"}, _v098Routes.Structured3D, v => _v098Routes.Structured3D=v);

        box.AddChild(new HSeparator()); box.AddChild(new Label { Text = "MODEL COMPONENTS", ThemeTypeVariation = "HeaderSmall" });
        _v098Components = new VBoxContainer(); box.AddChild(_v098Components);
        var modelRow = new HBoxContainer();
        AddV098Button(modelRow, "Refresh", async () => await RefreshV098Models());
        AddV098Button(modelRow, "Release VRAM", async () => { await _ai.ReleaseModelsAsync(); SetStatus("All loaded AI models released from memory."); });
        box.AddChild(modelRow);
        var recommended = new Button { Text = "Install Recommended 8GB Stack (SDXL + TripoSR + Hunyuan + CLIPSeg)" };
        recommended.Pressed += async () => await InstallV098Recommended(); box.AddChild(recommended);
        box.AddChild(new Label { Text = "FLUX.2 Klein is optional/heavy on 8GB cards. PartCrafter is optional and intended for structured multi-part generation. Installing a model never forces its use.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        box.AddChild(new HSeparator()); box.AddChild(new Label { Text = "GENERATION SHORTCUTS", ThemeTypeVariation = "HeaderSmall" });
        var genRow = new HBoxContainer(); AddV098Button(genRow,"Fast 3D from Approved Image",async()=>await V098GenerateFast3D()); AddV098Button(genRow,"Quality 3D",async()=>await V098GenerateQuality3D()); box.AddChild(genRow);
        _v098PartCount = new SpinBox { MinValue=1,MaxValue=16,Step=1,Value=4,Suffix=" parts",SizeFlagsHorizontal=Control.SizeFlags.ExpandFill }; box.AddChild(_v098PartCount);
        AddV098Button(box,"Generate Structured Parts (PartCrafter)",async()=>await V098GenerateParts());

        box.AddChild(new HSeparator()); box.AddChild(new Label { Text = "SELECTED DETAIL REFINEMENT", ThemeTypeVariation = "HeaderSmall" });
        box.AddChild(new Label { Text = "Create a Smart Selection first (/s helmet, /s face, etc.), enter the desired detail in the normal AI prompt, then run a 2D-only or full 3D detail pass. 3D refinement creates a non-destructive aligned preview before acceptance.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var detailRow = new HBoxContainer(); AddV098Button(detailRow,"Detail 2D",async()=>await V098Detail2DAsync()); AddV098Button(detailRow,"Detail 3D Preview",async()=>await V098Detail3DAsync()); box.AddChild(detailRow);
        var applyRow = new HBoxContainer(); AddV098Button(applyRow,"Apply Detail",async()=>await V098ApplyDetailAsync()); AddV098Button(applyRow,"Discard Preview",V098DiscardDetail); box.AddChild(applyRow);
        _v098DetailStatus = new Label { Text="No active detail preview.", AutowrapMode=TextServer.AutowrapMode.WordSmart }; box.AddChild(_v098DetailStatus);
        _ = RefreshV098Models();
    }

    void AddV098Button(Container parent, string text, Action action) { var b=new Button{Text=text,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill}; b.Pressed+=action; parent.AddChild(b); }
    void AddV098Route(Container parent,string label,string[] options,string current,Action<string> set)
    {
        parent.AddChild(new Label{Text=label}); var o=new OptionButton{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
        foreach(string s in options)o.AddItem(s); int index=Array.FindIndex(options,x=>x.Equals(current,StringComparison.OrdinalIgnoreCase)); o.Select(Math.Max(0,index));
        o.ItemSelected += i => { string value=options[(int)i]; set(value); ApplyV098Routes(); SaveV098Routes(); _=RefreshV098RoutingOnly(); }; parent.AddChild(o);
    }

    void LoadV098Routes()
    {
        try { if(File.Exists(V098RoutePath())) _v098Routes=JsonSerializer.Deserialize<V098RouteState>(File.ReadAllText(V098RoutePath()))??new V098RouteState(); } catch { _v098Routes=new V098RouteState(); }
    }
    void SaveV098Routes(){ try{File.WriteAllText(V098RoutePath(),JsonSerializer.Serialize(_v098Routes,new JsonSerializerOptions{WriteIndented=true}));}catch{} }
    void ApplyV098Routes()
    {
        _ai.ImageGenerateProvider=_v098Routes.ImageGenerate; _ai.ImageEditProvider=_v098Routes.ImageEdit; _ai.ImageDetailProvider=_v098Routes.ImageDetail;
        _ai.Fast3DProvider=_v098Routes.Fast3D; _ai.Quality3DProvider=_v098Routes.Quality3D; _ai.Detail3DProvider=_v098Routes.Detail3D; _ai.Structured3DProvider=_v098Routes.Structured3D;
    }

    async Task RefreshV098Models()
    {
        if (_v098Components == null) return;
        try
        {
            var status=await _ai.GetComponentsAsync();
            _v098Hardware!.Text=$"Hardware: {status.Hardware.Gpu ?? "CPU"} · VRAM {status.Hardware.VramMb:N0} MB · recommended quality {status.Hardware.RecommendedProfile}";
            foreach(var c in _v098Components.GetChildren()) c.QueueFree();
            foreach(var c in status.Components)
            {
                var row=new HBoxContainer(); var label=new Label{Text=$"{c.Name} · {(c.Installed?"installed":"not installed")} · ~{c.EstimatedGb:0.#} GB",SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,TooltipText=c.Description}; row.AddChild(label);
                var b=new Button{Text=c.Installed?"Remove":"Install"}; string id=c.Id; bool installed=c.Installed;
                b.Pressed += async () => { try{SetStatus($"{(installed?"Removing":"Installing")} {c.Name}…"); if(installed)await _ai.UninstallComponentAsync(id); else await _ai.InstallComponentAsync(id); await RefreshV098Models();SetStatus($"{c.Name} {(installed?"removed":"installed")}.");}catch(Exception ex){SetStatus($"{c.Name}: {ex.Message}");} }; row.AddChild(b); _v098Components.AddChild(row);
            }
            await RefreshV098RoutingOnly();
        }
        catch(Exception ex){ if(_v098Hardware!=null)_v098Hardware.Text="AI backend unavailable: "+ex.Message; }
    }

    async Task RefreshV098RoutingOnly()
    {
        if(_v098Routing==null)return; try{string json=await _ai.GetRoutingAsync();using var d=JsonDocument.Parse(json);var r=d.RootElement;
            string img=r.GetProperty("image").GetProperty("generate").TryGetProperty("provider",out var ip)?ip.GetString()??"?":"?";
            string edit=r.GetProperty("image").GetProperty("detail").TryGetProperty("provider",out var ep)?ep.GetString()??"?":"?";
            string fast=r.GetProperty("three_d").GetProperty("fast").TryGetProperty("provider",out var fp)?fp.GetString()??"?":"?";
            string quality=r.GetProperty("three_d").GetProperty("quality").TryGetProperty("provider",out var qp)?qp.GetString()??"?":"?";
            _v098Routing.Text=$"Auto routing now: 2D {img} · detail 2D {edit} · fast 3D {fast} · quality/detail 3D {quality}";
        }catch(Exception ex){_v098Routing.Text="Routing status unavailable: "+ex.Message;}}

    async Task InstallV098Recommended()
    {
        foreach(string id in new[]{"sdxl-base","triposr","hunyuan21-shape","clipseg-smart-select"})
        {
            try { var s=await _ai.GetComponentsAsync(); if(s.Components.Any(c=>c.Id==id&&c.Installed))continue; SetStatus($"Installing recommended component {id}…"); await _ai.InstallComponentAsync(id); }
            catch(Exception ex){SetStatus($"Recommended stack stopped at {id}: {ex.Message}"); await RefreshV098Models(); return;}
        }
        await RefreshV098Models(); SetStatus("Recommended v0.9.8 AI stack installed.");
    }

    string V098ApprovedImage()
    {
        string p=!string.IsNullOrEmpty(_lastEditedImage)?_lastEditedImage:_lastCapture;
        if(string.IsNullOrEmpty(p)||!File.Exists(p)){CaptureView();p=_lastCapture;} return p;
    }
    async Task V098GenerateFast3D()
    {
        string image=V098ApprovedImage(); if(string.IsNullOrEmpty(image)){SetStatus("No source image available.");return;} string outp=Path.Combine(V098WorkDir(),$"rough_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
        await RunAi(async()=>{string p=await _ai.Generate3DRoutedAsync(image,_prompt?.Text.Trim()??"",outp,"fast",_v098Routes.Fast3D);AddMeshObject(MeshIO.LoadStl(p),"AI Rough — v0.9.8");SetStatus("Fast 3D reconstruction added as a new object.");});
    }
    async Task V098GenerateQuality3D()
    {
        string image=V098ApprovedImage(); if(string.IsNullOrEmpty(image)){SetStatus("No source image available.");return;} string outp=Path.Combine(V098WorkDir(),$"quality_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
        await RunAi(async()=>{string p=await _ai.Generate3DRoutedAsync(image,_prompt?.Text.Trim()??"",outp,"quality",_v098Routes.Quality3D);AddMeshObject(MeshIO.LoadStl(p),"AI Quality — v0.9.8");SetStatus("Quality 3D reconstruction added as a new object.");});
    }
    async Task V098GenerateParts()
    {
        string image=V098ApprovedImage(); if(string.IsNullOrEmpty(image)){SetStatus("No source image available.");return;} int count=(int)(_v098PartCount?.Value??4); string dir=Path.Combine(V098WorkDir(),$"parts_{DateTime.Now:yyyyMMdd_HHmmss_fff}");
        await RunAi(async()=>{string json=await _ai.GeneratePartsAsync(image,dir,count,"miniscuplter",_v098Routes.Structured3D);using var d=JsonDocument.Parse(json);var parts=d.RootElement.GetProperty("parts");int i=0;foreach(var e in parts.EnumerateArray()){string? p=e.GetString();if(string.IsNullOrWhiteSpace(p)||!File.Exists(p))continue;AddMeshObject(MeshIO.LoadStl(p),$"AI Structured Part {++i}");}SetStatus($"Structured generation imported {i} separate part object(s).");});
    }

    bool V098SelectionBounds(out Vector3 lo,out Vector3 hi)
    {
        lo=hi=Vector3.Zero;V096ValidateSelection();if(_v096SelectionObject?.Mesh is not ArrayMesh mesh||_v096Selection==null||mesh.GetSurfaceCount()==0)return false;
        var mdt=new MeshDataTool();if(mdt.CreateFromSurface(mesh,0)!=Error.Ok||mdt.GetVertexCount()!=_v096Selection.Length)return false;bool any=false;
        for(int i=0;i<_v096Selection.Length;i++){if(_v096Selection[i]<.20f)continue;Vector3 p=_v096SelectionObject.GlobalTransform*mdt.GetVertex(i);if(!any){lo=hi=p;any=true;}else{lo=lo.Min(p);hi=hi.Max(p);}}
        if(!any)return false;Vector3 span=hi-lo;float pad=Math.Max(.15f,Math.Max(span.X,Math.Max(span.Y,span.Z))*.08f);lo-=Vector3.One*pad;hi+=Vector3.One*pad;return true;
    }

    void V098CaptureCleanSelection(out string capture,out string mask)
    {
        mask=BuildV096ViewportMask(); bool overlayVisible=_v096SelectionOverlay?.Visible??false;if(_v096SelectionOverlay!=null)_v096SelectionOverlay.Visible=false;
        try{CaptureView();capture=_lastCapture;}finally{if(_v096SelectionOverlay!=null)_v096SelectionOverlay.Visible=overlayVisible;}
        if(string.IsNullOrEmpty(capture)||!File.Exists(capture))throw new InvalidOperationException("Viewport capture failed.");
    }

    async Task V098Detail2DAsync(string? commandPrompt=null)
    {
        string prompt=(commandPrompt??_prompt?.Text??"").Trim();if(prompt.Length==0){SetStatus("Enter the desired detail first.");return;}V096ValidateSelection();if(_v096Selection==null){SetStatus("Create a Smart Selection first.");return;}
        V098CaptureCleanSelection(out string capture,out string mask);string output=Path.Combine(V098WorkDir(),$"detail2d_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        await RunAi(async()=>{_lastEditedImage=await _ai.Detail2DAsync(capture,mask,prompt,output);ShowAiPreview(_lastEditedImage);SetStatus("Selected-region high-resolution 2D detail pass ready.");});
    }

    async Task V098Detail3DAsync(string? commandPrompt=null)
    {
        string prompt=(commandPrompt??_prompt?.Text??"").Trim();if(prompt.Length==0){SetStatus("Enter the desired detail first.");return;}V096ValidateSelection();
        if(_v096SelectionObject?.Mesh is not ArrayMesh||_v096Selection==null){SetStatus("Create a Smart Selection on a mesh first.");return;}if(!V098SelectionBounds(out Vector3 lo,out Vector3 hi)){SetStatus("Smart Selection has no usable 3D bounds.");return;}
        V098DiscardDetail(false);MeshInstance3D source=_v096SelectionObject;Select(source);string sourcePath=ExportSelectedV09Input(source,"detail_source");V098CaptureCleanSelection(out string capture,out string mask);
        string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"),patch=Path.Combine(V098WorkDir(),$"detail_patch_{stamp}.stl"),image=Path.Combine(V098WorkDir(),$"detail_image_{stamp}.png"),crop=Path.Combine(V098WorkDir(),$"detail_crop_{stamp}.png");
        await RunAi(async()=>{string json=await _ai.Detail3DAsync(sourcePath,capture,mask,prompt,new[]{lo.X,lo.Y,lo.Z},new[]{hi.X,hi.Y,hi.Z},patch,image,crop);using var d=JsonDocument.Parse(json);string p=d.RootElement.GetProperty("patch_path").GetString()??throw new InvalidDataException("Detail backend returned no patch path");
            if(!GodotObject.IsInstanceValid(source)||!_objects.Contains(source)){SetStatus("Detail result discarded because source no longer exists.");return;}AddMeshObject(MeshIO.LoadStl(p),"AI Detail Preview — v0.9.8");_v098DetailPreview=_selected;_v098DetailSource=source;_v098DetailPatchPath=p;if(_v098DetailStatus!=null)_v098DetailStatus.Text=$"Preview ready · 2D {d.RootElement.GetProperty("image_provider").GetString()} · 3D {d.RootElement.GetProperty("three_d_provider").GetString()}";SetStatus("3D detail preview aligned to the Smart Selection. Apply or discard it.");});
    }

    async Task V098ApplyDetailAsync()
    {
        if(_v098DetailSource?.Mesh is not ArrayMesh sourceMesh||!GodotObject.IsInstanceValid(_v098DetailSource)||_v098DetailPreview==null||string.IsNullOrEmpty(_v098DetailPatchPath)||!File.Exists(_v098DetailPatchPath)){SetStatus("No valid detail preview to apply.");return;}
        MeshInstance3D source=_v098DetailSource;Select(source);string input=ExportSelectedV09Input(source,"detail_apply_source"),output=Path.Combine(V098WorkDir(),$"detail_applied_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");double pitch=_v09RepairVoxel?.Value??(_v097ActivePreset?.RepairVoxelMm??.20);
        await RunAi(async()=>{string path=await _ai.ApplyDetailAsync(input,_v098DetailPatchPath,output,pitch);if(!GodotObject.IsInstanceValid(source)||!_objects.Contains(source)){SetStatus("Applied detail result discarded because source no longer exists.");return;}var replacement=MeshIO.LoadStl(path);PushUndo(sourceMesh);source.Mesh=replacement;source.Transform=Transform3D.Identity;V095TopologyChanged(source);V098DiscardDetail(false);ClearV096Selection(false);Select(source);FrameSelected();SetStatus("AI detail accepted and watertight-unioned into the source model. Undo remains available.");});
    }

    void V098DiscardDetail(bool status=true)
    {
        if(_v098DetailPreview!=null&&GodotObject.IsInstanceValid(_v098DetailPreview)){_objects.Remove(_v098DetailPreview);_v098DetailPreview.QueueFree();}
        _v098DetailPreview=null;_v098DetailPatchPath="";var source=_v098DetailSource;_v098DetailSource=null;RebuildSceneList();if(source!=null&&GodotObject.IsInstanceValid(source)&&_objects.Contains(source))Select(source);if(_v098DetailStatus!=null)_v098DetailStatus.Text="No active detail preview.";if(status)SetStatus("AI detail preview discarded; source model unchanged.");
    }
}
