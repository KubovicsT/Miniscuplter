using Godot;
using System;

namespace Miniscuplter;

public partial class Main
{
    MeshInstance3D? _v096SelectionView;
    bool _v096SelectionSourceWasVisible;

    void V096SetSelectionView(bool showSelected)
    {
        V096ValidateSelection();
        if (_v096SelectionObject?.Mesh is not ArrayMesh mesh || _v096Selection == null || _world == null)
        {
            SetStatus("Create a Smart Selection first."); return;
        }
        V096RestoreSelectionView();
        var mdt = new MeshDataTool();
        if (mdt.CreateFromSurface(mesh, 0) != Error.Ok || mdt.GetVertexCount() != _v096Selection.Length)
        {
            SetStatus("Could not build Smart Selection view."); return;
        }
        var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
        for (int f=0; f<mdt.GetFaceCount(); f++)
        {
            int a=mdt.GetFaceVertex(f,0), b=mdt.GetFaceVertex(f,1), c=mdt.GetFaceVertex(f,2);
            bool selected = (_v096Selection[a]+_v096Selection[b]+_v096Selection[c])/3f >= .35f;
            if (selected != showSelected) continue;
            Vector3 n=mdt.GetFaceNormal(f); st.SetNormal(n); st.AddVertex(mdt.GetVertex(a)); st.SetNormal(n); st.AddVertex(mdt.GetVertex(b)); st.SetNormal(n); st.AddVertex(mdt.GetVertex(c));
        }
        var outMesh=st.Commit();
        if(outMesh==null||outMesh.GetSurfaceCount()==0){SetStatus(showSelected?"Selection contains no visible triangles.":"Selection covers the whole mesh.");return;}
        _v096SelectionSourceWasVisible=_v096SelectionObject.Visible; _v096SelectionObject.Visible=false;
        _v096SelectionOverlay?.Hide();
        var view=new MeshInstance3D{Name=showSelected?"Smart Selection Isolate":"Smart Selection Hidden",Mesh=outMesh,GlobalTransform=_v096SelectionObject.GlobalTransform};
        view.MaterialOverride=_v096SelectionObject.MaterialOverride; _world.AddChild(view); _v096SelectionView=view;
        SetStatus(showSelected?"Isolated Smart Selection. Use /show to restore the full object.":"Smart Selection hidden. Use /show to restore the full object.");
    }

    void V096RestoreSelectionView()
    {
        if(_v096SelectionView!=null){_v096SelectionView.QueueFree();_v096SelectionView=null;}
        if(_v096SelectionObject!=null&&GodotObject.IsInstanceValid(_v096SelectionObject))_v096SelectionObject.Visible=_v096SelectionSourceWasVisible||!_v096SelectionObject.Visible;
        if(_v096SelectionOverlay!=null&&GodotObject.IsInstanceValid(_v096SelectionOverlay))_v096SelectionOverlay.Show();
        _v096SelectionSourceWasVisible=false;
    }
}
