using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    Node3D? _v109GridRoot;
    Label? _v109ReferenceStatus;
    VBoxContainer? _v1093DPanel;
    Label? _v1093DStatus;
    Label? _v1093DDetail;
    ProgressBar? _v1093DActivity;
    Button? _v109Generate3D;
    Button? _v109Cancel3D;
    Timer? _v1093DTimer;
    DateTime _v1093DStarted;
    bool _v1093DBusy;
    string _v1093DProvider = "auto";
    Window? _v109SettingsWindow;
    OptionButton? _v109PerfMode;
    SpinBox? _v109VramTarget;
    Label? _v109HardwareLabel;
    VBoxContainer? _v109ModelStatus;
    CheckButton? _v109GridToggle;
    string _v109PerformancePath = "";
    Window? _v109PreviewWindow;
    TextureRect? _v109LargePreview;
    ScrollContainer? _v109PreviewScroll;
    float _v109PreviewZoom = 1f;
    Vector2 _v109PreviewNativeSize = new(1024, 1024);

    static readonly HttpClient V109ReferenceHttp = CreateV109ReferenceHttp();

    public void InstallV109Experience()
    {
        InstallV109ViewportClarity();
        InstallV109SettingsEntry();
        InstallV109LargePreview();
        InstallV109ReferenceSearch();
        InstallV1093DFeedback();
    }

    void InstallV109ViewportClarity()
    {
        if (FindChild("ViewportHost", true, false) is SubViewportContainer host && FindChild("Viewport", true, false) is SubViewport sub)
        {
            sub.OwnWorld3D = true;
            sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            void SyncSize()
            {
                var s = host.Size;
                sub.Size = new Vector2I(Math.Max(1, (int)s.X), Math.Max(1, (int)s.Y));
            }
            host.Resized += SyncSize;
            SyncSize();
        }

        if (_world == null || _v109GridRoot != null) return;
        _v109GridRoot = new Node3D { Name = "Viewport Grid v1.0.9" };
        _world.AddChild(_v109GridRoot);

        AddV109GridSurface("Minor grid", 10f, new Color(.24f, .27f, .31f), false);
        AddV109GridSurface("Major grid", 50f, new Color(.39f, .42f, .47f), true);
        AddV109Axis("X axis", new Vector3(-200, .035f, 0), new Vector3(200, .035f, 0), new Color(.72f, .30f, .27f));
        AddV109Axis("Z axis", new Vector3(0, .035f, -200), new Vector3(0, .035f, 200), new Color(.28f, .48f, .78f));

        if (_world.GetChildren().OfType<WorldEnvironment>().FirstOrDefault()?.Environment is Godot.Environment env)
        {
            env.BackgroundColor = new Color(.045f, .052f, .065f);
            env.AmbientLightColor = new Color(.42f, .44f, .50f);
            env.AmbientLightEnergy = .95f;
        }
    }

    void AddV109GridSurface(string name, float spacing, Color color, bool majorsOnly)
    {
        if (_v109GridRoot == null) return;
        var mesh = new ImmediateMesh();
        var mat = new StandardMaterial3D { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, mat);
        for (float p = -200; p <= 200.01f; p += spacing)
        {
            if (!majorsOnly && Math.Abs(p % 50f) < .01f) continue;
            mesh.SurfaceAddVertex(new Vector3(p, .03f, -200)); mesh.SurfaceAddVertex(new Vector3(p, .03f, 200));
            mesh.SurfaceAddVertex(new Vector3(-200, .03f, p)); mesh.SurfaceAddVertex(new Vector3(200, .03f, p));
        }
        mesh.SurfaceEnd();
        _v109GridRoot.AddChild(new MeshInstance3D { Name = name, Mesh = mesh });
    }

    void AddV109Axis(string name, Vector3 a, Vector3 b, Color color)
    {
        if (_v109GridRoot == null) return;
        var mesh = new ImmediateMesh();
        var mat = new StandardMaterial3D { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, mat); mesh.SurfaceAddVertex(a); mesh.SurfaceAddVertex(b); mesh.SurfaceEnd();
        _v109GridRoot.AddChild(new MeshInstance3D { Name = name, Mesh = mesh });
    }

    void InstallV109SettingsEntry()
    {
        if (FindChild("HBoxContainer", true, false) is not HBoxContainer top) return;
        var settings = new Button { Text = "Settings", TooltipText = "AI models, quality, GPU performance and viewport options" };
        settings.Pressed += ShowV109Settings;
        top.AddChild(settings);
    }

    void ShowV109Settings()
    {
        if (_v109SettingsWindow == null || !IsInstanceValid(_v109SettingsWindow)) BuildV109SettingsWindow();
        _v109SettingsWindow!.PopupCentered();
        _ = RefreshV109SettingsAsync();
    }

    void BuildV109SettingsWindow()
    {
        var window = new Window { Title = "Miniscuplter Settings — v1.0.9", Size = new Vector2I(840, 720), MinSize = new Vector2I(680, 560), Transient = true };
        AddChild(window); _v109SettingsWindow = window;
        window.CloseRequested += window.Hide;

        var root = new VBoxContainer(); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); root.OffsetLeft = 14; root.OffsetTop = 14; root.OffsetRight = -14; root.OffsetBottom = -14; window.AddChild(root);
        var tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; root.AddChild(tabs);

        var perf = new VBoxContainer { Name = "Performance" }; tabs.AddChild(perf);
        perf.AddChild(Heading("GPU / VRAM"));
        _v109HardwareLabel = new Label { Text = "Hardware: detecting…", AutowrapMode = TextServer.AutowrapMode.WordSmart }; perf.AddChild(_v109HardwareLabel);
        perf.AddChild(new Label { Text = "GPU mode" });
        _v109PerfMode = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var x in new[] { "Auto", "Fast", "Balanced", "Safe" }) _v109PerfMode.AddItem(x); perf.AddChild(_v109PerfMode);
        perf.AddChild(new Label { Text = "VRAM allocator ceiling (soft target)" });
        _v109VramTarget = new SpinBox { MinValue = 50, MaxValue = 95, Step = 1, Value = 85, Suffix = "%", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; perf.AddChild(_v109VramTarget);
        perf.AddChild(new Label { Text = "Auto uses full GPU on large cards, model CPU offload on 6–10 GB cards, and sequential offload below 6 GB. Fast tries full GPU and automatically falls back on CUDA memory pressure. The percentage is a safety ceiling for PyTorch allocations, not a promise to fill VRAM exactly.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var applyPerf = new Button { Text = "Apply Performance Settings" }; applyPerf.Pressed += async () => await SaveV109PerformanceAsync(); perf.AddChild(applyPerf);

        var quality = new VBoxContainer { Name = "Quality" }; tabs.AddChild(quality); quality.AddChild(Heading("QUALITY PRESET"));
        if (_v097Presets.Count == 0) LoadV097Presets();
        var qSelect = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var p in _v097Presets) qSelect.AddItem(p.BuiltIn ? p.Name : $"{p.Name} (Custom)");
        int active = Math.Max(0, _v097Presets.FindIndex(p => p.Id == _v097ActivePreset?.Id)); qSelect.Select(active);
        qSelect.ItemSelected += i => V097SelectPreset((int)i, true); quality.AddChild(qSelect);
        quality.AddChild(new Label { Text = "Built-in Low / Medium / High / Ultra and your custom presets control image resolution/steps, 3D steps, remesh, repair and analysis. Advanced custom editing remains available in the legacy Quality tab.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        var models = new VBoxContainer { Name = "Models" }; tabs.AddChild(models); models.AddChild(Heading("ACTIVE MODEL ROUTING"));
        AddV109Route(models, "2D generation", new[] { "auto", "sdxl", "sd21", "flux", "zimage", "qwen" }, () => _v098Routes.ImageGenerate, v => _v098Routes.ImageGenerate = v);
        AddV109Route(models, "2D edit", new[] { "auto", "sdxl", "sd21", "flux", "qwen-edit" }, () => _v098Routes.ImageEdit, v => _v098Routes.ImageEdit = v);
        AddV109Route(models, "Fast 3D", new[] { "auto", "triposr", "sf3d", "hunyuan-mini", "hunyuan" }, () => _v098Routes.Fast3D, v => _v098Routes.Fast3D = v);
        AddV109Route(models, "Quality 3D", new[] { "auto", "hunyuan", "hunyuan-mini", "spar3d", "trellis2", "triposr", "sf3d" }, () => _v098Routes.Quality3D, v => _v098Routes.Quality3D = v);
        models.AddChild(new HSeparator()); models.AddChild(new Label { Text = "INSTALLED MODELS", ThemeTypeVariation = "HeaderSmall" });
        _v109ModelStatus = new VBoxContainer(); models.AddChild(_v109ModelStatus);
        models.AddChild(new Label { Text = "Install/remove large model payloads in Miniscuplter Launcher. This panel controls which installed model is used for each role.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        var view = new VBoxContainer { Name = "Viewport" }; tabs.AddChild(view); view.AddChild(Heading("VIEWPORT"));
        _v109GridToggle = new CheckButton { Text = "Show floor grid", ButtonPressed = _v109GridRoot?.Visible ?? true };
        _v109GridToggle.Toggled += on => { if (_v109GridRoot != null) _v109GridRoot.Visible = on; }; view.AddChild(_v109GridToggle);
        view.AddChild(new Label { Text = "Grid: 10 mm minor spacing, 50 mm major spacing, with X/Z center axes. Models use lit materials against a dark viewport background for shape readability.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        var close = new Button { Text = "Close" }; close.Pressed += window.Hide; root.AddChild(close);
    }

    void AddV109Route(Container parent, string label, string[] options, Func<string> get, Action<string> set)
    {
        parent.AddChild(new Label { Text = label });
        var box = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var s in options) box.AddItem(s);
        int index = Array.FindIndex(options, s => s.Equals(get(), StringComparison.OrdinalIgnoreCase)); box.Select(Math.Max(0, index));
        box.ItemSelected += i => { set(options[(int)i]); ApplyV098Routes(); SaveV098Routes(); _ = RefreshV098RoutingOnly(); };
        parent.AddChild(box);
    }

    async Task RefreshV109SettingsAsync()
    {
        try
        {
            var status = await _ai.GetComponentsAsync();
            if (_v109HardwareLabel != null) _v109HardwareLabel.Text = $"Hardware: {status.Hardware.Gpu ?? "CPU"} · VRAM {status.Hardware.VramMb:N0} MB · CUDA {(status.Hardware.CudaAvailable ? "ready" : "unavailable")}";
            _v109PerformancePath = Path.Combine(status.DataRoot, "performance_runtime.json");
            LoadV109PerformanceFile();
            if (_v109ModelStatus != null)
            {
                foreach (var c in _v109ModelStatus.GetChildren()) c.QueueFree();
                foreach (var c in status.Components.Where(c => c.Installed))
                    _v109ModelStatus.AddChild(new Label { Text = $"• {c.Name} ({c.Kind})", TooltipText = c.Path ?? c.Description });
                if (!status.Components.Any(c => c.Installed)) _v109ModelStatus.AddChild(new Label { Text = "No local model payloads detected." });
            }
        }
        catch (Exception ex)
        {
            if (_v109HardwareLabel != null) _v109HardwareLabel.Text = "AI backend unavailable: " + ex.Message;
        }
    }

    void LoadV109PerformanceFile()
    {
        string mode = "auto"; double target = 85;
        try
        {
            if (!string.IsNullOrWhiteSpace(_v109PerformancePath) && File.Exists(_v109PerformancePath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(_v109PerformancePath));
                if (doc.RootElement.TryGetProperty("mode", out var m)) mode = m.GetString() ?? mode;
                if (doc.RootElement.TryGetProperty("vram_target_fraction", out var t)) target = t.GetDouble() * 100.0;
            }
        }
        catch { }
        if (_v109PerfMode != null)
        {
            string[] modes = { "auto", "fast", "balanced", "safe" }; int i = Array.FindIndex(modes, x => x == mode.ToLowerInvariant()); _v109PerfMode.Select(Math.Max(0, i));
        }
        if (_v109VramTarget != null) _v109VramTarget.Value = Math.Clamp(target, 50, 95);
    }

    async Task SaveV109PerformanceAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_v109PerformancePath)) await RefreshV109SettingsAsync();
            if (string.IsNullOrWhiteSpace(_v109PerformancePath)) throw new InvalidOperationException("AI data path is unavailable.");
            string[] modes = { "auto", "fast", "balanced", "safe" };
            string mode = modes[Math.Clamp(_v109PerfMode?.Selected ?? 0, 0, modes.Length - 1)]; double target = (_v109VramTarget?.Value ?? 85) / 100.0;
            Directory.CreateDirectory(Path.GetDirectoryName(_v109PerformancePath)!);
            string tmp = _v109PerformancePath + ".tmp"; File.WriteAllText(tmp, JsonSerializer.Serialize(new { mode, vram_target_fraction = target }, new JsonSerializerOptions { WriteIndented = true })); File.Move(tmp, _v109PerformancePath, true);
            await _ai.ReleaseModelsAsync();
            SetStatus($"GPU policy applied: {mode}, VRAM ceiling {target:P0}. Loaded AI models released so the next job uses it.");
        }
        catch (Exception ex) { V109ShowError("Performance settings", ex.Message); }
    }

    void InstallV109LargePreview()
    {
        if (_aiPreview == null) return;
        _aiPreview.MouseFilter = Control.MouseFilterEnum.Stop;
        _aiPreview.TooltipText = "Click to open a large zoomable preview";
        _aiPreview.GuiInput += ev =>
        {
            if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) ShowV109LargePreview();
        };
        if (_aiPreview.GetParent() is Container parent)
        {
            var open = new Button { Text = "Open Large 2D Preview" }; open.Pressed += ShowV109LargePreview;
            parent.AddChild(open); parent.MoveChild(open, _aiPreview.GetIndex());
        }
    }

    void ShowV109LargePreview()
    {
        string path = !string.IsNullOrEmpty(_lastEditedImage) ? _lastEditedImage : _lastCapture;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) { SetStatus("No 2D result is available to preview."); return; }
        var image = Image.LoadFromFile(path); if (image == null || image.IsEmpty()) { SetStatus("Could not load the 2D preview image."); return; }
        if (_v109PreviewWindow != null && IsInstanceValid(_v109PreviewWindow)) _v109PreviewWindow.QueueFree();

        var window = new Window { Title = "2D Preview — " + Path.GetFileName(path), Size = new Vector2I(1000, 780), MinSize = new Vector2I(640, 480), Transient = true };
        AddChild(window); _v109PreviewWindow = window; window.CloseRequested += window.QueueFree;
        var root = new VBoxContainer(); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); root.OffsetLeft = 10; root.OffsetTop = 10; root.OffsetRight = -10; root.OffsetBottom = -10; window.AddChild(root);
        var bar = new HBoxContainer(); root.AddChild(bar);
        var fit = new Button { Text = "Fit" }; var minus = new Button { Text = "−" }; var one = new Button { Text = "100%" }; var plus = new Button { Text = "+" }; var external = new Button { Text = "Open Externally" };
        bar.AddChild(fit); bar.AddChild(minus); bar.AddChild(one); bar.AddChild(plus); bar.AddChild(external);
        _v109PreviewScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; root.AddChild(_v109PreviewScroll);
        _v109LargePreview = new TextureRect { Texture = ImageTexture.CreateFromImage(image), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale };
        _v109PreviewNativeSize = new Vector2(image.GetWidth(), image.GetHeight()); _v109PreviewScroll.AddChild(_v109LargePreview);
        _v109PreviewZoom = 1f; ApplyV109PreviewZoom();
        minus.Pressed += () => { _v109PreviewZoom = Math.Max(.1f, _v109PreviewZoom / 1.25f); ApplyV109PreviewZoom(); };
        plus.Pressed += () => { _v109PreviewZoom = Math.Min(8f, _v109PreviewZoom * 1.25f); ApplyV109PreviewZoom(); };
        one.Pressed += () => { _v109PreviewZoom = 1f; ApplyV109PreviewZoom(); };
        fit.Pressed += FitV109Preview;
        external.Pressed += () => OS.ShellOpen(path);
        _v109PreviewScroll.GuiInput += ev =>
        {
            if (ev is InputEventMouseButton mb && mb.Pressed && mb.CtrlPressed && (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
            {
                _v109PreviewZoom = Math.Clamp(_v109PreviewZoom * (mb.ButtonIndex == MouseButton.WheelUp ? 1.15f : 1f / 1.15f), .1f, 8f); ApplyV109PreviewZoom();
            }
        };
        window.PopupCentered(); CallDeferred(nameof(FitV109Preview));
    }

    void ApplyV109PreviewZoom()
    {
        if (_v109LargePreview == null) return;
        _v109LargePreview.CustomMinimumSize = _v109PreviewNativeSize * _v109PreviewZoom;
    }

    void FitV109Preview()
    {
        if (_v109PreviewScroll == null) return;
        Vector2 available = _v109PreviewScroll.Size - new Vector2(24, 24); if (available.X <= 0 || available.Y <= 0) return;
        _v109PreviewZoom = Math.Clamp(Math.Min(available.X / _v109PreviewNativeSize.X, available.Y / _v109PreviewNativeSize.Y), .1f, 8f); ApplyV109PreviewZoom();
    }

    void InstallV109ReferenceSearch()
    {
        var ai = FindChild("AI", true, false) as VBoxContainer; if (ai == null) return;
        Button? old = null;
        foreach (var child in ai.GetChildren()) if (child is Button b && b.Text == "Search references from prompt") { old = b; break; }
        int index = old?.GetIndex() ?? 0; if (old != null) { old.Disabled = true; old.Visible = false; }
        var button = new Button { Text = "Search references from prompt" }; button.Pressed += async () => await SearchV109ReferencesAsync(); ai.AddChild(button); ai.MoveChild(button, Math.Clamp(index, 0, ai.GetChildCount() - 1));
        _v109ReferenceStatus = new Label { Text = "Reference search: ready", AutowrapMode = TextServer.AutowrapMode.WordSmart }; ai.AddChild(_v109ReferenceStatus); ai.MoveChild(_v109ReferenceStatus, Math.Clamp(index + 1, 0, ai.GetChildCount() - 1));
    }

    static HttpClient CreateV109ReferenceHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Miniscuplter/1.0.9 (+https://github.com/KubovicsT/Miniscuplter; desktop-reference-search)");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return http;
    }

    async Task SearchV109ReferencesAsync()
    {
        string query = _prompt?.Text.Trim() ?? ""; if (query.Length == 0) { _v109ReferenceStatus!.Text = "Reference search: enter terms in Prompt first."; return; }
        if (_internetToggle != null && !_internetToggle.ButtonPressed) { _v109ReferenceStatus!.Text = "Reference search: internet access is disabled."; return; }
        if (_referenceList == null) return;
        foreach (var c in _referenceList.GetChildren()) c.QueueFree();
        try
        {
            _v109ReferenceStatus!.Text = "Reference search: contacting Wikimedia Commons…";
            string url = "https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch=" + Uri.EscapeDataString(query) + "&gsrnamespace=6&gsrlimit=8&prop=imageinfo&iiprop=url&iiurlwidth=320&format=json";
            using var response = await V109ReferenceHttp.GetAsync(url); string body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Wikimedia Commons returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {body[..Math.Min(body.Length, 240)]}");
            using var doc = JsonDocument.Parse(body); var results = new List<(string Title, string Url)>();
            if (doc.RootElement.TryGetProperty("query", out var q) && q.TryGetProperty("pages", out var pages))
            {
                foreach (var page in pages.EnumerateObject())
                {
                    var e = page.Value; string title = e.TryGetProperty("title", out var t) ? t.GetString() ?? "Reference" : "Reference";
                    results.Add((title, "https://commons.wikimedia.org/wiki/" + Uri.EscapeDataString(title.Replace(' ', '_'))));
                }
            }
            foreach (var r in results)
            {
                var b = new Button { Text = r.Title, TooltipText = r.Url, Alignment = HorizontalAlignment.Left }; string page = r.Url; b.Pressed += () => OS.ShellOpen(page); _referenceList.AddChild(b);
            }
            _v109ReferenceStatus.Text = $"Reference search: {results.Count} result(s) from Wikimedia Commons.";
        }
        catch (Exception ex)
        {
            _v109ReferenceStatus!.Text = "Reference search FAILED: " + ex.Message;
            V109ShowError("Internet reference search failed", ex.Message);
        }
    }

    void InstallV1093DFeedback()
    {
        var ai = FindChild("AI", true, false) as VBoxContainer; if (ai == null) return;
        Button? old = null;
        foreach (var child in ai.GetChildren()) if (child is Button b && b.Text == "Approved 2D → Generate 3D Part") { old = b; break; }
        int index = old?.GetIndex() ?? 3; if (old != null) { old.Disabled = true; old.Visible = false; }
        _v1093DPanel = new VBoxContainer { Name = "3D Job Feedback v1.0.9" }; ai.AddChild(_v1093DPanel); ai.MoveChild(_v1093DPanel, Math.Clamp(index, 0, ai.GetChildCount() - 1));
        _v109Generate3D = new Button { Text = "Approved 2D → Generate 3D Part" }; _v109Generate3D.Pressed += V109Generate3DAsync; _v1093DPanel.AddChild(_v109Generate3D);
        _v1093DStatus = new Label { Text = "3D status: idle", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _v1093DPanel.AddChild(_v1093DStatus);
        _v1093DActivity = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 0, ShowPercentage = false, Visible = false, CustomMinimumSize = new Vector2(0, 12) }; _v1093DPanel.AddChild(_v1093DActivity);
        _v1093DDetail = new Label { Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart }; _v1093DPanel.AddChild(_v1093DDetail);
        _v109Cancel3D = new Button { Text = "Cancel 3D Job", Visible = false }; _v109Cancel3D.Pressed += () => { if (_v1093DBusy) { _ai.CancelCurrentRequest(); _v1093DStatus!.Text = "3D status: cancelling…"; } }; _v1093DPanel.AddChild(_v109Cancel3D);
        _v1093DTimer = new Timer { WaitTime = .75, OneShot = false }; _v1093DTimer.Timeout += TickV1093D; AddChild(_v1093DTimer);
    }

    async void V109Generate3DAsync()
    {
        if (_v1093DBusy) return;
        string image = !string.IsNullOrEmpty(_lastEditedImage) ? _lastEditedImage : _lastCapture;
        if (string.IsNullOrWhiteSpace(image) || !File.Exists(image)) { SetV1093DResult("3D status: no approved 2D source.", "Generate/open a 2D result first."); return; }
        string output = AppDataRoot.Resolve($"ai_part_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl"); string prompt = _prompt?.Text.Trim() ?? "";
        _v1093DBusy = true; _v1093DStarted = DateTime.UtcNow; _v1093DProvider = _v098Routes.Quality3D; SetV1093DBusy(true);
        try
        {
            SetV1093DPhase("Checking local AI service…", "Source image verified: " + Path.GetFileName(image), 5);
            if (!await _ai.HealthAsync()) throw new InvalidOperationException("The local AI backend did not answer its health check. Use Repair AI Runtime in Launcher.");
            if (_v097ActivePreset != null) { SetV1093DPhase("Applying quality preset…", $"{_v097ActivePreset.Name} · {_v097ActivePreset.ShapeSteps} 3D steps", 10); await PushV097PresetToBackendAsync(_v097ActivePreset); }
            SetV1093DPhase("Resolving 3D provider…", "Using your Settings → Models routing preference.", 15);
            if (_v1093DProvider.Equals("auto", StringComparison.OrdinalIgnoreCase)) _v1093DProvider = await ResolveV109Quality3DProviderAsync();
            SetV1093DPhase($"{_v1093DProvider} loading / reconstructing…", "Model loading can take time before GPU usage rises. The activity bar is a heartbeat because current 3D providers do not expose a reliable step percentage.", 22);
            string path = await _ai.Generate3DRoutedAsync(image, prompt, output, "quality", _v1093DProvider);
            SetV1093DPhase("Validating generated STL…", path, 90);
            if (!File.Exists(path) || new FileInfo(path).Length == 0) throw new InvalidOperationException("The 3D provider returned without a usable STL file.");
            var mesh = MeshIO.LoadStl(path); if (mesh.GetSurfaceCount() == 0) throw new InvalidOperationException("The generated STL contains no renderable mesh surface.");
            SetV1093DPhase("Importing mesh into scene…", "Adding the result non-destructively as a new scene object.", 96);
            AddMeshObject(mesh, $"AI 3D — {_v1093DProvider}"); FrameSelected();
            double sec = (DateTime.UtcNow - _v1093DStarted).TotalSeconds; SetV1093DResult($"3D status: completed with {_v1093DProvider} in {sec:0}s.", "Output: " + path); SetStatus("AI 3D part added non-destructively.");
        }
        catch (Exception ex)
        {
            double sec = (DateTime.UtcNow - _v1093DStarted).TotalSeconds; string detail = V108FriendlyAiError(ex); SetV1093DResult($"3D status: FAILED after {sec:0}s.", detail); SetStatus("3D AI error: " + detail); V109ShowError("2D → 3D generation failed", detail);
        }
        finally { _v1093DBusy = false; SetV1093DBusy(false); }
    }

    async Task<string> ResolveV109Quality3DProviderAsync()
    {
        try
        {
            string json = await _ai.GetRoutingAsync(); using var doc = JsonDocument.Parse(json); var q = doc.RootElement.GetProperty("three_d").GetProperty("quality");
            if (q.TryGetProperty("provider", out var p) && !string.IsNullOrWhiteSpace(p.GetString())) return p.GetString()!;
            if (q.TryGetProperty("error", out var e)) throw new InvalidOperationException(e.GetString() ?? "No quality 3D model is available.");
        }
        catch (Exception ex) { throw new InvalidOperationException("Could not resolve an installed 3D provider: " + ex.Message, ex); }
        return "auto";
    }

    void SetV1093DBusy(bool busy)
    {
        if (_v109Generate3D != null) { _v109Generate3D.Disabled = busy; _v109Generate3D.Text = busy ? "Generating 3D Part…" : "Approved 2D → Generate 3D Part"; }
        if (_v109Cancel3D != null) _v109Cancel3D.Visible = busy;
        if (_v1093DActivity != null) { _v1093DActivity.Visible = busy; if (busy) _v1093DActivity.Value = 5; else _v1093DActivity.Value = 100; }
        if (busy) _v1093DTimer?.Start(); else _v1093DTimer?.Stop();
    }

    void TickV1093D()
    {
        if (!_v1093DBusy) return; double elapsed = (DateTime.UtcNow - _v1093DStarted).TotalSeconds;
        if (_v1093DActivity != null && _v1093DActivity.Value >= 20 && _v1093DActivity.Value < 88) { double next = _v1093DActivity.Value + 4; _v1093DActivity.Value = next >= 88 ? 24 : next; }
        if (_v1093DStatus != null) _v1093DStatus.Text = $"3D status: {_v1093DProvider} working • {elapsed:0}s elapsed";
    }

    void SetV1093DPhase(string status, string detail, double activity)
    {
        if (_v1093DStatus != null) _v1093DStatus.Text = "3D status: " + status; if (_v1093DActivity != null) _v1093DActivity.Value = activity;
        if (_v1093DDetail != null) { _v1093DDetail.Text = detail; _v1093DDetail.Visible = !string.IsNullOrWhiteSpace(detail); }
    }

    void SetV1093DResult(string status, string detail)
    {
        if (_v1093DStatus != null) _v1093DStatus.Text = status; if (_v1093DDetail != null) { _v1093DDetail.Text = detail; _v1093DDetail.Visible = !string.IsNullOrWhiteSpace(detail); }
    }

    void V109ShowError(string title, string detail)
    {
        var dialog = new AcceptDialog { Title = title, DialogText = detail, MinSize = new Vector2I(680, 260) }; AddChild(dialog); dialog.Confirmed += dialog.QueueFree; dialog.Canceled += dialog.QueueFree; dialog.PopupCentered(new Vector2I(720, 320));
    }
}
