using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    SpinBox? _v09ThicknessTarget;
    SpinBox? _v09ThicknessSamples;
    CheckButton? _v09ThicknessOnlyBelow;
    Label? _v09ThicknessSummary;
    Label? _v09ThicknessHover;
    MeshInstance3D? _v09ThicknessOverlay;
    MeshInstance3D? _v09ThicknessSource;
    readonly List<Vector3> _v09ThicknessPositions = new();
    readonly List<float> _v09ThicknessValues = new();
    float _v09ThicknessTargetValue = .8f;

    public void InstallV09Thickness()
    {
        var panel = FindChild("Print Repair v0.9", true, false) as ScrollContainer;
        if (panel?.GetChildCount() == 0 || panel.GetChild(0) is not VBoxContainer box) return;

        box.AddChild(new HSeparator());
        box.AddChild(new Label { Text = "THICKNESS HEATMAP", ThemeTypeVariation = "HeaderSmall" });
        box.AddChild(new Label { Text = "Application-agnostic local wall-thickness estimate. Set the minimum thickness your use case needs; this does not enforce any 3D-printing rule.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        var row = new HBoxContainer();
        _v09ThicknessTarget = new SpinBox { MinValue = .01, MaxValue = 100, Step = .05, Value = .80, Suffix = " mm", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v09ThicknessSamples = new SpinBox { MinValue = 500, MaxValue = 50000, Step = 500, Value = 12000, Suffix = " samples", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(_v09ThicknessTarget); row.AddChild(_v09ThicknessSamples); box.AddChild(row);
        box.AddChild(new Label { Text = "Target minimum thickness · analysis sample budget" });

        _v09ThicknessOnlyBelow = new CheckButton { Text = "Show only areas below target" };
        _v09ThicknessOnlyBelow.Toggled += _ => RebuildV09ThicknessOverlay(); box.AddChild(_v09ThicknessOnlyBelow);

        var actions = new HBoxContainer();
        AddV09Button(actions, "Analyze + Show Heatmap", async () => await AnalyzeThicknessV09Async());
        AddV09Button(actions, "Clear Heatmap", ClearV09ThicknessHeatmap);
        box.AddChild(actions);

        _v09ThicknessSummary = new Label { Text = "No thickness analysis yet.", AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(_v09ThicknessSummary);
        _v09ThicknessHover = new Label { Text = "Hover model: —", AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(_v09ThicknessHover);
        box.AddChild(new Label { Text = "Heatmap scale: red = thinnest, yellow ≈ target, green/blue = thicker. Unresolved areas remain neutral. Values come from inward multi-direction surface rays and are estimates on difficult concave/non-watertight geometry.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        if (FindChild("ViewportHost", true, false) is SubViewportContainer host) host.GuiInput += OnV09ThicknessViewportInput;
    }

    async Task AnalyzeThicknessV09Async()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a mesh for thickness analysis."); return; }
        var source = _selected;
        try
        {
            ClearV09ThicknessHeatmap();
            string input = ExportSelectedV09Input(source, "thickness");
            double target = _v09ThicknessTarget?.Value ?? .80;
            int samples = (int)(_v09ThicknessSamples?.Value ?? 12000);
            SetStatus($"Analyzing local wall thickness at up to {samples:N0} surface samples…");
            string json = await _ai.ThicknessMapAsync(input, target, samples);
            ParseV09Thickness(json);
            _v09ThicknessSource = source;
            _v09ThicknessTargetValue = (float)target;
            RebuildV09ThicknessOverlay();
            SetStatus("Thickness heatmap ready. Hover the model to inspect local values.");
        }
        catch (Exception ex) { SetStatus("Thickness analysis failed: " + ex.Message); }
    }

    void ParseV09Thickness(string json)
    {
        _v09ThicknessPositions.Clear(); _v09ThicknessValues.Clear();
        using var doc = JsonDocument.Parse(json); var r = doc.RootElement;
        var pos = r.GetProperty("sample_positions_mm"); var vals = r.GetProperty("sample_values_mm");
        int count = Math.Min(pos.GetArrayLength(), vals.GetArrayLength());
        for (int i = 0; i < count; i++)
        {
            var p = pos[i];
            _v09ThicknessPositions.Add(new Vector3(p[0].GetSingle(), p[1].GetSingle(), p[2].GetSingle()));
            _v09ThicknessValues.Add(vals[i].GetSingle());
        }
        string min = r.TryGetProperty("minimum_mm", out var mn) && mn.ValueKind == JsonValueKind.Number ? $"{mn.GetDouble():0.000} mm" : "unresolved";
        string max = r.TryGetProperty("maximum_mm", out var mx) && mx.ValueKind == JsonValueKind.Number ? $"{mx.GetDouble():0.000} mm" : "unresolved";
        int below = r.GetProperty("below_target_samples").GetInt32();
        int resolved = r.GetProperty("resolved_samples").GetInt32();
        if (_v09ThicknessSummary != null) _v09ThicknessSummary.Text = $"Resolved samples: {resolved:N0} · Below target: {below:N0} · Min: {min} · Max: {max}";
    }

    void RebuildV09ThicknessOverlay()
    {
        if (_v09ThicknessOverlay != null) { _v09ThicknessOverlay.QueueFree(); _v09ThicknessOverlay = null; }
        if (_v09ThicknessSource?.Mesh == null || _v09ThicknessPositions.Count == 0 || _world == null) return;

        var source = _v09ThicknessSource;
        var mesh = new ArrayMesh();
        for (int s = 0; s < source.Mesh.GetSurfaceCount(); s++)
        {
            var arrays = source.Mesh.SurfaceGetArrays(s);
            var verts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var colors = new Color[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 world = source.GlobalTransform * verts[i];
                float thickness = NearestV09Thickness(world);
                colors[i] = V09ThicknessColor(thickness, _v09ThicknessTargetValue, _v09ThicknessOnlyBelow?.ButtonPressed ?? false);
            }
            arrays[(int)Mesh.ArrayType.Color] = colors;
            mesh.AddSurfaceFromArrays(source.Mesh.SurfaceGetPrimitiveType(s), arrays);
        }

        var mat = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 1f, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        _v09ThicknessOverlay = new MeshInstance3D { Name = "Thickness Heatmap v0.9", Mesh = mesh, MaterialOverride = mat, Transform = source.Transform };
        _world.AddChild(_v09ThicknessOverlay);
        source.Visible = false;
    }

    float NearestV09Thickness(Vector3 world)
    {
        float best = float.PositiveInfinity, value = float.NaN;
        for (int i = 0; i < _v09ThicknessPositions.Count; i++)
        {
            float d = world.DistanceSquaredTo(_v09ThicknessPositions[i]);
            if (d < best) { best = d; value = _v09ThicknessValues[i]; }
        }
        return value;
    }

    static Color V09ThicknessColor(float value, float target, bool onlyBelow)
    {
        if (!float.IsFinite(value)) return new Color(.45f, .45f, .45f, onlyBelow ? 0f : 1f);
        if (onlyBelow && value >= target) return new Color(0, 0, 0, 0);
        float ratio = value / Math.Max(target, .0001f);
        if (ratio < .5f) return new Color(1f, Math.Clamp(ratio * 2f, 0f, 1f), 0f, 1f);
        if (ratio < 1f) return new Color(1f, 1f, Math.Clamp((ratio - .5f) * .4f, 0f, .2f), 1f);
        float t = Math.Clamp((ratio - 1f) / 2f, 0f, 1f);
        return new Color(0f, 1f - .35f * t, .15f + .85f * t, 1f);
    }

    void OnV09ThicknessViewportInput(InputEvent ev)
    {
        if (_v09ThicknessSource?.Mesh == null || _camera == null || _v09ThicknessPositions.Count == 0) return;
        if (ev is not InputEventMouseMotion mm) return;
        var ro = _camera.ProjectRayOrigin(mm.Position); var rd = _camera.ProjectRayNormal(mm.Position);
        if (!RayMesh(ro, rd, _v09ThicknessSource, out var hit)) { if (_v09ThicknessHover != null) _v09ThicknessHover.Text = "Hover model: —"; return; }
        float value = NearestV09Thickness(hit);
        if (_v09ThicknessHover != null) _v09ThicknessHover.Text = float.IsFinite(value) ? $"Hover model: ≈ {value:0.000} mm local thickness ({(value < _v09ThicknessTargetValue ? "below" : "at/above")} {_v09ThicknessTargetValue:0.00} mm target)" : "Hover model: unresolved";
    }

    void ClearV09ThicknessHeatmap()
    {
        if (_v09ThicknessOverlay != null) { _v09ThicknessOverlay.QueueFree(); _v09ThicknessOverlay = null; }
        if (_v09ThicknessSource != null && GodotObject.IsInstanceValid(_v09ThicknessSource)) _v09ThicknessSource.Visible = true;
        _v09ThicknessSource = null; _v09ThicknessPositions.Clear(); _v09ThicknessValues.Clear();
        if (_v09ThicknessHover != null) _v09ThicknessHover.Text = "Hover model: —";
    }
}
