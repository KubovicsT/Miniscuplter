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
    float[]? _v096Selection;
    MeshInstance3D? _v096SelectionObject;
    MeshInstance3D? _v096SelectionOverlay;
    string _v096SelectionQuery = "";
    string _v096SelectionTopology = "";

    public void InstallV096SmartSelect()
    {
        if (FindChild("ViewportHost", true, false) is SubViewportContainer host)
            host.GuiInput += OnV096SelectionViewportInput;
    }

    static string V096MeshSignature(ArrayMesh mesh)
    {
        var p = new List<string> { mesh.GetSurfaceCount().ToString() };
        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
        {
            var a = mesh.SurfaceGetArrays(s);
            p.Add($"{a[(int)Mesh.ArrayType.Vertex].AsVector3Array().Length}:{a[(int)Mesh.ArrayType.Index].AsInt32Array().Length}");
        }
        return string.Join("|", p);
    }

    void V096ValidateSelection()
    {
        if (_v096SelectionObject == null || !GodotObject.IsInstanceValid(_v096SelectionObject) || _v096SelectionObject.Mesh is not ArrayMesh m || V096MeshSignature(m) != _v096SelectionTopology)
        {
            ClearV096Selection(false);
        }
    }

    async Task SmartSelectV096Async(string query, char mode = '=')
    {
        query = (query ?? "").Trim();
        if (query.Length == 0) { SetStatus("Smart Select needs a target, for example /s head."); return; }
        if (_selected?.Mesh is not ArrayMesh mesh || mesh.GetSurfaceCount() == 0) { SetStatus("Select a mesh object first."); return; }
        MeshInstance3D target = _selected;
        float[]? found = TryV096MetadataSelection(target, mesh, query) ?? TryV096RigSelection(target, mesh, query);
        string method = found != null ? "scene/rig context" : "semantic backend";
        if (found == null)
        {
            try
            {
                string dir = ProjectSettings.GlobalizePath("user://smart_select"); Directory.CreateDirectory(dir);
                string input = Path.Combine(dir, $"select_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
                MeshIO.SaveBinaryStl(mesh, input);
                string json = await _ai.SemanticSelectAsync(input, query);
                found = ParseV096Selection(mesh, json, out method);
            }
            catch (Exception ex) { SetStatus("Smart Select failed without changing the current selection: " + ex.Message); return; }
        }
        if (found == null || found.Length == 0) { SetStatus($"Smart Select could not identify '{query}'."); return; }

        int count = found.Length;
        if (mode != '=' && (_v096SelectionObject != target || _v096Selection == null || _v096Selection.Length != count))
            mode = '=';
        if (mode == '+') for (int i = 0; i < count; i++) _v096Selection![i] = Math.Max(_v096Selection[i], found[i]);
        else if (mode == '-') for (int i = 0; i < count; i++) _v096Selection![i] = Math.Min(_v096Selection[i], 1f - found[i]);
        else { _v096Selection = found; _v096SelectionObject = target; }

        _v096SelectionObject = target;
        _v096SelectionQuery = query;
        _v096SelectionTopology = V096MeshSignature(mesh);
        RebuildV096SelectionOverlay();
        ApplyV096SelectionToSculptMask();
        int selected = _v096Selection.Count(v => v >= .5f);
        SetStatus($"Smart Select '{query}' via {method}: {selected:N0}/{count:N0} vertices active. Sculpting is constrained to the active selection.");
    }

    float[]? TryV096MetadataSelection(MeshInstance3D target, ArrayMesh mesh, string query)
    {
        string q = query.ToLowerInvariant();
        string name = target.Name.ToString().ToLowerInvariant();
        if (name.Contains(q) || q.Contains(name)) return V096AllVertices(mesh);
        var attachment = _v07Attachments.FirstOrDefault(a => a.PartObjectName.Equals(target.Name.ToString(), StringComparison.OrdinalIgnoreCase));
        if (attachment != null)
        {
            var part = _v07Parts.FirstOrDefault(p => p.Id == attachment.LibraryId);
            if (part != null && (part.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || part.Category.Contains(query, StringComparison.OrdinalIgnoreCase) || part.SocketType.Contains(query, StringComparison.OrdinalIgnoreCase)))
                return V096AllVertices(mesh);
        }
        return null;
    }

    float[]? TryV096RigSelection(MeshInstance3D target, ArrayMesh mesh, string query)
    {
        if (_v06RiggedObject != target || _v06CurrentRig == null || mesh.GetSurfaceCount() == 0) return null;
        string[] words = query.ToLowerInvariant().Split(new[] {' ', '-', '_'}, StringSplitOptions.RemoveEmptyEntries);
        var jointIds = Enumerable.Range(0, _v06CurrentRig.Joints.Count).Where(i => words.Any(w => V096JointMatches(_v06CurrentRig.Joints[i].Name, w))).ToHashSet();
        if (jointIds.Count == 0) return null;
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return null;
        var result = new float[mdt.GetVertexCount()];
        for (int i = 0; i < result.Length; i++)
        {
            float score = GetV06SkinWeights(mdt.GetVertex(i), i).Where(x => jointIds.Contains(x.Joint)).Sum(x => x.Weight);
            result[i] = Math.Clamp(score * 1.8f, 0f, 1f);
        }
        return result.Any(v => v >= .25f) ? result : null;
    }

    static bool V096JointMatches(string name, string word)
    {
        string n = (name ?? "").ToLowerInvariant();
        if (n.Contains(word)) return true;
        return word switch
        {
            "head" or "face" => n.Contains("neck") || n.Contains("skull"),
            "hand" => n.Contains("wrist") || n.Contains("palm"),
            "arm" => n.Contains("shoulder") || n.Contains("elbow") || n.Contains("wrist"),
            "leg" => n.Contains("hip") || n.Contains("knee") || n.Contains("ankle"),
            "foot" => n.Contains("ankle") || n.Contains("toe"),
            "torso" or "body" or "chest" => n.Contains("spine") || n.Contains("chest") || n.Contains("pelvis"),
            _ => false
        };
    }

    static float[] V096AllVertices(ArrayMesh mesh)
    {
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return Array.Empty<float>();
        return Enumerable.Repeat(1f, mdt.GetVertexCount()).ToArray();
    }

    static float[] ParseV096Selection(ArrayMesh mesh, string json, out string method)
    {
        using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
        method = root.TryGetProperty("method", out var m) ? m.GetString() ?? "semantic backend" : "semantic backend";
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return Array.Empty<float>();
        int count = mdt.GetVertexCount(); var values = new float[count];
        if (root.TryGetProperty("weights", out var w) && w.ValueKind == JsonValueKind.Array)
        {
            int i = 0; foreach (var e in w.EnumerateArray()) { if (i >= count) break; values[i++] = Math.Clamp((float)e.GetDouble(), 0f, 1f); }
        }
        else if (root.TryGetProperty("indices", out var idx) && idx.ValueKind == JsonValueKind.Array)
            foreach (var e in idx.EnumerateArray()) { int i = e.GetInt32(); if (i >= 0 && i < count) values[i] = 1f; }
        return values;
    }

    void ApplyV096SelectionToSculptMask()
    {
        if (_v096SelectionObject?.Mesh is not ArrayMesh mesh || _v096Selection == null) return;
        var mask = GetV08Mask(mesh, true); if (mask == null || mask.Length != _v096Selection.Length) return;
        for (int i = 0; i < mask.Length; i++) mask[i] = 1f - _v096Selection[i];
        UpdateV08MaskStatus(mask);
    }

    void ClearV096Selection(bool status = true)
    {
        if (_v096SelectionObject?.Mesh is ArrayMesh mesh)
        {
            var mask = GetV08Mask(mesh, false); if (mask != null) { Array.Fill(mask, 0f); UpdateV08MaskStatus(mask); }
        }
        _v096Selection = null; _v096SelectionObject = null; _v096SelectionQuery = ""; _v096SelectionTopology = "";
        _v096SelectionOverlay?.QueueFree(); _v096SelectionOverlay = null;
        if (status) SetStatus("Smart Selection cleared; sculpting is unrestricted.");
    }

    void InvertV096Selection()
    {
        V096ValidateSelection(); if (_v096Selection == null) { SetStatus("There is no Smart Selection to invert."); return; }
        for (int i = 0; i < _v096Selection.Length; i++) _v096Selection[i] = 1f - _v096Selection[i];
        RebuildV096SelectionOverlay(); ApplyV096SelectionToSculptMask(); SetStatus("Smart Selection inverted.");
    }

    void RebuildV096SelectionOverlay()
    {
        _v096SelectionOverlay?.QueueFree(); _v096SelectionOverlay = null;
        if (_v096SelectionObject?.Mesh is not ArrayMesh mesh || _v096Selection == null || _world == null || mesh.GetSurfaceCount() == 0) return;
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok || mdt.GetVertexCount() != _v096Selection.Length) return;
        var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
        for (int f = 0; f < mdt.GetFaceCount(); f++)
        {
            int a = mdt.GetFaceVertex(f,0), b = mdt.GetFaceVertex(f,1), c = mdt.GetFaceVertex(f,2);
            if ((_v096Selection[a] + _v096Selection[b] + _v096Selection[c]) / 3f < .35f) continue;
            st.SetNormal(mdt.GetFaceNormal(f)); st.AddVertex(mdt.GetVertex(a));
            st.SetNormal(mdt.GetFaceNormal(f)); st.AddVertex(mdt.GetVertex(b));
            st.SetNormal(mdt.GetFaceNormal(f)); st.AddVertex(mdt.GetVertex(c));
        }
        var overlayMesh = st.Commit(); if (overlayMesh == null || overlayMesh.GetSurfaceCount() == 0) return;
        var overlay = new MeshInstance3D { Name = "Smart Selection v0.9.6", Mesh = overlayMesh, GlobalTransform = _v096SelectionObject.GlobalTransform };
        overlay.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, .62f, .1f, .5f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, NoDepthTest = true };
        _world.AddChild(overlay); _v096SelectionOverlay = overlay;
    }

    void OnV096SelectionViewportInput(InputEvent ev)
    {
        if (_v096SelectionOverlay != null && _v096SelectionObject != null && GodotObject.IsInstanceValid(_v096SelectionObject))
            _v096SelectionOverlay.GlobalTransform = _v096SelectionObject.GlobalTransform;
    }

    string BuildV096ViewportMask()
    {
        V096ValidateSelection();
        if (_v096SelectionObject?.Mesh is not ArrayMesh mesh || _v096Selection == null || _camera == null) throw new InvalidOperationException("Create a Smart Selection first.");
        var sub = FindChild("Viewport", true, false) as SubViewport ?? throw new InvalidOperationException("Viewport is unavailable.");
        int w = Math.Max(1, (int)sub.GetVisibleRect().Size.X), h = Math.Max(1, (int)sub.GetVisibleRect().Size.Y);
        var image = Image.CreateEmpty(w, h, false, Image.Format.L8); image.Fill(Colors.Black);
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) throw new InvalidOperationException("Could not read selected mesh.");
        for (int f=0; f<mdt.GetFaceCount(); f++)
        {
            int ia=mdt.GetFaceVertex(f,0), ib=mdt.GetFaceVertex(f,1), ic=mdt.GetFaceVertex(f,2);
            if ((_v096Selection[ia]+_v096Selection[ib]+_v096Selection[ic])/3f < .35f) continue;
            Vector3 wa=_v096SelectionObject.GlobalTransform*mdt.GetVertex(ia), wb=_v096SelectionObject.GlobalTransform*mdt.GetVertex(ib), wc=_v096SelectionObject.GlobalTransform*mdt.GetVertex(ic);
            if (_camera.IsPositionBehind(wa) && _camera.IsPositionBehind(wb) && _camera.IsPositionBehind(wc)) continue;
            V096RasterTriangle(image, _camera.UnprojectPosition(wa), _camera.UnprojectPosition(wb), _camera.UnprojectPosition(wc));
        }
        string dir=ProjectSettings.GlobalizePath("user://masks"); Directory.CreateDirectory(dir); string path=Path.Combine(dir,$"smart_select_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"); image.SavePng(path); return path;
    }

    static void V096RasterTriangle(Image img, Vector2 a, Vector2 b, Vector2 c)
    {
        int minX=Math.Clamp((int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X))),0,img.GetWidth()-1), maxX=Math.Clamp((int)Math.Ceiling(Math.Max(a.X,Math.Max(b.X,c.X))),0,img.GetWidth()-1);
        int minY=Math.Clamp((int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y))),0,img.GetHeight()-1), maxY=Math.Clamp((int)Math.Ceiling(Math.Max(a.Y,Math.Max(b.Y,c.Y))),0,img.GetHeight()-1);
        float area=V096Edge(a,b,c); if (Math.Abs(area)<1e-5f) return;
        for(int y=minY;y<=maxY;y++) for(int x=minX;x<=maxX;x++) { var p=new Vector2(x+.5f,y+.5f); float w0=V096Edge(b,c,p), w1=V096Edge(c,a,p), w2=V096Edge(a,b,p); if ((w0>=0&&w1>=0&&w2>=0)||(w0<=0&&w1<=0&&w2<=0)) img.SetPixel(x,y,Colors.White); }
    }
    static float V096Edge(Vector2 a,Vector2 b,Vector2 p)=>(p.X-a.X)*(b.Y-a.Y)-(p.Y-a.Y)*(b.X-a.X);
}
