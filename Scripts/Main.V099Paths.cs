using Godot;
using System;
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
        var tabs = FindChild("TabContainer", true, false) as TabContainer;
        if (tabs == null) return;
        var scroll = new ScrollContainer { Name = "Locations v0.9.9", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(box); tabs.AddChild(scroll);
        box.AddChild(new Label { Text = "FILES & LOCATIONS — v0.9.9", ThemeTypeVariation = "HeaderSmall" });
        box.AddChild(new Label { Text = "These locations are independent from the application and AI model install folders. Changing them does not move the application or downloaded AI models.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        _v099ProjectPath = AddV099LocationRow(box, "Default project location", V099ProjectRoot(), () => PickV099Folder("Choose default project location", p => { _v099Locations.ProjectDirectory = p; SaveV099Locations(); RefreshV099LocationFields(); }));
        _v099LibraryPath = AddV099LocationRow(box, "Reusable model / parts library", V099LibraryRoot(), () => PickV099Folder("Choose model library location", p => { _v099Locations.LibraryDirectory = p; SaveV099Locations(); RefreshV099LocationFields(); LoadV07Library(); RebuildV07LibraryList(); }));
        _v099ExportPath = AddV099LocationRow(box, "Default STL export location", V099ExportRoot(), () => PickV099Folder("Choose STL export location", p => { _v099Locations.ExportDirectory = p; SaveV099Locations(); RefreshV099LocationFields(); }));

        var reset = new Button { Text = "Reset to default locations" };
        reset.Pressed += () => { _v099Locations = V099Defaults(); SaveV099Locations(); RefreshV099LocationFields(); LoadV07Library(); RebuildV07LibraryList(); SetStatus("File locations reset to defaults."); };
        box.AddChild(reset);
        box.AddChild(new HSeparator());
        box.AddChild(new Label { Text = "Application + AI model storage is selected during installation and managed by Miniscuplter Launcher. Projects, your reusable model library and exported STL files may live on completely different drives.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
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
