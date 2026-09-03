using Godot;
using System;
using System.IO;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV095ExportGuards()
    {
        ReplaceV095Button("Export STL", OpenV095SafeExportDialog);
    }

    void OpenV095SafeExportDialog()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a mesh first."); return; }
        var d = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.stl ; STL meshes" },
            CurrentFile = _selected.Name + ".stl",
            UseNativeDialog = true
        };
        AddChild(d);
        d.FileSelected += p => { SafeV095ExportStl(p); d.QueueFree(); };
        d.Canceled += d.QueueFree;
        d.PopupCenteredRatio(.75f);
    }

    void SafeV095ExportStl(string destination)
    {
        string? temp = null;
        try
        {
            if (_selected?.Mesh == null) throw new InvalidOperationException("The selected object no longer exists.");
            if (!_selected.GlobalTransform.IsFinite()) throw new InvalidDataException("Selected object has a non-finite transform.");

            string full = Path.GetFullPath(destination);
            if (!full.EndsWith(".stl", StringComparison.OrdinalIgnoreCase)) full += ".stl";
            string parent = Path.GetDirectoryName(full) ?? throw new InvalidOperationException("Export destination has no parent directory.");
            Directory.CreateDirectory(parent);
            temp = Path.Combine(parent, Path.GetFileNameWithoutExtension(full) + "." + Guid.NewGuid().ToString("N") + ".tmp.stl");

            ArrayMesh baked = BakeToWorldMesh(_selected);
            if (baked.GetSurfaceCount() == 0) throw new InvalidDataException("Selected object contains no exportable surfaces.");
            MeshIO.SaveBinaryStl(baked, temp);
            if (!File.Exists(temp) || new FileInfo(temp).Length == 0) throw new IOException("Temporary STL export was not written.");

            ArrayMesh verification = MeshIO.LoadStl(temp);
            if (verification.GetSurfaceCount() == 0) throw new InvalidDataException("Export verification reopened an empty STL.");
            int triangles = 0;
            for (int s = 0; s < verification.GetSurfaceCount(); s++)
            {
                var arrays = verification.SurfaceGetArrays(s);
                var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                if (vertices.Length == 0) throw new InvalidDataException("Export verification found an empty surface.");
                foreach (var v in vertices) if (!v.IsFinite()) throw new InvalidDataException("Export verification found non-finite coordinates.");
                triangles += (indices.Length > 0 ? indices.Length : vertices.Length) / 3;
            }
            if (triangles == 0) throw new InvalidDataException("Export verification found no triangles.");

            File.Move(temp, full, true);
            temp = null;
            SetStatus($"Exported validated STL ({triangles:N0} triangles): {full}");
        }
        catch (Exception ex)
        {
            if (temp != null) { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
            SetStatus("STL export failed safely; an existing destination file was not replaced: " + ex.Message);
        }
    }
}
