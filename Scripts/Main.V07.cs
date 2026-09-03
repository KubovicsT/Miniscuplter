using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Miniscuplter;

public partial class Main
{
    public sealed class V07SocketDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string OwnerObject { get; set; } = "";
        public string Name { get; set; } = "Socket";
        public string Type { get; set; } = "Generic";
        public float[] LocalPosition { get; set; } = new float[3];
        public float[] LocalNormal { get; set; } = new float[] { 0, 1, 0 };
        public int RigJoint { get; set; } = -1;
        public float[] JointOffset { get; set; } = new float[3];
        public float SurfaceOffset { get; set; } = 0f;
        public float RollDeg { get; set; } = 0f;
    }

    public sealed class V07AttachmentDto
    {
        public string PartObjectName { get; set; } = "";
        public string SocketId { get; set; } = "";
        public string LibraryId { get; set; } = "";
        public float[] LocalOffset { get; set; } = new float[3];
        public float[] LocalRotationDeg { get; set; } = new float[3];
        public float UniformScale { get; set; } = 1f;
    }

    sealed class V07PartDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Part";
        public string Category { get; set; } = "Generic";
        public string SocketType { get; set; } = "Generic";
        public string MeshPath { get; set; } = "";
        public string Builtin { get; set; } = "";
        public float DefaultScale { get; set; } = 1f;
        public float[] MountPoint { get; set; } = new float[3];
        public float[] MountNormal { get; set; } = new float[] { 0, 1, 0 };
        public float MountRollDeg { get; set; } = 0f;
    }

    readonly List<V07SocketDto> _v07Sockets = new();
    readonly List<V07AttachmentDto> _v07Attachments = new();
    readonly List<V07PartDefinition> _v07Parts = new();
    VBoxContainer? _v07LibraryList;
    VBoxContainer? _v07SocketList;
    LineEdit? _v07Search;
    OptionButton? _v07Category;
    OptionButton? _v07SocketType;
    Label? _v07SelectionStatus;
    string _v07SelectedPartId = "";
    string _v07SelectedSocketId = "";
    bool _v07SocketPlacementMode;
    MeshInstance3D? _v07SocketOwner;
    Node3D? _v07SocketVisualRoot;
    Node3D? _v07MountVisualRoot;
    SpinBox? _v07SocketRoll;
    SpinBox? _v07AttachOffsetX; SpinBox? _v07AttachOffsetY; SpinBox? _v07AttachOffsetZ;
    SpinBox? _v07AttachRotX; SpinBox? _v07AttachRotY; SpinBox? _v07AttachRotZ;
    SpinBox? _v07AttachScale;
    bool _v07MountPointMode;

    static readonly string[] V07Categories = { "All", "Head", "Body", "Armour", "Weapon", "Shield", "Back", "Base", "Accessory", "Creature", "Generic" };
    static readonly string[] V07SocketTypes = { "Generic", "Head", "LeftHand", "RightHand", "LeftShoulder", "RightShoulder", "Back", "Waist", "Base", "Accessory" };

    public void InstallV07Extras()
    {
        var tabs = FindChild("TabContainer", true, false) as TabContainer;
        if (tabs == null) return;
        var scroll = new ScrollContainer { Name = "Kitbash", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var panel = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(panel); tabs.AddChild(scroll);

        panel.AddChild(new Label { Text = "PARTS LIBRARY & SOCKETS — v0.7", ThemeTypeVariation = "HeaderSmall" });
        panel.AddChild(new Label { Text = "Build a reusable STL parts catalog, place typed sockets on character surfaces, and snap accessories non-destructively. Rig-linked sockets follow the current pose.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        _v07Search = new LineEdit { PlaceholderText = "Search parts..." };
        _v07Search.TextChanged += _ => RebuildV07LibraryList(); panel.AddChild(_v07Search);
        var filters = new HBoxContainer();
        _v07Category = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (string c in V07Categories) _v07Category.AddItem(c);
        _v07Category.ItemSelected += _ => RebuildV07LibraryList(); filters.AddChild(_v07Category);
        _v07SocketType = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (string s in V07SocketTypes) _v07SocketType.AddItem(s); filters.AddChild(_v07SocketType); panel.AddChild(filters);

        var importRow = new HBoxContainer();
        AddV07Button(importRow, "Import STL to Library", OpenV07ImportPartDialog);
        AddV07Button(importRow, "Save Selected as Part", SaveV07SelectedAsPart);
        panel.AddChild(importRow);
        var addRow = new HBoxContainer();
        AddV07Button(addRow, "Add Selected Part", AddSelectedV07Part);
        AddV07Button(addRow, "Open Library Folder", () => OS.ShellOpen(V07LibraryRoot()));
        panel.AddChild(addRow);
        _v07SelectionStatus = new Label { Text = "No library part selected.", AutowrapMode = TextServer.AutowrapMode.WordSmart }; panel.AddChild(_v07SelectionStatus);
        var libraryScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 210) };
        _v07LibraryList = new VBoxContainer(); libraryScroll.AddChild(_v07LibraryList); panel.AddChild(libraryScroll);

        panel.AddChild(new HSeparator());
        panel.AddChild(new Label { Text = "SOCKETS", ThemeTypeVariation = "HeaderSmall" });
        var socketRow = new HBoxContainer();
        AddV07Button(socketRow, "Place Socket on Surface", BeginV07SocketPlacement);
        AddV07Button(socketRow, "Auto Starter Sockets", AutoCreateV07Sockets);
        panel.AddChild(socketRow);
        var snapRow = new HBoxContainer();
        AddV07Button(snapRow, "Snap Selected Object", SnapSelectedV07Object);
        AddV07Button(snapRow, "Detach Selected", DetachSelectedV07Object);
        AddV07Button(snapRow, "Refresh Attachments", RefreshV07Attachments);
        panel.AddChild(snapRow);

        var orientRow = new HBoxContainer();
        _v07SocketRoll = new SpinBox { MinValue = -180, MaxValue = 180, Step = 1, Suffix = "°", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v07SocketRoll.ValueChanged += v => { var s = _v07Sockets.FirstOrDefault(x => x.Id == _v07SelectedSocketId); if (s != null) { s.RollDeg = (float)v; RefreshV07Attachments(); RebuildV07SocketVisuals(); } };
        orientRow.AddChild(new Label { Text = "Socket roll" }); orientRow.AddChild(_v07SocketRoll);
        AddV07Button(orientRow, "Set Part Mount Point", BeginV07MountPointPlacement);
        panel.AddChild(orientRow);

        panel.AddChild(new Label { Text = "Attachment fine tune (local X/Y/Z, rotation XYZ, scale)" });
        var offRow = new HBoxContainer();
        _v07AttachOffsetX = V07FineSpin(-100, 100, .1); _v07AttachOffsetY = V07FineSpin(-100, 100, .1); _v07AttachOffsetZ = V07FineSpin(-100, 100, .1);
        offRow.AddChild(_v07AttachOffsetX); offRow.AddChild(_v07AttachOffsetY); offRow.AddChild(_v07AttachOffsetZ); panel.AddChild(offRow);
        var rotRow = new HBoxContainer();
        _v07AttachRotX = V07FineSpin(-180, 180, 1); _v07AttachRotY = V07FineSpin(-180, 180, 1); _v07AttachRotZ = V07FineSpin(-180, 180, 1);
        rotRow.AddChild(_v07AttachRotX); rotRow.AddChild(_v07AttachRotY); rotRow.AddChild(_v07AttachRotZ); panel.AddChild(rotRow);
        var scaleRow = new HBoxContainer();
        _v07AttachScale = V07FineSpin(.05, 20, .01); _v07AttachScale.Value = 1;
        scaleRow.AddChild(_v07AttachScale); AddV07Button(scaleRow, "Apply Fine Tune", ApplyV07AttachmentFineTune); AddV07Button(scaleRow, "Reset Fine Tune", ResetV07AttachmentFineTune); panel.AddChild(scaleRow);

        var socketScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 190) };
        _v07SocketList = new VBoxContainer(); socketScroll.AddChild(_v07SocketList); panel.AddChild(socketScroll);

        if (FindChild("ViewportHost", true, false) is SubViewportContainer host) host.GuiInput += OnV07ViewportInput;
        LoadV07Library(); RebuildV07LibraryList(); RebuildV07SocketList(); RebuildV07SocketVisuals();
    }

    static void AddV07Button(Container parent, string text, Action action)
    {
        var b = new Button { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; b.Pressed += action; parent.AddChild(b);
    }

    static SpinBox V07FineSpin(double min, double max, double step) => new() { MinValue = min, MaxValue = max, Step = step, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

    string V07LibraryRoot()
    {
        string root = ProjectSettings.GlobalizePath("user://parts_library"); Directory.CreateDirectory(root); Directory.CreateDirectory(Path.Combine(root, "meshes")); return root;
    }
    string V07LibraryIndex() => Path.Combine(V07LibraryRoot(), "parts.json");

    void LoadV07Library()
    {
        _v07Parts.Clear();
        try
        {
            if (File.Exists(V07LibraryIndex()))
            {
                var loaded = JsonSerializer.Deserialize<List<V07PartDefinition>>(File.ReadAllText(V07LibraryIndex()), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (loaded != null) _v07Parts.AddRange(loaded.Where(p => !string.IsNullOrWhiteSpace(p.Id)));
            }
        }
        catch (Exception ex) { SetStatus("Parts library index could not be read: " + ex.Message); }
        AddV07Builtin("builtin_base", "Round Base", "Base", "Base", "base");
        AddV07Builtin("builtin_horn", "Simple Horn", "Accessory", "Accessory", "horn");
        AddV07Builtin("builtin_sword", "Blockout Sword", "Weapon", "RightHand", "sword");
        AddV07Builtin("builtin_shield", "Round Shield", "Shield", "LeftHand", "shield");
        AddV07Builtin("builtin_pack", "Blockout Backpack", "Back", "Back", "pack");
    }

    void AddV07Builtin(string id, string name, string category, string socket, string builtin)
    {
        if (_v07Parts.Any(p => p.Id == id)) return;
        _v07Parts.Add(new V07PartDefinition { Id = id, Name = name, Category = category, SocketType = socket, Builtin = builtin });
    }

    void SaveV07Library()
    {
        try
        {
            var persistent = _v07Parts.Where(p => string.IsNullOrEmpty(p.Builtin)).ToList();
            File.WriteAllText(V07LibraryIndex(), JsonSerializer.Serialize(persistent, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { SetStatus("Could not save parts library index: " + ex.Message); }
    }

    string CurrentV07Category() => _v07Category?.GetItemText(_v07Category.Selected) ?? "Generic";
    string CurrentV07SocketType() => _v07SocketType?.GetItemText(_v07SocketType.Selected) ?? "Generic";

    void RebuildV07LibraryList()
    {
        if (_v07LibraryList == null) return; foreach (var c in _v07LibraryList.GetChildren()) c.QueueFree();
        string q = (_v07Search?.Text ?? "").Trim(); string cat = CurrentV07Category();
        foreach (var p in _v07Parts.Where(p => (cat == "All" || p.Category.Equals(cat, StringComparison.OrdinalIgnoreCase)) && (q.Length == 0 || p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || p.Category.Contains(q, StringComparison.OrdinalIgnoreCase))))
        {
            var b = new Button { Text = $"{p.Name}   [{p.Category} → {p.SocketType}]", Alignment = HorizontalAlignment.Left };
            b.Pressed += () => { _v07SelectedPartId = p.Id; if (_v07SelectionStatus != null) _v07SelectionStatus.Text = $"Selected: {p.Name} · compatible socket: {p.SocketType}"; RebuildV07MountVisual(); }; _v07LibraryList.AddChild(b);
        }
    }

    void OpenV07ImportPartDialog()
    {
        var d = new FileDialog { FileMode = FileDialog.FileModeEnum.OpenFile, Access = FileDialog.AccessEnum.Filesystem, Filters = new[] { "*.stl ; STL mesh" }, UseNativeDialog = true };
        AddChild(d); d.FileSelected += p => { try { ImportV07PartFile(p); } finally { d.QueueFree(); } }; d.Canceled += d.QueueFree; d.PopupCenteredRatio(.75f);
    }

    void ImportV07PartFile(string source)
    {
        if (!File.Exists(source)) { SetStatus("Part file not found."); return; }
        string id = Guid.NewGuid().ToString("N"); string dest = Path.Combine(V07LibraryRoot(), "meshes", id + ".stl"); File.Copy(source, dest, true);
        var def = new V07PartDefinition { Id = id, Name = Path.GetFileNameWithoutExtension(source), Category = CurrentV07Category() == "All" ? "Generic" : CurrentV07Category(), SocketType = CurrentV07SocketType(), MeshPath = dest };
        _v07Parts.Add(def); SaveV07Library(); _v07SelectedPartId = id; RebuildV07LibraryList(); SetStatus($"Added {def.Name} to the reusable parts library.");
    }

    void SaveV07SelectedAsPart()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a scene mesh first."); return; }
        string id = Guid.NewGuid().ToString("N"); string dest = Path.Combine(V07LibraryRoot(), "meshes", id + ".stl"); MeshIO.SaveBinaryStl(BakeToWorldMesh(_selected), dest);
        var def = new V07PartDefinition { Id = id, Name = _selected.Name.ToString(), Category = CurrentV07Category() == "All" ? "Generic" : CurrentV07Category(), SocketType = CurrentV07SocketType(), MeshPath = dest };
        _v07Parts.Add(def); SaveV07Library(); _v07SelectedPartId = id; RebuildV07LibraryList(); SetStatus($"Saved {_selected.Name} as a reusable {def.Category} part.");
    }

    void AddSelectedV07Part()
    {
        var def = _v07Parts.FirstOrDefault(p => p.Id == _v07SelectedPartId); if (def == null) { SetStatus("Choose a library part first."); return; }
        ArrayMesh mesh;
        try { mesh = !string.IsNullOrEmpty(def.Builtin) ? BuildV07BuiltinMesh(def.Builtin) : MeshIO.LoadStl(def.MeshPath); }
        catch (Exception ex) { SetStatus("Could not load library part: " + ex.Message); return; }
        AddMeshObject(mesh, $"Part — {def.Name}"); if (_selected != null) { _selected.Scale = Vector3.One * def.DefaultScale; ImportV06Role(_selected.Name.ToString(), "attachment"); }
        SetStatus($"Added {def.Name} as a separate attachment object. Select a socket and snap it, or position it manually.");
    }

    static ArrayMesh PrimitiveV07(PrimitiveMesh mesh)
    {
        var a = new ArrayMesh(); for (int s = 0; s < mesh.GetSurfaceCount(); s++) a.AddSurfaceFromArrays(mesh.SurfaceGetPrimitiveType(s), mesh.SurfaceGetArrays(s)); return a;
    }

    static ArrayMesh BuildV07BuiltinMesh(string kind)
    {
        return kind switch
        {
            "base" => PrimitiveV07(new CylinderMesh { TopRadius = 15, BottomRadius = 15, Height = 2.5f, RadialSegments = 64 }),
            "horn" => PrimitiveV07(new CylinderMesh { TopRadius = .25f, BottomRadius = 2f, Height = 12f, RadialSegments = 24 }),
            "sword" => PrimitiveV07(new BoxMesh { Size = new Vector3(2.2f, 22f, 1f) }),
            "shield" => PrimitiveV07(new CylinderMesh { TopRadius = 8f, BottomRadius = 8f, Height = 1.5f, RadialSegments = 48 }),
            "pack" => PrimitiveV07(new BoxMesh { Size = new Vector3(9f, 11f, 4f) }),
            _ => PrimitiveV07(new SphereMesh { Radius = 3f, Height = 6f, RadialSegments = 24, Rings = 12 })
        };
    }

    void BeginV07MountPointPlacement()
    {
        if (_selected == null) { SetStatus("Select a part object in the scene first."); return; }
        var def = _v07Parts.FirstOrDefault(p => p.Id == _v07SelectedPartId);
        if (def == null) { SetStatus("Select the corresponding library part first."); return; }
        _v07MountPointMode = true;
        SetStatus("Mount-point mode: click the surface point on the selected part that should touch the socket.");
    }

    void BeginV07SocketPlacement()
    {
        if (_selected == null) { SetStatus("Select the character/body object that owns the socket first."); return; }
        _v07SocketOwner = _selected; _v07SocketPlacementMode = true; SetStatus($"Socket placement: click the surface of {_selected.Name}. Type: {CurrentV07SocketType()}.");
    }

    void OnV07ViewportInput(InputEvent ev)
    {
        if (_camera == null) return;
        if (ev is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
        Vector3 ro = _camera.ProjectRayOrigin(mb.Position), rd = _camera.ProjectRayNormal(mb.Position);

        if (_v07MountPointMode)
        {
            if (_selected == null) return;
            var def = _v07Parts.FirstOrDefault(p => p.Id == _v07SelectedPartId); if (def == null) return;
            if (!RayMeshDetailedV055(ro, rd, _selected, out var mhit, out var mnormal)) { SetStatus("No part surface hit."); return; }
            Transform3D minv = _selected.GlobalTransform.AffineInverse();
            Vector3 mlocal = minv * mhit; Vector3 mln = (minv.Basis * mnormal).Normalized();
            def.MountPoint = Vec(mlocal); def.MountNormal = Vec(mln); SaveV07Library(); _v07MountPointMode = false; RebuildV07MountVisual();
            SetStatus($"Saved mount point for {def.Name}. Future snaps align this point to the socket."); GetViewport().SetInputAsHandled(); return;
        }

        if (!_v07SocketPlacementMode || _v07SocketOwner == null) return;
        if (!RayMeshDetailedV055(ro, rd, _v07SocketOwner, out var hit, out var normal)) { SetStatus("No surface hit. Click directly on the selected owner mesh."); return; }
        Transform3D inv = _v07SocketOwner.GlobalTransform.AffineInverse(); Vector3 local = inv * hit; Vector3 localNormal = (inv.Basis * normal).Normalized();
        var socket = new V07SocketDto { OwnerObject = _v07SocketOwner.Name.ToString(), Name = CurrentV07SocketType(), Type = CurrentV07SocketType(), LocalPosition = Vec(local), LocalNormal = Vec(localNormal) };
        LinkV07SocketToNearestRigJoint(socket); _v07Sockets.Add(socket); _v07SelectedSocketId = socket.Id; _v07SocketPlacementMode = false; RebuildV07SocketList(); RebuildV07SocketVisuals(); GetViewport().SetInputAsHandled();
        SetStatus($"Placed {socket.Type} socket on {socket.OwnerObject}." + (socket.RigJoint >= 0 ? " It is linked to the nearest rig joint." : ""));
    }

    void LinkV07SocketToNearestRigJoint(V07SocketDto socket)
    {
        var rig = _v06Rigs.FirstOrDefault(r => r.ObjectName == socket.OwnerObject); if (rig == null || rig.Joints.Count == 0) return;
        Vector3 p = new(socket.LocalPosition[0], socket.LocalPosition[1], socket.LocalPosition[2]); int nearest = Enumerable.Range(0, rig.Joints.Count).OrderBy(i => JointPos(rig.Joints[i]).DistanceSquaredTo(p)).First();
        Vector3 off = p - JointPos(rig.Joints[nearest]); socket.RigJoint = nearest; socket.JointOffset = Vec(off);
    }

    void AutoCreateV07Sockets()
    {
        if (_selected == null) { SetStatus("Select the character/body object first."); return; }
        string owner = _selected.Name.ToString(); Aabb a = _selected.GetAabb(); Vector3 c = a.GetCenter(), half = a.Size * .5f;
        var defs = new (string type, Vector3 p, Vector3 n)[]
        {
            ("Head", c + Vector3.Up * half.Y, Vector3.Up),
            ("Base", c - Vector3.Up * half.Y, Vector3.Down),
            ("LeftHand", c - Vector3.Right * half.X, Vector3.Left),
            ("RightHand", c + Vector3.Right * half.X, Vector3.Right),
            ("Back", c - Vector3.Forward * half.Z, -Vector3.Forward),
            ("LeftShoulder", c - Vector3.Right * half.X * .7f + Vector3.Up * half.Y * .35f, Vector3.Left),
            ("RightShoulder", c + Vector3.Right * half.X * .7f + Vector3.Up * half.Y * .35f, Vector3.Right)
        };
        foreach (var d in defs)
        {
            if (_v07Sockets.Any(s => s.OwnerObject == owner && s.Type == d.type)) continue;
            var socket = new V07SocketDto { OwnerObject = owner, Name = d.type, Type = d.type, LocalPosition = Vec(d.p), LocalNormal = Vec(d.n) }; LinkV07SocketToNearestRigJoint(socket); _v07Sockets.Add(socket);
        }
        RebuildV07SocketList(); RebuildV07SocketVisuals(); SetStatus("Created starter head/hand/back/shoulder/base sockets. Surface-place replacements where the automatic positions are unsuitable.");
    }

    void RebuildV07SocketList()
    {
        if (_v07SocketList == null) return; foreach (var c in _v07SocketList.GetChildren()) c.QueueFree();
        foreach (var s in _v07Sockets)
        {
            var row = new HBoxContainer(); var b = new Button { Text = $"{s.Type} · {s.OwnerObject}" + (s.RigJoint >= 0 ? $" · joint {s.RigJoint}" : ""), Alignment = HorizontalAlignment.Left, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            b.Pressed += () => { _v07SelectedSocketId = s.Id; if (_v07SocketRoll != null) _v07SocketRoll.Value = s.RollDeg; RebuildV07SocketVisuals(); SetStatus($"Selected {s.Type} socket on {s.OwnerObject}."); }; row.AddChild(b);
            var del = new Button { Text = "×" }; del.Pressed += () => DeleteV07Socket(s.Id); row.AddChild(del); _v07SocketList.AddChild(row);
        }
    }

    void DeleteV07Socket(string id)
    {
        _v07Attachments.RemoveAll(a => a.SocketId == id); _v07Sockets.RemoveAll(s => s.Id == id); if (_v07SelectedSocketId == id) _v07SelectedSocketId = ""; RebuildV07SocketList(); RebuildV07SocketVisuals();
    }

    void RebuildV07SocketVisuals()
    {
        _v07SocketVisualRoot?.QueueFree(); _v07SocketVisualRoot = null; if (_world == null) return;
        var root = new Node3D { Name = "Kitbash Sockets v0.7" }; _world.AddChild(root); _v07SocketVisualRoot = root;
        foreach (var s in _v07Sockets)
        {
            if (!TryGetV07SocketWorld(s, out var p, out var n)) continue; float radius = s.Id == _v07SelectedSocketId ? 1.5f : 1f;
            var sphere = new MeshInstance3D { Mesh = new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = 12, Rings = 6 }, GlobalPosition = p }; root.AddChild(sphere);
            var stem = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = .18f, BottomRadius = .18f, Height = 4f, RadialSegments = 8 }, GlobalPosition = p + n * 2f };
            stem.GlobalBasis = V07SocketBasis(n, s.RollDeg); root.AddChild(stem);
        }
        RebuildV07MountVisual();
    }

    static Basis V07SocketBasis(Vector3 normal, float rollDeg)
    {
        Vector3 n = normal.Normalized(); Basis b = new Basis(new Quaternion(Vector3.Up, n));
        if (Math.Abs(rollDeg) > .001f) b = new Basis(new Quaternion(n, Mathf.DegToRad(rollDeg))) * b;
        return b;
    }

    void RebuildV07MountVisual()
    {
        _v07MountVisualRoot?.QueueFree(); _v07MountVisualRoot = null;
        if (_world == null || _selected == null) return;
        var def = _v07Parts.FirstOrDefault(p => p.Id == _v07SelectedPartId); if (def == null) return;
        Vector3 local = new(def.MountPoint[0], def.MountPoint[1], def.MountPoint[2]);
        Vector3 n = new(def.MountNormal[0], def.MountNormal[1], def.MountNormal[2]).Normalized();
        Vector3 wp = _selected.GlobalTransform * local; Vector3 wn = (_selected.GlobalTransform.Basis * n).Normalized();
        var root = new Node3D { Name = "Part Mount Preview v0.7" }; _world.AddChild(root); _v07MountVisualRoot = root;
        root.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = .9f, Height = 1.8f, RadialSegments = 12, Rings = 6 }, GlobalPosition = wp });
        var stem = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = .15f, BottomRadius = .15f, Height = 3f, RadialSegments = 8 }, GlobalPosition = wp + wn * 1.5f };
        stem.GlobalBasis = new Basis(new Quaternion(Vector3.Up, wn)); root.AddChild(stem);
    }

    bool TryGetV07SocketWorld(V07SocketDto socket, out Vector3 worldPos, out Vector3 worldNormal)
    {
        worldPos = default; worldNormal = Vector3.Up; var owner = _objects.FirstOrDefault(o => o.Name.ToString() == socket.OwnerObject); if (owner == null) return false;
        Vector3 local = new(socket.LocalPosition[0], socket.LocalPosition[1], socket.LocalPosition[2]); Vector3 n = new(socket.LocalNormal[0], socket.LocalNormal[1], socket.LocalNormal[2]);
        var rig = _v06Rigs.FirstOrDefault(r => r.ObjectName == socket.OwnerObject);
        if (rig != null && socket.RigJoint >= 0 && socket.RigJoint < rig.Joints.Count)
        {
            if (_v06CurrentRig == rig && _v06RiggedObject == owner)
            {
                ComputeV06Pose(out var posed, out var rots); if (socket.RigJoint < posed.Length)
                {
                    Vector3 off = new(socket.JointOffset[0], socket.JointOffset[1], socket.JointOffset[2]); local = posed[socket.RigJoint] + rots[socket.RigJoint] * off; n = rots[socket.RigJoint] * n;
                }
            }
            else
            {
                Vector3 off = new(socket.JointOffset[0], socket.JointOffset[1], socket.JointOffset[2]); local = JointPos(rig.Joints[socket.RigJoint]) + off;
            }
        }
        worldNormal = (owner.GlobalTransform.Basis * n).Normalized(); worldPos = owner.GlobalTransform * local + worldNormal * socket.SurfaceOffset; return true;
    }

    void SnapSelectedV07Object()
    {
        if (_selected == null) { SetStatus("Select the part object to snap first."); return; } var socket = _v07Sockets.FirstOrDefault(s => s.Id == _v07SelectedSocketId); if (socket == null) { SetStatus("Choose a socket first."); return; }
        if (_selected.Name.ToString() == socket.OwnerObject) { SetStatus("The socket owner cannot be snapped to its own socket."); return; }
        if (!TryGetV07SocketWorld(socket, out var p, out var n)) { SetStatus("Socket owner no longer exists."); return; }
        string library = _v07SelectedPartId; var def = _v07Parts.FirstOrDefault(x => x.Id == library);
        Basis basis = V07SocketBasis(n, socket.RollDeg);
        Vector3 mountPoint = def == null ? Vector3.Zero : new Vector3(def.MountPoint[0], def.MountPoint[1], def.MountPoint[2]);
        Vector3 mountNormal = def == null ? Vector3.Up : new Vector3(def.MountNormal[0], def.MountNormal[1], def.MountNormal[2]).Normalized();
        Basis mountBasis = new Basis(new Quaternion(mountNormal, Vector3.Up));
        Basis finalBasis = basis * mountBasis;
        var gt = _selected.GlobalTransform; gt.Basis = finalBasis.Scaled(_selected.Scale); gt.Origin = p - gt.Basis * mountPoint; _selected.GlobalTransform = gt;
        _v07Attachments.RemoveAll(a => a.PartObjectName == _selected.Name.ToString()); _v07Attachments.Add(new V07AttachmentDto { PartObjectName = _selected.Name.ToString(), SocketId = socket.Id, LibraryId = library, UniformScale = 1f }); ImportV06Role(_selected.Name.ToString(), "attachment");
        SetStatus($"Snapped {_selected.Name} to {socket.Type}. It remains a separate editable object until you voxel-union it.");
    }

    void DetachSelectedV07Object()
    {
        if (_selected == null) return; int removed = _v07Attachments.RemoveAll(a => a.PartObjectName == _selected.Name.ToString()); SetStatus(removed > 0 ? "Attachment link removed; object remains in its current position." : "Selected object was not socket-linked.");
    }

    internal void RefreshV07Attachments()
    {
        foreach (var a in _v07Attachments.ToList())
        {
            var part = _objects.FirstOrDefault(o => o.Name.ToString() == a.PartObjectName); var socket = _v07Sockets.FirstOrDefault(s => s.Id == a.SocketId); if (part == null || socket == null) continue;
            if (!TryGetV07SocketWorld(socket, out var p, out var n)) continue;
            var def = _v07Parts.FirstOrDefault(x => x.Id == a.LibraryId);
            Vector3 mountPoint = def == null ? Vector3.Zero : new Vector3(def.MountPoint[0], def.MountPoint[1], def.MountPoint[2]);
            Vector3 mountNormal = def == null ? Vector3.Up : new Vector3(def.MountNormal[0], def.MountNormal[1], def.MountNormal[2]).Normalized();
            Basis basis = V07SocketBasis(n, socket.RollDeg) * new Basis(new Quaternion(mountNormal, Vector3.Up));
            Vector3 rot = new(Mathf.DegToRad(a.LocalRotationDeg[0]), Mathf.DegToRad(a.LocalRotationDeg[1]), Mathf.DegToRad(a.LocalRotationDeg[2]));
            basis = basis * new Basis(Quaternion.FromEuler(rot));
            Vector3 scale = Vector3.One * Math.Max(.01f, a.UniformScale);
            var gt = part.GlobalTransform; gt.Basis = basis.Scaled(scale);
            Vector3 localOffset = new(a.LocalOffset[0], a.LocalOffset[1], a.LocalOffset[2]);
            gt.Origin = p + basis * localOffset - gt.Basis * mountPoint; part.GlobalTransform = gt;
        }
        RebuildV07SocketVisuals();
    }

    void ApplyV07AttachmentFineTune()
    {
        if (_selected == null) return;
        var a = _v07Attachments.FirstOrDefault(x => x.PartObjectName == _selected.Name.ToString()); if (a == null) { SetStatus("Selected object is not attached to a socket."); return; }
        a.LocalOffset = new[] { (float)(_v07AttachOffsetX?.Value ?? 0), (float)(_v07AttachOffsetY?.Value ?? 0), (float)(_v07AttachOffsetZ?.Value ?? 0) };
        a.LocalRotationDeg = new[] { (float)(_v07AttachRotX?.Value ?? 0), (float)(_v07AttachRotY?.Value ?? 0), (float)(_v07AttachRotZ?.Value ?? 0) };
        a.UniformScale = (float)(_v07AttachScale?.Value ?? 1); RefreshV07Attachments(); SetStatus("Applied attachment fine-tune offsets.");
    }

    void ResetV07AttachmentFineTune()
    {
        if (_selected == null) return; var a = _v07Attachments.FirstOrDefault(x => x.PartObjectName == _selected.Name.ToString()); if (a == null) return;
        a.LocalOffset = new float[3]; a.LocalRotationDeg = new float[3]; a.UniformScale = 1f;
        if (_v07AttachOffsetX != null) _v07AttachOffsetX.Value = 0; if (_v07AttachOffsetY != null) _v07AttachOffsetY.Value = 0; if (_v07AttachOffsetZ != null) _v07AttachOffsetZ.Value = 0;
        if (_v07AttachRotX != null) _v07AttachRotX.Value = 0; if (_v07AttachRotY != null) _v07AttachRotY.Value = 0; if (_v07AttachRotZ != null) _v07AttachRotZ.Value = 0; if (_v07AttachScale != null) _v07AttachScale.Value = 1;
        RefreshV07Attachments(); SetStatus("Attachment fine tune reset.");
    }

    internal List<V07SocketDto> ExportV07Sockets() => _v07Sockets;
    internal List<V07AttachmentDto> ExportV07Attachments() => _v07Attachments;
    internal void ImportV07State(List<V07SocketDto>? sockets, List<V07AttachmentDto>? attachments)
    {
        _v07Sockets.Clear(); _v07Attachments.Clear(); if (sockets != null) _v07Sockets.AddRange(sockets); if (attachments != null) _v07Attachments.AddRange(attachments); RebuildV07SocketList(); RebuildV07SocketVisuals(); RefreshV07Attachments();
    }
}
