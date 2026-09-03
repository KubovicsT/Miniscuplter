using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    SpinBox? _v09ThicknessTarget;
    SpinBox? _v09ThicknessSamples;
    CheckButton? _v09ThicknessEmphasize;
    Label? _v09ThicknessSummary;
    MeshInstance3D? _v09ThicknessOverlay;
    MeshInstance3D? _v09ThicknessSource;

    public void InstallV09ThicknessHeatmap()
    {
        var tab = FindChild("Print Repair v0.9", true, false) as ScrollContainer;
        var panel = tab?.GetChildCount() > 0 ? tab.GetChild(0) as VBoxContainer : null;
        if (panel == null) return;

        panel.AddChild(new HSeparator());
        panel.AddChild(new Label { Text = "THICKNESS HEATMAP", ThemeTypeVariation = "HeaderSmall" });
        panel.AddChild(new Label { Text = "Application-agnostic local wall-thickness inspection. Rays are cast inward from the surface toward the nearest plausible opposite surface; this does not impose any printer or manufacturing rule.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        var row = new HBoxContainer();
        _v09ThicknessTarget = new SpinBox { MinValue = .01, MaxValue = 100, Step = .05, Value = .80, Suffix = " mm target", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v09ThicknessSamples = new SpinBox { MinValue = 500, MaxValue = 50000, Step = 500, Value = 12000, Suffix = " samples", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(_v09ThicknessTarget); row.AddChild(_v09ThicknessSamples); panel.AddChild(row);

        _v09ThicknessEmphasize = new CheckButton { Text = "Emphasize below-target regions", ButtonPressed = false };
        panel.AddChild(_v09ThicknessEmphasize);
        var buttons = new HBoxContainer();
        AddV09Button(buttons, "Generate Heatmap", async () => await GenerateV09ThicknessHeatmapAsync());
        AddV09Button(buttons, "Clear Heatmap", ClearV09ThicknessHeatmap);
        panel.AddChild(buttons);
        _v09ThicknessSummary = new Label { Text = "No thickness map yet.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        panel.AddChild(_v09ThicknessSummary);
        panel.AddChild(new Label { Text = "Legend: red = substantially below target · yellow = near target · green = comfortably above target · blue = much thicker. Values are estimated local wall thickness in model units (mm).", AutowrapMode = TextServer.AutowrapMode.WordSmart });
    }

    async Task GenerateV09ThicknessHeatmapAsync()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a mesh for thickness analysis."); return; }
        var source = _selected;
        try
        {
            ClearV09ThicknessHeatmap();
            string input = ExportSelectedV09Input(source, "thickness");
            double target = _v09ThicknessTarget?.Value ?? .80;
            int samples = (int)(_v09ThicknessSamples?.Value ?? 12000);
            SetStatus($"Calculating local thickness ({samples:N0} ray samples)…");
            string json = await _ai.ThicknessMapAsync(input, target, samples);
            using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
            var positionsJson = root.GetProperty("sample_positions_mm");
            var valuesJson = root.GetProperty("sample_values_mm");
            int count = Math.Min(positionsJson.GetArrayLength(), valuesJson.GetArrayLength());
            if (count == 0) throw new InvalidOperationException("No opposite-surface thickness samples could be resolved. The mesh may be open, single-sided, or have inconsistent normals.");

            var positions = new Vector3[count]; var values = new float[count];
            for (int i = 0; i < count; i++)
            {
                var p = positionsJson[i]; positions[i] = new Vector3(p[0].GetSingle(), p[1].GetSingle(), p[2].GetSingle()); values[i] = valuesJson[i].GetSingle();
            }
            var colored = BuildV09HeatmapMesh(source, positions, values, (float)target, _v09ThicknessEmphasize?.ButtonPressed ?? false);
            _v09ThicknessOverlay = new MeshInstance3D { Name = "Thickness Heatmap v0.9", Mesh = colored, GlobalTransform = source.GlobalTransform };
            _v09ThicknessOverlay.MaterialOverride = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = .72f, Metallic = 0f };
            _world?.AddChild(_v09ThicknessOverlay);
            _v09ThicknessSource = source; source.Visible = false;

            double min = root.TryGetProperty("minimum_mm", out var mn) && mn.ValueKind == JsonValueKind.Number ? mn.GetDouble() : double.NaN;
            double max = root.TryGetProperty("maximum_mm", out var mx) && mx.ValueKind == JsonValueKind.Number ? mx.GetDouble() : double.NaN;
            int below = root.GetProperty("below_target_samples").GetInt32();
            if (_v09ThicknessSummary != null) _v09ThicknessSummary.Text = $"Resolved {count:N0} surface samples. Estimated range: {min:0.###}–{max:0.###} mm. {below:N0} sampled locations are below the {target:0.###} mm target.";
            SetStatus("Thickness heatmap ready. The original model is unchanged.");
        }
        catch (Exception ex) { ClearV09ThicknessHeatmap(); SetStatus("Thickness heatmap failed: " + ex.Message); }
    }

    ArrayMesh BuildV09HeatmapMesh(MeshInstance3D source, Vector3[] samplePositions, float[] sampleValues, float target, bool emphasize)
    {
        var output = new ArrayMesh(); if (source.Mesh == null) return output;
        float cell = Math.Max(.05f, target * .55f);
        var grid = new Dictionary<Vector3I, List<int>>();
        for (int i = 0; i < samplePositions.Length; i++)
        {
            var key = V09Cell(samplePositions[i], cell); if (!grid.TryGetValue(key, out var list)) grid[key] = list = new List<int>(); list.Add(i);
        }
        for (int s = 0; s < source.Mesh.GetSurfaceCount(); s++)
        {
            var arrays = source.Mesh.SurfaceGetArrays(s); var verts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array(); var colors = new Color[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 world = source.GlobalTransform * verts[i]; float thickness = V09NearestThickness(world, samplePositions, sampleValues, grid, cell, target * 4f);
                colors[i] = V09ThicknessColor(thickness, target, emphasize);
            }
            arrays[(int)Mesh.ArrayType.Color] = colors; output.AddSurfaceFromArrays(source.Mesh.SurfaceGetPrimitiveType(s), arrays);
        }
        return output;
    }

    static Vector3I V09Cell(Vector3 p, float size) => new((int)MathF.Floor(p.X / size), (int)MathF.Floor(p.Y / size), (int)MathF.Floor(p.Z / size));

    static float V09NearestThickness(Vector3 p, Vector3[] pos, float[] val, Dictionary<Vector3I, List<int>> grid, float cell, float fallback)
    {
        var c = V09Cell(p, cell); float best = float.PositiveInfinity, result = fallback;
        for (int radius = 0; radius <= 10; radius++)
        {
            bool found = false;
            for (int x = -radius; x <= radius; x++) for (int y = -radius; y <= radius; y++) for (int z = -radius; z <= radius; z++)
            {
                if (radius > 0 && Math.Abs(x) != radius && Math.Abs(y) != radius && Math.Abs(z) != radius) continue;
                if (!grid.TryGetValue(c + new Vector3I(x, y, z), out var ids)) continue;
                foreach (int id in ids) { float d = p.DistanceSquaredTo(pos[id]); if (d < best) { best = d; result = val[id]; found = true; } }
            }
            if (found) break;
        }
        return result;
    }

    static Color V09ThicknessColor(float value, float target, bool emphasize)
    {
        float ratio = value / Math.Max(target, .0001f);
        if (emphasize && ratio >= 1f) return new Color(.18f, .18f, .18f);
        if (ratio < .6f) return new Color(1f, .05f, .03f);
        if (ratio < 1f) { float t = (ratio - .6f) / .4f; return new Color(1f, .12f + .78f * t, .02f); }
        if (ratio < 2f) { float t = ratio - 1f; return new Color(.15f * (1f - t), .95f, .12f + .45f * t); }
        float b = Math.Clamp((ratio - 2f) / 2f, 0f, 1f); return new Color(.05f, .85f * (1f - b) + .25f, .6f + .4f * b);
    }

    void ClearV09ThicknessHeatmap()
    {
        if (_v09ThicknessOverlay != null && GodotObject.IsInstanceValid(_v09ThicknessOverlay)) _v09ThicknessOverlay.QueueFree();
        _v09ThicknessOverlay = null;
        if (_v09ThicknessSource != null && GodotObject.IsInstanceValid(_v09ThicknessSource)) _v09ThicknessSource.Visible = true;
        _v09ThicknessSource = null;
    }
}
