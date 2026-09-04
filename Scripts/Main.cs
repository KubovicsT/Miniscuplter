using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main : Node
{
    readonly AIClient _ai = new();
    readonly List<MeshInstance3D> _objects = new();
    readonly Stack<ArrayMesh> _undo = new();
    readonly Stack<ArrayMesh> _redo = new();
    MeshInstance3D? _selected;
    Node3D? _world;
    Camera3D? _camera;
    Label? _status;
    VBoxContainer? _sceneList;
    VBoxContainer? _referenceList;
    TextEdit? _prompt;
    OptionButton? _brushSelect;
    HSlider? _radius;
    HSlider? _strength;
    CheckButton? _internetToggle;
    bool _orbiting, _panning, _sculpting;
    Vector2 _lastMouse;
    float _yaw = -0.65f, _pitch = -0.25f, _distance = 120f;
    Vector3 _focus = new(0, 25, 0);
    string _lastCapture = "";
    string _lastEditedImage = "";

    public override void _Ready()
    {
        BuildUi();
        BuildWorld();
        AddStarterMesh();
        UpdateCamera();
        SetStatus("Ready — Miniscuplter v1.0");
    }

    void BuildUi()
    {
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var toolbar = new HBoxContainer { CustomMinimumSize = new Vector2(0, 46) };
        root.AddChild(toolbar);
        AddButton(toolbar, "New", NewScene);
        AddButton(toolbar, "Import STL", ImportStlDialog);
        AddButton(toolbar, "Export STL", ExportStlDialog);
        AddButton(toolbar, "Undo", Undo);
        AddButton(toolbar, "Redo", Redo);
        AddButton(toolbar, "Duplicate", DuplicateSelected);
        AddButton(toolbar, "Delete", DeleteSelected);
        AddButton(toolbar, "Capture View", CaptureView);
        AddButton(toolbar, "Frame", FrameSelected);

        var body = new HSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SplitOffset = 235 };
        root.AddChild(body);

        var left = new VBoxContainer { CustomMinimumSize = new Vector2(220, 0) };
        body.AddChild(left);
        left.AddChild(Heading("SCULPT"));
        _brushSelect = new OptionButton();
        foreach (var n in Enum.GetNames<SculptBrush>()) _brushSelect.AddItem(n);
        left.AddChild(_brushSelect);
        left.AddChild(new Label { Text = "Brush radius (mm)" });
        _radius = new HSlider { MinValue = 0.5, MaxValue = 25, Value = 6, Step = 0.25 }; left.AddChild(_radius);
        left.AddChild(new Label { Text = "Strength" });
        _strength = new HSlider { MinValue = 0.05, MaxValue = 4, Value = 0.7, Step = 0.05 }; left.AddChild(_strength);
        left.AddChild(new Label { Text = "LMB sculpt • RMB orbit • MMB pan • wheel zoom", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        left.AddChild(new HSeparator());
        left.AddChild(Heading("SCENE"));
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _sceneList = new VBoxContainer(); scroll.AddChild(_sceneList); left.AddChild(scroll);

        var rightSplit = new HSplitContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SplitOffset = 890 };
        body.AddChild(rightSplit);
        var viewportHost = new SubViewportContainer { Name = "ViewportHost", Stretch = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.All };
        viewportHost.GuiInput += OnViewportInput;
        var sub = new SubViewport { Name = "Viewport", HandleInputLocally = false, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, TransparentBg = false };
        viewportHost.AddChild(sub); rightSplit.AddChild(viewportHost);

        var tabs = new TabContainer { CustomMinimumSize = new Vector2(330, 0) }; rightSplit.AddChild(tabs);
        tabs.AddChild(BuildAiTab());
        tabs.AddChild(BuildTransformTab());
        tabs.AddChild(BuildPrintTab());

        _status = new Label { Text = "Ready", CustomMinimumSize = new Vector2(0, 26) }; root.AddChild(_status);
    }

    Control BuildAiTab()
    {
        var panel = new VBoxContainer { Name = "AI" };
        panel.AddChild(Heading("AI CREATE / MODIFY"));
        panel.AddChild(new Label { Text = "Prompt" });
        _prompt = new TextEdit { CustomMinimumSize = new Vector2(0, 125), PlaceholderText = "Describe the miniature, change, detail, or reference you want..." };
        panel.AddChild(_prompt);
        AddButton(panel, "Generate Concept", async () => await GenerateConcept());
        AddButton(panel, "Capture → 2D AI Edit", async () => await AiEditCapture());
        AddButton(panel, "Approved 2D → Generate 3D Part", async () => await Generate3DPart());
        panel.AddChild(new HSeparator());
        panel.AddChild(Heading("INTERNET REFERENCES"));
        _internetToggle = new CheckButton { Text = "Allow internet reference search", ButtonPressed = true };
        _internetToggle.Toggled += v => _ai.InternetReferencesEnabled = v; panel.AddChild(_internetToggle);
        AddButton(panel, "Search references from prompt", async () => await SearchReferences());
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _referenceList = new VBoxContainer(); scroll.AddChild(_referenceList); panel.AddChild(scroll);
        panel.AddChild(new Label { Text = "Generation runs through the bundled/local AI service. Internet references are a separate, explicit network feature.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        return panel;
    }

    Control BuildTransformTab()
    {
        var p = new VBoxContainer { Name = "Transform" };
        p.AddChild(Heading("OBJECT / KITBASH"));
        AddButton(p, "Move +X 1 mm", () => Nudge(new Vector3(1,0,0)));
        AddButton(p, "Move -X 1 mm", () => Nudge(new Vector3(-1,0,0)));
        AddButton(p, "Move +Y 1 mm", () => Nudge(new Vector3(0,1,0)));
        AddButton(p, "Move -Y 1 mm", () => Nudge(new Vector3(0,-1,0)));
        AddButton(p, "Rotate Y +5°", () => RotateSelected(Vector3.Up, 5));
        AddButton(p, "Rotate Y -5°", () => RotateSelected(Vector3.Up, -5));
        AddButton(p, "Scale +5%", () => ScaleSelected(1.05f));
        AddButton(p, "Scale -5%", () => ScaleSelected(0.95f));
        p.AddChild(new HSeparator());
        p.AddChild(Heading("POSE"));
        p.AddChild(new Label { Text = "Rigged models can be posed through the rig tools. STL and kitbash parts remain separate until you deliberately finalize or remesh them.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        AddButton(p, "Bake selected transforms", BakeSelectedTransform);
        return p;
    }

    Control BuildPrintTab()
    {
        var p = new VBoxContainer { Name = "Model" };
        p.AddChild(Heading("MODEL / FINALIZE"));
        AddButton(p, "Basic mesh statistics", AnalyzeMesh);
        AddButton(p, "Place selected on Y=0", CenterOnBuildPlane);
        AddButton(p, "Analyze structural integrity", async () => await AnalyzeSelectedV09Async());
        AddButton(p, "Repair selected model", async () => await RepairSelectedV09Async());
        AddButton(p, "Finalize visible scene", async () => await BakeSceneV09Async());
        p.AddChild(new Label { Text = "Scene units are millimeters. Finalization and repair are optional: a valid mesh can be exported directly without voxel reconstruction, preserving its original detail.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        return p;
    }

    void BuildWorld()
    {
        var sub = GetNode<SubViewport>("VBoxContainer/HSplitContainer/HSplitContainer/ViewportHost/Viewport");
        _world = new Node3D { Name = "World" }; sub.AddChild(_world);
        _camera = new Camera3D { Current = true, Near = 0.05f, Far = 5000f, Fov = 45 }; _world.AddChild(_camera);
        var light = new DirectionalLight3D { RotationDegrees = new Vector3(-55,-30,0), ShadowEnabled = true, LightEnergy = 1.3f }; _world.AddChild(light);
        var fill = new OmniLight3D { Position = new Vector3(-60,70,70), OmniRange = 250, LightEnergy = 2.0f }; _world.AddChild(fill);
        var env = new WorldEnvironment(); var e = new Godot.Environment(); e.BackgroundMode = Godot.Environment.BGMode.Color; e.BackgroundColor = new Color(0.055f,0.06f,0.07f); e.AmbientLightSource = Godot.Environment.AmbientSource.Color; e.AmbientLightColor = new Color(0.35f,0.36f,0.4f); e.AmbientLightEnergy = 0.8f; env.Environment = e; _world.AddChild(env);
        var grid = new GridMap(); _world.AddChild(grid);
        AddGroundGuide();
    }

    void AddGroundGuide()
    {
        if (_world == null) return;
        var plane = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(200,200), SubdivideWidth = 20, SubdivideDepth = 20 } };
        var mat = new StandardMaterial3D { AlbedoColor = new Color(0.11f,0.12f,0.135f), Roughness = 1f }; plane.MaterialOverride = mat; _world.AddChild(plane);
    }

    void AddStarterMesh()
    {
        var sphere = new SphereMesh { Radius = 15, Height = 30, RadialSegments = 64, Rings = 32 };
        var arrays = sphere.SurfaceGetArrays(0); var arr = new ArrayMesh(); arr.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        AddMeshObject(arr, "Starter sphere", new Vector3(0,15,0));
    }

    void AddMeshObject(ArrayMesh mesh, string name, Vector3 position = default)
    {
        if (_world == null) return;
        var obj = new MeshInstance3D { Mesh = mesh, Name = name, Position = position };
        obj.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.62f,0.64f,0.68f), Roughness = 0.82f };
        _world.AddChild(obj); _objects.Add(obj); Select(obj); RebuildSceneList();
    }

    void Select(MeshInstance3D obj)
    {
        _selected = obj;
        foreach (var o in _objects)
        {
            if (o.MaterialOverride is StandardMaterial3D m) m.AlbedoColor = o == obj ? new Color(0.78f,0.72f,0.52f) : new Color(0.62f,0.64f,0.68f);
        }
    }

    void RebuildSceneList()
    {
        if (_sceneList == null) return;
        foreach (var c in _sceneList.GetChildren()) c.QueueFree();
        foreach (var obj in _objects)
        {
            var b = new Button { Text = obj.Name, Alignment = HorizontalAlignment.Left };
            b.Pressed += () => { Select(obj); RebuildSceneList(); }; _sceneList.AddChild(b);
        }
    }

    void OnViewportInput(InputEvent ev)
    {
        if (_camera == null) return;
        if (ev is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Right) { _orbiting = mb.Pressed; _lastMouse = mb.Position; }
            else if (mb.ButtonIndex == MouseButton.Middle) { _panning = mb.Pressed; _lastMouse = mb.Position; }
            else if (mb.ButtonIndex == MouseButton.Left) { _sculpting = mb.Pressed; _lastMouse = mb.Position; if (mb.Pressed) SculptAt(mb.Position, Vector2.Zero); }
            else if (mb.Pressed && mb.ButtonIndex == MouseButton.WheelUp) { _distance = Math.Max(5, _distance * 0.9f); UpdateCamera(); }
            else if (mb.Pressed && mb.ButtonIndex == MouseButton.WheelDown) { _distance = Math.Min(2000, _distance * 1.1f); UpdateCamera(); }
        }
        else if (ev is InputEventMouseMotion mm)
        {
            Vector2 delta = mm.Position - _lastMouse; _lastMouse = mm.Position;
            if (_orbiting) { _yaw -= delta.X * 0.008f; _pitch = Math.Clamp(_pitch - delta.Y * 0.008f, -1.5f, 1.5f); UpdateCamera(); }
            else if (_panning) { var right = _camera.GlobalTransform.Basis.X; var up = _camera.GlobalTransform.Basis.Y; _focus += (-right * delta.X + up * delta.Y) * (_distance * 0.0015f); UpdateCamera(); }
            else if (_sculpting) SculptAt(mm.Position, delta);
        }
    }

    void SculptAt(Vector2 screenPos, Vector2 drag)
    {
        if (_selected?.Mesh is not ArrayMesh mesh || _camera == null) return;
        var ro = _camera.ProjectRayOrigin(screenPos); var rd = _camera.ProjectRayNormal(screenPos);
        if (!RayMesh(ro, rd, _selected, out var worldHit)) return;
        var inv = _selected.GlobalTransform.AffineInverse(); var localHit = inv * worldHit;
        var localDrag = (inv.Basis * (_camera.GlobalTransform.Basis.X * drag.X - _camera.GlobalTransform.Basis.Y * drag.Y)) * 0.02f;
        if (!_sculpting || drag == Vector2.Zero) PushUndo(mesh);
        var brush = (SculptBrush)(_brushSelect?.Selected ?? 0);
        var result = SculptEngine.Apply(mesh, localHit, localDrag, (float)(_radius?.Value ?? 6), (float)(_strength?.Value ?? .7), brush);
        _selected.Mesh = result;
    }

    static bool RayMesh(Vector3 ro, Vector3 rd, MeshInstance3D obj, out Vector3 hit)
    {
        hit = default; if (obj.Mesh == null) return false; float best = float.PositiveInfinity; bool found = false; var gt = obj.GlobalTransform;
        for (int s = 0; s < obj.Mesh.GetSurfaceCount(); s++)
        {
            var a = obj.Mesh.SurfaceGetArrays(s); var verts = a[(int)Mesh.ArrayType.Vertex].AsVector3Array(); var idx = a[(int)Mesh.ArrayType.Index].AsInt32Array();
            int count = idx.Length > 0 ? idx.Length : verts.Length;
            for (int i = 0; i + 2 < count; i += 3)
            {
                var v0 = gt * verts[idx.Length > 0 ? idx[i] : i]; var v1 = gt * verts[idx.Length > 0 ? idx[i+1] : i+1]; var v2 = gt * verts[idx.Length > 0 ? idx[i+2] : i+2];
                var p = Geometry3D.RayIntersectsTriangle(ro, rd, v0, v1, v2);
                if (p.VariantType == Variant.Type.Vector3)
                {
                    var point = p.AsVector3();
                    float d = ro.DistanceSquaredTo(point);
                    if (d < best) { best = d; hit = point; found = true; }
                }
            }
        }
        return found;
    }

    void PushUndo(ArrayMesh mesh) { _undo.Push(CloneMesh(mesh)); _redo.Clear(); while (_undo.Count > 30) { var list = _undo.Reverse().Take(30).Reverse().ToArray(); _undo.Clear(); foreach (var m in list) _undo.Push(m); } }
    static ArrayMesh CloneMesh(ArrayMesh src) { var dst = new ArrayMesh(); for (int i=0;i<src.GetSurfaceCount();i++) dst.AddSurfaceFromArrays(src.SurfaceGetPrimitiveType(i), src.SurfaceGetArrays(i)); return dst; }
    void Undo() { if (_selected?.Mesh is not ArrayMesh cur || _undo.Count==0) return; _redo.Push(CloneMesh(cur)); _selected.Mesh = _undo.Pop(); SetStatus("Undo"); }
    void Redo() { if (_selected?.Mesh is not ArrayMesh cur || _redo.Count==0) return; _undo.Push(CloneMesh(cur)); _selected.Mesh = _redo.Pop(); SetStatus("Redo"); }

    void ImportStlDialog()
    {
        var d = new FileDialog { FileMode = FileDialog.FileModeEnum.OpenFile, Access = FileDialog.AccessEnum.Filesystem, Filters = new[]{"*.stl ; STL meshes"}, UseNativeDialog = true };
        AddChild(d); d.FileSelected += p => { try { AddMeshObject(MeshIO.LoadStl(p), Path.GetFileNameWithoutExtension(p)); SetStatus("Imported " + p); } catch(Exception ex) { SetStatus("Import failed: " + ex.Message); } d.QueueFree(); }; d.PopupCenteredRatio(.75f);
    }

    void ExportStlDialog()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a mesh first."); return; }
        var d = new FileDialog { FileMode = FileDialog.FileModeEnum.SaveFile, Access = FileDialog.AccessEnum.Filesystem, Filters = new[]{"*.stl ; STL meshes"}, CurrentFile = _selected.Name + ".stl", UseNativeDialog = true };
        AddChild(d); d.FileSelected += p => { try { MeshIO.SaveBinaryStl(BakeToWorldMesh(_selected), p); SetStatus("Exported " + p); } catch(Exception ex){ SetStatus("Export failed: " + ex.Message); } d.QueueFree(); }; d.PopupCenteredRatio(.75f);
    }

    static ArrayMesh BakeToWorldMesh(MeshInstance3D obj)
    {
        var outMesh = new ArrayMesh(); if (obj.Mesh == null) return outMesh;
        for (int s=0;s<obj.Mesh.GetSurfaceCount();s++)
        {
            var arrays=obj.Mesh.SurfaceGetArrays(s); var verts=arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array(); for(int i=0;i<verts.Length;i++) verts[i]=obj.GlobalTransform*verts[i]; arrays[(int)Mesh.ArrayType.Vertex]=verts; outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
        }
        return outMesh;
    }

    void NewScene() { foreach(var o in _objects) o.QueueFree(); _objects.Clear(); _selected=null; _undo.Clear(); _redo.Clear(); AddStarterMesh(); SetStatus("New scene"); }
    void DuplicateSelected()
    {
        if (_selected?.Mesh is not ArrayMesh m) return;
        var source = _selected;
        string role = V06RoleFor(source.Name.ToString());
        AddMeshObject(CloneMesh(m), source.Name+" copy", source.Position + new Vector3(5,0,0));
        if (_selected != null)
        {
            _selected.Rotation = source.Rotation;
            _selected.Scale = source.Scale;
            ImportV06Role(_selected.Name.ToString(), role);
        }
    }
    void DeleteSelected() { if (_selected==null) return; var x=_selected; _objects.Remove(x); x.QueueFree(); _selected=_objects.LastOrDefault(); if(_selected!=null) Select(_selected); RebuildSceneList(); }
    void Nudge(Vector3 v) { if(_selected==null)return; _selected.Position+=v; }
    void RotateSelected(Vector3 axis,float deg){ if(_selected==null)return; _selected.Rotate(axis,Mathf.DegToRad(deg)); }
    void ScaleSelected(float f){ if(_selected==null)return; _selected.Scale*=f; }
    void BakeSelectedTransform(){ if(_selected?.Mesh==null)return; _selected.Mesh=BakeToWorldMesh(_selected); _selected.Transform=Transform3D.Identity; V095TopologyChanged(_selected); SetStatus("Transforms baked into mesh."); }

    void CenterOnBuildPlane()
    {
        if(_selected?.Mesh==null)return; var aabb=_selected.GetAabb(); var bottom=(_selected.Transform*aabb.Position).Y; _selected.Position-=new Vector3(0,bottom,0); SetStatus("Placed selected object on Y=0.");
    }

    void AnalyzeMesh()
    {
        if(_selected?.Mesh==null)return; int vertices=0,triangles=0; for(int s=0;s<_selected.Mesh.GetSurfaceCount();s++){var a=_selected.Mesh.SurfaceGetArrays(s);int vc=a[(int)Mesh.ArrayType.Vertex].AsVector3Array().Length;int ic=a[(int)Mesh.ArrayType.Index].AsInt32Array().Length;vertices+=vc;triangles+=(ic>0?ic:vc)/3;}
        var size=_selected.GetAabb().Size*_selected.Scale; SetStatus($"Mesh: {vertices:N0} vertices, {triangles:N0} triangles, approx {size.X:0.0} × {size.Y:0.0} × {size.Z:0.0} mm.");
    }

    void FrameSelected(){ if(_selected==null)return; var a=_selected.GetAabb(); _focus=_selected.GlobalTransform*(a.Position+a.Size/2); _distance=Math.Max(15,a.Size.Length()*2.2f*_selected.Scale.Length()/1.732f); UpdateCamera(); }
    void UpdateCamera(){ if(_camera==null)return; var dir=new Vector3(Mathf.Cos(_pitch)*Mathf.Sin(_yaw),Mathf.Sin(_pitch),Mathf.Cos(_pitch)*Mathf.Cos(_yaw)); _camera.Position=_focus-dir*_distance; _camera.LookAt(_focus,Vector3.Up); }

    void CaptureView()
    {
        var sub=GetNode<SubViewport>("VBoxContainer/HSplitContainer/HSplitContainer/ViewportHost/Viewport"); var img=sub.GetTexture().GetImage(); Directory.CreateDirectory(ProjectSettings.GlobalizePath("user://captures")); _lastCapture=ProjectSettings.GlobalizePath($"user://captures/capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"); img.SavePng(_lastCapture); SetStatus("Captured viewport: "+_lastCapture);
    }

    async Task GenerateConcept()
    {
        string prompt=_prompt?.Text.Trim()??""; if(prompt.Length==0){SetStatus("Enter a prompt first.");return;} string outPath=ProjectSettings.GlobalizePath($"user://concept_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"); await RunAi(async()=>{_lastEditedImage=await _ai.GenerateConceptAsync(prompt,outPath); ShowAiPreview(_lastEditedImage); SetStatus("Concept generated: "+_lastEditedImage);});
    }

    async Task AiEditCapture()
    {
        if(string.IsNullOrEmpty(_lastCapture)) CaptureView(); string prompt=_prompt?.Text.Trim()??""; if(prompt.Length==0){SetStatus("Describe the desired change first.");return;} string outPath=ProjectSettings.GlobalizePath($"user://edit_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"); await RunAi(async()=>{_lastEditedImage=await _ai.EditImageAsync(_lastCapture,null,prompt,outPath); ShowAiPreview(_lastEditedImage); SetStatus("2D edit generated. Review file: "+_lastEditedImage);});
    }

    async Task Generate3DPart()
    {
        string image=string.IsNullOrEmpty(_lastEditedImage)?_lastCapture:_lastEditedImage; if(string.IsNullOrEmpty(image)){SetStatus("Generate or capture an approved 2D image first.");return;} string outPath=ProjectSettings.GlobalizePath($"user://ai_part_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl"); string prompt=_prompt?.Text.Trim()??""; await RunAi(async()=>{var p=await _ai.Generate3DAsync(image,prompt,outPath); AddMeshObject(MeshIO.LoadStl(p),"AI part"); SetStatus("AI 3D part added non-destructively.");});
    }

    async Task SearchReferences()
    {
        string q=_prompt?.Text.Trim()??""; if(q.Length==0){SetStatus("Enter reference terms in the prompt box.");return;} if(_referenceList==null)return; foreach(var c in _referenceList.GetChildren())c.QueueFree(); try{SetStatus("Searching internet references…");var items=await _ai.SearchReferencesAsync(q);foreach(var r in items){var b=new Button{Text=r.Title,TooltipText=r.PageUrl,Alignment=HorizontalAlignment.Left};b.Pressed+=()=>OS.ShellOpen(r.PageUrl);_referenceList.AddChild(b);}SetStatus($"Found {items.Count} Wikimedia Commons references.");}catch(Exception ex){SetStatus("Reference search failed: "+ex.Message);}
    }

    async Task RunAi(Func<Task> action)
    {
        try
        {
            SetStatus("Checking local AI service…");
            if(!await _ai.HealthAsync())
            {
                SetStatus("AI service is not running. Open Miniscuplter Launcher and use Repair AI Runtime, then restart the editor.");
                return;
            }
            if (_v097ActivePreset != null) await PushV097PresetToBackendAsync(_v097ActivePreset);
            SetStatus("AI working…");
            await action();
        }
        catch(Exception ex){SetStatus("AI error: "+ex.Message);}
    }

    static Label Heading(string text)=>new(){Text=text,ThemeTypeVariation="HeaderSmall"};
    static Button AddButton(Node parent,string text,Action action){var b=new Button{Text=text};b.Pressed+=action;parent.AddChild(b);return b;}
    void SetStatus(string s){if(_status!=null)_status.Text=s;GD.Print(s);}
}