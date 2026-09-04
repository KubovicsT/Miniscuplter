using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV095TopologyGuards()
    {
        ReplaceV095Button("Repair Selected", async () => await SafeV095RepairSelectedAsync());
    }

    void V095PrepareRiggedDirectMeshEdit(ArrayMesh visibleMesh)
    {
        if (_selected == null || _v06RiggedObject != _selected || _v06CurrentRig == null) return;

        bool posed = _v06CurrentRig.Joints.Any(j => j.RotationDeg != null && j.RotationDeg.Length >= 3 &&
            (Math.Abs(j.RotationDeg[0]) > .0001f || Math.Abs(j.RotationDeg[1]) > .0001f || Math.Abs(j.RotationDeg[2]) > .0001f));

        if (posed || _v06RestMesh == null)
        {
            _v06RestMesh = CloneMesh(visibleMesh);
            foreach (var joint in _v06CurrentRig.Joints) joint.RotationDeg = new float[3];
            _v06WeightCache.Clear();
            _v06SelectedJoint = Math.Clamp(_v06SelectedJoint, -1, _v06CurrentRig.Joints.Count - 1);
            RebuildV06RigVisual();
            LoadV06JointControls();
            if (posed) SetStatus("Sculpting a posed rig: the visible pose was safely adopted as the new rest mesh and pose rotations were reset before editing.");
        }
    }

    void V095CommitRiggedSculptRest(MeshInstance3D target)
    {
        if (_v06RiggedObject != target || target.Mesh is not ArrayMesh current) return;
        _v06RestMesh = CloneMesh(current);
        _v06WeightCache.Clear();
        _v095TopologyRigObject = target;
        _v095RigTopologySignature = V095TopologySignature(current);
    }

    void V095TopologyChanged(MeshInstance3D target)
    {
        _v08Masks.Remove(target.Name.ToString());
        if (_v09ThicknessSource == target) ClearV09ThicknessHeatmap();

        if (_v06RiggedObject == target)
        {
            _v06WeightCache.Clear();
            if (target.Mesh is ArrayMesh current)
            {
                _v06RestMesh = CloneMesh(current);
                if (_v06CurrentRig != null)
                {
                    foreach (var joint in _v06CurrentRig.Joints) joint.RotationDeg = new float[3];
                    _v06SelectedJoint = Math.Clamp(_v06SelectedJoint, -1, _v06CurrentRig.Joints.Count - 1);
                    RebuildV06RigVisual();
                    LoadV06JointControls();
                }
                _v095TopologyRigObject = target;
                _v095RigTopologySignature = V095TopologySignature(current);
            }
            SetStatus("Topology changed: sculpt mask and cached skin weights were invalidated; the existing skeleton was kept and pose rotations were reset against the new mesh.");
        }
    }

    async Task SafeV095RepairSelectedAsync()
    {
        if (_selected?.Mesh is not ArrayMesh sourceMesh) { SetStatus("Select a mesh to repair."); return; }
        MeshInstance3D target = _selected;
        try
        {
            string input = ExportSelectedV09Input(target, "repair_input");
            string output = Path.Combine(V09PrepDir(), $"repaired_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
            double pitch = _v09RepairVoxel?.Value ?? .30;
            SetStatus($"Repairing selected mesh at {pitch:0.00} mm voxel pitch…");
            string path = await _ai.RepairGeometryAsync(input, output, pitch);
            if (!GodotObject.IsInstanceValid(target) || !_objects.Contains(target)) { SetStatus("Repair result discarded because the source object no longer exists."); return; }
            var repaired = MeshIO.LoadStl(path);
            if (repaired.GetSurfaceCount() == 0) throw new InvalidDataException("Repair result contains no mesh surfaces.");

            PushUndo(sourceMesh);
            target.Mesh = repaired;
            target.Transform = Transform3D.Identity;
            V095TopologyChanged(target);
            Select(target);
            FrameSelected();
            await AnalyzeSelectedV09Async();
        }
        catch (Exception ex)
        {
            SetStatus("Repair failed without replacing the source mesh: " + ex.Message);
        }
    }
}
