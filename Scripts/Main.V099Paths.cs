using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Miniscuplter;

public partial class Main
{
    sealed class V099LocationSettings
    {
        public string ProjectDirectory { get; set; } = "";
        public string LibraryDirectory { get; set; } = "";
        public string ExportDirectory { get; set; } = "";
    }

    V099LocationSettings _v099Locations = new();
    LineEdit? _v099ProjectPath, _v099LibraryPath, _v099ExportPath;

    public void InstallV099Locations()
    {
        LoadV099Locations();
        EnsureV099LibraryCompatibilityLink();

        // Replace only the dialogs/default folders. The underlying v0.9.5 transactional
        // project save, strict load/recovery and validated STL export remain authoritative.
        ReplaceV095Button("Save Project", OpenV099ProjectSaveDialog);
        ReplaceV095Button("Load Project", OpenV099ProjectLoadDialog);
        ReplaceV095Button("Export STL", OpenV099SafeExportDialog);

        var tabs = FindChild("TabContainer", true, false) as TabContainer;
        if (tabs == null) return;
        var scroll = new ScrollContainer { Name = "Locations v0.9.9", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(box); tabs.AddChild(scroll);
        box.AddChild(new Label { Text = "FILES & LOCATIONS — v0.9.9", ThemeTypeVariation = "HeaderSmall" });
        box.AddChild(new Label { Text = "These locations are independent from the application and AI model install folders. Changing them does not move the application or downloaded AI models.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        _v099ProjectPath = AddV099LocationRow(box, "Default project location", V099ProjectRoot(), () => PickV099Folder("Choose default project location", p => { _v099Locations.ProjectDirectory = p; SaveV099Locations(); RefreshV099LocationFields(); }));
        _v099LibraryPath = AddV099LocationRow(box, "Reusable model / parts library", V099LibraryRoot(), () => PickV099Folder("Choose model library location", ChangeV099LibraryLocation));
        _v099ExportPath = AddV099LocationRow(box, "Default STL export location", V099ExportRoot(), () => PickV099Folder("Choose STL export location", p => { _v099Locations.ExportDirectory = p; SaveV099Locations(); RefreshV099LocationFields(); }));

        var reset = new Button { Text = "Reset to default locations" };
        reset.Pressed += () =>
        {
            string oldLibrary = V099LibraryRoot();
            _v099Locations = V099Defaults();
            CopyV099Directory(oldLibrary, _v099Locations.LibraryDirectory, overwrite: false);
            SaveV099Locations(); EnsureV099LibraryCompatibilityLink(); RefreshV099LocationFields(); LoadV07Library(); RebuildV07LibraryList();
            SetStatus("File locations reset to defaults. Existing library files were preserved/copy-migrated where possible.");
        };
        box.AddChild(reset);
        box.AddChild(new HSeparator());
        box.AddChild(new Label { Text = "Application + AI model storage is selected during installation and managed by Miniscuplter Launcher. Projects, your reusable model library and exported STL files may live on completely different drives.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
    }

    void OpenV099ProjectSaveDialog()
    {
        var d = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.msculpt ; Miniscuplter projects" },
            CurrentDir = V099ProjectRoot(), CurrentFile = "miniature.msculpt", UseNativeDialog = true
        };
        AddChild(d); d.FileSelected += p => { SafeV095SaveProject(p); d.QueueFree(); }; d.Canceled += d.QueueFree; d.PopupCenteredRatio(.75f);
    }

    void OpenV099ProjectLoadDialog()
    {
        var d = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.msculpt ; Miniscuplter projects" },
            CurrentDir = V099ProjectRoot(), UseNativeDialog = true
        };
        AddChild(d); d.FileSelected += p => { StrictV095LoadProject(p); d.QueueFree(); }; d.Canceled += d.QueueFree; d.PopupCenteredRatio(.75f);
    }

    void OpenV099SafeExportDialog()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a mesh first."); return; }
        var d = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.stl ; STL meshes" },
            CurrentDir = V099ExportRoot(), CurrentFile = _selected.Name + ".stl", UseNativeDialog = true
        };
        AddChild(d); d.FileSelected += p => { SafeV095ExportStl(p); d.QueueFree(); }; d.Canceled += d.QueueFree; d.PopupCenteredRatio(.75f);
    }

    LineEdit AddV099LocationRow(Container parent, string label, string value, Action browse)
    {
        parent.AddChild(new Label { Text = label });
        var row = new HBoxContainer();
        var edit = new LineEdit { Text = value, Editable = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var choose = new Button { Text = "Browse…" }; choose.Pressed += browse;
        var open = new Button { Text = "Open" }; open.Pressed += () => { string p = edit.Text; if (!string.IsNullOrWhiteSpace(p)) { Directory.CreateDirectory(p); OS.ShellOpen(p); } };
        row.AddChild(edit); row.AddChild(choose); row.AddChild(open); parent.AddChild(row); return edit;
    }

    void PickV099Folder(string title, Action<string> accepted)
    {
        var d = new FileDialog { Title = title, FileMode = FileDialog.FileModeEnum.OpenDir, Access = FileDialog.AccessEnum.Filesystem, UseNativeDialog = true };
        AddChild(d);
        d.DirSelected += p => { try { accepted(V099NormalizeDirectory(p)); } finally { d.QueueFree(); } };
        d.Canceled += d.QueueFree; d.PopupCenteredRatio(.75f);
    }

    string V099LocationsPath() => ProjectSettings.GlobalizePath("user://locations_v099.json");

    V099LocationSettings V099Defaults()
    {
        string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(docs)) docs = ProjectSettings.GlobalizePath("user://");
        string baseDir = Path.Combine(docs, "Miniscuplter");
        return new V099LocationSettings
        {
            ProjectDirectory = Path.Combine(baseDir, "Projects"),
            LibraryDirectory = Path.Combine(baseDir, "Model Library"),
            ExportDirectory = Path.Combine(baseDir, "Exports")
        };
    }

    void LoadV099Locations()
    {
        var defaults = V099Defaults();
        try
        {
            if (File.Exists(V099LocationsPath()))
                _v099Locations = JsonSerializer.Deserialize<V099LocationSettings>(File.ReadAllText(V099LocationsPath())) ?? defaults;
            else _v099Locations = defaults;
        }
        catch { _v099Locations = defaults; }
        if (string.IsNullOrWhiteSpace(_v099Locations.ProjectDirectory)) _v099Locations.ProjectDirectory = defaults.ProjectDirectory;
        if (string.IsNullOrWhiteSpace(_v099Locations.LibraryDirectory)) _v099Locations.LibraryDirectory = defaults.LibraryDirectory;
        if (string.IsNullOrWhiteSpace(_v099Locations.ExportDirectory)) _v099Locations.ExportDirectory = defaults.ExportDirectory;
        _v099Locations.ProjectDirectory = V099NormalizeDirectory(_v099Locations.ProjectDirectory);
        _v099Locations.LibraryDirectory = V099NormalizeDirectory(_v099Locations.LibraryDirectory);
        _v099Locations.ExportDirectory = V099NormalizeDirectory(_v099Locations.ExportDirectory);
        Directory.CreateDirectory(_v099Locations.ProjectDirectory); Directory.CreateDirectory(_v099Locations.LibraryDirectory); Directory.CreateDirectory(_v099Locations.ExportDirectory);
    }

    void SaveV099Locations()
    {
        try
        {
            File.WriteAllText(V099LocationsPath(), JsonSerializer.Serialize(_v099Locations, new JsonSerializerOptions { WriteIndented = true }));
            Directory.CreateDirectory(V099ProjectRoot()); Directory.CreateDirectory(V099LibraryRoot()); Directory.CreateDirectory(V099ExportRoot());
        }
        catch (Exception ex) { SetStatus("Could not save file locations: " + ex.Message); }
    }

    void ChangeV099LibraryLocation(string newPath)
    {
        string oldPath = V099LibraryRoot();
        newPath = V099NormalizeDirectory(newPath);
        if (Path.GetFullPath(oldPath).Equals(Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            CopyV099Directory(oldPath, newPath, overwrite: false);
            _v099Locations.LibraryDirectory = newPath; SaveV099Locations(); EnsureV099LibraryCompatibilityLink();
            RefreshV099LocationFields(); LoadV07Library(); RebuildV07LibraryList();
            SetStatus("Model library location changed. Existing library files were copied where missing; the previous folder was left intact as a safety copy.");
        }
        catch (Exception ex) { SetStatus("Could not change model library location; the previous library remains active: " + ex.Message); }
    }

    void EnsureV099LibraryCompatibilityLink()
    {
        // v0.7-v0.9.8 stored the reusable parts library at user://parts_library. Preserve
        // compatibility by making that old path a directory link/junction to the new user-selected
        // physical library location. This keeps every existing library call using the configured folder.
        string legacy = Path.GetFullPath(ProjectSettings.GlobalizePath("user://parts_library"));
        string target = Path.GetFullPath(V099LibraryRoot());
        if (legacy.Equals(target, StringComparison.OrdinalIgnoreCase)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(legacy)!); Directory.CreateDirectory(target);
        try
        {
            if (Directory.Exists(legacy))
            {
                var attrs = File.GetAttributes(legacy);
                bool link = (attrs & FileAttributes.ReparsePoint) != 0;
                if (link) Directory.Delete(legacy);
                else
                {
                    CopyV099Directory(legacy, target, overwrite: false);
                    string backup = legacy + ".pre_v099_backup";
                    if (!Directory.Exists(backup)) Directory.Move(legacy, backup);
                    else Directory.Delete(legacy, true);
                }
            }
            if (OperatingSystem.IsWindows())
            {
                var psi = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                psi.ArgumentList.Add("/c"); psi.ArgumentList.Add("mklink"); psi.ArgumentList.Add("/J"); psi.ArgumentList.Add(legacy); psi.ArgumentList.Add(target);
                using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start mklink.");
                p.WaitForExit(); if (p.ExitCode != 0) throw new IOException(p.StandardError.ReadToEnd());
            }
            else Directory.CreateSymbolicLink(legacy, target);
        }
        catch (Exception ex)
        {
            // Never delete/corrupt the configured target if linking fails. The backup remains available.
            SetStatus("Model library path is configured, but the compatibility link could not be created: " + ex.Message);
        }
    }

    static void CopyV099Directory(string source, string destination, bool overwrite)
    {
        if (!Directory.Exists(source)) { Directory.CreateDirectory(destination); return; }
        Directory.CreateDirectory(destination);
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, dir)));
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string dest = Path.Combine(destination, Path.GetRelativePath(source, file)); Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (overwrite || !File.Exists(dest)) File.Copy(file, dest, overwrite);
        }
    }

    void RefreshV099LocationFields()
    {
        if (_v099ProjectPath != null) _v099ProjectPath.Text = V099ProjectRoot();
        if (_v099LibraryPath != null) _v099LibraryPath.Text = V099LibraryRoot();
        if (_v099ExportPath != null) _v099ExportPath.Text = V099ExportRoot();
    }

    static string V099NormalizeDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Directory cannot be empty.");
        string full = Path.GetFullPath(path.Trim()); Directory.CreateDirectory(full); return full;
    }

    string V099ProjectRoot() { if (string.IsNullOrWhiteSpace(_v099Locations.ProjectDirectory)) LoadV099Locations(); Directory.CreateDirectory(_v099Locations.ProjectDirectory); return _v099Locations.ProjectDirectory; }
    string V099LibraryRoot() { if (string.IsNullOrWhiteSpace(_v099Locations.LibraryDirectory)) LoadV099Locations(); Directory.CreateDirectory(_v099Locations.LibraryDirectory); Directory.CreateDirectory(Path.Combine(_v099Locations.LibraryDirectory, "meshes")); return _v099Locations.LibraryDirectory; }
    string V099ExportRoot() { if (string.IsNullOrWhiteSpace(_v099Locations.ExportDirectory)) LoadV099Locations(); Directory.CreateDirectory(_v099Locations.ExportDirectory); return _v099Locations.ExportDirectory; }
}
