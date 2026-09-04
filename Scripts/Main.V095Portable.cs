using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Miniscuplter;

public partial class Main
{
    sealed class V095PortableState
    {
        public int Version { get; set; } = 2;
        public List<V095MountSnapshot> Mounts { get; set; } = new();
        public V096SelectionSnapshot? SmartSelection { get; set; }
    }

    sealed class V095MountSnapshot
    {
        public string LibraryId { get; set; } = "";
        public string Name { get; set; } = "Part";
        public string Category { get; set; } = "Generic";
        public string SocketType { get; set; } = "Generic";
        public float DefaultScale { get; set; } = 1f;
        public float[] MountPoint { get; set; } = new float[3];
        public float[] MountNormal { get; set; } = new float[] { 0, 1, 0 };
        public float MountRollDeg { get; set; }
    }

    sealed class V096SelectionSnapshot
    {
        public string ObjectName { get; set; } = "";
        public string Query { get; set; } = "";
        public string TopologySignature { get; set; } = "";
        public float[] Weights { get; set; } = Array.Empty<float>();
    }

    void WriteV095PortableState(string generationDir)
    {
        var state = new V095PortableState();
        foreach (string id in _v07Attachments.Select(a => a.LibraryId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
        {
            var def = _v07Parts.FirstOrDefault(p => p.Id == id);
            if (def == null) continue;
            float[] mountPoint = def.MountPoint is { Length: >= 3 } ? (float[])def.MountPoint.Clone() : new float[3];
            float[] mountNormal = def.MountNormal is { Length: >= 3 } ? (float[])def.MountNormal.Clone() : new float[] { 0, 1, 0 };
            state.Mounts.Add(new V095MountSnapshot
            {
                LibraryId = def.Id,
                Name = def.Name,
                Category = def.Category,
                SocketType = def.SocketType,
                DefaultScale = def.DefaultScale,
                MountPoint = mountPoint,
                MountNormal = mountNormal,
                MountRollDeg = def.MountRollDeg
            });
        }
        V096ValidateSelection();
        if (_v096SelectionObject != null && _v096Selection != null)
        {
            state.SmartSelection = new V096SelectionSnapshot
            {
                ObjectName = _v096SelectionObject.Name.ToString(),
                Query = _v096SelectionQuery,
                TopologySignature = _v096SelectionTopology,
                Weights = (float[])_v096Selection.Clone()
            };
        }
        File.WriteAllText(Path.Combine(generationDir, "v095_state.json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    void ApplyV095PortableState(string? generationDir)
    {
        ClearV096Selection(false);
        if (string.IsNullOrWhiteSpace(generationDir)) return;
        string path = Path.Combine(generationDir, "v095_state.json");
        if (!File.Exists(path)) return;
        try
        {
            var state = JsonSerializer.Deserialize<V095PortableState>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (state == null) return;
            if (state.Mounts != null)
            {
                foreach (var snap in state.Mounts)
                {
                    if (string.IsNullOrWhiteSpace(snap.LibraryId) || snap.MountPoint is not { Length: >= 3 } || snap.MountNormal is not { Length: >= 3 }) continue;
                    float[] mountPoint = (float[])snap.MountPoint.Clone();
                    float[] mountNormal = (float[])snap.MountNormal.Clone();
                    var def = _v07Parts.FirstOrDefault(p => p.Id == snap.LibraryId);
                    if (def == null)
                    {
                        def = new V07PartDefinition
                        {
                            Id = snap.LibraryId,
                            Name = snap.Name,
                            Category = snap.Category,
                            SocketType = snap.SocketType,
                            DefaultScale = snap.DefaultScale,
                            MeshPath = "",
                            MountPoint = mountPoint,
                            MountNormal = mountNormal,
                            MountRollDeg = snap.MountRollDeg
                        };
                        _v07Parts.Add(def);
                    }
                    else
                    {
                        def.DefaultScale = snap.DefaultScale;
                        def.MountPoint = mountPoint;
                        def.MountNormal = mountNormal;
                        def.MountRollDeg = snap.MountRollDeg;
                    }
                }
                RefreshV095Attachments();
            }

            var sel = state.SmartSelection;
            if (sel != null && sel.Weights != null && sel.Weights.Length > 0)
            {
                var obj = _objects.FirstOrDefault(o => GodotObject.IsInstanceValid(o) && o.Name.ToString().Equals(sel.ObjectName, StringComparison.OrdinalIgnoreCase));
                if (obj?.Mesh is ArrayMesh mesh && V096MeshSignature(mesh) == sel.TopologySignature)
                {
                    var mdt = new MeshDataTool();
                    if (mdt.CreateFromSurface(mesh, 0) == Error.Ok && mdt.GetVertexCount() == sel.Weights.Length && sel.Weights.All(v => float.IsFinite(v) && v >= 0f && v <= 1f))
                    {
                        _v096SelectionObject = obj;
                        _v096Selection = (float[])sel.Weights.Clone();
                        _v096SelectionQuery = sel.Query ?? "";
                        _v096SelectionTopology = sel.TopologySignature;
                        RebuildV096SelectionOverlay();
                        ApplyV096SelectionToSculptMask();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ClearV096Selection(false);
            SetStatus("Project loaded, but embedded portable metadata could not be fully restored: " + ex.Message);
        }
    }

    static string? V095GenerationDirectory(string assetsRoot, IEnumerable<ObjectDto> objects)
    {
        foreach (var item in objects)
        {
            if (string.IsNullOrWhiteSpace(item.Mesh)) continue;
            string? relativeDir = Path.GetDirectoryName(item.Mesh.Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(relativeDir)) continue;
            string candidate = Path.GetFullPath(Path.Combine(assetsRoot, relativeDir));
            string root = Path.GetFullPath(assetsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return candidate;
        }
        return null;
    }
}
