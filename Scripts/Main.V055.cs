using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Miniscuplter;

public partial class Main
{
    bool _v055CancelRequested;
    Timer? _v055AutosaveTimer;
    Label? _v055JobStatus;
    Vector3 _v055AnchorNormal = Vector3.Up;
    float _v055AnchorScale = 1f;
    readonly List<AiLayerDto> _v055AiLayers = new();

    public sealed class AiLayerDto
    {
        public string ObjectName { get; set; } = "";
        public string SourceObjectName { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string Quality { get; set; } = "standard";
        public string SourceImage { get; set; } = "";
        public string MaskImage { get; set; } = "";
        public float[] Anchor { get; set; } = new float[3];
        public float[] Normal { get; set; } = new float[] { 0, 1, 0 };
        public float InitialScale { get; set; } = 1f;
        public long CreatedUtcTicks { get; set; }
    }

    public void InstallV055Extras()
    {
        var ai = FindChild("AI", true, false) as VBoxContainer;
        if (ai != null)
        {
            ai.AddChild(new HSeparator());
            ai.AddChild(new Label { Text = "STABILIZATION — v0.5.5", ThemeTypeVariation = "HeaderSmall" });
            var ctx = new Button { Text = "Capture RGB + Depth + Normals" };
            ctx.Pressed += CaptureEnhancedGeometryContext;
            ai.AddChild(ctx);
            var cancel = new Button { Text = "Cancel Current AI / Geometry Job" };
            cancel.Pressed += CancelCurrentV055Job;
            ai.AddChild(cancel);
            _v055JobStatus = new Label { Text = "Job controls ready.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
            ai.AddChild(_v055JobStatus);
        }

        var transform = FindChild("Transform", true, false) as VBoxContainer;
        if (transform != null)
        {
            transform.AddChild(new HSeparator());
            transform.AddChild(new Label { Text = "PRECISE TRANSFORM — v0.5.5", ThemeTypeVariation = "HeaderSmall" });
            var offset = new SpinBox { MinValue = -1000, MaxValue = 1000, Step = 0.1, Value = 0 };
            transform.AddChild(new Label { Text = "Move along current patch normal (mm)" });
            transform.AddChild(offset);
            var applyOffset = new Button { Text = "Apply normal offset" };
            applyOffset.Pressed += () =>
            {
                if (_selected == null) return;
                _selected.Position += _v055AnchorNormal.Normalized() * (float)offset.Value;
                SetStatus("Applied precise surface-normal offset.");
            };
            transform.AddChild(applyOffset);
        }

        var print = FindChild("Print", true, false) as VBoxContainer;
        if (print != null)
        {
            print.AddChild(new HSeparator());
            print.AddChild(new Label { Text = "MESH VALIDATION — v0.5.5", ThemeTypeVariation = "HeaderSmall" });
            var validate = new Button { Text = "Validate selected mesh" };
            validate.Pressed += ValidateSelectedMeshV055;
            print.AddChild(validate);
        }

        _v055AutosaveTimer = new Timer { WaitTime = 120, OneShot = false, Autostart = true };
        _v055AutosaveTimer.Timeout += AutosaveV055;
        AddChild(_v055AutosaveTimer);
    }

    internal void ResetV055Cancellation()
    {
        _v055CancelRequested = false;
        if (_v055JobStatus != null) _v055JobStatus.Text = "Job running…";
    }

    internal bool V055CancellationRequested => _v055CancelRequested;

    void CancelCurrentV055Job()
    {
        _v055CancelRequested = true;
        _ai.CancelCurrentRequest();
        if (_v055JobStatus != null) _v055JobStatus.Text = "Cancellation requested. Providers that support cooperative cancellation stop immediately; others may finish their current internal step.";
        SetStatus("Cancellation requested for the current job.");
    }

    internal void RememberV055Patch(MeshInstance3D patch, MeshInstance3D? source, string prompt, string quality, string image, string mask)
    {
        Vector3 anchor = _v05Anchor ?? patch.Position;
        _v055AiLayers.RemoveAll(x => x.ObjectName == patch.Name.ToString());
        _v055AiLayers.Add(new AiLayerDto
        {
            ObjectName = patch.Name.ToString(),
            SourceObjectName = source?.Name.ToString() ?? "",
            Prompt = prompt,
            Quality = quality,
            SourceImage = image,
            MaskImage = mask,
            Anchor = new[] { anchor.X, anchor.Y, anchor.Z },
            Normal = new[] { _v055AnchorNormal.X, _v055AnchorNormal.Y, _v055AnchorNormal.Z },
            InitialScale = _v055AnchorScale,
            CreatedUtcTicks = DateTime.UtcNow.Ticks
        });
    }

    internal List<AiLayerDto> ExportV055AiLayers() => _v055AiLayers.Select(x => new AiLayerDto
    {
        ObjectName = x.ObjectName, SourceObjectName = x.SourceObjectName, Prompt = x.Prompt, Quality = x.Quality,
        SourceImage = x.SourceImage, MaskImage = x.MaskImage, Anchor = x.Anchor.ToArray(), Normal = x.Normal.ToArray(),
        InitialScale = x.InitialScale, CreatedUtcTicks = x.CreatedUtcTicks
    }).ToList();

    internal void ImportV055AiLayers(List<AiLayerDto>? layers)
    {
        _v055AiLayers.Clear();
        if (layers != null) _v055AiLayers.AddRange(layers);
    }

    internal void SetV055AnchorSurface(Vector3 normal, float scale)
    {
        _v055AnchorNormal = normal.LengthSquared() > 0.0001f ? normal.Normalized() : Vector3.Up;
        _v055AnchorScale = Math.Clamp(scale, 0.05f, 100f);
    }

    internal void ApplyV055PatchAlignment(MeshInstance3D patch, Vector3 anchor)
    {
        patch.Position = anchor;
        var n = _v055AnchorNormal.Normalized();
        var q = new Quaternion(Vector3.Up, n);
        patch.Basis = new Basis(q).Scaled(Vector3.One * _v055AnchorScale);
    }

    void CaptureEnhancedGeometryContext()
    {
        if (_camera == null) { SetStatus("Camera is not ready."); return; }
        CaptureView();
        var sub = FindChild("Viewport", true, false) as SubViewport;
        if (sub == null) return;

        const int maxSide = 256;
        Vector2 viewport = sub.GetVisibleRect().Size;
        int w = Math.Max(32, Math.Min(maxSide, (int)viewport.X));
        int h = Math.Max(32, Math.Min(maxSide, (int)Math.Round(viewport.Y * (w / Math.Max(1.0, viewport.X)))));
        var depth = new float[w * h];
        var normals = new Vector3[w * h];
        Array.Fill(depth, float.PositiveInfinity);

        float sx = w / Math.Max(1f, viewport.X);
        float sy = h / Math.Max(1f, viewport.Y);
        var camInv = _camera.GlobalTransform.AffineInverse();

        foreach (var obj in _objects)
        {
            if (obj.Mesh == null) continue;
            var gt = obj.GlobalTransform;
            for (int s = 0; s < obj.Mesh.GetSurfaceCount(); s++)
            {
                var arrays = obj.Mesh.SurfaceGetArrays(s);
                var verts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var idx = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                int count = idx.Length > 0 ? idx.Length : verts.Length;
                for (int i = 0; i + 2 < count; i += 3)
                {
                    Vector3 a = gt * verts[idx.Length > 0 ? idx[i] : i];
                    Vector3 b = gt * verts[idx.Length > 0 ? idx[i + 1] : i + 1];
                    Vector3 c = gt * verts[idx.Length > 0 ? idx[i + 2] : i + 2];
                    float za = -(camInv * a).Z, zb = -(camInv * b).Z, zc = -(camInv * c).Z;
                    if (za <= 0 && zb <= 0 && zc <= 0) continue;
                    Vector2 pa = _camera.UnprojectPosition(a) * new Vector2(sx, sy);
                    Vector2 pb = _camera.UnprojectPosition(b) * new Vector2(sx, sy);
                    Vector2 pc = _camera.UnprojectPosition(c) * new Vector2(sx, sy);
                    RasterTriangle(pa, pb, pc, za, zb, zc, (b - a).Cross(c - a).Normalized(), depth, normals, w, h);
                }
            }
        }

        float minD = depth.Where(float.IsFinite).DefaultIfEmpty(0f).Min();
        float maxD = depth.Where(float.IsFinite).DefaultIfEmpty(1f).Max();
        float span = Math.Max(0.0001f, maxD - minD);
        var depthImg = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);
        var normalImg = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            int k = y * w + x;
            if (!float.IsFinite(depth[k])) { depthImg.SetPixel(x, y, Colors.Black); normalImg.SetPixel(x, y, new Color(0.5f, 0.5f, 0.5f)); continue; }
            float d = 1f - Math.Clamp((depth[k] - minD) / span, 0f, 1f);
            depthImg.SetPixel(x, y, new Color(d, d, d));
            Vector3 n = normals[k];
            normalImg.SetPixel(x, y, new Color(n.X * .5f + .5f, n.Y * .5f + .5f, n.Z * .5f + .5f));
        }

        string folder = ProjectSettings.GlobalizePath("user://geometry_context");
        Directory.CreateDirectory(folder);
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string depthPath = Path.Combine(folder, $"depth_{stamp}.png");
        string normalPath = Path.Combine(folder, $"normals_{stamp}.png");
        string metaPath = Path.Combine(folder, $"enhanced_{stamp}.json");
        depthImg.SavePng(depthPath); normalImg.SavePng(normalPath);
        File.WriteAllText(metaPath, JsonSerializer.Serialize(new
        {
            version = "0.5.5", rgb_path = _lastCapture, depth_path = depthPath, normal_path = normalPath,
            buffer_width = w, buffer_height = h, depth_min_mm = minD, depth_max_mm = maxD,
            camera_position = new[] { _camera.GlobalPosition.X, _camera.GlobalPosition.Y, _camera.GlobalPosition.Z },
            camera_fov = _camera.Fov
        }, new JsonSerializerOptions { WriteIndented = true }));
        SetStatus("Enhanced geometry context captured with RGB, depth and world-space normals.");
    }

    static void RasterTriangle(Vector2 a, Vector2 b, Vector2 c, float za, float zb, float zc, Vector3 normal,
        float[] depth, Vector3[] normals, int w, int h)
    {
        float area = Edge(a, b, c);
        if (Math.Abs(area) < 0.00001f) return;
        int minX = Math.Clamp((int)Math.Floor(Math.Min(a.X, Math.Min(b.X, c.X))), 0, w - 1);
        int maxX = Math.Clamp((int)Math.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))), 0, w - 1);
        int minY = Math.Clamp((int)Math.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y))), 0, h - 1);
        int maxY = Math.Clamp((int)Math.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y))), 0, h - 1);
        for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
        {
            Vector2 p = new(x + .5f, y + .5f);
            float w0 = Edge(b, c, p) / area, w1 = Edge(c, a, p) / area, w2 = Edge(a, b, p) / area;
            if (w0 < 0 || w1 < 0 || w2 < 0) continue;
            float z = za * w0 + zb * w1 + zc * w2;
            int k = y * w + x;
            if (z > 0 && z < depth[k]) { depth[k] = z; normals[k] = normal; }
        }
    }

    static float Edge(Vector2 a, Vector2 b, Vector2 p) => (p.X - a.X) * (b.Y - a.Y) - (p.Y - a.Y) * (b.X - a.X);

    internal static bool RayMeshDetailedV055(Vector3 ro, Vector3 rd, MeshInstance3D obj, out Vector3 hit, out Vector3 normal)
    {
        hit = default; normal = Vector3.Up;
        if (obj.Mesh == null) return false;
        float best = float.PositiveInfinity; bool found = false; var gt = obj.GlobalTransform;
        for (int s = 0; s < obj.Mesh.GetSurfaceCount(); s++)
        {
            var a = obj.Mesh.SurfaceGetArrays(s); var verts = a[(int)Mesh.ArrayType.Vertex].AsVector3Array(); var idx = a[(int)Mesh.ArrayType.Index].AsInt32Array();
            int count = idx.Length > 0 ? idx.Length : verts.Length;
            for (int i = 0; i + 2 < count; i += 3)
            {
                var v0 = gt * verts[idx.Length > 0 ? idx[i] : i]; var v1 = gt * verts[idx.Length > 0 ? idx[i + 1] : i + 1]; var v2 = gt * verts[idx.Length > 0 ? idx[i + 2] : i + 2];
                var p = Geometry3D.RayIntersectsTriangle(ro, rd, v0, v1, v2);
                if (p.VariantType != Variant.Type.Vector3) continue;
                var point = p.AsVector3(); float d = ro.DistanceSquaredTo(point);
                if (d < best) { best = d; hit = point; normal = (v1 - v0).Cross(v2 - v0).Normalized(); found = true; }
            }
        }
        return found;
    }

    internal float EstimatePatchScaleFromMaskV055(MeshInstance3D source)
    {
        if (_v05PaintMask == null) return 1f;
        long painted = 0; long total = (long)_v05PaintMask.GetWidth() * _v05PaintMask.GetHeight();
        for (int y = 0; y < _v05PaintMask.GetHeight(); y++) for (int x = 0; x < _v05PaintMask.GetWidth(); x++)
            if (_v05PaintMask.GetPixel(x, y).R > .5f) painted++;
        float frac = total > 0 ? (float)painted / total : .01f;
        Vector3 size = source.GetAabb().Size * source.Scale.Abs();
        float objectSize = Math.Max(size.X, Math.Max(size.Y, size.Z));
        return Math.Clamp((float)(Math.Sqrt(Math.Max(frac, .0001f)) * objectSize / 20f), .15f, 5f);
    }

    void ValidateSelectedMeshV055()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a mesh first."); return; }
        long triangles = 0, openEdges = 0, nonManifold = 0;
        var edges = new Dictionary<string, int>();
        for (int s = 0; s < _selected.Mesh.GetSurfaceCount(); s++)
        {
            var arrays = _selected.Mesh.SurfaceGetArrays(s); var v = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array(); var idx = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            int count = idx.Length > 0 ? idx.Length : v.Length;
            for (int i = 0; i + 2 < count; i += 3)
            {
                triangles++;
                int ia = idx.Length > 0 ? idx[i] : i, ib = idx.Length > 0 ? idx[i + 1] : i + 1, ic = idx.Length > 0 ? idx[i + 2] : i + 2;
                AddEdge(edges, v[ia], v[ib]); AddEdge(edges, v[ib], v[ic]); AddEdge(edges, v[ic], v[ia]);
            }
        }
        foreach (int n in edges.Values) { if (n == 1) openEdges++; else if (n > 2) nonManifold++; }
        string verdict = openEdges == 0 && nonManifold == 0 ? "closed/manifold by edge test" : "needs attention";
        SetStatus($"Mesh validation: {triangles:N0} triangles, {openEdges:N0} open edges, {nonManifold:N0} non-manifold edges — {verdict}.");
    }

    static void AddEdge(Dictionary<string, int> edges, Vector3 a, Vector3 b)
    {
        string A = Key(a), B = Key(b); string k = string.CompareOrdinal(A, B) <= 0 ? A + "|" + B : B + "|" + A;
        edges[k] = edges.TryGetValue(k, out int n) ? n + 1 : 1;
    }
    static string Key(Vector3 v) => $"{Math.Round(v.X, 4)},{Math.Round(v.Y, 4)},{Math.Round(v.Z, 4)}";

    void AutosaveV055()
    {
        if (_objects.Count == 0) return;
        try
        {
            string p = ProjectSettings.GlobalizePath("user://recovery/autosave.msculpt");
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            SaveProject(p);
            if (_v055JobStatus != null) _v055JobStatus.Text = "Autosaved recovery project.";
        }
        catch { }
    }
}
