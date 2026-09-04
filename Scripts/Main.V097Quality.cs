using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    sealed class V097QualityPreset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Custom";
        public bool BuiltIn { get; set; }
        public int ImageSize { get; set; } = 512;
        public int ImageSteps { get; set; } = 24;
        public double ImageGuidance { get; set; } = 7.0;
        public double ImageEditStrength { get; set; } = .58;
        public int MaxInputPx { get; set; } = 2048;
        public int ShapeSteps { get; set; } = 30;
        public double RemeshVoxelMm { get; set; } = .28;
        public double RepairVoxelMm { get; set; } = .30;
        public long MaxVoxelCells { get; set; } = 100_000_000;
        public int ThicknessSamples { get; set; } = 12_000;
        public int SmartSelectViews { get; set; } = 6;
        public int SmartSelectRenderSize { get; set; } = 352;
    }

    sealed class V097PresetFile
    {
        public string ActiveId { get; set; } = "";
        public bool ExplicitSelection { get; set; }
        public List<V097QualityPreset> Custom { get; set; } = new();
    }

    readonly List<V097QualityPreset> _v097Presets = new();
    V097QualityPreset? _v097ActivePreset;
    bool _v097ExplicitSelection;
    bool _v097LoadingUi;
    OptionButton? _v097PresetSelect;
    Label? _v097Recommendation;
    Label? _v097ActiveSummary;
    LineEdit? _v097Name;
    SpinBox? _v097ImageSize, _v097ImageSteps, _v097ImageGuidance, _v097EditStrength, _v097MaxInput;
    SpinBox? _v097ShapeSteps, _v097RemeshVoxel, _v097RepairVoxel, _v097MaxVoxelCells;
    SpinBox? _v097ThicknessSamples, _v097SmartViews, _v097SmartSize;
    Button? _v097UpdateCustom, _v097DeleteCustom;

    static IEnumerable<V097QualityPreset> V097BuiltIns()
    {
        yield return new V097QualityPreset { Id="builtin-low", Name="Low", BuiltIn=true, ImageSize=384, ImageSteps=12, ImageGuidance=6.0, ImageEditStrength=.52, MaxInputPx=1024, ShapeSteps=18, RemeshVoxelMm=.45, RepairVoxelMm=.45, MaxVoxelCells=50_000_000, ThicknessSamples=6_000, SmartSelectViews=4, SmartSelectRenderSize=256 };
        yield return new V097QualityPreset { Id="builtin-medium", Name="Medium", BuiltIn=true, ImageSize=512, ImageSteps=24, ImageGuidance=6.5, ImageEditStrength=.58, MaxInputPx=2048, ShapeSteps=30, RemeshVoxelMm=.28, RepairVoxelMm=.30, MaxVoxelCells=100_000_000, ThicknessSamples=12_000, SmartSelectViews=6, SmartSelectRenderSize=352 };
        yield return new V097QualityPreset { Id="builtin-high", Name="High", BuiltIn=true, ImageSize=640, ImageSteps=36, ImageGuidance=7.0, ImageEditStrength=.62, MaxInputPx=3072, ShapeSteps=45, RemeshVoxelMm=.16, RepairVoxelMm=.18, MaxVoxelCells=200_000_000, ThicknessSamples=30_000, SmartSelectViews=8, SmartSelectRenderSize=512 };
        yield return new V097QualityPreset { Id="builtin-ultra", Name="Ultra", BuiltIn=true, ImageSize=1024, ImageSteps=50, ImageGuidance=7.5, ImageEditStrength=.65, MaxInputPx=4096, ShapeSteps=60, RemeshVoxelMm=.10, RepairVoxelMm=.10, MaxVoxelCells=400_000_000, ThicknessSamples=50_000, SmartSelectViews=12, SmartSelectRenderSize=768 };
    }

    public void InstallV097QualityPresets()
    {
        LoadV097Presets();
        var tabs = FindChild("TabContainer", true, false) as TabContainer;
        if (tabs == null) return;
        var scroll = new ScrollContainer { Name = "Quality v0.9.7", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(box); tabs.AddChild(scroll);
        box.AddChild(new Label { Text = "QUALITY PRESETS — v0.9.7", ThemeTypeVariation = "HeaderSmall" });
        box.AddChild(new Label { Text = "Presets configure AI generation, geometry processing and analysis together. Hardware recommendation is only a starting point: any preset may be selected, and custom presets are unlimited.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        _v097Recommendation = new Label { Text = "Recommended: detecting hardware…", AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(_v097Recommendation);
        _v097PresetSelect = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; box.AddChild(_v097PresetSelect);
        _v097PresetSelect.ItemSelected += i => { if (!_v097LoadingUi) V097SelectPreset((int)i, true); };
        _v097ActiveSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(_v097ActiveSummary);

        box.AddChild(new HSeparator());
        box.AddChild(new Label { Text = "PRESET VALUES", ThemeTypeVariation = "HeaderSmall" });
        _v097Name = new LineEdit { PlaceholderText = "Custom preset name" }; box.AddChild(_v097Name);

        _v097ImageSize = AddV097Spin(box, "2D resolution (square pixels)", 256, 1536, 64, 512, " px");
        _v097ImageSteps = AddV097Spin(box, "2D inference steps", 4, 100, 1, 24);
        _v097ImageGuidance = AddV097Spin(box, "2D guidance scale", 1, 20, .1, 7);
        _v097EditStrength = AddV097Spin(box, "2D edit strength", .05, .95, .01, .58);
        _v097MaxInput = AddV097Spin(box, "Maximum input image dimension", 512, 8192, 128, 2048, " px");
        _v097ShapeSteps = AddV097Spin(box, "Hunyuan 3D inference steps", 8, 100, 1, 30);
        _v097RemeshVoxel = AddV097Spin(box, "Sculpt/remesh voxel pitch", .04, 5, .01, .28, " mm");
        _v097RepairVoxel = AddV097Spin(box, "Repair/final-bake voxel pitch", .04, 5, .01, .30, " mm");
        _v097MaxVoxelCells = AddV097Spin(box, "Voxel safety budget", 1_000_000, 2_000_000_000, 1_000_000, 100_000_000, " cells");
        _v097ThicknessSamples = AddV097Spin(box, "Thickness analysis samples", 100, 100_000, 500, 12_000);
        _v097SmartViews = AddV097Spin(box, "Smart Select rendered views", 2, 12, 1, 6);
        _v097SmartSize = AddV097Spin(box, "Smart Select render resolution", 128, 1024, 32, 352, " px");

        var row = new HBoxContainer();
        var create = new Button { Text = "Save as New Custom", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v097UpdateCustom = new Button { Text = "Update Custom" };
        _v097DeleteCustom = new Button { Text = "Delete Custom" };
        create.Pressed += V097CreateCustom; _v097UpdateCustom.Pressed += V097UpdateCustom; _v097DeleteCustom.Pressed += V097DeleteCustom;
        row.AddChild(create); row.AddChild(_v097UpdateCustom); row.AddChild(_v097DeleteCustom); box.AddChild(row);
        box.AddChild(new Label { Text = "Built-in presets are read-only. Change their displayed values and choose ‘Save as New Custom’ to create a derived configuration. Custom presets are stored in user data and there is no application-imposed count limit.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        RefreshV097PresetList();
        int initial = Math.Max(0, _v097Presets.FindIndex(p => p.Id == (_v097ActivePreset?.Id ?? "builtin-medium")));
        V097SelectPreset(initial, false);
        _ = DetectAndApplyV097RecommendationAsync();
    }

    SpinBox AddV097Spin(Container parent, string label, double min, double max, double step, double value, string suffix = "")
    {
        parent.AddChild(new Label { Text = label });
        var s = new SpinBox { MinValue=min, MaxValue=max, Step=step, Value=value, Suffix=suffix, SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };
        parent.AddChild(s); return s;
    }

    string V097PresetPath() => ProjectSettings.GlobalizePath("user://quality_presets_v097.json");

    void LoadV097Presets()
    {
        _v097Presets.Clear(); _v097Presets.AddRange(V097BuiltIns());
        try
        {
            if (!File.Exists(V097PresetPath())) { _v097ActivePreset = _v097Presets[1]; return; }
            var state = JsonSerializer.Deserialize<V097PresetFile>(File.ReadAllText(V097PresetPath()));
            if (state != null)
            {
                foreach (var p in state.Custom.Where(p => !string.IsNullOrWhiteSpace(p.Name))) { p.BuiltIn = false; if (string.IsNullOrWhiteSpace(p.Id)) p.Id = Guid.NewGuid().ToString("N"); _v097Presets.Add(p); }
                _v097ExplicitSelection = state.ExplicitSelection;
                _v097ActivePreset = _v097Presets.FirstOrDefault(p => p.Id == state.ActiveId);
            }
        }
        catch { }
        _v097ActivePreset ??= _v097Presets[1];
    }

    void SaveV097Presets()
    {
        try
        {
            var state = new V097PresetFile { ActiveId=_v097ActivePreset?.Id ?? "builtin-medium", ExplicitSelection=_v097ExplicitSelection, Custom=_v097Presets.Where(p => !p.BuiltIn).ToList() };
            File.WriteAllText(V097PresetPath(), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented=true }));
        }
        catch (Exception ex) { SetStatus("Could not save quality presets: " + ex.Message); }
    }

    void RefreshV097PresetList()
    {
        if (_v097PresetSelect == null) return;
        _v097LoadingUi = true; _v097PresetSelect.Clear();
        foreach (var p in _v097Presets) _v097PresetSelect.AddItem(p.BuiltIn ? p.Name : $"{p.Name} (Custom)");
        _v097LoadingUi = false;
    }

    void V097SelectPreset(int index, bool explicitSelection)
    {
        if (index < 0 || index >= _v097Presets.Count) return;
        _v097ActivePreset = _v097Presets[index];
        if (explicitSelection) _v097ExplicitSelection = true;
        _v097LoadingUi = true; _v097PresetSelect?.Select(index); PopulateV097Editor(_v097ActivePreset); _v097LoadingUi = false;
        ApplyV097PresetLocally(_v097ActivePreset);
        SaveV097Presets(); _ = PushV097PresetToBackendAsync(_v097ActivePreset);
    }

    void PopulateV097Editor(V097QualityPreset p)
    {
        if (_v097Name != null) _v097Name.Text = p.Name;
        SetV097(_v097ImageSize,p.ImageSize); SetV097(_v097ImageSteps,p.ImageSteps); SetV097(_v097ImageGuidance,p.ImageGuidance); SetV097(_v097EditStrength,p.ImageEditStrength);
        SetV097(_v097MaxInput,p.MaxInputPx); SetV097(_v097ShapeSteps,p.ShapeSteps); SetV097(_v097RemeshVoxel,p.RemeshVoxelMm); SetV097(_v097RepairVoxel,p.RepairVoxelMm);
        SetV097(_v097MaxVoxelCells,p.MaxVoxelCells); SetV097(_v097ThicknessSamples,p.ThicknessSamples); SetV097(_v097SmartViews,p.SmartSelectViews); SetV097(_v097SmartSize,p.SmartSelectRenderSize);
        if (_v097UpdateCustom != null) _v097UpdateCustom.Disabled = p.BuiltIn;
        if (_v097DeleteCustom != null) _v097DeleteCustom.Disabled = p.BuiltIn;
        if (_v097ActiveSummary != null) _v097ActiveSummary.Text = $"Active: {p.Name} · 2D {p.ImageSize}px/{p.ImageSteps} steps · 3D {p.ShapeSteps} steps · remesh {p.RemeshVoxelMm:0.00} mm · repair {p.RepairVoxelMm:0.00} mm · voxel budget {p.MaxVoxelCells:N0}";
    }

    static void SetV097(SpinBox? s, double value) { if (s != null) s.Value = Math.Clamp(value, s.MinValue, s.MaxValue); }

    V097QualityPreset ReadV097Editor(string id, bool builtIn)
    {
        string name = _v097Name?.Text.Trim() ?? ""; if (name.Length == 0) name = "Custom";
        return new V097QualityPreset {
            Id=id, Name=name, BuiltIn=builtIn,
            ImageSize=(int)(_v097ImageSize?.Value ?? 512), ImageSteps=(int)(_v097ImageSteps?.Value ?? 24), ImageGuidance=_v097ImageGuidance?.Value ?? 7,
            ImageEditStrength=_v097EditStrength?.Value ?? .58, MaxInputPx=(int)(_v097MaxInput?.Value ?? 2048), ShapeSteps=(int)(_v097ShapeSteps?.Value ?? 30),
            RemeshVoxelMm=_v097RemeshVoxel?.Value ?? .28, RepairVoxelMm=_v097RepairVoxel?.Value ?? .30, MaxVoxelCells=(long)(_v097MaxVoxelCells?.Value ?? 100_000_000),
            ThicknessSamples=(int)(_v097ThicknessSamples?.Value ?? 12000), SmartSelectViews=(int)(_v097SmartViews?.Value ?? 6), SmartSelectRenderSize=(int)(_v097SmartSize?.Value ?? 352)
        };
    }

    void V097CreateCustom()
    {
        var p = ReadV097Editor(Guid.NewGuid().ToString("N"), false); _v097Presets.Add(p); _v097ActivePreset=p; _v097ExplicitSelection=true;
        RefreshV097PresetList(); V097SelectPreset(_v097Presets.Count-1, true); SetStatus($"Custom quality preset '{p.Name}' created and activated.");
    }

    void V097UpdateCustom()
    {
        if (_v097ActivePreset == null || _v097ActivePreset.BuiltIn) { SetStatus("Built-in presets cannot be overwritten; save a new custom preset instead."); return; }
        int i=_v097Presets.IndexOf(_v097ActivePreset); if (i < 0) return;
        var p=ReadV097Editor(_v097ActivePreset.Id,false); _v097Presets[i]=p; _v097ActivePreset=p; _v097ExplicitSelection=true;
        RefreshV097PresetList(); V097SelectPreset(i,true); SetStatus($"Custom quality preset '{p.Name}' updated.");
    }

    void V097DeleteCustom()
    {
        if (_v097ActivePreset == null || _v097ActivePreset.BuiltIn) return;
        string name=_v097ActivePreset.Name; _v097Presets.Remove(_v097ActivePreset); _v097ActivePreset=_v097Presets[1]; _v097ExplicitSelection=true;
        RefreshV097PresetList(); V097SelectPreset(1,true); SetStatus($"Custom quality preset '{name}' deleted. Medium activated.");
    }

    void ApplyV097PresetLocally(V097QualityPreset p)
    {
        if (_v08RemeshVoxel != null) { _v08RemeshVoxel.MinValue=.04; _v08RemeshVoxel.Value=p.RemeshVoxelMm; }
        if (_v09RepairVoxel != null) { _v09RepairVoxel.MinValue=.04; _v09RepairVoxel.Value=p.RepairVoxelMm; }
        if (_v09ThicknessSamples != null) { _v09ThicknessSamples.MaxValue=100000; _v09ThicknessSamples.Value=p.ThicknessSamples; }
        if (_v05Quality != null)
        {
            int legacy = p.ImageSize <= 448 ? 0 : p.ImageSize <= 576 ? 1 : 2; _v05Quality.Select(Math.Clamp(legacy,0,Math.Max(0,_v05Quality.ItemCount-1))); RefreshEtaLabel();
        }
        PopulateV097Editor(p);
    }

    async Task PushV097PresetToBackendAsync(V097QualityPreset p)
    {
        try
        {
            if (!await _ai.HealthAsync()) return;
            await _ai.ApplyQualityConfigAsync(p.ImageSize,p.ImageSteps,p.ImageGuidance,p.ImageEditStrength,p.MaxInputPx,p.ShapeSteps,p.RemeshVoxelMm,p.RepairVoxelMm,p.MaxVoxelCells,p.ThicknessSamples,p.SmartSelectViews,p.SmartSelectRenderSize);
        }
        catch (Exception ex) { SetStatus("Preset selected locally; backend quality sync failed: " + ex.Message); }
    }

    async Task DetectAndApplyV097RecommendationAsync()
    {
        try
        {
            if (!await _ai.HealthAsync()) { if (_v097Recommendation != null) _v097Recommendation.Text="Recommended: backend unavailable; Medium is the safe starting preset."; return; }
            var s=await _ai.GetComponentsAsync();
            string id = !s.Hardware.CudaAvailable ? "builtin-low" : s.Hardware.VramMb <= 4096 ? "builtin-low" : s.Hardware.VramMb <= 8192 ? "builtin-medium" : s.Hardware.VramMb <= 12288 ? "builtin-high" : "builtin-ultra";
            var recommended=_v097Presets.First(p=>p.Id==id);
            if (_v097Recommendation != null) _v097Recommendation.Text=$"Recommended for detected hardware: {recommended.Name} ({s.Hardware.Gpu ?? "CPU"}, {(s.Hardware.VramMb>0 ? $"{s.Hardware.VramMb/1024.0:0.0} GB VRAM" : "no CUDA VRAM")}). This is advisory only.";
            if (!_v097ExplicitSelection)
            {
                int i=_v097Presets.IndexOf(recommended); V097SelectPreset(i,false); SetStatus($"Hardware recommendation applied as starting preset: {recommended.Name}. You can select any preset at any time.");
            }
            else if (_v097ActivePreset != null) await PushV097PresetToBackendAsync(_v097ActivePreset);
        }
        catch (Exception ex) { if (_v097Recommendation != null) _v097Recommendation.Text="Recommended: hardware detection failed; current preset remains active."; SetStatus("Quality recommendation check failed safely: " + ex.Message); }
    }
}
