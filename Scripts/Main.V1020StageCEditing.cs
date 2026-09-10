using Godot;
using Miniscuplter.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    bool _v1020TransformGestureActive;
    bool _v1020SculptGestureActive;
    bool _v1020ViewportEditingObserverAttached;
    ObjectId _v1020SculptObjectId;
    RevisionId _v1020SculptInputRevisionId;
    ArrayMesh? _v1020LegacySculptUndoMarker;

    public void InstallV1020StageCEditingAuthority()
    {
        HookV1020TransformButton("Move +X 1 mm", "move");
        HookV1020TransformButton("Move -X 1 mm", "move");
        HookV1020TransformButton("Move +Y 1 mm", "move");
        HookV1020TransformButton("Move -Y 1 mm", "move");
        HookV1020TransformButton("Rotate Y +5°", "rotate");
        HookV1020TransformButton("Rotate Y -5°", "rotate");
        HookV1020TransformButton("Scale +5%", "scale");
        HookV1020TransformButton("Scale -5%", "scale");
        HookV1020TransformButton("Place selected on Y=0", "ground");

        foreach (Button button in FindChildren("*", "Button", true, false).OfType<Button>())
        {
            if (button.Text == "Undo")
            {
                button.Pressed -= Undo;
                button.Pressed += V1020UndoStageCAware;
            }
            else if (button.Text == "Redo")
            {
                button.Pressed -= Redo;
                button.Pressed += V1020RedoStageCAware;
            }
        }

        // v1.0.18 installs the authoritative viewport input handler lazily from _Process. Attach
        // only after that replacement is live so our observer always runs after the visible edit.
        _ = V1020AttachViewportEditingObserverAsync();
    }

    async Task V1020AttachViewportEditingObserverAsync()
    {
        for (int frame = 0; frame < 120 && !_v1020ViewportEditingObserverAttached; frame++)
        {
            if (_v1018ViewportInstalled && FindChild("ViewportHost", true, false) is SubViewportContainer host)
            {
                host.GuiInput += V1020ObserveViewportEditingCommit;
                _v1020ViewportEditingObserverAttached = true;
                return;
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!GodotObject.IsInstanceValid(this)) return;
        }
        if (!_v1020ViewportEditingObserverAttached)
            SetStatus("Stage-C editing bridge could not attach to the authoritative viewport tool; transform buttons remain durable, viewport drag commits are unavailable.");
    }

    void HookV1020TransformButton(string text, string operation)
    {
        foreach (Button button in FindChildren("*", "Button", true, false).OfType<Button>().Where(b => b.Text == text))
            button.Pressed += () => CallDeferred(nameof(V1020CommitSelectedTransformDeferred), operation);
    }

    async void V1020CommitSelectedTransformDeferred(string operation)
    {
        await V1020CommitSelectedTransformAsync(operation);
    }

    async Task<bool> V1020CommitSelectedTransformAsync(string operation)
    {
        MeshInstance3D? target = _selected;
        if (target == null || !GodotObject.IsInstanceValid(target) ||
            !_v1013ObjectIds.TryGetValue(target.GetInstanceId(), out ObjectId objectId) ||
            _v1020StageCSession == null)
            return false;

        TransformState requested = V1020TransformState(target);
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!session.Current.Objects.ContainsKey(objectId)) return false;
                bool changed = StageCEditing.SetTransform(session, objectId, requested, operation);
                if (!changed) return true;
                await V1020SaveSessionAsync();
                V1020ProjectObjectStateToScene(target, session.Current.Objects[objectId], reloadMesh: false);
            }
            finally { _v1020StageCGate.Release(); }
            SetStatus($"Stage-C {operation} committed to project state.");
            return true;
        }
        catch (Exception ex)
        {
            V1020RestoreMappedObjectFromCurrentState(target, objectId, reloadMesh: false);
            SetStatus("Stage-C transform failed safely; restored durable transform: " + ex.Message);
            return true;
        }
    }

    void V1020ObserveViewportEditingCommit(InputEvent ev)
    {
        if (ev is not InputEventMouseButton button || button.ButtonIndex != MouseButton.Left)
            return;

        if (button.Pressed)
        {
            _v1020TransformGestureActive = _v1018Dragging && V1020SelectedIsMappedStageC(out _, out _);
            if (_v1018Tool == V1018ViewportTool.Sculpt && _sculpting &&
                V1020SelectedIsMappedStageC(out ObjectId objectId, out ProjectObject projectObject))
            {
                _v1020SculptGestureActive = true;
                _v1020SculptObjectId = objectId;
                _v1020SculptInputRevisionId = projectObject.ActiveMeshRevisionId;
                _v1020LegacySculptUndoMarker = _undo.Count > 0 ? _undo.Peek() : null;
            }
            else
            {
                _v1020SculptGestureActive = false;
                _v1020LegacySculptUndoMarker = null;
            }
            return;
        }

        if (_v1020TransformGestureActive)
        {
            _v1020TransformGestureActive = false;
            _ = V1020CommitSelectedTransformAsync(_v1018Tool.ToString().ToLowerInvariant());
        }

        if (_v1020SculptGestureActive)
        {
            _v1020SculptGestureActive = false;
            _ = V1020CommitSculptStrokeAsync(_v1020SculptObjectId, _v1020SculptInputRevisionId, _v1020LegacySculptUndoMarker);
            _v1020LegacySculptUndoMarker = null;
        }
    }

    async Task V1020CommitSculptStrokeAsync(ObjectId objectId, RevisionId inputRevisionId, ArrayMesh? legacyUndoMarker)
    {
        MeshInstance3D? target = V1020FindSceneObject(objectId);
        if (target?.Mesh is not ArrayMesh editedMesh) return;

        try
        {
            MeshData data = V1013MeshData(editedMesh);
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? current) ||
                    current.ActiveMeshRevisionId != inputRevisionId)
                    throw new InvalidOperationException("The edited object advanced before this sculpt stroke could be committed.");

                MeshRevision revision = await _v1020StageCStore.CreateMeshRevisionAsync(
                    _v1020StageCProjectPath,
                    objectId,
                    data,
                    "stagec-edit:sculpt-stroke",
                    inputRevisionId);
                StageCEditing.CommitMeshRevision(session, objectId, inputRevisionId, revision, "sculpt stroke");
                await V1020SaveSessionAsync();
                V1020RemoveDuplicatedLegacySculptUndo(legacyUndoMarker);
                V1020ProjectObjectStateToScene(target, session.Current.Objects[objectId]);
            }
            finally { _v1020StageCGate.Release(); }
            SetStatus("Sculpt stroke committed as a new immutable Stage-C mesh revision.");
        }
        catch (Exception ex)
        {
            V1020RemoveDuplicatedLegacySculptUndo(legacyUndoMarker);
            V1020RestoreMappedObjectFromCurrentState(target, objectId);
            SetStatus("Stage-C sculpt commit failed safely; restored durable mesh: " + ex.Message);
        }
    }

    void V1020RemoveDuplicatedLegacySculptUndo(ArrayMesh? marker)
    {
        if (marker != null && _undo.Count > 0 && ReferenceEquals(_undo.Peek(), marker))
            _undo.Pop();
    }

    void V1020UndoStageCAware()
    {
        if (!V1020SelectedIsMappedStageC(out ObjectId objectId, out _) ||
            _v1020StageCSession == null || !_v1020StageCSession.CanUndo)
        {
            Undo();
            return;
        }
        ProjectTransaction transaction = _v1020StageCSession.UndoTransactions.First();
        if (!StageCEditing.IsEditingTransaction(transaction) || !transaction.AffectedObjectIds.Contains(objectId))
        {
            Undo();
            return;
        }
        _ = V1020UndoRedoStageCAsync(objectId, undo: true);
    }

    void V1020RedoStageCAware()
    {
        if (!V1020SelectedIsMappedStageC(out ObjectId objectId, out _) ||
            _v1020StageCSession == null || !_v1020StageCSession.CanRedo)
        {
            Redo();
            return;
        }
        ProjectTransaction transaction = _v1020StageCSession.RedoTransactions.First();
        if (!StageCEditing.IsEditingTransaction(transaction) || !transaction.AffectedObjectIds.Contains(objectId))
        {
            Redo();
            return;
        }
        _ = V1020UndoRedoStageCAsync(objectId, undo: false);
    }

    async Task V1020UndoRedoStageCAsync(ObjectId objectId, bool undo)
    {
        MeshInstance3D? target = V1020FindSceneObject(objectId);
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                ProjectTransaction transaction = undo ? session.Undo() : session.Redo();
                if (!StageCEditing.IsEditingTransaction(transaction) || !transaction.AffectedObjectIds.Contains(objectId))
                    throw new InvalidOperationException("The requested history entry is not an edit of the selected Stage-C object.");
                await V1020SaveSessionAsync();
                if (target != null && session.Current.Objects.TryGetValue(objectId, out ProjectObject? obj))
                    V1020ProjectObjectStateToScene(target, obj);
            }
            finally { _v1020StageCGate.Release(); }
            SetStatus(undo ? "Stage-C edit undone and saved." : "Stage-C edit redone and saved.");
        }
        catch (Exception ex)
        {
            V1020RestoreMappedObjectFromCurrentState(target, objectId);
            SetStatus($"Stage-C {(undo ? "undo" : "redo")} failed safely: {ex.Message}");
        }
    }

    bool V1020SelectedIsMappedStageC(out ObjectId objectId, out ProjectObject projectObject)
    {
        objectId = default;
        projectObject = null!;
        if (_selected == null || !GodotObject.IsInstanceValid(_selected) || _v1020StageCSession == null)
            return false;
        if (!_v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out objectId)) return false;
        return _v1020StageCSession.Current.Objects.TryGetValue(objectId, out projectObject!);
    }

    MeshInstance3D? V1020FindSceneObject(ObjectId objectId) =>
        _objects.FirstOrDefault(obj => GodotObject.IsInstanceValid(obj) &&
            _v1013ObjectIds.TryGetValue(obj.GetInstanceId(), out ObjectId mapped) && mapped == objectId);

    TransformState V1020TransformState(MeshInstance3D target) =>
        new(V1013Vec(target.Position), V1013Vec(target.Rotation), V1013Vec(target.Scale));

    void V1020RestoreMappedObjectFromCurrentState(MeshInstance3D? target, ObjectId objectId, bool reloadMesh = true)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || _v1020StageCSession == null) return;
        if (_v1020StageCSession.Current.Objects.TryGetValue(objectId, out ProjectObject? obj))
            V1020ProjectObjectStateToScene(target, obj, reloadMesh);
    }

    void V1020ProjectObjectStateToScene(MeshInstance3D target, ProjectObject obj, bool reloadMesh = true)
    {
        target.Position = new Vector3(obj.Transform.Position.X, obj.Transform.Position.Y, obj.Transform.Position.Z);
        target.Rotation = new Vector3(obj.Transform.RotationEuler.X, obj.Transform.RotationEuler.Y, obj.Transform.RotationEuler.Z);
        target.Scale = new Vector3(obj.Transform.Scale.X, obj.Transform.Scale.Y, obj.Transform.Scale.Z);

        if (reloadMesh && _v1020StageCSession != null &&
            _v1020StageCSession.Current.MeshRevisions.TryGetValue(obj.ActiveMeshRevisionId, out MeshRevision? revision))
        {
            string asset = ProjectStore.ResolveAsset(ProjectLayout.FromManifest(_v1020StageCProjectPath), revision.AssetPath);
            if (File.Exists(asset))
            {
                target.Mesh = V1020ArrayMeshFromData(MeshBinaryCodec.Read(asset));
                V095TopologyChanged(target);
            }
        }
        V1017UpdateGizmo();
    }
}
