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
    ObjectId _v1020TransformGestureObjectId;
    RevisionId _v1020TransformGestureMeshRevisionId;
    TransformState _v1020TransformGestureDurableStart;
    TransformState _v1020TransformGestureSceneStart;
    V1018ViewportTool _v1020TransformGestureTool;
    bool _v1020SculptGestureActive;
    bool _v1020ViewportEditingObserverAttached;
    ObjectId _v1020SculptObjectId;
    RevisionId _v1020SculptInputRevisionId;
    ArrayMesh? _v1020LegacySculptUndoMarker;

    public void InstallV1020StageCEditingAuthority()
    {
        // MS-019: Stage-C owns durable nudge commands for mapped objects. Historical button
        // handlers may still update Godot presentation first during migration, but scene values
        // are no longer consulted for the bounded move/rotate/scale command pairs retired below.
        HookV1020MoveButton("Move +X 1 mm", new Vec3(1, 0, 0));
        HookV1020MoveButton("Move -X 1 mm", new Vec3(-1, 0, 0));
        HookV1020MoveButton("Move +Y 1 mm", new Vec3(0, 1, 0));
        HookV1020MoveButton("Move -Y 1 mm", new Vec3(0, -1, 0));
        HookV1020RotateButton("Rotate Y +5°", Mathf.DegToRad(5f));
        HookV1020RotateButton("Rotate Y -5°", Mathf.DegToRad(-5f));
        HookV1020ScaleButton("Scale +5%", 1.05f);
        HookV1020ScaleButton("Scale -5%", 0.95f);
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

    void HookV1020MoveButton(string text, Vec3 delta)
    {
        foreach (Button button in FindChildren("*", "Button", true, false).OfType<Button>().Where(b => b.Text == text))
            button.Pressed += () => CallDeferred(nameof(V1020CommitMoveCommandDeferred), delta.X, delta.Y, delta.Z);
    }

    async void V1020CommitMoveCommandDeferred(float x, float y, float z)
    {
        await V1020CommitMoveCommandAsync(new Vec3(x, y, z));
    }

    async Task<bool> V1020CommitMoveCommandAsync(Vec3 delta)
    {
        MeshInstance3D? target = _selected;
        if (target == null || !GodotObject.IsInstanceValid(target) ||
            !_v1013ObjectIds.TryGetValue(target.GetInstanceId(), out ObjectId objectId) ||
            _v1020StageCSession == null)
            return false;

        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? current)) return false;

                Vec3 position = current.Transform.Position;
                TransformState requested = new(
                    new Vec3(position.X + delta.X, position.Y + delta.Y, position.Z + delta.Z),
                    current.Transform.RotationEuler,
                    current.Transform.Scale);
                bool changed = StageCEditing.SetTransform(session, objectId, requested, "move");
                if (changed)
                    await V1020SaveSessionAsync();
                V1020ProjectObjectStateToScene(target, session.Current.Objects[objectId], reloadMesh: false);
            }
            finally { _v1020StageCGate.Release(); }
            SetStatus("Stage-C move committed to project state.");
            return true;
        }
        catch (Exception ex)
        {
            V1020RestoreMappedObjectFromCurrentState(target, objectId, reloadMesh: false);
            SetStatus("Stage-C move failed safely; restored durable transform: " + ex.Message);
            return true;
        }
    }

    void HookV1020RotateButton(string text, float deltaYRadians)
    {
        foreach (Button button in FindChildren("*", "Button", true, false).OfType<Button>().Where(b => b.Text == text))
            button.Pressed += () => CallDeferred(nameof(V1020CommitRotateCommandDeferred), deltaYRadians);
    }

    async void V1020CommitRotateCommandDeferred(float deltaYRadians)
    {
        await V1020CommitRotateCommandAsync(deltaYRadians);
    }

    async Task<bool> V1020CommitRotateCommandAsync(float deltaYRadians)
    {
        MeshInstance3D? target = _selected;
        if (target == null || !GodotObject.IsInstanceValid(target) ||
            !_v1013ObjectIds.TryGetValue(target.GetInstanceId(), out ObjectId objectId) ||
            _v1020StageCSession == null)
            return false;

        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? current)) return false;

                Vec3 rotation = current.Transform.RotationEuler;
                TransformState requested = new(
                    current.Transform.Position,
                    new Vec3(rotation.X, rotation.Y + deltaYRadians, rotation.Z),
                    current.Transform.Scale);
                bool changed = StageCEditing.SetTransform(session, objectId, requested, "rotate");
                if (changed)
                    await V1020SaveSessionAsync();
                V1020ProjectObjectStateToScene(target, session.Current.Objects[objectId], reloadMesh: false);
            }
            finally { _v1020StageCGate.Release(); }
            SetStatus("Stage-C rotate committed to project state.");
            return true;
        }
        catch (Exception ex)
        {
            V1020RestoreMappedObjectFromCurrentState(target, objectId, reloadMesh: false);
            SetStatus("Stage-C rotate failed safely; restored durable transform: " + ex.Message);
            return true;
        }
    }

    void HookV1020ScaleButton(string text, float factor)
    {
        foreach (Button button in FindChildren("*", "Button", true, false).OfType<Button>().Where(b => b.Text == text))
            button.Pressed += () => CallDeferred(nameof(V1020CommitScaleCommandDeferred), factor);
    }

    async void V1020CommitScaleCommandDeferred(float factor)
    {
        await V1020CommitScaleCommandAsync(factor);
    }

    async Task<bool> V1020CommitScaleCommandAsync(float factor)
    {
        MeshInstance3D? target = _selected;
        if (target == null || !GodotObject.IsInstanceValid(target) ||
            !_v1013ObjectIds.TryGetValue(target.GetInstanceId(), out ObjectId objectId) ||
            _v1020StageCSession == null)
            return false;

        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? current)) return false;

                Vec3 scale = current.Transform.Scale;
                TransformState requested = new(
                    current.Transform.Position,
                    current.Transform.RotationEuler,
                    new Vec3(scale.X * factor, scale.Y * factor, scale.Z * factor));
                bool changed = StageCEditing.SetTransform(session, objectId, requested, "scale");
                if (changed)
                    await V1020SaveSessionAsync();
                V1020ProjectObjectStateToScene(target, session.Current.Objects[objectId], reloadMesh: false);
            }
            finally { _v1020StageCGate.Release(); }
            SetStatus("Stage-C scale committed to project state.");
            return true;
        }
        catch (Exception ex)
        {
            V1020RestoreMappedObjectFromCurrentState(target, objectId, reloadMesh: false);
            SetStatus("Stage-C scale failed safely; restored durable transform: " + ex.Message);
            return true;
        }
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
            ObjectId transformObjectId = default;
            ProjectObject projectObject = null!;
            _v1020TransformGestureActive = _v1018Dragging &&
                V1020SelectedIsMappedStageC(out transformObjectId, out projectObject);
            if (_v1020TransformGestureActive && _selected != null)
            {
                _v1020TransformGestureObjectId = transformObjectId;
                _v1020TransformGestureMeshRevisionId = projectObject.ActiveMeshRevisionId;
                _v1020TransformGestureDurableStart = projectObject.Transform;
                _v1020TransformGestureSceneStart = V1020TransformState(_selected);
                _v1020TransformGestureTool = _v1018Tool;
            }

            if (_v1018Tool == V1018ViewportTool.Sculpt && _sculpting &&
                V1020SelectedIsMappedStageC(out ObjectId objectId, out ProjectObject sculptProjectObject))
            {
                _v1020SculptGestureActive = true;
                _v1020SculptObjectId = objectId;
                _v1020SculptInputRevisionId = sculptProjectObject.ActiveMeshRevisionId;
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
            MeshInstance3D? target = V1020FindSceneObject(_v1020TransformGestureObjectId);
            if (target != null)
            {
                TransformState sceneEnd = V1020TransformState(target);
                _ = V1020CommitViewportTransformGestureAsync(
                    _v1020TransformGestureObjectId,
                    _v1020TransformGestureMeshRevisionId,
                    _v1020TransformGestureDurableStart,
                    _v1020TransformGestureSceneStart,
                    sceneEnd,
                    _v1020TransformGestureTool);
            }
        }

        if (_v1020SculptGestureActive)
        {
            _v1020SculptGestureActive = false;
            _ = V1020CommitSculptStrokeAsync(_v1020SculptObjectId, _v1020SculptInputRevisionId, _v1020LegacySculptUndoMarker);
            _v1020LegacySculptUndoMarker = null;
        }
    }

    async Task V1020CommitViewportTransformGestureAsync(
        ObjectId objectId,
        RevisionId inputRevisionId,
        TransformState durableStart,
        TransformState sceneStart,
        TransformState sceneEnd,
        V1018ViewportTool tool)
    {
        MeshInstance3D? target = V1020FindSceneObject(objectId);
        if (target == null) return;

        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? current))
                    throw new InvalidOperationException("The dragged Stage-C object no longer exists.");
                if (current.ActiveMeshRevisionId != inputRevisionId || current.Transform != durableStart)
                    throw new InvalidOperationException("The dragged object advanced before the viewport gesture could be committed.");

                TransformState requested = V1020ViewportTransformRequest(durableStart, sceneStart, sceneEnd, tool);
                string operation = "viewport-" + tool.ToString().ToLowerInvariant();
                bool changed = StageCEditing.SetTransform(session, objectId, requested, operation);
                if (changed)
                    await V1020SaveSessionAsync();
                V1020ProjectObjectStateToScene(target, session.Current.Objects[objectId], reloadMesh: false);
            }
            finally { _v1020StageCGate.Release(); }
            SetStatus($"Stage-C viewport {tool.ToString().ToLowerInvariant()} committed to project state.");
        }
        catch (Exception ex)
        {
            V1020RestoreMappedObjectFromCurrentState(target, objectId, reloadMesh: false);
            SetStatus("Stage-C viewport transform failed safely; restored durable transform: " + ex.Message);
        }
    }

    static TransformState V1020ViewportTransformRequest(
        TransformState durableStart,
        TransformState sceneStart,
        TransformState sceneEnd,
        V1018ViewportTool tool)
    {
        return tool switch
        {
            V1018ViewportTool.Move => new TransformState(
                new Vec3(
                    durableStart.Position.X + (sceneEnd.Position.X - sceneStart.Position.X),
                    durableStart.Position.Y + (sceneEnd.Position.Y - sceneStart.Position.Y),
                    durableStart.Position.Z + (sceneEnd.Position.Z - sceneStart.Position.Z)),
                durableStart.RotationEuler,
                durableStart.Scale),
            V1018ViewportTool.Rotate => new TransformState(
                durableStart.Position,
                new Vec3(
                    durableStart.RotationEuler.X + (sceneEnd.RotationEuler.X - sceneStart.RotationEuler.X),
                    durableStart.RotationEuler.Y + (sceneEnd.RotationEuler.Y - sceneStart.RotationEuler.Y),
                    durableStart.RotationEuler.Z + (sceneEnd.RotationEuler.Z - sceneStart.RotationEuler.Z)),
                durableStart.Scale),
            V1018ViewportTool.Scale => new TransformState(
                durableStart.Position,
                durableStart.RotationEuler,
                V1020ScaleByViewportRatio(durableStart.Scale, sceneStart.Scale, sceneEnd.Scale)),
            _ => throw new InvalidOperationException("Only viewport move/rotate/scale gestures can commit durable transforms.")
        };
    }

    static Vec3 V1020ScaleByViewportRatio(Vec3 durableScale, Vec3 sceneStart, Vec3 sceneEnd)
    {
        double startSquared = (double)sceneStart.X * sceneStart.X + (double)sceneStart.Y * sceneStart.Y + (double)sceneStart.Z * sceneStart.Z;
        double endSquared = (double)sceneEnd.X * sceneEnd.X + (double)sceneEnd.Y * sceneEnd.Y + (double)sceneEnd.Z * sceneEnd.Z;
        if (!double.IsFinite(startSquared) || !double.IsFinite(endSquared) || startSquared <= 1e-12 || endSquared <= 0)
            throw new InvalidOperationException("Viewport scale gesture produced an invalid scale ratio.");

        float factor = (float)Math.Sqrt(endSquared / startSquared);
        if (!float.IsFinite(factor) || factor <= 0)
            throw new InvalidOperationException("Viewport scale gesture produced an invalid scale factor.");
        return new Vec3(durableScale.X * factor, durableScale.Y * factor, durableScale.Z * factor);
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