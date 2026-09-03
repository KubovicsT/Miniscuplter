using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    public sealed class RigJointDto
    {
        public string Name { get; set; } = "joint";
        public int Parent { get; set; } = -1;
        public float[] Position { get; set; } = new float[3];
        public float[] RotationDeg { get; set; } = new float[3];
    }

    public sealed class RigRecordDto
    {
        public string ObjectName { get; set; } = "";
        public string Provider { get; set; } = "";
        public string Mode { get; set; } = "quick";
        public List<RigJointDto> Joints { get; set; } = new();
    }

    sealed class RigJson
    {
        public string Provider { get; set; } = "unknown";
        public string Mode { get; set; } = "quick";
        public List<RigJsonJoint> Joints { get; set; } = new();
    }

    sealed class RigJsonJoint
    {
        public string Name { get; set; } = "joint";
        public int Parent { get; set; } = -1;
        public float[] Position { get; set; } = new float[3];
    }

    readonly List<RigRecordDto> _v06Rigs = new();
    readonly Dictionary<string, string> _v06Roles = new();
    RigRecordDto? _v06CurrentRig;
    MeshInstance3D? _v06RiggedObject;
    ArrayMesh? _v06RestMesh;
    Node3D? _v06RigVisual;
    VBoxContainer? _v06JointList;
    Label? _v06RigStatus;
    SpinBox? _v06JointX; SpinBox? _v06JointY; SpinBox? _v06JointZ;
    SpinBox? _v06RotX; SpinBox? _v06RotY; SpinBox? _v06RotZ;
    SpinBox? _v06ParentIndex;
    int _v06SelectedJoint = -1;
    bool _v06UpdatingControls;

    public void InstallV06Extras()
    {
        var transform = FindChild("Transform", true, false) as VBoxContainer;
        if (transform == null) return;

        transform.AddChild(new HSeparator());
        transform.AddChild(new Label { Text = "RIGGING & POSING — v0.6", ThemeTypeVariation = "HeaderSmall" });
        transform.AddChild(new Label
        {
            Text = "Quick Rig creates an immediate geometry-derived skeleton without assuming a humanoid topology. Universal AI Rig uses the configured provider adapter. Refine joints before posing.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var roles = new HBoxContainer();
        AddV06Button(roles, "Character", () => SetV06Role("character"));
        AddV06Button(roles, "Attachment", () => SetV06Role("attachment"));
        AddV06Button(roles, "Do Not Rig", () => SetV06Role("exclude"));
        transform.AddChild(roles);

        var generate = new HBoxContainer();
        AddV06Button(generate, "Quick Rig", async () => await GenerateV06Rig("quick"));
        AddV06Button(generate, "Universal AI Rig", async () => await GenerateV06Rig("universal"));
        transform.AddChild(generate);
        _v06RigStatus = new Label { Text = "No rig selected.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        transform.AddChild(_v06RigStatus);

        transform.AddChild(new Label { Text = "Skeleton joints" });
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 150) };
        _v06JointList = new VBoxContainer(); scroll.AddChild(_v06JointList); transform.AddChild(scroll);

        var editRow = new HBoxContainer();
        _v06JointX = JointSpin(); _v06JointY = JointSpin(); _v06JointZ = JointSpin();
        editRow.AddChild(_v06JointX); editRow.AddChild(_v06JointY); editRow.AddChild(_v06JointZ); transform.AddChild(new Label { Text = "Joint rest position X / Y / Z (mm)" }); transform.AddChild(editRow);
        _v06JointX.ValueChanged += _ => UpdateV06JointPositionFromControls();
        _v06JointY.ValueChanged += _ => UpdateV06JointPositionFromControls();
        _v06JointZ.ValueChanged += _ => UpdateV06JointPositionFromControls();

        var parentRow = new HBoxContainer();
        _v06ParentIndex = new SpinBox { MinValue = -1, MaxValue = 255, Step = 1, Value = -1, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        parentRow.AddChild(_v06ParentIndex);
        AddV06Button(parentRow, "Set Parent", SetV06Parent);
        AddV06Button(parentRow, "Add Joint", AddV06Joint);
        AddV06Button(parentRow, "Delete Leaf", DeleteV06Joint);
        transform.AddChild(parentRow);

        var poseRow = new HBoxContainer();
        _v06RotX = RotSpin(); _v06RotY = RotSpin(); _v06RotZ = RotSpin();
        poseRow.AddChild(_v06RotX); poseRow.AddChild(_v06RotY); poseRow.AddChild(_v06RotZ); transform.AddChild(new Label { Text = "Selected joint pose X / Y / Z (degrees)" }); transform.AddChild(poseRow);
        _v06RotX.ValueChanged += _ => UpdateV06PoseFromControls();
        _v06RotY.ValueChanged += _ => UpdateV06PoseFromControls();
        _v06RotZ.ValueChanged += _ => UpdateV06PoseFromControls();

        var poseButtons = new HBoxContainer();
        AddV06Button(poseButtons, "Preview Pose", ApplyV06PosePreview);
        AddV06Button(poseButtons, "Reset Pose", ResetV06Pose);
        AddV06Button(poseButtons, "Apply Pose", CommitV06Pose);
        transform.AddChild(poseButtons);
    }

    static SpinBox JointSpin() => new() { MinValue = -5000, MaxValue = 5000, Step = .1, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    static SpinBox RotSpin() => new() { MinValue = -180, MaxValue = 180, Step = 1, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    static void AddV06Button(Container parent, string text, Action action) { var b = new Button { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; b.Pressed += action; parent.AddChild(b); }

    void SetV06Role(string role)
    {
        if (_selected == null) { SetStatus("Select an object first."); return; }
        _v06Roles[_selected.Name.ToString()] = role;
        SetStatus($"{_selected.Name}: role set to {role}.");
    }

    internal string V06RoleFor(string objectName)
        => _v06Roles.TryGetValue(objectName, out var role) ? role : "mesh";

    internal void ImportV06Role(string objectName, string role)
    {
        if (!string.IsNullOrWhiteSpace(objectName) && !string.IsNullOrWhiteSpace(role)) _v06Roles[objectName] = role;
    }

    async Task GenerateV06Rig(string mode)
    {
        if (_selected?.Mesh is not ArrayMesh) { SetStatus("Select a mesh object to rig."); return; }
        if (V06RoleFor(_selected.Name.ToString()) is "attachment" or "exclude")
        {
            SetStatus("This object is marked as an attachment/do-not-rig object. Mark the creature body as Character first."); return;
        }

        MeshInstance3D source = _selected;
        string dir = ProjectSettings.GlobalizePath($"user://rig/job_{DateTime.Now:yyyyMMdd_HHmmss_fff}");
        Directory.CreateDirectory(dir);
        string input = Path.Combine(dir, "character.stl");
        string output = Path.Combine(dir, "skeleton.json");
        MeshIO.SaveBinaryStl(BakeToWorldMesh(source), input);
        var sw = Stopwatch.StartNew();
        try
        {
            ResetV055Cancellation();
            if (_v06RigStatus != null) _v06RigStatus.Text = $"{mode} rig running… {EstimateText("rig-" + mode, "standard")}";
            string path = await _ai.PredictRigAsync(input, output, mode);
            sw.Stop(); RecordJob("rig-" + mode, "standard", sw.Elapsed.TotalSeconds);
            LoadV06RigJson(path, source);
            if (_v06RigStatus != null) _v06RigStatus.Text = $"{_v06CurrentRig?.Provider}: {_v06CurrentRig?.Joints.Count ?? 0} joints · {FormatSeconds(sw.Elapsed.TotalSeconds)}";
            SetStatus($"{mode} rig generated. Refine joints, then pose the character.");
        }
        catch (Exception ex)
        {
            SetStatus($"{mode} rig failed: {ex.Message}");
            if (_v06RigStatus != null) _v06RigStatus.Text = ex.Message;
        }
    }

    void LoadV06RigJson(string path, MeshInstance3D source)
    {
        var parsed = JsonSerializer.Deserialize<RigJson>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Rig provider returned invalid JSON.");
        if (parsed.Joints.Count == 0) throw new InvalidDataException("Rig provider returned no joints.");
        Transform3D inv = source.GlobalTransform.AffineInverse();
        var record = new RigRecordDto { ObjectName = source.Name.ToString(), Provider = parsed.Provider, Mode = parsed.Mode };
        foreach (var j in parsed.Joints)
        {
            if (j.Position.Length < 3) continue;
            Vector3 local = inv * new Vector3(j.Position[0], j.Position[1], j.Position[2]);
            record.Joints.Add(new RigJointDto { Name = j.Name, Parent = j.Parent, Position = new[] { local.X, local.Y, local.Z } });
        }
        _v06Rigs.RemoveAll(x => x.ObjectName == record.ObjectName);
        _v06Rigs.Add(record);
        _v06CurrentRig = record; _v06RiggedObject = source; _v06RestMesh = CloneMesh((ArrayMesh)source.Mesh!); _v06SelectedJoint = record.Joints.Count > 0 ? 0 : -1;
        RebuildV06JointList(); RebuildV06RigVisual(); LoadV06JointControls();
    }

    void RebuildV06JointList()
    {
        if (_v06JointList == null) return;
        foreach (var n in _v06JointList.GetChildren()) n.QueueFree();
        if (_v06CurrentRig == null) return;
        for (int i = 0; i < _v06CurrentRig.Joints.Count; i++)
        {
            int index = i; var j = _v06CurrentRig.Joints[i];
            var b = new Button { Text = $"{i}: {j.Name}  parent {j.Parent}", Alignment = HorizontalAlignment.Left };
            b.Pressed += () => { _v06SelectedJoint = index; LoadV06JointControls(); };
            _v06JointList.AddChild(b);
        }
    }

    void LoadV06JointControls()
    {
        if (_v06CurrentRig == null || _v06SelectedJoint < 0 || _v06SelectedJoint >= _v06CurrentRig.Joints.Count) return;
        var j = _v06CurrentRig.Joints[_v06SelectedJoint]; _v06UpdatingControls = true;
        if (_v06JointX != null) _v06JointX.Value = j.Position[0]; if (_v06JointY != null) _v06JointY.Value = j.Position[1]; if (_v06JointZ != null) _v06JointZ.Value = j.Position[2];
        if (_v06RotX != null) _v06RotX.Value = j.RotationDeg[0]; if (_v06RotY != null) _v06RotY.Value = j.RotationDeg[1]; if (_v06RotZ != null) _v06RotZ.Value = j.RotationDeg[2];
        if (_v06ParentIndex != null) _v06ParentIndex.Value = j.Parent; _v06UpdatingControls = false;
    }

    void UpdateV06JointPositionFromControls()
    {
        if (_v06UpdatingControls || _v06CurrentRig == null || _v06SelectedJoint < 0) return;
        var j = _v06CurrentRig.Joints[_v06SelectedJoint];
        j.Position = new[] { (float)(_v06JointX?.Value ?? 0), (float)(_v06JointY?.Value ?? 0), (float)(_v06JointZ?.Value ?? 0) };
        RebuildV06RigVisual();
    }

    void UpdateV06PoseFromControls()
    {
        if (_v06UpdatingControls || _v06CurrentRig == null || _v06SelectedJoint < 0) return;
        var j = _v06CurrentRig.Joints[_v06SelectedJoint];
        j.RotationDeg = new[] { (float)(_v06RotX?.Value ?? 0), (float)(_v06RotY?.Value ?? 0), (float)(_v06RotZ?.Value ?? 0) };
        ApplyV06PosePreview();
    }

    void SetV06Parent()
    {
        if (_v06CurrentRig == null || _v06SelectedJoint < 0) return;
        int p = (int)(_v06ParentIndex?.Value ?? -1);
        if (p >= _v06CurrentRig.Joints.Count || p == _v06SelectedJoint) { SetStatus("Invalid parent joint."); return; }
        if (p >= 0 && IsV06Descendant(p, _v06SelectedJoint)) { SetStatus("Parent change would create a skeleton cycle."); return; }
        _v06CurrentRig.Joints[_v06SelectedJoint].Parent = p; RebuildV06JointList(); RebuildV06RigVisual();
    }

    bool IsV06Descendant(int candidate, int ancestor)
    {
        if (_v06CurrentRig == null) return false;
        int p = candidate; int guard = 0;
        while (p >= 0 && p < _v06CurrentRig.Joints.Count && guard++ < 512) { if (p == ancestor) return true; p = _v06CurrentRig.Joints[p].Parent; }
        return false;
    }

    void AddV06Joint()
    {
        if (_v06CurrentRig == null || _v06RiggedObject == null) return;
        Vector3 p = _v06SelectedJoint >= 0 ? JointPos(_v06CurrentRig.Joints[_v06SelectedJoint]) : _v06RiggedObject.GetAabb().GetCenter();
        _v06CurrentRig.Joints.Add(new RigJointDto { Name = $"joint_{_v06CurrentRig.Joints.Count}", Parent = _v06SelectedJoint, Position = new[] { p.X, p.Y + 5f, p.Z } });
        _v06SelectedJoint = _v06CurrentRig.Joints.Count - 1; RebuildV06JointList(); RebuildV06RigVisual(); LoadV06JointControls();
    }

    void DeleteV06Joint()
    {
        if (_v06CurrentRig == null || _v06SelectedJoint < 0) return;
        if (_v06CurrentRig.Joints.Any(x => x.Parent == _v06SelectedJoint)) { SetStatus("Only leaf joints can be deleted; reparent children first."); return; }
        int removed = _v06SelectedJoint; _v06CurrentRig.Joints.RemoveAt(removed);
        foreach (var j in _v06CurrentRig.Joints) if (j.Parent > removed) j.Parent--;
        _v06SelectedJoint = Math.Min(removed, _v06CurrentRig.Joints.Count - 1); RebuildV06JointList(); RebuildV06RigVisual(); LoadV06JointControls();
    }

    void RebuildV06RigVisual()
    {
        _v06RigVisual?.QueueFree(); _v06RigVisual = null;
        if (_v06RiggedObject == null || _v06CurrentRig == null) return;
        var root = new Node3D { Name = "Rig v0.6" }; _v06RiggedObject.AddChild(root); _v06RigVisual = root;
        for (int i = 0; i < _v06CurrentRig.Joints.Count; i++)
        {
            Vector3 p = JointPos(_v06CurrentRig.Joints[i]);
            var sphere = new MeshInstance3D { Mesh = new SphereMesh { Radius = .8f, Height = 1.6f, RadialSegments = 12, Rings = 6 }, Position = p };
            root.AddChild(sphere);
            int parent = _v06CurrentRig.Joints[i].Parent;
            if (parent < 0 || parent >= _v06CurrentRig.Joints.Count) continue;
            Vector3 a = JointPos(_v06CurrentRig.Joints[parent]); Vector3 d = p - a; float len = d.Length(); if (len < .001f) continue;
            var bone = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = .28f, BottomRadius = .28f, Height = len, RadialSegments = 8 }, Position = (a + p) * .5f };
            bone.Basis = new Basis(new Quaternion(Vector3.Up, d.Normalized())); root.AddChild(bone);
        }
    }

    static Vector3 JointPos(RigJointDto j) => new(j.Position[0], j.Position[1], j.Position[2]);
    static Vector3 JointRot(RigJointDto j) => new(Mathf.DegToRad(j.RotationDeg[0]), Mathf.DegToRad(j.RotationDeg[1]), Mathf.DegToRad(j.RotationDeg[2]));

    void ComputeV06Pose(out Vector3[] posed, out Basis[] rotations)
    {
        if (_v06CurrentRig == null) { posed = Array.Empty<Vector3>(); rotations = Array.Empty<Basis>(); return; }
        int n = _v06CurrentRig.Joints.Count; posed = new Vector3[n]; rotations = new Basis[n];
        for (int i = 0; i < n; i++)
        {
            var j = _v06CurrentRig.Joints[i]; Basis localRot = new(Quaternion.FromEuler(JointRot(j)));
            if (j.Parent < 0 || j.Parent >= i) { posed[i] = JointPos(j); rotations[i] = localRot; }
            else
            {
                int p = j.Parent; Vector3 restOffset = JointPos(j) - JointPos(_v06CurrentRig.Joints[p]);
                posed[i] = posed[p] + rotations[p] * restOffset; rotations[i] = rotations[p] * localRot;
            }
        }
    }

    void ApplyV06PosePreview()
    {
        if (_v06RiggedObject == null || _v06CurrentRig == null || _v06RestMesh == null) return;
        ComputeV06Pose(out var posed, out var rotations); if (posed.Length == 0) return;
        var result = new ArrayMesh();
        for (int s = 0; s < _v06RestMesh.GetSurfaceCount(); s++)
        {
            var arrays = _v06RestMesh.SurfaceGetArrays(s); var verts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var moved = new Vector3[verts.Length];
            for (int v = 0; v < verts.Length; v++)
            {
                var nearest = Enumerable.Range(0, posed.Length).Select(i => (i, d: verts[v].DistanceSquaredTo(JointPos(_v06CurrentRig.Joints[i])))).OrderBy(x => x.d).Take(3).ToArray();
                Vector3 sum = Vector3.Zero; float weightSum = 0;
                foreach (var item in nearest)
                {
                    float w = 1f / Math.Max(.25f, item.d); int j = item.i;
                    Vector3 transformed = posed[j] + rotations[j] * (verts[v] - JointPos(_v06CurrentRig.Joints[j]));
                    sum += transformed * w; weightSum += w;
                }
                moved[v] = weightSum > 0 ? sum / weightSum : verts[v];
            }
            arrays[(int)Mesh.ArrayType.Vertex] = moved; result.AddSurfaceFromArrays(_v06RestMesh.SurfaceGetPrimitiveType(s), arrays);
        }
        _v06RiggedObject.Mesh = result; RebuildV06RigVisual();
    }

    void ResetV06Pose()
    {
        if (_v06CurrentRig == null || _v06RiggedObject == null || _v06RestMesh == null) return;
        foreach (var j in _v06CurrentRig.Joints) j.RotationDeg = new float[3];
        _v06RiggedObject.Mesh = CloneMesh(_v06RestMesh); LoadV06JointControls(); RebuildV06RigVisual(); SetStatus("Pose reset to rig rest state.");
    }

    void CommitV06Pose()
    {
        if (_v06CurrentRig == null || _v06RiggedObject?.Mesh is not ArrayMesh current) return;
        ComputeV06Pose(out var posed, out _);
        for (int i = 0; i < _v06CurrentRig.Joints.Count && i < posed.Length; i++)
        {
            _v06CurrentRig.Joints[i].Position = new[] { posed[i].X, posed[i].Y, posed[i].Z }; _v06CurrentRig.Joints[i].RotationDeg = new float[3];
        }
        _v06RestMesh = CloneMesh(current); RebuildV06RigVisual(); RebuildV06JointList(); LoadV06JointControls(); SetStatus("Pose applied to the mesh. You can continue sculpting or voxel-remesh for printable topology.");
    }

    internal List<RigRecordDto> ExportV06Rigs() => _v06Rigs;

    internal void ImportV06Rigs(List<RigRecordDto>? rigs)
    {
        _v06Rigs.Clear(); if (rigs != null) _v06Rigs.AddRange(rigs);
    }

    internal void RestoreV06RigForObject(MeshInstance3D obj)
    {
        var rig = _v06Rigs.FirstOrDefault(x => x.ObjectName == obj.Name.ToString()); if (rig == null || obj.Mesh is not ArrayMesh arr) return;
        _v06CurrentRig = rig; _v06RiggedObject = obj; _v06RestMesh = CloneMesh(arr); _v06SelectedJoint = rig.Joints.Count > 0 ? 0 : -1;
        RebuildV06JointList(); RebuildV06RigVisual(); LoadV06JointControls();
    }
}
