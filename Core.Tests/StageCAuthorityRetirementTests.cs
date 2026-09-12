using System.Runtime.CompilerServices;

internal static class StageCAuthorityRetirementTests
{
    [ModuleInitializer]
    internal static void ValidateStageCAuthority()
    {
        string root = Directory.GetCurrentDirectory();
        string legacyPath = Path.Combine(root, "Scripts", "Main.V109Experience.cs");
        string stageCPath = Path.Combine(root, "Scripts", "Main.V1020StageCBridge.cs");
        string editingPath = Path.Combine(root, "Scripts", "Main.V1020StageCEditing.cs");
        string viewportPath = Path.Combine(root, "Scripts", "Main.V1018Foundation.cs");
        string selectionPath = Path.Combine(root, "Scripts", "Main.V1027SelectionAuthority.cs");
        if (!File.Exists(legacyPath) || !File.Exists(stageCPath) || !File.Exists(editingPath) ||
            !File.Exists(viewportPath) || !File.Exists(selectionPath))
            throw new InvalidOperationException("TEST FAILED: Stage-C authority source files are missing");

        string legacy = File.ReadAllText(legacyPath);
        string stageC = File.ReadAllText(stageCPath);
        string editing = File.ReadAllText(editingPath);
        string viewport = File.ReadAllText(viewportPath);
        string selection = File.ReadAllText(selectionPath);

        Assert(
            legacy.Contains("void V109Generate3DAsync() => V1020Generate3DAsync();", StringComparison.Ordinal),
            "historical v1.0.9 generation entry point must delegate to Stage-C authority");
        Assert(
            !legacy.Contains("Generate3DRoutedAsync(", StringComparison.Ordinal),
            "historical v1.0.9 layer must not own a direct 3D provider execution path");
        Assert(
            stageC.Contains("StageCGeneration.BeginImageToMesh", StringComparison.Ordinal),
            "Stage-C generation must remain bound to durable project identity");
        Assert(
            stageC.Contains("Generate3DStageCAsync", StringComparison.Ordinal),
            "Stage-C must remain the backend transport authority");
        Assert(
            stageC.Contains("StageCGeneration.RegisterResult", StringComparison.Ordinal) &&
            stageC.Contains("StageCGeneration.ApplyCandidate", StringComparison.Ordinal),
            "Stage-C must retain candidate registration and explicit Apply ownership");

        Assert(
            stageC.Contains("CallDeferred(nameof(V1020RestoreStageCState));", StringComparison.Ordinal) &&
            stageC.Contains("async void V1020RestoreStageCState()", StringComparison.Ordinal),
            "accepted Stage-C baseline must restore after installer composition rather than racing compatibility UI");
        Assert(
            stageC.Contains("_v1011BaselineImage = path;", StringComparison.Ordinal) &&
            stageC.Contains("_lastEditedImage = path;", StringComparison.Ordinal) &&
            stageC.Contains("_v109Generate3D.Disabled = false;", StringComparison.Ordinal) &&
            stageC.Contains("_v109Generate3D.Text = \"Generate 3D from Accepted Baseline\";", StringComparison.Ordinal),
            "durable baseline restore must restore baseline identity and immediate Generate 3D eligibility together");

        Assert(
            editing.Contains("V1020CommitMoveCommandAsync", StringComparison.Ordinal) &&
            editing.Contains("Vec3 position = current.Transform.Position;", StringComparison.Ordinal),
            "mapped move nudges must derive from Core durable transform state");
        Assert(
            editing.Contains("new Vec3(position.X + delta.X, position.Y + delta.Y, position.Z + delta.Z)", StringComparison.Ordinal),
            "mapped move nudges must apply their command delta to durable Core state");
        Assert(
            !editing.Contains("HookV1020TransformButton(\"Move +X 1 mm\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Move -X 1 mm\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Move +Y 1 mm\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Move -Y 1 mm\"", StringComparison.Ordinal),
            "mapped nudge commands must not regress to scene-observed transform persistence");

        Assert(
            editing.Contains("V1020CommitRotateCommandAsync", StringComparison.Ordinal) &&
            editing.Contains("Vec3 rotation = current.Transform.RotationEuler;", StringComparison.Ordinal),
            "mapped rotate nudges must derive from Core durable transform state");
        Assert(
            editing.Contains("new Vec3(rotation.X, rotation.Y + deltaYRadians, rotation.Z)", StringComparison.Ordinal),
            "mapped rotate nudges must apply their command delta to durable Core state");
        Assert(
            !editing.Contains("HookV1020TransformButton(\"Rotate Y +5°\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Rotate Y -5°\"", StringComparison.Ordinal),
            "mapped rotate commands must not regress to scene-observed transform persistence");

        Assert(
            editing.Contains("V1020CommitScaleCommandAsync", StringComparison.Ordinal) &&
            editing.Contains("Vec3 scale = current.Transform.Scale;", StringComparison.Ordinal),
            "mapped scale nudges must derive from Core durable transform state");
        Assert(
            editing.Contains("new Vec3(scale.X * factor, scale.Y * factor, scale.Z * factor)", StringComparison.Ordinal),
            "mapped scale nudges must apply their command factor to durable Core state");
        Assert(
            !editing.Contains("HookV1020TransformButton(\"Scale +5%\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Scale -5%\"", StringComparison.Ordinal),
            "mapped scale commands must not regress to scene-observed transform persistence");

        Assert(
            editing.Contains("HookV1020GroundButton(\"Place selected on Y=0\")", StringComparison.Ordinal) &&
            editing.Contains("button.Pressed -= CenterOnBuildPlane;", StringComparison.Ordinal),
            "mapped ground placement must retire the legacy scene-mutating handler");
        Assert(
            editing.Contains("V1020CommitGroundCommandAsync(objectId, projectObject.ActiveMeshRevisionId, projectObject.Transform)", StringComparison.Ordinal),
            "ground placement must capture stable object/revision/transform identity before asynchronous commit");
        Assert(
            editing.Contains("current.ActiveMeshRevisionId != expectedRevisionId || current.Transform != expectedTransform", StringComparison.Ordinal) &&
            editing.Contains("MeshBinaryCodec.Read(asset)", StringComparison.Ordinal) &&
            editing.Contains("V1020GroundedTransform(expectedTransform, mesh, 0f)", StringComparison.Ordinal),
            "ground placement must derive from the exact durable transform and immutable active mesh revision");
        Assert(
            editing.Contains("StageCEditing.SetTransformIfCurrent(", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Place selected on Y=0\"", StringComparison.Ordinal) &&
            !editing.Contains("V1020CommitSelectedTransformAsync", StringComparison.Ordinal),
            "ground placement must not regress to scene-observed durable persistence");
        Assert(
            editing.Contains("Stage-C ground placement failed safely; restored durable transform", StringComparison.Ordinal),
            "ground placement failure must restore Godot presentation from Core state");

        Assert(
            editing.Contains("_v1020TransformGestureDurableStart = projectObject.Transform;", StringComparison.Ordinal) &&
            editing.Contains("_v1020TransformGestureSceneStart = V1020TransformState(_selected);", StringComparison.Ordinal),
            "viewport transform gestures must capture durable Core state and presentation start state at gesture start");
        Assert(
            editing.Contains("V1020CommitViewportTransformGestureAsync", StringComparison.Ordinal) &&
            editing.Contains("current.ActiveMeshRevisionId != inputRevisionId || current.Transform != durableStart", StringComparison.Ordinal),
            "viewport transform commits must reject stale object state before persistence");
        Assert(
            editing.Contains("V1020ViewportTransformRequest(durableStart, sceneStart, sceneEnd, tool)", StringComparison.Ordinal) &&
            editing.Contains("durableStart.Position.X + (sceneEnd.Position.X - sceneStart.Position.X)", StringComparison.Ordinal) &&
            editing.Contains("durableStart.RotationEuler.Y + (sceneEnd.RotationEuler.Y - sceneStart.RotationEuler.Y)", StringComparison.Ordinal) &&
            editing.Contains("V1020ScaleByViewportRatio(durableStart.Scale, sceneStart.Scale, sceneEnd.Scale)", StringComparison.Ordinal),
            "viewport drag persistence must apply presentation deltas to the captured durable transform");
        Assert(
            !editing.Contains("_ = V1020CommitSelectedTransformAsync(_v1018Tool.ToString().ToLowerInvariant());", StringComparison.Ordinal),
            "viewport drag release must not persist the already-mutated scene transform as authority");
        Assert(
            editing.Contains("V1020RestoreMappedObjectFromCurrentState(target, objectId, reloadMesh: false);", StringComparison.Ordinal),
            "viewport transform failures must restore Godot presentation from durable Core state");

        Assert(
            viewport.Contains("V1027SelectStableViewportHit(selected);", StringComparison.Ordinal) &&
            !viewport.Contains("Select(selected);", StringComparison.Ordinal),
            "production viewport picking must hand scene hits to the stable selection bridge rather than treating a scene node as identity");
        Assert(
            viewport.Contains("V1027ReconcileStableViewportSelection();", StringComparison.Ordinal),
            "viewport selection must reconcile stable identity against current project state");
        Assert(
            selection.Contains("StageCSelection.BindObject(session.Current, objectId)", StringComparison.Ordinal) &&
            selection.Contains("V1020FindSceneObject(binding.ObjectId)", StringComparison.Ordinal),
            "viewport selection must bind Core object/revision identity before projecting selection back to Godot");
        Assert(
            selection.Contains("StageCSelection.RebindWholeObject(state, binding)", StringComparison.Ordinal) &&
            selection.Contains("rebound.MeshRevisionId != binding.MeshRevisionId", StringComparison.Ordinal) &&
            selection.Contains("_v1027ViewportSelection = rebound;", StringComparison.Ordinal),
            "whole-object selection revision transfer must be explicit when the active mesh revision advances");
        Assert(
            selection.Contains("V1027InvalidateStableViewportSelection(binding.ObjectId)", StringComparison.Ordinal),
            "stable viewport selection must invalidate when its Core object no longer exists");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
