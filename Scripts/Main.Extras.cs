using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    bool _regionMode;
    bool _regionDragging;
    Vector2 _regionStart;
    string _lastMask = "";
    TextureRect? _aiPreview;

    public void InstallV01Extras()
    {
        var top = FindChild("HBoxContainer", true, false) as HBoxContainer;
        if (top != null)
        {
            AddButton(top, "Save Project", SaveProjectDialog);
            AddButton(top, "Load Project", LoadProjectDialog);
        }

        var aiPanel = FindChild("AI", true, false) as VBoxContainer;
        if (aiPanel != null)
        {
            var select = new Button { Text = "Select AI Edit Region" };
            select.Pressed += BeginRegionSelection;
            aiPanel.AddChild(select);

            var edit = new Button { Text = "AI Edit Selected Region" };
            edit.Pressed += async () => await AiEditSelectedRegion();
            aiPanel.AddChild(edit);

            var open = new Button { Text = "Open Last 2D Result" };
            open.Pressed += () =>
            {
                string p = string.IsNullOrEmpty(_lastEditedImage) ? _lastCapture : _lastEditedImage;
                if (!string.IsNullOrEmpty(p) && File.Exists(p)) OS.ShellOpen(p);
            };
            aiPanel.AddChild(open);

            aiPanel.AddChild(new Label { Text = "2D Preview" });
            _aiPreview = new TextureRect
            {
                CustomMinimumSize = new Vector2(0, 220),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            aiPanel.AddChild(_aiPreview);
        }

        if (FindChild("ViewportHost", true, false) is SubViewportContainer host)
            host.GuiInput += OnRegionGuiInput;
    }

    void BeginRegionSelection()
    {
        _regionMode = true;
        _regionDragging = false;
        SetStatus("AI region mode: drag a rectangle with the left mouse button over the viewport.");
    }

    void OnRegionGuiInput(InputEvent ev)
    {
        if (!_regionMode) return;
        if (ev is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _regionDragging = true;
                _regionStart = mb.Position;
                GetViewport().SetInputAsHandled();
            }
            else if (_regionDragging)
            {
                _regionDragging = false;
                CreateRegionMask(_regionStart, mb.Position);
                _regionMode = false;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    void CreateRegionMask(Vector2 a, Vector2 b)
    {
        var sub = FindChild("Viewport", true, false) as SubViewport;
        if (sub == null) return;
        var size = sub.GetVisibleRect().Size;
        int w = Math.Max(1, (int)size.X);
        int h = Math.Max(1, (int)size.Y);
        int x0 = Math.Clamp((int)Math.Min(a.X, b.X), 0, w - 1);
        int y0 = Math.Clamp((int)Math.Min(a.Y, b.Y), 0, h - 1);
        int x1 = Math.Clamp((int)Math.Max(a.X, b.X), x0 + 1, w);
        int y1 = Math.Clamp((int)Math.Max(a.Y, b.Y), y0 + 1, h);

        var image = Image.Create(w, h, false, Image.Format.L8);
        image.Fill(Colors.Black);
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                image.SetPixel(x, y, Colors.White);

        Directory.CreateDirectory(ProjectSettings.GlobalizePath("user://masks"));
        _lastMask = ProjectSettings.GlobalizePath($"user://masks/mask_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        image.SavePng(_lastMask);
        SetStatus($"AI edit region selected: {x1 - x0} × {y1 - y0}px.");
    }

    async Task AiEditSelectedRegion()
    {
        if (string.IsNullOrEmpty(_lastMask) || !File.Exists(_lastMask))
        {
            SetStatus("Select an AI edit region first.");
            return;
        }
        CaptureView();
        string prompt = _prompt?.Text.Trim() ?? "";
        if (prompt.Length == 0)
        {
            SetStatus("Describe the desired regional change first.");
            return;
        }
        string outPath = ProjectSettings.GlobalizePath($"user://edit_region_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        await RunAi(async () =>
        {
            _lastEditedImage = await _ai.EditImageAsync(_lastCapture, _lastMask, prompt, outPath);
            ShowAiPreview(_lastEditedImage);
            SetStatus("Regional 2D edit generated. Approve it, regenerate, or generate the 3D part.");
        });
    }

    void ShowAiPreview(string path)
    {
        if (_aiPreview == null || !File.Exists(path)) return;
        var image = Image.LoadFromFile(path);
        if (image == null || image.IsEmpty()) return;
        _aiPreview.Texture = ImageTexture.CreateFromImage(image);
    }

    sealed class ProjectDto
    {
        public int Version { get; set; } = 1;
        public List<ObjectDto> Objects { get; set; } = new();
    }

    sealed class ObjectDto
    {
        public string Name { get; set; } = "Object";
        public string Mesh { get; set; } = "";
        public float[] Position { get; set; } = new float[3];
        public float[] Rotation { get; set; } = new float[3];
        public float[] Scale { get; set; } = new float[] { 1, 1, 1 };
    }

    void SaveProjectDialog()
    {
        var d = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.msculpt ; Miniscuplter projects" },
            CurrentFile = "miniature.msculpt",
            UseNativeDialog = true
        };
        AddChild(d);
        d.FileSelected += p => { SaveProject(p); d.QueueFree(); };
        d.PopupCenteredRatio(.75f);
    }

    void SaveProject(string projectPath)
    {
        try
        {
            string full = Path.GetFullPath(projectPath);
            string dir = Path.Combine(Path.GetDirectoryName(full)!, Path.GetFileNameWithoutExtension(full) + "_assets");
            Directory.CreateDirectory(dir);
            var dto = new ProjectDto();
            for (int i = 0; i < _objects.Count; i++)
            {
                var obj = _objects[i];
                if (obj.Mesh == null) continue;
                string meshFile = $"mesh_{i:000}.stl";
                MeshIO.SaveBinaryStl(obj.Mesh, Path.Combine(dir, meshFile));
                dto.Objects.Add(new ObjectDto
                {
                    Name = obj.Name,
                    Mesh = meshFile,
                    Position = new[] { obj.Position.X, obj.Position.Y, obj.Position.Z },
                    Rotation = new[] { obj.Rotation.X, obj.Rotation.Y, obj.Rotation.Z },
                    Scale = new[] { obj.Scale.X, obj.Scale.Y, obj.Scale.Z }
                });
            }
            File.WriteAllText(full, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
            SetStatus($"Saved project with {dto.Objects.Count} objects: {full}");
        }
        catch (Exception ex) { SetStatus("Project save failed: " + ex.Message); }
    }

    void LoadProjectDialog()
    {
        var d = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.msculpt ; Miniscuplter projects" },
            UseNativeDialog = true
        };
        AddChild(d);
        d.FileSelected += p => { LoadProject(p); d.QueueFree(); };
        d.PopupCenteredRatio(.75f);
    }

    void LoadProject(string projectPath)
    {
        try
        {
            string full = Path.GetFullPath(projectPath);
            var dto = JsonSerializer.Deserialize<ProjectDto>(File.ReadAllText(full)) ?? throw new InvalidDataException("Invalid project file.");
            string dir = Path.Combine(Path.GetDirectoryName(full)!, Path.GetFileNameWithoutExtension(full) + "_assets");
            foreach (var o in _objects) o.QueueFree();
            _objects.Clear(); _selected = null; _undo.Clear(); _redo.Clear();
            foreach (var item in dto.Objects)
            {
                string meshPath = Path.Combine(dir, item.Mesh);
                AddMeshObject(MeshIO.LoadStl(meshPath), item.Name,
                    new Vector3(item.Position[0], item.Position[1], item.Position[2]));
                if (_selected != null)
                {
                    _selected.Rotation = new Vector3(item.Rotation[0], item.Rotation[1], item.Rotation[2]);
                    _selected.Scale = new Vector3(item.Scale[0], item.Scale[1], item.Scale[2]);
                }
            }
            RebuildSceneList();
            FrameSelected();
            SetStatus($"Loaded project with {_objects.Count} objects.");
        }
        catch (Exception ex) { SetStatus("Project load failed: " + ex.Message); }
    }
}
