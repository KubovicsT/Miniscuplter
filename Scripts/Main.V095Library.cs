using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Miniscuplter;

public partial class Main
{
    Timer? _v095LibraryGuardTimer;
    DateTime _v095LibrarySeenWriteUtc;

    public void InstallV095LibraryGuards()
    {
        ReplaceV095Button("Import STL to Library", OpenV095ImportPartDialog);
        RecoverV095LibraryIndexIfNeeded();
        _v095LibraryGuardTimer = new Timer { WaitTime = .5, OneShot = false, Autostart = true };
        _v095LibraryGuardTimer.Timeout += GuardV095LibraryIndex;
        AddChild(_v095LibraryGuardTimer);
    }

    static bool V095LibraryJsonValid(string path)
    {
        try
        {
            if (!File.Exists(path)) return true;
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return false;
            return JsonSerializer.Deserialize<List<V07PartDefinition>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) != null;
        }
        catch { return false; }
    }

    void RecoverV095LibraryIndexIfNeeded()
    {
        string path = V07LibraryIndex();
        string backup = path + ".bak";
        try
        {
            if (File.Exists(path) && V095LibraryJsonValid(path))
            {
                File.Copy(path, backup, true);
                _v095LibrarySeenWriteUtc = File.GetLastWriteTimeUtc(path);
                return;
            }
            if (File.Exists(backup) && V095LibraryJsonValid(backup))
            {
                File.Copy(backup, path, true);
                _v095LibrarySeenWriteUtc = File.GetLastWriteTimeUtc(path);
                LoadV07Library();
                RebuildV07LibraryList();
                SetStatus("Recovered the reusable parts library from its last validated index backup.");
            }
        }
        catch (Exception ex) { SetStatus("Parts-library recovery check failed: " + ex.Message); }
    }

    void GuardV095LibraryIndex()
    {
        string path = V07LibraryIndex();
        if (!File.Exists(path)) return;
        DateTime write = File.GetLastWriteTimeUtc(path);
        if (write == _v095LibrarySeenWriteUtc) return;
        _v095LibrarySeenWriteUtc = write;
        try
        {
            if (V095LibraryJsonValid(path))
            {
                File.Copy(path, path + ".bak", true);
            }
            else
            {
                string backup = path + ".bak";
                if (!File.Exists(backup) || !V095LibraryJsonValid(backup)) throw new InvalidDataException("parts.json is invalid and no valid backup is available.");
                File.Copy(backup, path, true);
                _v095LibrarySeenWriteUtc = File.GetLastWriteTimeUtc(path);
                LoadV07Library();
                RebuildV07LibraryList();
                SetStatus("Invalid parts-library index detected and automatically restored from the last validated backup.");
            }
        }
        catch (Exception ex) { SetStatus("Parts-library integrity guard: " + ex.Message); }
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
            try
            {
                SaveV095LibraryIndexAtomic();
                if (V095LibraryJsonValid(V07LibraryIndex())) File.Copy(V07LibraryIndex(), V07LibraryIndex() + ".bak", true);
            }
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
