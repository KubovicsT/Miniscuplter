using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    Timer? _v095IntegrityTimer;
    MeshInstance3D? _v095TopologyRigObject;
    string _v095RigTopologySignature = "";

    public void InstallV095IntegrityGuards()
    {
        if (_v095IntegrityTimer != null) return;
        _v095IntegrityTimer = new Timer { WaitTime = .15, OneShot = false, Autostart = true };
        _v095IntegrityTimer.Timeout += V095IntegrityTick;
        AddChild(_v095IntegrityTimer);
    }

    static string V095TopologySignature(Mesh? mesh)
    {
        if (mesh == null) return "none";
        var parts = new List<string> { mesh.GetSurfaceCount().ToString() };
        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
        {
            var arrays = mesh.SurfaceGetArrays(s);
            int vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array().Length;
            int indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array().Length;
            parts.Add($"{vertices}:{indices}");
        }
        return string.Join("|", parts);
    }

    void V095IntegrityTick()
    {
        if (_v08MaskPaintMode && !_sculpting) CaptureV08MaskRedoHistory();

        if (_v09ThicknessOverlay != null && (_v09ThicknessSource == null || !GodotObject.IsInstanceValid(_v09ThicknessSource)))
        {
            ClearV09ThicknessHeatmap();
        }

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in _objects.Where(GodotObject.IsInstanceValid))
        {
            string original = obj.Name.ToString();
            if (used.Add(original)) continue;

            string stem = string.IsNullOrWhiteSpace(original) ? "Object" : original;
            int suffix = 2;
            string candidate;
            do candidate = $"{stem} ({suffix++})"; while (used.Contains(candidate) || _objects.Any(o => GodotObject.IsInstanceValid(o) && o != obj && o.Name.ToString().Equals(candidate, StringComparison.OrdinalIgnoreCase)));
            obj.Name = candidate;
            used.Add(candidate);
            if (_selected == obj) SetStatus($"Duplicate object identity corrected: '{original}' → '{candidate}'. Stable unique names are required for rigs, masks and attachments.");
        }

        if (_selected != null && !GodotObject.IsInstanceValid(_selected)) _selected = null;
        if (_v06RiggedObject != null && !GodotObject.IsInstanceValid(_v06RiggedObject))
        {
            _v06RiggedObject = null;
            _v06CurrentRig = null;
            _v06RestMesh = null;
            _v06WeightCache.Clear();
            _v06RigVisual?.QueueFree();
            _v06RigVisual = null;
            _v095TopologyRigObject = null;
            _v095RigTopologySignature = "";
            return;
        }

        if (_v06RiggedObject == null)
        {
            _v095TopologyRigObject = null;
            _v095RigTopologySignature = "";
            return;
        }

        string signature = V095TopologySignature(_v06RiggedObject.Mesh);
        if (_v095TopologyRigObject != _v06RiggedObject)
        {
            _v095TopologyRigObject = _v06RiggedObject;
            _v095RigTopologySignature = signature;
        }
        else if (!string.Equals(signature, _v095RigTopologySignature, StringComparison.Ordinal))
        {
            _v095RigTopologySignature = signature;
            V095TopologyChanged(_v06RiggedObject);
        }
    }
}
