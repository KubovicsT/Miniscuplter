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
            ClearV096Selection(false);
    }

    async Task SmartSelectV096Async(string query, char mode = '=')
    {
        query = (query ?? "").Trim();
        if (query.Length == 0) { SetStatus("Smart Select needs a target, for example /s head."); return; }
        if (_selected == null) { SetStatus("Select a mesh object first."); return; }

        MeshInstance3D target = FindV096SemanticObject(query) ?? _selected;
        if (target.Mesh is not ArrayMesh mesh || mesh.GetSurfaceCount() == 0) { SetStatus("Smart Select target has no editable mesh."); return; }
        if (target != _selected) Select(target);

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
        if (found == null || found.Length == 0 || !found.Any(v => v >= .05f)) { SetStatus($"Smart Select could not confidently identify '{query}'. Install Smart Select AI or refine the query."); return; }

        int count = found.Length;
        if (mode != '=' && (_v096SelectionObject != target || _v096Selection == null || _v096Selection.Length != count)) mode = '=';
        if (mode == '+') for (int i = 0; i < count; i++) _v096Selection![i] = Math.Max(_v096Selection[i], found[i]);
        else if (mode == '-') for (int i = 0; i < count; i++) _v096Selection![i] = Math.Min(_v096Selection[i], 1f - found[i]);
        else { _v096Selection = found; _v096SelectionObject = target; }

        _v096SelectionObject = target;
        _v096SelectionQuery = query;
        _v096SelectionTopology = V096MeshSignature(mesh);
        V096RestoreSelectionView();
        RebuildV096SelectionOverlay();
        ApplyV096SelectionToSculptMask();
        int selected = _v096Selection.Count(v => v >= .5f);
        SetStatus($"Smart Select '{query}' via {method}: {selected:N0}/{count:N0} vertices active. Sculpting is constrained to the active selection.");
    }

    MeshInstance3D? FindV096SemanticObject(string query)
    {
        string q = query.Trim();
        foreach (var obj in _objects.Where(o => GodotObject.IsInstanceValid(o) && o.Mesh != null))
            if (obj.Name.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)) return obj;
        foreach (var a in _v07Attachments)
        {
            var part = _v07Parts.FirstOrDefault(p => p.Id == a.LibraryId);
            if (part == null) continue;
            if (part.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || part.Category.Contains(q, StringComparison.OrdinalIgnoreCase) || part.SocketType.Contains(q, StringComparison.OrdinalIgnoreCase))
                return _objects.FirstOrDefault(o => GodotObject.IsInstanceValid(o) && o.Name.ToString().Equals(a.PartObjectName, StringComparison.OrdinalIgnoreCase));
        }
        return null;
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
            if (part != null && (part.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || part.Category.Contains(query, StringComparison.OrdinalIgnoreCase) || part.SocketType.Contains(query, StringComparison.OrdinalIgnoreCase))) return V096AllVertices(mesh);
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

    float[]? TryV096GeometrySelection(ArrayMesh mesh, string query)
    {
        string q = query.ToLowerInvariant();
        bool known = new[] { "head","face","hair","helmet","skull","horn","torso","body","chest","waist","abdomen","left hand","right hand","left arm","right arm","left leg","right leg","left foot","right foot","base","ground","wing","tail" }.Any(q.Contains);
        if (!known) return null;
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok || mdt.GetVertexCount() == 0) return null;
        Vector3 lo = mdt.GetVertex(0), hi = lo;
        for (int i=1;i<mdt.GetVertexCount();i++) { var p=mdt.GetVertex(i); lo=lo.Min(p); hi=hi.Max(p); }
        Vector3 span = hi-lo; span.X=Math.Max(span.X,.0001f); span.Y=Math.Max(span.Y,.0001f); span.Z=Math.Max(span.Z,.0001f);
        var values=new float[mdt.GetVertexCount()];
        for(int i=0;i<values.Length;i++)
        {
            Vector3 n=(mdt.GetVertex(i)-lo)/span; float x=n.X,y=n.Y,z=n.Z; float cx=1f-Math.Min(1f,Math.Abs(x-.5f)*2f); float w=0f;
            if (q.Contains("head")||q.Contains("face")||q.Contains("hair")||q.Contains("helmet")||q.Contains("skull")||q.Contains("horn")) { w=Math.Clamp((y-.68f)/.20f,0f,1f)*Math.Clamp(cx*1.4f,0f,1f); if(q.Contains("face")) w*=Math.Clamp((z-.42f)/.35f,0f,1f); }
            else if(q.Contains("torso")||q.Contains("body")||q.Contains("chest")||q.Contains("waist")||q.Contains("abdomen")) w=Math.Clamp(1f-Math.Abs(y-.55f)/.32f,0f,1f)*Math.Clamp(cx*1.5f,0f,1f);
            else if(q.Contains("left hand")||q.Contains("left arm")) w=Math.Clamp((.40f-x)/.30f,0f,1f)*Math.Clamp(1f-Math.Abs(y-(q.Contains("hand")?.45f:.60f))/.40f,0f,1f);
            else if(q.Contains("right hand")||q.Contains("right arm")) w=Math.Clamp((x-.60f)/.30f,0f,1f)*Math.Clamp(1f-Math.Abs(y-(q.Contains("hand")?.45f:.60f))/.40f,0f,1f);
            else if(q.Contains("left leg")||q.Contains("left foot")) { w=Math.Clamp((.52f-x)/.35f,0f,1f)*Math.Clamp((.55f-y)/.45f,0f,1f); if(q.Contains("foot"))w*=Math.Clamp((.22f-y)/.22f,0f,1f); }
            else if(q.Contains("right leg")||q.Contains("right foot")) { w=Math.Clamp((x-.48f)/.35f,0f,1f)*Math.Clamp((.55f-y)/.45f,0f,1f); if(q.Contains("foot"))w*=Math.Clamp((.22f-y)/.22f,0f,1f); }
            else if(q.Contains("base")||q.Contains("ground")) w=Math.Clamp((.18f-y)/.18f,0f,1f);
            else if(q.Contains("wing")) w=Math.Clamp((Math.Abs(x-.5f)-.20f)/.30f,0f,1f)*Math.Clamp((y-.35f)/.40f,0f,1f);
            else if(q.Contains("tail")) w=Math.Clamp((.38f-y)/.35f,0f,1f)*Math.Clamp(Math.Abs(z-.5f)*2f-.20f,0f,1f);
            values[i]=w;
        }
        return values.Any(v=>v>=.2f)?values:null;
    }

    static bool V096JointMatches(string name, string word)
    {
        string n = (name ?? "").ToLowerInvariant(); if (n.Contains(word)) return true;
        return word switch { "head" or "face" => n.Contains("neck") || n.Contains("skull"), "hand" => n.Contains("wrist") || n.Contains("palm"), "arm" => n.Contains("shoulder") || n.Contains("elbow") || n.Contains("wrist"), "leg" => n.Contains("hip") || n.Contains("knee") || n.Contains("ankle"), "foot" => n.Contains("ankle") || n.Contains("toe"), "torso" or "body" or "chest" => n.Contains("spine") || n.Contains("chest") || n.Contains("pelvis"), _ => false };
    }

    static float[] V096AllVertices(ArrayMesh mesh)
    {
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return Array.Empty<float>();
        return Enumerable.Repeat(1f, mdt.GetVertexCount()).ToArray();
    }

    static Vector3I V096PositionKey(Vector3 p) => new((int)MathF.Round(p.X*10000f),(int)MathF.Round(p.Y*10000f),(int)MathF.Round(p.Z*10000f));

    static float[] ParseV096Selection(ArrayMesh mesh, string json, out string method)
    {
        using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
        method = root.TryGetProperty("method", out var m) ? m.GetString() ?? "semantic backend" : "semantic backend";
        var mdt = new MeshDataTool(); if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return Array.Empty<float>();
        int count=mdt.GetVertexCount(); var values=new float[count];
        var map=new Dictionary<Vector3I,List<int>>();
        for(int i=0;i<count;i++){var k=V096PositionKey(mdt.GetVertex(i)); if(!map.TryGetValue(k,out var list))map[k]=list=new List<int>(); list.Add(i);}
        if(root.TryGetProperty("sample_positions_mm",out var pos)&&root.TryGetProperty("sample_weights",out var weights)&&pos.ValueKind==JsonValueKind.Array&&weights.ValueKind==JsonValueKind.Array)
        {
            int n=Math.Min(pos.GetArrayLength(),weights.GetArrayLength());
            for(int s=0;s<n;s++){var p=pos[s]; if(p.GetArrayLength()<3)continue; var key=V096PositionKey(new Vector3(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle())); if(!map.TryGetValue(key,out var ids))continue; float w=Math.Clamp(weights[s].GetSingle(),0f,1f); foreach(int i in ids)values[i]=Math.Max(values[i],w);}
        }
        return values;
    }

    void ApplyV096SelectionToSculptMask()
    {
        if (_v096SelectionObject?.Mesh is not ArrayMesh mesh || _v096Selection == null) return;
        var mask = GetV08Mask(mesh, true); if (mask == null || mask.Length != _v096Selection.Length) return;
        for (int i = 0; i < mask.Length; i++) mask[i] = 1f - _v096Selection[i]; UpdateV08MaskStatus(mask);
    }

    void ClearV096Selection(bool status = true)
    {
        V096RestoreSelectionView();
        if (_v096SelectionObject?.Mesh is ArrayMesh mesh) { var mask=GetV08Mask(mesh,false); if(mask!=null){Array.Fill(mask,0f);UpdateV08MaskStatus(mask);} }
        _v096Selection=null; _v096SelectionObject=null; _v096SelectionQuery=""; _v096SelectionTopology=""; _v096SelectionOverlay?.QueueFree(); _v096SelectionOverlay=null;
        if(status)SetStatus("Smart Selection cleared; sculpting is unrestricted.");
    }

    void InvertV096Selection()
    {
        V096ValidateSelection(); if(_v096Selection==null){SetStatus("There is no Smart Selection to invert.");return;} for(int i=0;i<_v096Selection.Length;i++)_v096Selection[i]=1f-_v096Selection[i]; RebuildV096SelectionOverlay();ApplyV096SelectionToSculptMask();SetStatus("Smart Selection inverted.");
    }

    void RebuildV096SelectionOverlay()
    {
        _v096SelectionOverlay?.QueueFree();_v096SelectionOverlay=null;
        if(_v096SelectionObject?.Mesh is not ArrayMesh mesh||_v096Selection==null||_world==null||mesh.GetSurfaceCount()==0)return;
        var mdt=new MeshDataTool();if(mdt.CreateFromSurface(mesh,0)!=Error.Ok||mdt.GetVertexCount()!=_v096Selection.Length)return;var st=new SurfaceTool();st.Begin(Mesh.PrimitiveType.Triangles);
        for(int f=0;f<mdt.GetFaceCount();f++){int a=mdt.GetFaceVertex(f,0),b=mdt.GetFaceVertex(f,1),c=mdt.GetFaceVertex(f,2);if((_v096Selection[a]+_v096Selection[b]+_v096Selection[c])/3f<.35f)continue;st.SetNormal(mdt.GetFaceNormal(f));st.AddVertex(mdt.GetVertex(a));st.SetNormal(mdt.GetFaceNormal(f));st.AddVertex(mdt.GetVertex(b));st.SetNormal(mdt.GetFaceNormal(f));st.AddVertex(mdt.GetVertex(c));}
        var overlayMesh=st.Commit();if(overlayMesh==null||overlayMesh.GetSurfaceCount()==0)return;var overlay=new MeshInstance3D{Name="Smart Selection v0.9.6",Mesh=overlayMesh,GlobalTransform=_v096SelectionObject.GlobalTransform};overlay.MaterialOverride=new StandardMaterial3D{AlbedoColor=new Color(1f,.62f,.1f,.5f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,NoDepthTest=true};_world.AddChild(overlay);_v096SelectionOverlay=overlay;
    }

    void OnV096SelectionViewportInput(InputEvent ev){if(_v096SelectionOverlay!=null&&_v096SelectionObject!=null&&GodotObject.IsInstanceValid(_v096SelectionObject))_v096SelectionOverlay.GlobalTransform=_v096SelectionObject.GlobalTransform;}

    string BuildV096ViewportMask()
    {
        V096ValidateSelection();if(_v096SelectionObject?.Mesh is not ArrayMesh mesh||_v096Selection==null||_camera==null)throw new InvalidOperationException("Create a Smart Selection first.");var sub=FindChild("Viewport",true,false)as SubViewport??throw new InvalidOperationException("Viewport is unavailable.");int w=Math.Max(1,(int)sub.GetVisibleRect().Size.X),h=Math.Max(1,(int)sub.GetVisibleRect().Size.Y);var image=Image.CreateEmpty(w,h,false,Image.Format.L8);image.Fill(Colors.Black);var mdt=new MeshDataTool();if(mdt.CreateFromSurface(mesh,0)!=Error.Ok)throw new InvalidOperationException("Could not read selected mesh.");
        for(int f=0;f<mdt.GetFaceCount();f++){int ia=mdt.GetFaceVertex(f,0),ib=mdt.GetFaceVertex(f,1),ic=mdt.GetFaceVertex(f,2);if((_v096Selection[ia]+_v096Selection[ib]+_v096Selection[ic])/3f<.35f)continue;Vector3 wa=_v096SelectionObject.GlobalTransform*mdt.GetVertex(ia),wb=_v096SelectionObject.GlobalTransform*mdt.GetVertex(ib),wc=_v096SelectionObject.GlobalTransform*mdt.GetVertex(ic);Vector3 center=(wa+wb+wc)/3f;Vector3 normal=(_v096SelectionObject.GlobalTransform.Basis*mdt.GetFaceNormal(f)).Normalized();if(normal.Dot((_camera.GlobalPosition-center).Normalized())<=0f)continue;if(_camera.IsPositionBehind(center))continue;V096RasterTriangle(image,_camera.UnprojectPosition(wa),_camera.UnprojectPosition(wb),_camera.UnprojectPosition(wc));}
        string dir=ProjectSettings.GlobalizePath("user://masks");Directory.CreateDirectory(dir);string path=Path.Combine(dir,$"smart_select_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");image.SavePng(path);return path;
    }

    static void V096RasterTriangle(Image img,Vector2 a,Vector2 b,Vector2 c){int minX=Math.Clamp((int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X))),0,img.GetWidth()-1),maxX=Math.Clamp((int)Math.Ceiling(Math.Max(a.X,Math.Max(b.X,c.X))),0,img.GetWidth()-1);int minY=Math.Clamp((int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y))),0,img.GetHeight()-1),maxY=Math.Clamp((int)Math.Ceiling(Math.Max(a.Y,Math.Max(b.Y,c.Y))),0,img.GetHeight()-1);float area=V096Edge(a,b,c);if(Math.Abs(area)<1e-5f)return;for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++){var p=new Vector2(x+.5f,y+.5f);float w0=V096Edge(b,c,p),w1=V096Edge(c,a,p),w2=V096Edge(a,b,p);if((w0>=0&&w1>=0&&w2>=0)||(w0<=0&&w1<=0&&w2<=0))img.SetPixel(x,y,Colors.White);}}
    static float V096Edge(Vector2 a,Vector2 b,Vector2 p)=>(p.X-a.X)*(b.Y-a.Y)-(p.Y-a.Y)*(b.X-a.X);
}
