using Godot;
using Miniscuplter.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV1020StageCCleanupExport()
    {
        ReplaceV095Button("Repair Selected", async () => await V1020RepairSelectedStageCAwareAsync());
        ReplaceV095Button("Repair selected model", async () => await V1020RepairSelectedStageCAwareAsync());
        ReplaceV095Button("Export STL", OpenV1020StageCAwareExportDialog);
    }

    bool V1020TryResolveSelectedProjectObject(out ObjectId objectId, out ProjectObject projectObject)
    {
        objectId = default;
        projectObject = null!;
        if (_selected == null || !GodotObject.IsInstanceValid(_selected) || _v1020StageCSession == null)
            return false;
        if (!_v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out objectId))
            return false;
        return _v1020StageCSession.Current.Objects.TryGetValue(objectId, out projectObject!);
    }

    async Task V1020RepairSelectedStageCAwareAsync()
    {
        if (_selected?.Mesh is not ArrayMesh)
        {
            SetStatus("Select a mesh to repair.");
            return;
        }
        if (!V1020TryResolveSelectedProjectObject(out var objectId, out _))
        {
            await RepairSelectedV09Async();
            return;
        }

        var target = _selected;
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                var binding = StageCCleanup.Begin(session.Current, objectId);
                var inputRevision = StageCCleanup.ResolveExportRevision(session.Current, objectId, binding.InputMeshRevisionId);
                string inputAsset = ProjectStore.ResolveAsset(ProjectLayout.FromManifest(_v1020StageCProjectPath), inputRevision.AssetPath);
                ArrayMesh inputMesh = V1020ArrayMeshFromData(MeshBinaryCodec.Read(inputAsset));

                string workDir = V09PrepDir();
                string input = Path.Combine(workDir, $"stagec_repair_input_{binding.InputMeshRevisionId}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
                string output = Path.Combine(workDir, $"stagec_repaired_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
                MeshIO.SaveBinaryStl(inputMesh, input);
                double pitch = _v09RepairVoxel?.Value ?? .30;
                SetStatus($"Repairing project revision {binding.InputMeshRevisionId} at {pitch:0.00} mm…");

                string repairedPath = await _ai.RepairGeometryAsync(input, output, pitch);
                ArrayMesh repaired = MeshIO.LoadStl(repairedPath);
                if (repaired.GetSurfaceCount() == 0)
                    throw new InvalidDataException("Cleanup backend returned an empty mesh.");
                MeshData data = V1013MeshData(repaired);
                MeshRevision outputRevision = await _v1020StageCStore.CreateMeshRevisionAsync(
                    _v1020StageCProjectPath,
                    binding.ObjectId,
                    data,
                    $"stagec-cleanup:voxel-repair:{pitch:0.###}mm",
                    binding.InputMeshRevisionId);

                StageCCleanup.ApplyResult(session, binding, outputRevision);
                await V1020SaveSessionAsync();

                // Publish the repaired mesh only after the new immutable revision is durable.
                target.Mesh = repaired;
                V095TopologyChanged(target);
                Select(target);
                FrameSelected();
                SetStatus($"Repair complete and saved as new immutable revision {outputRevision.Id}; source revision {binding.InputMeshRevisionId} remains preserved.");
            }
            finally { _v1020StageCGate.Release(); }
            await AnalyzeSelectedV09Async();
        }
        catch (Exception ex)
        {
            SetStatus("Stage-C repair failed safely; the durable source revision remains active: " + ex.Message);
        }
    }

    void OpenV1020StageCAwareExportDialog()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a mesh first."); return; }
        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.stl ; STL meshes" },
            CurrentFile = _selected.Name + ".stl",
            UseNativeDialog = true
        };
        AddChild(dialog);
        dialog.FileSelected += async path =>
        {
            try { await V1020ExportSelectedStageCAwareAsync(path); }
            finally { dialog.QueueFree(); }
        };
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCenteredRatio(.75f);
    }

    async Task V1020ExportSelectedStageCAwareAsync(string destination)
    {
        if (!V1020TryResolveSelectedProjectObject(out var objectId, out _))
        {
            SafeV095ExportStl(destination);
            return;
        }

        string? temp = null;
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                ProjectObject obj = session.Current.Objects[objectId];
                MeshRevision revision = StageCCleanup.ResolveExportRevision(session.Current, objectId, obj.ActiveMeshRevisionId);
                string asset = ProjectStore.ResolveAsset(ProjectLayout.FromManifest(_v1020StageCProjectPath), revision.AssetPath);
                ArrayMesh mesh = V1020ArrayMeshFromData(MeshBinaryCodec.Read(asset));
                Transform3D transform = V1020GodotTransform(obj.Transform);

                string full = Path.GetFullPath(destination);
                if (!full.EndsWith(".stl", StringComparison.OrdinalIgnoreCase)) full += ".stl";
                string parent = Path.GetDirectoryName(full) ?? throw new InvalidOperationException("Export destination has no parent directory.");
                Directory.CreateDirectory(parent);
                temp = Path.Combine(parent, Path.GetFileNameWithoutExtension(full) + "." + Guid.NewGuid().ToString("N") + ".tmp.stl");

                MeshIO.SaveBinaryStl(V1020BakeMesh(mesh, transform), temp);
                if (!File.Exists(temp) || new FileInfo(temp).Length == 0)
                    throw new IOException("Temporary Stage-C STL export was not written.");
                ArrayMesh verification = MeshIO.LoadStl(temp);
                int triangles = V1020ValidateExportMesh(verification);
                File.Move(temp, full, true);
                temp = null;
                SetStatus($"Exported validated Stage-C STL from object {objectId}, revision {revision.Id} ({triangles:N0} triangles): {full}");
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex)
        {
            if (temp != null) { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
            SetStatus("Stage-C STL export failed safely; an existing destination file was not replaced: " + ex.Message);
        }
    }

    static ArrayMesh V1020ArrayMeshFromData(MeshData data)
    {
        var vertices = new Vector3[data.VertexCount];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]);
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = data.Indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    static Transform3D V1020GodotTransform(TransformState state)
    {
        var rotation = new Vector3(state.RotationEuler.X, state.RotationEuler.Y, state.RotationEuler.Z);
        var scale = new Vector3(state.Scale.X, state.Scale.Y, state.Scale.Z);
        var basis = Basis.FromEuler(rotation).Scaled(scale);
        return new Transform3D(basis, new Vector3(state.Position.X, state.Position.Y, state.Position.Z));
    }

    static ArrayMesh V1020BakeMesh(ArrayMesh source, Transform3D transform)
    {
        var result = new ArrayMesh();
        for (int surface = 0; surface < source.GetSurfaceCount(); surface++)
        {
            var arrays = source.SurfaceGetArrays(surface);
            var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            for (int i = 0; i < vertices.Length; i++) vertices[i] = transform * vertices[i];
            arrays[(int)Mesh.ArrayType.Vertex] = vertices;
            result.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        }
        return result;
    }

    static int V1020ValidateExportMesh(ArrayMesh mesh)
    {
        if (mesh.GetSurfaceCount() == 0) throw new InvalidDataException("Export verification reopened an empty STL.");
        int triangles = 0;
        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
        {
            var arrays = mesh.SurfaceGetArrays(s);
            var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            if (vertices.Length == 0) throw new InvalidDataException("Export verification found an empty surface.");
            foreach (var vertex in vertices)
                if (!vertex.IsFinite()) throw new InvalidDataException("Export verification found non-finite coordinates.");
            triangles += (indices.Length > 0 ? indices.Length : vertices.Length) / 3;
        }
        if (triangles <= 0) throw new InvalidDataException("Export verification found no triangles.");
        return triangles;
    }
}
