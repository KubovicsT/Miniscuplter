using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    OptionButton? _v05Quality;
    Label? _v05Eta;
    VBoxContainer? _v05Candidates;
    bool _v05MaskPaintMode;
    bool _v05MaskErase;
    int _v05MaskRadius = 28;
    Image? _v05PaintMask;
    string _v05PaintMaskPath = "";
    Vector3? _v05Anchor;
    string _v05HardwareKey = "unknown";
    readonly List<JobSample> _v05History = new();

    sealed class JobSample
    {
        public string Type { get; set; } = "";
        public string Quality { get; set; } = "standard";
        public string Hardware { get; set; } = "unknown";
        public double Seconds { get; set; }
        public long UtcTicks { get; set; }
    }

    public void InstallV05Extras()
    {
        LoadJobHistory();
        var ai = FindChild("AI", true, false) as VBoxContainer;
        if (ai == null) return;
        ai.AddChild(new HSeparator());
        ai.AddChild(new Label { Text = "AI PATCH WORKFLOW — v0.5", ThemeTypeVariation = "HeaderSmall" });
        ai.AddChild(new Label { Text = "Quality preset" });
        _v05Quality = new OptionButton();
        _v05Quality.AddItem("Preview"); _v05Quality.AddItem("Standard"); _v05Quality.AddItem("High");
        _v05Quality.Selected = 1;
        _v05Quality.ItemSelected += _ => RefreshEtaLabel();
        ai.AddChild(_v05Quality);
        _v05Eta = new Label { Text = "ETA: learning from this machine...", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        ai.AddChild(_v05Eta);

        var maskRow = new HBoxContainer();
        var paint = new Button { Text = "Paint AI Mask", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var erase = new Button { Text = "Erase Mask" }; var clear = new Button { Text = "Clear" };
        paint.Pressed += () => BeginPaintMask(false); erase.Pressed += () => BeginPaintMask(true); clear.Pressed += ClearPaintMask;
        maskRow.AddChild(paint); maskRow.AddChild(erase); maskRow.AddChild(clear); ai.AddChild(maskRow);
        var radius = new HSlider { MinValue = 4, MaxValue = 100, Step = 2, Value = _v05MaskRadius };
        radius.ValueChanged += v => _v05MaskRadius = (int)v;
        ai.AddChild(new Label { Text = "Mask brush radius" }); ai.AddChild(radius);

        var candidates = new Button { Text = "Generate 4 Preview Candidates" };
        candidates.Pressed += async () => await GenerateCandidateSet(); ai.AddChild(candidates);
        _v05Candidates = new VBoxContainer(); ai.AddChild(_v05Candidates);
        var patch = new Button { Text = "Generate 3D Patch at Mask Anchor" };
        patch.Pressed += async () => await GenerateAlignedPatch(); ai.AddChild(patch);
        var history = new Button { Text = "Reset Learned Time Estimates" };
        history.Pressed += ResetJobHistory; ai.AddChild(history);
        if (FindChild("ViewportHost", true, false) is SubViewportContainer host) host.GuiInput += OnV05ViewportInput;
        _ = LoadHardwareKey(); RefreshEtaLabel();
    }

    string CurrentQuality() => (_v05Quality?.Selected ?? 1) switch { 0 => "preview", 2 => "high", _ => "standard" };

    async Task LoadHardwareKey()
    {
        try
        {
            if (!await _ai.HealthAsync()) return;
            var s = await _ai.GetComponentsAsync();
            _v05HardwareKey = $"{s.Hardware.Gpu}|{s.Hardware.VramMb}|{s.Hardware.RecommendedProfile}";
            RefreshEtaLabel();
        }
        catch { }
    }

    void BeginPaintMask(bool erase)
    {
        var sub = FindChild("Viewport", true, false) as SubViewport; if (sub == null) return;
        int w = Math.Max(1, (int)sub.GetVisibleRect().Size.X), h = Math.Max(1, (int)sub.GetVisibleRect().Size.Y);
        if (_v05PaintMask == null || _v05PaintMask.GetWidth() != w || _v05PaintMask.GetHeight() != h)
        {
            _v05PaintMask = Image.CreateEmpty(w, h, false, Image.Format.L8); _v05PaintMask.Fill(Colors.Black);
        }
        _v05MaskErase = erase; _v05MaskPaintMode = true;
        SetStatus(erase ? "Mask erase mode: drag LMB over the viewport." : "Mask paint mode: drag LMB over the viewport.");
    }

    void ClearPaintMask()
    {
        _v05PaintMask = null; _v05PaintMaskPath = ""; _v05Anchor = null;
        SetStatus("Painted AI mask cleared.");
    }

    void OnV05ViewportInput(InputEvent ev)
    {
        if (!_v05MaskPaintMode || _v05PaintMask == null) return;
        Vector2? p = null; bool active = false;
        if (ev is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            active = mb.Pressed; p = mb.Position;
            if (!mb.Pressed) { SavePaintMask(); _v05MaskPaintMode = false; return; }
        }
        else if (ev is InputEventMouseMotion mm && (mm.ButtonMask & MouseButtonMask.Left) != 0) { active = true; p = mm.Position; }
        if (!active || p == null) return;
        PaintMaskCircle((int)p.Value.X, (int)p.Value.Y, _v05MaskErase ? 0f : 1f);
        if (!_v05MaskErase && _v05Anchor == null && _camera != null && _selected != null)
        {
            var ro = _camera.ProjectRayOrigin(p.Value); var rd = _camera.ProjectRayNormal(p.Value);
            if (RayMeshDetailedV055(ro, rd, _selected, out var hit, out var normal))
            {
                _v05Anchor = hit;
                SetV055AnchorSurface(normal, EstimatePatchScaleFromMaskV055(_selected));
            }
        }
        GetViewport().SetInputAsHandled();
    }

    void PaintMaskCircle(int cx, int cy, float value)
    {
        if (_v05PaintMask == null) return;
        int r = _v05MaskRadius, w = _v05PaintMask.GetWidth(), h = _v05PaintMask.GetHeight();
        for (int y = Math.Max(0, cy-r); y < Math.Min(h, cy+r+1); y++)
            for (int x = Math.Max(0, cx-r); x < Math.Min(w, cx+r+1); x++)
                if ((x-cx)*(x-cx)+(y-cy)*(y-cy) <= r*r) _v05PaintMask.SetPixel(x,y,new Color(value,value,value));
    }

    void SavePaintMask()
    {
        if (_v05PaintMask == null) return;
        string dir = ProjectSettings.GlobalizePath("user://masks"); Directory.CreateDirectory(dir);
        _v05PaintMaskPath = Path.Combine(dir, $"paint_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        _v05PaintMask.SavePng(_v05PaintMaskPath);
        if (_selected != null) SetV055AnchorSurface(_v055AnchorNormal, EstimatePatchScaleFromMaskV055(_selected));
        SetStatus("Painted AI mask saved with surface anchor information.");
    }

    async Task GenerateCandidateSet()
    {
        ResetV055Cancellation();
        string source = !string.IsNullOrEmpty(_lastEditedImage) ? _lastEditedImage : _lastCapture;
        if (string.IsNullOrEmpty(source) || !File.Exists(source)) { CaptureView(); source = _lastCapture; }
        string prompt = _prompt?.Text.Trim() ?? ""; if (prompt.Length == 0) { SetStatus("Enter an edit prompt first."); return; }
        if (_v05Candidates != null) foreach (var c in _v05Candidates.GetChildren()) c.QueueFree();
        for (int i=0;i<4;i++)
        {
            if (V055CancellationRequested) { SetStatus($"Candidate job cancelled after {i} result(s)."); return; }
            int n=i+1; string outPath = ProjectSettings.GlobalizePath($"user://candidate_{DateTime.Now:yyyyMMdd_HHmmss}_{n}.png");
            var sw = Stopwatch.StartNew();
            string result;
            try { result = await _ai.EditImageAsync(source, string.IsNullOrEmpty(_v05PaintMaskPath)?null:_v05PaintMaskPath, prompt, outPath, "preview"); }
            catch (Exception ex) { SetStatus(ex.Message); return; }
            sw.Stop(); RecordJob("2d-edit", "preview", sw.Elapsed.TotalSeconds);
            if (_v05Candidates != null)
            {
                var b = new Button { Text = $"Use candidate {n} ({sw.Elapsed.TotalSeconds:0}s)" };
                b.Pressed += () => { _lastEditedImage = result; ShowAiPreview(result); SetStatus($"Candidate {n} selected as approved 2D source."); };
                _v05Candidates.AddChild(b);
            }
            if (i==0) { _lastEditedImage = result; ShowAiPreview(result); }
        }
        RefreshEtaLabel(); SetStatus("Generated 4 preview candidates. Choose one before creating the 3D patch.");
    }

    async Task GenerateAlignedPatch()
    {
        ResetV055Cancellation();
        string image = !string.IsNullOrEmpty(_lastEditedImage) ? _lastEditedImage : _lastCapture;
        if (string.IsNullOrEmpty(image) || !File.Exists(image)) { SetStatus("Choose or generate an approved 2D image first."); return; }
        string quality = CurrentQuality(), outPath = ProjectSettings.GlobalizePath($"user://ai_patch_{DateTime.Now:yyyyMMdd_HHmmss}.stl");
        string prompt = _prompt?.Text.Trim() ?? ""; var sourceObject = _selected; var sw = Stopwatch.StartNew(); bool success = false;
        await RunAi(async () =>
        {
            SetStatus($"Generating {quality} 3D patch. {EstimateText("3d", quality)}");
            string p = await _ai.Generate3DAsync(image, prompt, outPath, quality);
            if (V055CancellationRequested) return;
            AddMeshObject(MeshIO.LoadStl(p), $"AI Edit Layer — {quality}");
            if (_selected != null && _v05Anchor is Vector3 anchor)
            {
                ApplyV055PatchAlignment(_selected, anchor);
                RememberV055Patch(_selected, sourceObject, prompt, quality, image, _v05PaintMaskPath);
            }
            success = true;
        });
        sw.Stop();
        if (success) RecordJob("3d", quality, sw.Elapsed.TotalSeconds);
        RefreshEtaLabel();
        if (success) SetStatus($"3D patch aligned to the selected surface in {FormatSeconds(sw.Elapsed.TotalSeconds)}. Fine-tune if needed, then voxel-union when accepted.");
    }

    void RecordJob(string type, string quality, double seconds)
    {
        _v05History.Add(new JobSample { Type=type, Quality=quality, Hardware=_v05HardwareKey, Seconds=seconds, UtcTicks=DateTime.UtcNow.Ticks });
        while (_v05History.Count > 200) _v05History.RemoveAt(0); SaveJobHistory();
    }

    string EstimateText(string type, string quality)
    {
        var samples = _v05History.Where(x => x.Type==type && x.Quality==quality && x.Hardware==_v05HardwareKey).OrderByDescending(x=>x.UtcTicks).Take(8).ToList();
        if (samples.Count==0) return "ETA: no personal history yet";
        double weighted=0, weights=0;
        for(int i=0;i<samples.Count;i++){ double w=samples.Count-i; weighted += samples[i].Seconds*w; weights += w; }
        double mean=weighted/weights, min=samples.Min(x=>x.Seconds), max=samples.Max(x=>x.Seconds);
        return $"ETA ~{FormatSeconds(mean)} (recent range {FormatSeconds(min)}–{FormatSeconds(max)}, {samples.Count} run{(samples.Count==1?"":"s")})";
    }

    void RefreshEtaLabel()
    {
        if (_v05Eta == null) return; string q=CurrentQuality();
        _v05Eta.Text = $"2D: {EstimateText("2d-edit", q)}\n3D: {EstimateText("3d", q)}";
    }

    static string FormatSeconds(double seconds)
    {
        if (seconds < 90) return $"{seconds:0}s"; var t=TimeSpan.FromSeconds(seconds);
        return t.TotalHours>=1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m {t.Seconds}s";
    }

    string HistoryPath() => ProjectSettings.GlobalizePath("user://job_history_v05.json");
    void LoadJobHistory()
    {
        try { if (!File.Exists(HistoryPath())) return; var list=JsonSerializer.Deserialize<List<JobSample>>(File.ReadAllText(HistoryPath())); if (list!=null) _v05History.AddRange(list); }
        catch { }
    }
    void SaveJobHistory(){ try { File.WriteAllText(HistoryPath(), JsonSerializer.Serialize(_v05History,new JsonSerializerOptions{WriteIndented=true})); } catch { } }
    void ResetJobHistory(){ _v05History.Clear(); SaveJobHistory(); RefreshEtaLabel(); SetStatus("Learned generation-time history reset."); }
}
