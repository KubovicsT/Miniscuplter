using Godot;
using System;
using System.IO;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV095LibraryGuards()
    {
        ReplaceV095Button("Import STL to Library", OpenV095ImportPartDialog);
    }

    void OpenV095ImportPartDialog()
    {
        var d = new FileDialog { FileMode = FileDialog.FileModeEnum.OpenFile, Access = FileDialog.AccessEnum.Filesystem, Filters = new[] { "*.stl ; STL mesh" }, UseNativeDialog = true };
        AddChild(d);
        d.FileSelected += p => { SafeV095ImportPartFile(p); d.QueueFree(); };
        d.Canceled += d.QueueFree;
        d.PopupCenteredRatio(.75f);
    }

    void SafeV095ImportPartFile(string source)
    {
        string? temp = null;
        string? dest = null;
        V07PartDefinition? def = null;
        try
        {
            if (!File.Exists(source) || new FileInfo(source).Length == 0) throw new FileNotFoundException("Part STL is missing or empty.", source);
            var validation = MeshIO.LoadStl(source);
            if (validation.GetSurfaceCount() == 0) throw new InvalidDataException("Part STL contains no mesh surfaces.");

            string id = Guid.NewGuid().ToString("N");
            dest = Path.Combine(V07LibraryRoot(), "meshes", id + ".stl");
            temp = dest + ".tmp";
            File.Copy(source, temp, true);
            if (new FileInfo(temp).Length == 0) throw new IOException("Temporary library copy is empty.");
            _ = MeshIO.LoadStl(temp);
            File.Move(temp, dest, true);
            temp = null;

            def = new V07PartDefinition
            {
                Id = id,
                Name = Path.GetFileNameWithoutExtension(source),
                Category = CurrentV07Category() == "All" ? "Generic" : CurrentV07Category(),
                SocketType = CurrentV07SocketType(),
                MeshPath = dest,
                DefaultScale = 1f
            };
            _v07Parts.Add(def);
            try { SaveV095LibraryIndexAtomic(); }
            catch
            {
                _v07Parts.Remove(def);
                if (File.Exists(dest)) File.Delete(dest);
                throw;
            }

            _v07SelectedPartId = id;
            RebuildV07LibraryList();
            SetStatus($"Validated and added {def.Name} to the reusable parts library.");
        }
        catch (Exception ex)
        {
            if (def != null) _v07Parts.Remove(def);
            if (temp != null) { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
            SetStatus("Part-library import failed without changing the library index: " + ex.Message);
        }
    }
}
