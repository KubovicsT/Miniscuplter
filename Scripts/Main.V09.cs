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
    SpinBox? _v09FeatureThreshold;
    SpinBox? _v09RepairVoxel;
    RichTextLabel? _v09Report;
    Label? _v09Verdict;
    string _v09LastReportJson = "";

    public void InstallV09Extras()
    {
        var tabs = FindChild("TabContainer", true, false) as TabContainer;
        if (tabs == null) return;
        var scroll = new ScrollContainer { Name = "Model Integrity", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var panel = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(panel); tabs.AddChild(scroll);

        panel.AddChild(new Label { Text = "MODEL VALIDATION & REPAIR", ThemeTypeVariation = "HeaderSmall" });
        panel.AddChild(new Label { Text = "Structural topology checks determine mesh integrity. Feature-size and self-intersection results are advisory geometry heuristics; they do not decide whether a model is valid or require the model to be intended for 3D printing.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        var settings = new HBoxContainer();
        _v09FeatureThreshold = new SpinBox { MinValue = .05, MaxValue = 10, Step = .05, Value = .60, Suffix = " mm", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v09RepairVoxel = new SpinBox { MinValue = .04, MaxValue = 5, Step = .02, Value = .30, Suffix = " mm", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        settings.AddChild(_v09FeatureThreshold); settings.AddChild(_v09RepairVoxel); panel.AddChild(settings);
        panel.AddChild(new Label { Text = "Left: optional feature-size inspection threshold · Right: repair/finalization voxel pitch" });

        var analyzeRow = new HBoxContainer();
        AddV09Button(analyzeRow, "Analyze Selected", async () => await AnalyzeSelectedV09Async());
        AddV09Button(analyzeRow, "Save Report JSON", SaveV09ReportDialog);
        panel.AddChild(analyzeRow);

        var repairRow = new HBoxContainer();
        AddV09Button(repairRow, "Repair Selected", async () => await RepairSelectedV09Async());
        AddV09Button(repairRow, "Finalize Visible Scene", async () => await BakeSceneV09Async());
        panel.AddChild(repairRow);

        _v09Verdict = new Label { Text = "No model analysis yet.", AutowrapMode = TextServer.AutowrapMode.WordSmart }; panel.AddChild(_v09Verdict);
        _v09Report = new RichTextLabel { CustomMinimumSize = new Vector2(0, 420), FitContent = false, BbcodeEnabled = true, ScrollActive = true };
        panel.AddChild(_v09Report);
    }

    static void AddV09Button(Container parent, string text, Action action)
    {
        var b = new Button { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        b.Pressed += action; parent.AddChild(b);
    }

    string V09PrepDir()
    {
        string dir = ProjectSettings.GlobalizePath("user://model_integrity"); Directory.CreateDirectory(dir); return dir;
    }

    string ExportSelectedV09Input(MeshInstance3D obj, string prefix)
    {
        string path = Path.Combine(V09PrepDir(), $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
        MeshIO.SaveBinaryStl(BakeToWorldMesh(obj), path); return path;
    }

    async Task AnalyzeSelectedV09Async()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a mesh to analyze."); return; }
        try
        {
            string input = ExportSelectedV09Input(_selected, "analyze");
            SetStatus("Running model integrity analysis…");
            string json = await _ai.AnalyzeGeometryAsync(input, _v09FeatureThreshold?.Value ?? .60);
            _v09LastReportJson = json; RenderV09Report(json); SetStatus("Model integrity analysis complete.");
        }
        catch (Exception ex) { SetStatus("Model integrity analysis failed: " + ex.Message); }
    }

    void RenderV09Report(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json); var r = doc.RootElement;
            bool structurallyValid = r.TryGetProperty("structurally_valid", out var valid)
                ? valid.GetBoolean()
                : r.TryGetProperty("structurally_printable", out var legacy) && legacy.GetBoolean();
            bool watertight = r.GetProperty("watertight").GetBoolean();
            bool winding = r.GetProperty("winding_consistent").GetBoolean();
            int open = r.GetProperty("open_edges").GetInt32();
            int nonmanifold = r.GetProperty("nonmanifold_edges").GetInt32();
            int shells = r.GetProperty("connected_shells").GetInt32();
            int degenerate = r.GetProperty("degenerate_faces").GetInt32();
            var fs = r.GetProperty("feature_size"); var si = r.GetProperty("self_intersection");
            int fragile = fs.GetProperty("flagged_triangles").GetInt32();
            int intersectionCandidates = si.GetProperty("candidate_pairs").GetInt32();
            bool truncated = si.GetProperty("truncated").GetBoolean();
            string volume = r.TryGetProperty("volume_mm3", out var vol) && vol.ValueKind == JsonValueKind.Number ? $"{vol.GetDouble():N1} mm³" : "n/a (not closed)";
            var b = r.GetProperty("bounds_mm").EnumerateArray().Select(x => x.GetDouble()).ToArray();

            if (_v09Verdict != null)
                _v09Verdict.Text = structurallyValid ? "STRUCTURAL VERDICT: valid closed/manifold mesh by current checks." : "STRUCTURAL VERDICT: structural repair or inspection recommended.";
            if (_v09Report != null)
            {
                _v09Report.Text =
                    $"[b]Mesh[/b]\nVertices: {r.GetProperty("vertices").GetInt32():N0}\nTriangles: {r.GetProperty("triangles").GetInt32():N0}\nBounds: {b[0]:0.00} × {b[1]:0.00} × {b[2]:0.00} mm\nVolume: {volume}\n\n" +
                    $"[b]Structural integrity[/b]\nWatertight: {watertight}\nWinding consistent: {winding}\nOpen edges: {open:N0}\nNon-manifold edges: {nonmanifold:N0}\nConnected shells: {shells:N0}\nDegenerate faces: {degenerate:N0}\n\n" +
                    $"[b]Advisory geometry heuristics[/b]\nFeature-size flagged triangles (< {fs.GetProperty("threshold_mm").GetDouble():0.00} mm altitude): {fragile:N0}\nPotential self-intersection broad-phase pairs: {intersectionCandidates:N0}{(truncated ? " (scan capped)" : "")}\n\n" +
                    "[i]Feature-size is triangle geometry, not wall thickness. Self-intersection uses non-adjacent triangle bounding-box overlap and can contain false positives. These advisory values do not make an otherwise structurally valid model invalid.[/i]";
            }
        }
        catch (Exception ex) { if (_v09Report != null) _v09Report.Text = "Could not render report: " + ex.Message; }
    }

    async Task RepairSelectedV09Async()
    {
        if (_selected?.Mesh is not ArrayMesh mesh) { SetStatus("Select a mesh to repair."); return; }
        var target = _selected;
        try
        {
            string input = ExportSelectedV09Input(target, "repair_input");
            string output = Path.Combine(V09PrepDir(), $"repaired_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
            double pitch = _v09RepairVoxel?.Value ?? .30;
            SetStatus($"Repairing by filled voxel reconstruction at {pitch:0.00} mm…");
            string path = await _ai.RepairGeometryAsync(input, output, pitch);
            var repaired = MeshIO.LoadStl(path);
            PushUndo(mesh);
            target.Mesh = repaired; target.Transform = Transform3D.Identity;
            V095TopologyChanged(target);
            Select(target); FrameSelected();
            SetStatus("Repair complete. Mesh was destructively voxel-reconstructed; fine detail below the selected pitch may be softened. Undo remains available.");
            await AnalyzeSelectedV09Async();
        }
        catch (Exception ex) { SetStatus("Repair failed safely; original mesh remains active: " + ex.Message); }
    }

    async Task BakeSceneV09Async()
    {
        var meshes = _objects.Where(o => GodotObject.IsInstanceValid(o) && o.Visible && o.Mesh != null).ToList();
        if (meshes.Count == 0) { SetStatus("Scene contains no visible meshes to finalize."); return; }
        try
        {
            var inputs = new List<string>();
            foreach (var obj in meshes) inputs.Add(ExportSelectedV09Input(obj, "scene_part"));
            string output = Path.Combine(V09PrepDir(), $"final_model_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
            double pitch = _v09RepairVoxel?.Value ?? .30;
            SetStatus($"Finalizing {inputs.Count} visible scene mesh(es) into one filled voxel shell…");
            string path = await _ai.VoxelRemeshAsync(inputs, output, pitch);
            AddMeshObject(MeshIO.LoadStl(path), "Final Model");
            SetStatus($"Final Model added as a new object; all source objects remain untouched. Intermediate STL: {path}");
            await AnalyzeSelectedV09Async();
        }
        catch (Exception ex) { SetStatus("Scene finalization failed safely; source objects remain unchanged: " + ex.Message); }
    }

    void SaveV09ReportDialog()
    {
        if (string.IsNullOrWhiteSpace(_v09LastReportJson)) { SetStatus("Run an analysis first."); return; }
        var d = new FileDialog { FileMode = FileDialog.FileModeEnum.SaveFile, Access = FileDialog.AccessEnum.Filesystem, Filters = new[] { "*.json ; JSON report" }, CurrentFile = "model_integrity_report.json", UseNativeDialog = true };
        AddChild(d); d.FileSelected += p => { try { File.WriteAllText(p, _v09LastReportJson); SetStatus("Saved model integrity report: " + p); } catch (Exception ex) { SetStatus("Report save failed: " + ex.Message); } d.QueueFree(); }; d.Canceled += d.QueueFree; d.PopupCenteredRatio(.75f);
    }
}