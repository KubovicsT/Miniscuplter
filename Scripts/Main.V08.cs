using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    readonly Dictionary<string, float[]> _v08Masks = new();
    OptionButton? _v08Falloff;
    OptionButton? _v08Alpha;
    CheckButton? _v08SymX; CheckButton? _v08SymY; CheckButton? _v08SymZ;
    CheckButton? _v08AutoRemesh;
    SpinBox? _v08RemeshVoxel;
    Label? _v08MaskStatus;
    bool _v08MaskPaintMode;
    bool _v08MaskErase;
    Node3D? _v08BrushCursor;
    int _v08CompletedStrokes;
    bool _v08RemeshRunning;
    bool _v08StrokeArmed;
    readonly List<ArrayMesh> _v08MaskRedoBackup = new();

    public void InstallV08Extras()
    {
        var tabs = FindChild("TabContainer", true, false) as TabContainer;
        if (tabs == null) return;
        var scroll = new ScrollContainer { Name = "Sculpt v0.8", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var panel = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(panel); tabs.AddChild(scroll);

        panel.AddChild(new Label { Text = "ADVANCED SCULPTING — v0.8", ThemeTypeVariation = "HeaderSmall" });
        panel.AddChild(new Label { Text = "Symmetry, masks, procedural brush alphas, improved falloff, additional brushes, brush feedback and periodic voxel detail remeshing.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        panel.AddChild(new Label { Text = "Symmetry (object-local planes)" });
        var sym = new HBoxContainer();
        _v08SymX = new CheckButton { Text = "X", ButtonPressed = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v08SymY = new CheckButton { Text = "Y", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v08SymZ = new CheckButton { Text = "Z", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        sym.AddChild(_v08SymX); sym.AddChild(_v08SymY); sym.AddChild(_v08SymZ); panel.AddChild(sym);

        var shaping = new HBoxContainer();
        _v08Falloff = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (string n in Enum.GetNames<SculptFalloff>()) _v08Falloff.AddItem(n);
        _v08Alpha = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (string n in Enum.GetNames<SculptAlpha>()) _v08Alpha.AddItem(n);
        shaping.AddChild(_v08Falloff); shaping.AddChild(_v08Alpha); panel.AddChild(shaping);
        panel.AddChild(new Label { Text = "Falloff / procedural alpha. Existing brush Radius and Strength controls still apply." });

        panel.AddChild(new HSeparator());
        panel.AddChild(new Label { Text = "SCULPT MASK" });
        var maskRow = new HBoxContainer();
        AddV08Button(maskRow, "Paint Protect Mask", () => SetV08MaskMode(true, false));
        AddV08Button(maskRow, "Erase Mask", () => SetV08MaskMode(true, true));
        AddV08Button(maskRow, "Sculpt Mode", () => SetV08MaskMode(false, false));
        panel.AddChild(maskRow);
        var maskOps = new HBoxContainer();
        AddV08Button(maskOps, "Mask All", () => FillV08Mask(1f));
        AddV08Button(maskOps, "Clear Mask", () => FillV08Mask(0f));
        AddV08Button(maskOps, "Invert Mask", InvertV08Mask);
        panel.AddChild(maskOps);
        _v08MaskStatus = new Label { Text = "Mask: none", AutowrapMode = TextServer.AutowrapMode.WordSmart }; panel.AddChild(_v08MaskStatus);

        panel.AddChild(new HSeparator());
        panel.AddChild(new Label { Text = "DETAIL / REMESH" });
        _v08RemeshVoxel = new SpinBox { MinValue = .08, MaxValue = 2.0, Step = .02, Value = .28, Suffix = " mm", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddChild(_v08RemeshVoxel);
        var remeshRow = new HBoxContainer();
        AddV08Button(remeshRow, "Remesh Selected Now", async () => await V08RemeshSelectedAsync());
        _v08AutoRemesh = new CheckButton { Text = "Auto every 12 strokes", ButtonPressed = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        remeshRow.AddChild(_v08AutoRemesh); panel.AddChild(remeshRow);
        panel.AddChild(new Label { Text = "v0.8 periodic remesh uses the existing CPU voxel backend. It is a robust topology refresh, not ZBrush-style per-triangle dynamic topology. Smaller voxels preserve more detail but cost substantially more RAM/CPU.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        if (FindChild("ViewportHost", true, false) is SubViewportContainer host) host.GuiInput += OnV08ViewportInput;
        RefreshV08BrushNames();
    }

    static void AddV08Button(Container parent, string text, Action action)
    {
        var b = new Button { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; b.Pressed += action; parent.AddChild(b);
    }

    void RefreshV08BrushNames()
    {
        if (_brushSelect == null) return;
        int previous = _brushSelect.Selected;
        _brushSelect.Clear();
        foreach (string n in Enum.GetNames<SculptBrush>()) _brushSelect.AddItem(n);
        _brushSelect.Select(Math.Clamp(previous, 0, _brushSelect.ItemCount - 1));
    }

    ArrayMesh ApplyV08Sculpt(ArrayMesh mesh, Vector3 hitLocal, Vector3 dragLocal, float radius, float strength, SculptBrush brush)
    {
        if (_v08MaskPaintMode)
        {
            if (dragLocal.LengthSquared() < 1e-12f) CancelV08MaskMeshHistoryEntry();
            PaintV08Mask(mesh, hitLocal, radius, _v08MaskErase ? -Math.Max(.15f, strength * .35f) : Math.Max(.15f, strength * .35f));
            return mesh;
        }

        V095PrepareRiggedDirectMeshEdit(mesh);
        var centers = V08SymmetryCenters(hitLocal);
        float[]? mask = GetV08Mask(mesh, false);
        var falloff = (SculptFalloff)(_v08Falloff?.Selected ?? 0);
        var alpha = (SculptAlpha)(_v08Alpha?.Selected ?? 0);
        return SculptEngine.ApplyAdvanced(mesh, centers, dragLocal, radius, strength, brush, falloff, alpha, mask);
    }

    void CaptureV08MaskRedoHistory()
    {
        if (!_v08MaskPaintMode || _sculpting) return;
        _v08MaskRedoBackup.Clear();
        foreach (var mesh in _redo.Reverse()) _v08MaskRedoBackup.Add(CloneMesh(mesh));
    }

    void CancelV08MaskMeshHistoryEntry()
    {
        if (_undo.Count > 0) _undo.Pop();
        _redo.Clear();
        foreach (var mesh in _v08MaskRedoBackup) _redo.Push(CloneMesh(mesh));
    }

    List<Vector3> V08SymmetryCenters(Vector3 p)
    {
        var result = new List<Vector3> { p };
        if (_v08SymX?.ButtonPressed == true) MirrorV08(result, 0);
        if (_v08SymY?.ButtonPressed == true) MirrorV08(result, 1);
        if (_v08SymZ?.ButtonPressed == true) MirrorV08(result, 2);
        return result.Distinct().ToList();
    }

    static void MirrorV08(List<Vector3> points, int axis)
    {
        foreach (Vector3 p in points.ToArray())
        {
            Vector3 q = p;
            if (axis == 0) q.X = -q.X; else if (axis == 1) q.Y = -q.Y; else q.Z = -q.Z;
            points.Add(q);
        }
    }

    void SetV08MaskMode(bool enabled, bool erase)
    {
        _v08MaskPaintMode = enabled; _v08MaskErase = erase;
        if (enabled) CaptureV08MaskRedoHistory(); else _v08MaskRedoBackup.Clear();
        SetStatus(enabled ? (erase ? "Mask erase mode: LMB removes protection." : "Mask paint mode: LMB paints protected vertices.") : "Sculpt mode restored.");
    }

    float[]? GetV08Mask(ArrayMesh mesh, bool create)
    {
        if (_selected == null || mesh.GetSurfaceCount() == 0) return null;
        string key = _selected.Name.ToString();
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return null;
        int count = mdt.GetVertexCount();
        if (_v08Masks.TryGetValue(key, out var existing) && existing.Length == count) return existing;
        if (!create) return null;
        var values = new float[count]; _v08Masks[key] = values; return values;
    }

    void PaintV08Mask(ArrayMesh mesh, Vector3 hit, float radius, float delta)
    {
        float[]? mask = GetV08Mask(mesh, true); if (mask == null) return;
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return;
        for (int i = 0; i < mask.Length; i++)
        {
            float d = mdt.GetVertex(i).DistanceTo(hit); if (d > radius) continue;
            float w = 1f - d / Math.Max(radius, .0001f); mask[i] = Math.Clamp(mask[i] + delta * w, 0f, 1f);
        }
        UpdateV08MaskStatus(mask);
    }

    void FillV08Mask(float value)
    {
        if (_selected?.Mesh is not ArrayMesh mesh) return; float[]? mask = GetV08Mask(mesh, true); if (mask == null) return;
        Array.Fill(mask, value); UpdateV08MaskStatus(mask); SetStatus(value > .5f ? "All vertices protected by sculpt mask." : "Sculpt mask cleared.");
    }

    void InvertV08Mask()
    {
        if (_selected?.Mesh is not ArrayMesh mesh) return; float[]? mask = GetV08Mask(mesh, true); if (mask == null) return;
        for (int i = 0; i < mask.Length; i++) mask[i] = 1f - mask[i]; UpdateV08MaskStatus(mask); SetStatus("Sculpt mask inverted.");
    }

    void UpdateV08MaskStatus(float[] mask)
    {
        if (_v08MaskStatus == null) return;
        int protectedCount = mask.Count(v => v > .05f); double average = mask.Length == 0 ? 0 : mask.Average(v => (double)v);
        _v08MaskStatus.Text = $"Mask: {protectedCount:N0}/{mask.Length:N0} vertices affected · average protection {average:P0}";
    }

    void OnV08ViewportInput(InputEvent ev)
    {
        if (_camera == null || _selected == null) return;
        Vector2? pos = ev switch { InputEventMouseMotion mm => mm.Position, InputEventMouseButton mb => mb.Position, _ => null };
        if (pos.HasValue) UpdateV08Cursor(pos.Value);

        if (ev is InputEventMouseButton press && press.ButtonIndex == MouseButton.Left && press.Pressed)
        {
            Vector3 ro = _camera.ProjectRayOrigin(press.Position), rd = _camera.ProjectRayNormal(press.Position);
            _v08StrokeArmed = !_v07SocketPlacementMode && !_v07MountPointMode && !_regionMode && RayMeshDetailedV055(ro, rd, _selected, out _, out _);
        }
        else if (ev is InputEventMouseButton release && release.ButtonIndex == MouseButton.Left && !release.Pressed)
        {
            if (_v08StrokeArmed && !_v08MaskPaintMode)
            {
                V095CommitRiggedSculptRest(_selected);
                _v08CompletedStrokes++;
                if (_v08AutoRemesh?.ButtonPressed == true && _v08CompletedStrokes % 12 == 0) _ = V08RemeshSelectedAsync();
            }
            _v08StrokeArmed = false;
            if (_v08MaskPaintMode) CaptureV08MaskRedoHistory();
        }
    }

    void UpdateV08Cursor(Vector2 screen)
    {
        if (_camera == null || _selected == null || _world == null) return;
        Vector3 ro = _camera.ProjectRayOrigin(screen), rd = _camera.ProjectRayNormal(screen);
        if (!RayMeshDetailedV055(ro, rd, _selected, out var hit, out var normal)) { if (_v08BrushCursor != null) _v08BrushCursor.Visible = false; return; }
        if (_v08BrushCursor == null)
        {
            var cursor = new MeshInstance3D { Name = "Sculpt Brush Cursor v0.8" };
            cursor.Mesh = new CylinderMesh { TopRadius = 1f, BottomRadius = 1f, Height = .035f, RadialSegments = 48 };
            cursor.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, .55f, .15f), EmissionEnabled = true, Emission = new Color(.8f, .2f, .04f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
            _world.AddChild(cursor); _v08BrushCursor = cursor;
        }
        _v08BrushCursor.Visible = true;
        _v08BrushCursor.GlobalPosition = hit + normal * .03f;
        _v08BrushCursor.GlobalBasis = new Basis(new Quaternion(Vector3.Up, normal.Normalized())).Scaled(Vector3.One * (float)(_radius?.Value ?? 6));
    }

    async Task V08RemeshSelectedAsync()
    {
        if (_v08RemeshRunning || _selected?.Mesh is not ArrayMesh mesh) return;
        MeshInstance3D target = _selected;
        string? dir = null;
        _v08RemeshRunning = true;
        try
        {
            dir = ProjectSettings.GlobalizePath($"user://sculpt_remesh/job_{DateTime.Now:yyyyMMdd_HHmmss_fff}"); Directory.CreateDirectory(dir);
            string input = Path.Combine(dir, "input.stl"), output = Path.Combine(dir, "output.stl");
            MeshIO.SaveBinaryStl(mesh, input);
            SetStatus("v0.8 detail remesh running…");
            string result = await _ai.VoxelRemeshAsync(new[] { input }, output, _v08RemeshVoxel?.Value ?? .28);
            if (!GodotObject.IsInstanceValid(target) || !_objects.Contains(target)) { SetStatus("Remesh result discarded because the source object no longer exists."); return; }
            var loaded = MeshIO.LoadStl(result);
            if (loaded.GetSurfaceCount() == 0) throw new InvalidDataException("Remesh result contains no surfaces.");
            if (target.Mesh is ArrayMesh previous) PushUndo(previous);
            target.Mesh = loaded;
            _v08Masks.Remove(target.Name.ToString());
            V095TopologyChanged(target);
            SetStatus($"Detail remesh complete at {_v08RemeshVoxel?.Value ?? .28:0.00} mm voxels. Sculpt mask cleared because topology changed.");
        }
        catch (Exception ex) { SetStatus("Detail remesh failed without replacing the source mesh: " + ex.Message); }
        finally
        {
            _v08RemeshRunning = false;
            if (dir != null) { try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { } }
        }
    }
}
