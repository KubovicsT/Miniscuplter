using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

/// <summary>
/// A real 2D workspace canvas. It displays the current source image in the center workspace and
/// converts a dragged rectangle into image-pixel coordinates, so image editing never depends on
/// a viewport capture or on the 3D camera.
/// </summary>
public sealed partial class V1015ImageCanvas : Control
{
    ImageTexture? _texture;
    Vector2I _imageSize;
    bool _dragging;
    Vector2 _dragStart;
    Vector2 _dragEnd;

    public string SourcePath { get; private set; } = "";
    public Rect2I? SelectionPixels { get; private set; }
    public bool HasImage => _texture != null && _imageSize.X > 0 && _imageSize.Y > 0;
    public event Action? SelectionChanged;

    public V1015ImageCanvas()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        ClipContents = true;
    }

    public bool LoadSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            SourcePath = "";
            _texture = null;
            _imageSize = Vector2I.Zero;
            ClearSelection();
            QueueRedraw();
            return false;
        }

        var image = Image.LoadFromFile(path);
        if (image == null || image.IsEmpty()) return false;
        SourcePath = Path.GetFullPath(path);
        _imageSize = new Vector2I(image.GetWidth(), image.GetHeight());
        _texture = ImageTexture.CreateFromImage(image);
        ClearSelection();
        QueueRedraw();
        return true;
    }

    public void ClearSelection()
    {
        _dragging = false;
        SelectionPixels = null;
        QueueRedraw();
        SelectionChanged?.Invoke();
    }

    Rect2 ImageRect()
    {
        if (!HasImage || Size.X <= 1 || Size.Y <= 1) return new Rect2();
        float scale = Math.Min(Size.X / _imageSize.X, Size.Y / _imageSize.Y);
        Vector2 draw = new(_imageSize.X * scale, _imageSize.Y * scale);
        return new Rect2((Size - draw) * .5f, draw);
    }

    Vector2 ClampToImage(Vector2 p)
    {
        Rect2 r = ImageRect();
        return new Vector2(Math.Clamp(p.X, r.Position.X, r.End.X), Math.Clamp(p.Y, r.Position.Y, r.End.Y));
    }

    Rect2 LocalSelectionRect()
    {
        Vector2 a = ClampToImage(_dragStart);
        Vector2 b = ClampToImage(_dragEnd);
        Vector2 pos = new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        Vector2 end = new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        return new Rect2(pos, end - pos);
    }

    Rect2I ToPixels(Rect2 local)
    {
        Rect2 r = ImageRect();
        if (r.Size.X <= 0 || r.Size.Y <= 0) return new Rect2I();
        float sx = _imageSize.X / r.Size.X;
        float sy = _imageSize.Y / r.Size.Y;
        int x0 = Math.Clamp((int)Math.Floor((local.Position.X - r.Position.X) * sx), 0, Math.Max(0, _imageSize.X - 1));
        int y0 = Math.Clamp((int)Math.Floor((local.Position.Y - r.Position.Y) * sy), 0, Math.Max(0, _imageSize.Y - 1));
        int x1 = Math.Clamp((int)Math.Ceiling((local.End.X - r.Position.X) * sx), x0 + 1, _imageSize.X);
        int y1 = Math.Clamp((int)Math.Ceiling((local.End.Y - r.Position.Y) * sy), y0 + 1, _imageSize.Y);
        return new Rect2I(x0, y0, x1 - x0, y1 - y0);
    }

    Rect2 PixelSelectionToLocal(Rect2I pixels)
    {
        Rect2 r = ImageRect();
        if (r.Size.X <= 0 || r.Size.Y <= 0) return new Rect2();
        Vector2 scale = new(r.Size.X / _imageSize.X, r.Size.Y / _imageSize.Y);
        return new Rect2(r.Position + new Vector2(pixels.Position.X * scale.X, pixels.Position.Y * scale.Y),
            new Vector2(pixels.Size.X * scale.X, pixels.Size.Y * scale.Y));
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!HasImage) return;
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                if (!ImageRect().HasPoint(mb.Position)) return;
                _dragging = true;
                _dragStart = ClampToImage(mb.Position);
                _dragEnd = _dragStart;
                GrabFocus();
                AcceptEvent();
                QueueRedraw();
            }
            else if (_dragging)
            {
                _dragging = false;
                _dragEnd = ClampToImage(mb.Position);
                Rect2 local = LocalSelectionRect();
                SelectionPixels = local.Size.X >= 3 && local.Size.Y >= 3 ? ToPixels(local) : null;
                AcceptEvent();
                QueueRedraw();
                SelectionChanged?.Invoke();
            }
        }
        else if (@event is InputEventMouseMotion mm && _dragging)
        {
            _dragEnd = ClampToImage(mm.Position);
            AcceptEvent();
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!HasImage) return; // Keep the 3D floor visible behind the empty 2D workspace on launch.
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(.025f, .029f, .036f), true);
        Rect2 imageRect = ImageRect();
        DrawTextureRect(_texture!, imageRect, false, Colors.White);
        DrawRect(imageRect, new Color(.55f, .58f, .64f), false, 1f);

        Rect2 selection = _dragging ? LocalSelectionRect() : SelectionPixels is Rect2I px ? PixelSelectionToLocal(px) : new Rect2();
        if (selection.Size.X > 0 && selection.Size.Y > 0)
        {
            DrawRect(selection, new Color(.20f, .55f, 1f, .18f), true);
            DrawRect(selection, new Color(.32f, .68f, 1f), false, 2f);
        }
    }
}

public partial class Main
{
    sealed record V1015Reference(string Title, string PageUrl, string ImageUrl, string ThumbnailUrl);

    V1015ImageCanvas? _v1015ImageCanvas;
    Label? _v1015CanvasHint;
    Label? _v1015EditStatus;
    Label? _v1015ReferenceStatus;
    VBoxContainer? _v1015ReferenceResults;
    TabContainer? _v1015WorkflowTabs;

    public void InstallV1015ThinSlice()
    {
        InstallV1015CenterWorkspace();
        InstallV1015ImageEditing();
        InstallV1015ReferenceBrowser();
        RebuildV1015ReliableGrid();
        HideV1015Obsolete2DControls();
        UpdateV1015WorkspaceForTab(_v1015WorkflowTabs?.CurrentTab ?? 0);
    }

    void InstallV1015CenterWorkspace()
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host) return;
        _v1015WorkflowTabs = (host.GetParent() as HSplitContainer)?.GetChildren().OfType<TabContainer>().FirstOrDefault();

        _v1015ImageCanvas = new V1015ImageCanvas
        {
            Name = "2D Image Workspace",
            Visible = false,
            ZIndex = 20
        };
        _v1015ImageCanvas.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        host.AddChild(_v1015ImageCanvas);
        _v1015ImageCanvas.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _v1015ImageCanvas.SelectionChanged += UpdateV1015SelectionStatus;

        _v1015CanvasHint = new Label
        {
            Text = "2D WORKSPACE\nGenerate a concept, load your own image, or choose an internet reference.\nThe image will appear here; drag directly over it to select an AI-edit region.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 21
        };
        host.AddChild(_v1015CanvasHint);
        _v1015CanvasHint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        if (_v1015WorkflowTabs != null)
            _v1015WorkflowTabs.TabChanged += UpdateV1015WorkspaceForTab;

        string current = CurrentV1015ImageSource();
        if (!string.IsNullOrWhiteSpace(current)) SyncV1015CanvasSource(current);
    }

    void UpdateV1015WorkspaceForTab(long tab)
    {
        if (_v1015WorkflowTabs == null || _v1015ImageCanvas == null) return;
        int i = Math.Clamp((int)tab, 0, Math.Max(0, _v1015WorkflowTabs.GetTabCount() - 1));
        string title = _v1015WorkflowTabs.GetTabCount() > 0 ? _v1015WorkflowTabs.GetTabTitle(i) : "";
        bool twoD = title.Equals("2D", StringComparison.OrdinalIgnoreCase);
        _v1015ImageCanvas.Visible = twoD;
        if (_v1015CanvasHint != null) _v1015CanvasHint.Visible = twoD && !_v1015ImageCanvas.HasImage;
        if (!twoD && _v109GridRoot != null) _v109GridRoot.Visible = true;
    }

    internal void SyncV1015CanvasSource(string path)
    {
        if (_v1015ImageCanvas == null) return;
        bool loaded = _v1015ImageCanvas.LoadSource(path);
        if (_v1015CanvasHint != null) _v1015CanvasHint.Visible = _v1015ImageCanvas.Visible && !loaded;
        if (_v1015EditStatus != null && loaded)
            _v1015EditStatus.Text = $"2D source: {Path.GetFileName(path)} · drag over the image to select a region.";
    }

    string CurrentV1015ImageSource()
    {
        if (!string.IsNullOrWhiteSpace(_lastEditedImage) && File.Exists(_lastEditedImage)) return _lastEditedImage;
        if (!string.IsNullOrWhiteSpace(_v03StartingImage) && File.Exists(_v03StartingImage)) return _v03StartingImage;
        string approved = V1011Approved2DSource();
        if (!string.IsNullOrWhiteSpace(approved)) return approved;
        if (!string.IsNullOrWhiteSpace(_lastCapture) && File.Exists(_lastCapture)) return _lastCapture;
        return "";
    }

    void InstallV1015ImageEditing()
    {
        if (FindChild("2D", true, false) is not VBoxContainer twoD) return;
        var section = new VBoxContainer { Name = "2D Canvas Editing" };
        section.AddChild(Heading("EDIT CURRENT 2D IMAGE"));
        section.AddChild(new Label
        {
            Text = "The center workspace is the actual 2D image. Drag a rectangle over the part you want to change, describe the change in Prompt, then run the regional edit. The 3D viewport is not used to create this mask.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        var row = new HBoxContainer();
        var select = new Button { Text = "Select Edit Region", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        select.Pressed += () =>
        {
            if (_v1015ImageCanvas == null || !_v1015ImageCanvas.HasImage) SetStatus("Generate or load a 2D image first.");
            else SetStatus("Drag a rectangle directly over the image in the center workspace.");
        };
        var clear = new Button { Text = "Clear Selection" };
        clear.Pressed += () => _v1015ImageCanvas?.ClearSelection();
        row.AddChild(select); row.AddChild(clear); section.AddChild(row);
        var editRegion = new Button { Text = "AI Edit Selected 2D Region" };
        editRegion.Pressed += async () => await RunV1015ImageEditAsync(regional: true); section.AddChild(editRegion);
        var editWhole = new Button { Text = "AI Edit Whole 2D Image" };
        editWhole.Pressed += async () => await RunV1015ImageEditAsync(regional: false); section.AddChild(editWhole);
        _v1015EditStatus = new Label { Text = "2D source: generate or load an image first.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        section.AddChild(_v1015EditStatus);
        section.AddChild(new HSeparator());
        twoD.AddChild(section);
        twoD.MoveChild(section, Math.Min(5, twoD.GetChildCount() - 1));

        string current = CurrentV1015ImageSource();
        if (!string.IsNullOrWhiteSpace(current)) SyncV1015CanvasSource(current);
    }

    void UpdateV1015SelectionStatus()
    {
        if (_v1015EditStatus == null || _v1015ImageCanvas == null) return;
        if (_v1015ImageCanvas.SelectionPixels is Rect2I r)
            _v1015EditStatus.Text = $"Selected region: {r.Size.X} × {r.Size.Y} px at ({r.Position.X}, {r.Position.Y}). Enter the desired change in Prompt.";
        else if (_v1015ImageCanvas.HasImage)
            _v1015EditStatus.Text = "No region selected. Drag over the image, or use AI Edit Whole 2D Image.";
    }

    async Task RunV1015ImageEditAsync(bool regional)
    {
        string source = CurrentV1015ImageSource();
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source)) { SetStatus("Generate, load, or choose a 2D source image first."); return; }
        string prompt = _prompt?.Text.Trim() ?? "";
        if (prompt.Length == 0) { SetStatus("Describe the desired image change in Prompt first."); return; }

        string? mask = null;
        if (regional)
        {
            if (_v1015ImageCanvas?.SelectionPixels is not Rect2I selected) { SetStatus("Drag a region over the 2D image first."); return; }
            var sourceImage = Image.LoadFromFile(source);
            if (sourceImage == null || sourceImage.IsEmpty()) { SetStatus("The current 2D source could not be decoded."); return; }
            var maskImage = Image.CreateEmpty(sourceImage.GetWidth(), sourceImage.GetHeight(), false, Image.Format.L8);
            maskImage.Fill(Colors.Black);
            int x0 = Math.Clamp(selected.Position.X, 0, sourceImage.GetWidth() - 1);
            int y0 = Math.Clamp(selected.Position.Y, 0, sourceImage.GetHeight() - 1);
            int x1 = Math.Clamp(selected.End.X, x0 + 1, sourceImage.GetWidth());
            int y1 = Math.Clamp(selected.End.Y, y0 + 1, sourceImage.GetHeight());
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) maskImage.SetPixel(x, y, Colors.White);
            string maskDir = ProjectSettings.GlobalizePath("user://masks"); Directory.CreateDirectory(maskDir);
            mask = Path.Combine(maskDir, $"image_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            if (maskImage.SavePng(mask) != Error.Ok) { SetStatus("Could not save the 2D edit mask."); return; }
        }

        string output = ProjectSettings.GlobalizePath($"user://image_edit_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        await RunAi(async () =>
        {
            _lastEditedImage = await _ai.EditImageAsync(source, mask, prompt, output);
            ShowAiPreview(_lastEditedImage);
            SyncV1015CanvasSource(_lastEditedImage);
            _v1015ImageCanvas?.ClearSelection();
            if (_v1015EditStatus != null) _v1015EditStatus.Text = "AI edit generated. Review it in the center canvas; accept it as the baseline only when satisfied.";
            SetStatus("2D AI edit completed. Review the result, edit again if needed, then Accept Current Image as Baseline.");
        });
    }

    void InstallV1015ReferenceBrowser()
    {
        if (FindChild("2D", true, false) is not VBoxContainer twoD) return;
        if (_referenceList?.GetParent() is Control oldScroll) oldScroll.Visible = false;
        if (_v109ReferenceStatus != null) _v109ReferenceStatus.Visible = false;

        foreach (var b in DescendantsV1015<Button>(twoD).Where(b => b.Text == "Search references from prompt").ToList()) b.Visible = false;

        var section = new VBoxContainer { Name = "Reference Image Browser" };
        section.AddChild(Heading("REFERENCE IMAGES"));
        section.AddChild(new Label
        {
            Text = "Search Wikimedia Commons and choose a visible image as your 2D source. Results stay separate from local AI generation until you explicitly choose one.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        var search = new Button { Text = "Search Reference Images" };
        search.Pressed += async () => await SearchV1015ReferencesAsync(); section.AddChild(search);
        _v1015ReferenceStatus = new Label { Text = "Reference search: ready", AutowrapMode = TextServer.AutowrapMode.WordSmart }; section.AddChild(_v1015ReferenceStatus);
        _v1015ReferenceResults = new VBoxContainer(); section.AddChild(_v1015ReferenceResults);
        section.AddChild(new HSeparator());
        twoD.AddChild(section);
    }

    async Task SearchV1015ReferencesAsync()
    {
        string query = _prompt?.Text.Trim() ?? "";
        if (query.Length == 0) { if (_v1015ReferenceStatus != null) _v1015ReferenceStatus.Text = "Reference search: enter search terms in Prompt first."; return; }
        if (_internetToggle != null && !_internetToggle.ButtonPressed) { if (_v1015ReferenceStatus != null) _v1015ReferenceStatus.Text = "Reference search: internet access is disabled."; return; }
        if (_v1015ReferenceResults == null) return;
        foreach (var c in _v1015ReferenceResults.GetChildren()) c.QueueFree();

        try
        {
            _v1015ReferenceStatus!.Text = "Reference search: loading visible image results…";
            string url = "https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch=" + Uri.EscapeDataString(query)
                + "&gsrnamespace=6&gsrlimit=6&prop=imageinfo&iiprop=url%7Cmime&iiurlwidth=1024&format=json";
            using var response = await V109ReferenceHttp.GetAsync(url);
            string body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Wikimedia Commons returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
            using var doc = JsonDocument.Parse(body);
            var results = new List<V1015Reference>();
            if (doc.RootElement.TryGetProperty("query", out var q) && q.TryGetProperty("pages", out var pages))
            {
                foreach (var page in pages.EnumerateObject())
                {
                    var e = page.Value;
                    if (!e.TryGetProperty("imageinfo", out var ii) || ii.GetArrayLength() == 0) continue;
                    var info = ii[0];
                    string title = e.TryGetProperty("title", out var t) ? t.GetString() ?? "Reference" : "Reference";
                    string original = info.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                    string thumb = info.TryGetProperty("thumburl", out var tu) ? tu.GetString() ?? original : original;
                    string pageUrl = info.TryGetProperty("descriptionurl", out var du) ? du.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(pageUrl)) pageUrl = "https://commons.wikimedia.org/wiki/" + Uri.EscapeDataString(title.Replace(' ', '_'));
                    if (!string.IsNullOrWhiteSpace(thumb)) results.Add(new V1015Reference(title, pageUrl, string.IsNullOrWhiteSpace(thumb) ? original : thumb, thumb));
                }
            }

            foreach (var item in results) await AddV1015ReferenceCardAsync(item);
            _v1015ReferenceStatus.Text = results.Count == 0 ? "Reference search: no usable image results." : $"Reference search: {results.Count} visible result(s). Choose Use as 2D Source to work with one.";
        }
        catch (Exception ex)
        {
            if (_v1015ReferenceStatus != null) _v1015ReferenceStatus.Text = "Reference search FAILED: " + ex.Message;
            V109ShowError("Internet reference search failed", ex.Message);
        }
    }

    async Task AddV1015ReferenceCardAsync(V1015Reference item)
    {
        if (_v1015ReferenceResults == null) return;
        var card = new VBoxContainer();
        card.AddChild(new Label { Text = item.Title, AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var preview = new TextureRect
        {
            CustomMinimumSize = new Vector2(0, 150),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        card.AddChild(preview);
        var row = new HBoxContainer();
        var use = new Button { Text = "Use as 2D Source", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        use.Pressed += async () => await UseV1015ReferenceAsync(item);
        var open = new Button { Text = "Open Source" }; open.Pressed += () => OS.ShellOpen(item.PageUrl);
        row.AddChild(use); row.AddChild(open); card.AddChild(row);
        card.AddChild(new HSeparator());
        _v1015ReferenceResults.AddChild(card);

        try
        {
            string cached = await DownloadV1015ReferenceAsync(item.ThumbnailUrl, "thumb");
            var img = Image.LoadFromFile(cached);
            if (img != null && !img.IsEmpty()) preview.Texture = ImageTexture.CreateFromImage(img);
        }
        catch (Exception ex)
        {
            preview.TooltipText = "Thumbnail could not be loaded: " + ex.Message;
        }
    }

    async Task UseV1015ReferenceAsync(V1015Reference item)
    {
        try
        {
            if (_v1015ReferenceStatus != null) _v1015ReferenceStatus.Text = "Downloading selected reference image…";
            string cached = await DownloadV1015ReferenceAsync(item.ImageUrl, "source");
            SetStartingImage(cached); // normalizes/copies the image into the local 2D source workspace.
            string current = CurrentV1015ImageSource();
            if (!string.IsNullOrWhiteSpace(current)) SyncV1015CanvasSource(current);
            if (_v1015ReferenceStatus != null) _v1015ReferenceStatus.Text = "Selected reference is now the current 2D source. Review/edit it, then accept it as baseline when ready.";
        }
        catch (Exception ex)
        {
            if (_v1015ReferenceStatus != null) _v1015ReferenceStatus.Text = "Could not use reference: " + ex.Message;
            V109ShowError("Reference image", ex.Message);
        }
    }

    async Task<string> DownloadV1015ReferenceAsync(string url, string prefix)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Reference result has no downloadable image URL.");
        using var response = await V109ReferenceHttp.GetAsync(url);
        response.EnsureSuccessStatusCode();
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        if (bytes.Length == 0) throw new InvalidDataException("Reference server returned an empty image.");
        string media = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
        string ext = media.Contains("png") ? ".png" : media.Contains("webp") ? ".webp" : ".jpg";
        string dir = ProjectSettings.GlobalizePath("user://reference_cache"); Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"{prefix}_{Guid.NewGuid():N}{ext}");
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    void HideV1015Obsolete2DControls()
    {
        if (FindChild("2D", true, false) is not VBoxContainer twoD) return;
        string[] obsolete = { "Capture → 2D AI Edit", "Select AI Edit Region", "AI Edit Selected Region" };
        foreach (var b in DescendantsV1015<Button>(twoD))
            if (obsolete.Contains(b.Text)) b.Visible = false;
    }

    static IEnumerable<T> DescendantsV1015<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T typed) yield return typed;
            foreach (var nested in DescendantsV1015<T>(child)) yield return nested;
        }
    }

    void RebuildV1015ReliableGrid()
    {
        if (_world == null) return;
        if (_v109GridRoot != null && IsInstanceValid(_v109GridRoot)) _v109GridRoot.QueueFree();
        _v109GridRoot = new Node3D { Name = "Viewport Grid" };
        _world.AddChild(_v109GridRoot);

        var ground = new MeshInstance3D
        {
            Name = "Grid ground",
            Mesh = new PlaneMesh { Size = new Vector2(500, 500) },
            Position = new Vector3(0, -.03f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(.055f, .061f, .073f), Roughness = 1f }
        };
        _v109GridRoot.AddChild(ground);
        AddV1015GridBars(10f, .16f, new Color(.30f, .34f, .40f), false);
        AddV1015GridBars(50f, .42f, new Color(.52f, .56f, .64f), true);
        AddV1015AxisBar(true, .72f, new Color(.92f, .28f, .24f));
        AddV1015AxisBar(false, .72f, new Color(.25f, .55f, 1f));

        if (FindChild("Viewport", true, false) is SubViewport sub)
        {
            sub.TransparentBg = false;
            sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            if (FindChild("ViewportHost", true, false) is SubViewportContainer host)
            {
                host.Stretch = true;
                Vector2 s = host.Size;
                sub.Size = new Vector2I(Math.Max(1, (int)Math.Round(s.X)), Math.Max(1, (int)Math.Round(s.Y)));
            }
        }
        if (_camera != null) { _camera.Current = true; UpdateCamera(); }
        if (_world.GetChildren().OfType<WorldEnvironment>().FirstOrDefault()?.Environment is Godot.Environment env)
        {
            env.BackgroundMode = Godot.Environment.BGMode.Color;
            env.BackgroundColor = new Color(.025f, .030f, .039f);
            env.AmbientLightSource = Godot.Environment.AmbientSource.Color;
            env.AmbientLightColor = new Color(.48f, .50f, .56f);
            env.AmbientLightEnergy = 1.0f;
        }
        _v109GridRoot.Visible = true;
    }

    void AddV1015GridBars(float spacing, float width, Color color, bool majorsOnly)
    {
        if (_v109GridRoot == null) return;
        var mesh = new ImmediateMesh();
        var mat = new StandardMaterial3D { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles, mat);
        for (float p = -250; p <= 250.01f; p += spacing)
        {
            if (!majorsOnly && Math.Abs(p % 50f) < .01f) continue;
            AddV1015HorizontalQuad(mesh, new Vector3(0, .04f, p), 500f, width, alongX: true);
            AddV1015HorizontalQuad(mesh, new Vector3(p, .04f, 0), 500f, width, alongX: false);
        }
        mesh.SurfaceEnd();
        _v109GridRoot.AddChild(new MeshInstance3D { Name = majorsOnly ? "Major grid" : "Minor grid", Mesh = mesh });
    }

    void AddV1015AxisBar(bool xAxis, float width, Color color)
    {
        if (_v109GridRoot == null) return;
        var mesh = new ImmediateMesh();
        var mat = new StandardMaterial3D { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles, mat);
        AddV1015HorizontalQuad(mesh, new Vector3(0, .065f, 0), 500f, width, xAxis);
        mesh.SurfaceEnd();
        _v109GridRoot.AddChild(new MeshInstance3D { Name = xAxis ? "X axis" : "Z axis", Mesh = mesh });
    }

    static void AddV1015HorizontalQuad(ImmediateMesh mesh, Vector3 center, float length, float width, bool alongX)
    {
        float hl = length * .5f, hw = width * .5f;
        Vector3 a, b, c, d;
        if (alongX)
        {
            a = center + new Vector3(-hl, 0, -hw); b = center + new Vector3(hl, 0, -hw);
            c = center + new Vector3(hl, 0, hw); d = center + new Vector3(-hl, 0, hw);
        }
        else
        {
            a = center + new Vector3(-hw, 0, -hl); b = center + new Vector3(hw, 0, -hl);
            c = center + new Vector3(hw, 0, hl); d = center + new Vector3(-hw, 0, hl);
        }
        // Counter-clockwise from above (+Y), rendered as triangles rather than driver-sensitive line primitives.
        mesh.SurfaceAddVertex(a); mesh.SurfaceAddVertex(c); mesh.SurfaceAddVertex(b);
        mesh.SurfaceAddVertex(a); mesh.SurfaceAddVertex(d); mesh.SurfaceAddVertex(c);
    }
}
