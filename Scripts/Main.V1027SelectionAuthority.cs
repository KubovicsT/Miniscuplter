using Godot;
using Miniscuplter.Core;

namespace Miniscuplter;

public partial class Main
{
    ObjectSelectionRef? _v1027ViewportSelection;

    void V1027SelectStableViewportHit(MeshInstance3D hit)
    {
        if (_v1020StageCSession == null ||
            !_v1013ObjectIds.TryGetValue(hit.GetInstanceId(), out ObjectId objectId) ||
            !_v1020StageCSession.Current.Objects.ContainsKey(objectId))
        {
            _v1027ViewportSelection = null;
            Select(hit);
            return;
        }

        ProjectSession session = _v1020StageCSession;
        ObjectSelectionRef binding = StageCSelection.BindObject(session.Current, objectId);
        MeshInstance3D? presentation = V1020FindSceneObject(binding.ObjectId);
        if (presentation == null)
        {
            _v1027ViewportSelection = null;
            return;
        }

        _v1027ViewportSelection = binding;
        Select(presentation);
    }

    void V1027ReconcileStableViewportSelection()
    {
        if (_v1027ViewportSelection is not { } binding || _v1020StageCSession == null)
            return;

        if (_selected != null && GodotObject.IsInstanceValid(_selected) &&
            _v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out ObjectId selectedObjectId) &&
            selectedObjectId != binding.ObjectId)
        {
            // A different legacy selection path intentionally superseded this bounded viewport binding.
            _v1027ViewportSelection = null;
            return;
        }

        ProjectState state = _v1020StageCSession.Current;
        if (!state.Objects.ContainsKey(binding.ObjectId))
        {
            V1027InvalidateStableViewportSelection(binding.ObjectId);
            return;
        }

        ObjectSelectionRef rebound = StageCSelection.RebindWholeObject(state, binding);
        if (rebound.MeshRevisionId != binding.MeshRevisionId)
        {
            // Whole-object selection has no topology indices, so revision advancement may transfer
            // explicitly. Component/region selections must define stricter invalidation rules.
            _v1027ViewportSelection = rebound;
        }

        if (_selected == null || !GodotObject.IsInstanceValid(_selected))
        {
            MeshInstance3D? presentation = V1020FindSceneObject(rebound.ObjectId);
            if (presentation == null)
            {
                V1027InvalidateStableViewportSelection(rebound.ObjectId);
                return;
            }
            Select(presentation);
        }
    }

    void V1027InvalidateStableViewportSelection(ObjectId objectId)
    {
        _v1027ViewportSelection = null;
        if (_selected == null || !GodotObject.IsInstanceValid(_selected) ||
            !_v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out ObjectId selectedObjectId) ||
            selectedObjectId != objectId)
            return;

        if (_selected.MaterialOverride is StandardMaterial3D material)
            material.AlbedoColor = new Color(0.62f, 0.64f, 0.68f);
        _selected = null;
        V1017UpdateGizmo();
    }
}
