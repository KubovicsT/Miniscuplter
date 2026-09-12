using Godot;
using Miniscuplter.Core;
using System;

namespace Miniscuplter;

public partial class Main
{
    ObjectSelectionRef? _v1027ViewportSelection;
    bool _v1027SculptObserverInstalled;
    ObjectSelectionRef? _v1027SculptGestureSelection;
    ArrayMesh? _v1027SculptUndoMarker;

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
        V1027EnsureRevisionBoundSculptObserver();

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

    void V1027EnsureRevisionBoundSculptObserver()
    {
        if (_v1027SculptObserverInstalled || !_v1020ViewportEditingObserverAttached)
            return;
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host)
            return;

        // Keep the visible Godot tool first, then capture durable identity before the legacy
        // Stage-C observer. Re-add the legacy observer last so this bounded seam can suppress
        // only its sculpt commit while leaving transform persistence untouched.
        host.GuiInput -= V1020ObserveViewportEditingCommit;
        host.GuiInput += V1027ObserveRevisionBoundSculpt;
        host.GuiInput += V1020ObserveViewportEditingCommit;
        _v1027SculptObserverInstalled = true;
    }

    void V1027ObserveRevisionBoundSculpt(InputEvent ev)
    {
        if (ev is not InputEventMouseButton button || button.ButtonIndex != MouseButton.Left)
            return;

        if (button.Pressed)
        {
            _v1027SculptGestureSelection = null;
            _v1027SculptUndoMarker = null;
            if (_v1018Tool != V1018ViewportTool.Sculpt || !_sculpting ||
                _v1020StageCSession == null ||
                !V1020SelectedIsMappedStageC(out ObjectId objectId, out _))
                return;

            _v1027SculptGestureSelection = StageCSelection.BindObject(_v1020StageCSession.Current, objectId);
            _v1027SculptUndoMarker = _undo.Count > 0 ? _undo.Peek() : null;
            return;
        }

        if (_v1027SculptGestureSelection is not { } selection)
            return;

        // The older observer runs after this handler. Disable only its sculpt commit so a stroke
        // has one durable authority path and cannot create duplicate revisions/history entries.
        _v1020SculptGestureActive = false;
        ArrayMesh? undoMarker = _v1027SculptUndoMarker;
        _v1027SculptGestureSelection = null;
        _v1027SculptUndoMarker = null;
        _ = V1027CommitRevisionBoundSculptStrokeAsync(selection, undoMarker);
    }

    async System.Threading.Tasks.Task V1027CommitRevisionBoundSculptStrokeAsync(
        ObjectSelectionRef selection,
        ArrayMesh? legacyUndoMarker)
    {
        MeshInstance3D? target = V1020FindSceneObject(selection.ObjectId);
        if (target?.Mesh is not ArrayMesh editedMesh)
            return;

        try
        {
            MeshData data = V1013MeshData(editedMesh);
            await _v1020StageCGate.WaitAsync();
            try
            {
                ProjectSession session = _v1020StageCSession ??
                    throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!StageCSelection.IsCurrent(session.Current, selection))
                    throw new InvalidOperationException("The sculpt selection is stale because the object revision advanced before commit.");
                if (_selected == null || !GodotObject.IsInstanceValid(_selected) ||
                    !_v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out ObjectId selectedObjectId) ||
                    selectedObjectId != selection.ObjectId)
                    throw new InvalidOperationException("The sculpt selection changed before the stroke could be committed.");
                if (_v1027ViewportSelection is { } viewportSelection &&
                    viewportSelection.ObjectId == selection.ObjectId && viewportSelection != selection)
                    throw new InvalidOperationException("The stable viewport selection advanced before the sculpt stroke could be committed.");

                MeshRevision revision = await _v1020StageCStore.CreateMeshRevisionAsync(
                    _v1020StageCProjectPath,
                    selection.ObjectId,
                    data,
                    "stagec-edit:revision-bound-sculpt-stroke",
                    selection.MeshRevisionId);
                StageCEditing.CommitMeshRevision(
                    session,
                    selection.ObjectId,
                    selection.MeshRevisionId,
                    revision,
                    "revision-bound sculpt stroke");
                await V1020SaveSessionAsync();

                if (_v1027ViewportSelection is { } currentBinding && currentBinding == selection)
                    _v1027ViewportSelection = StageCSelection.RebindWholeObject(session.Current, selection);

                V1020RemoveDuplicatedLegacySculptUndo(legacyUndoMarker);
                V1020ProjectObjectStateToScene(target, session.Current.Objects[selection.ObjectId]);
            }
            finally
            {
                _v1020StageCGate.Release();
            }

            SetStatus("Sculpt stroke committed from exact object/revision identity as a new immutable mesh revision.");
        }
        catch (Exception ex)
        {
            V1020RemoveDuplicatedLegacySculptUndo(legacyUndoMarker);
            V1020RestoreMappedObjectFromCurrentState(target, selection.ObjectId);
            SetStatus("Stage-D sculpt commit failed safely; restored durable Core state: " + ex.Message);
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
