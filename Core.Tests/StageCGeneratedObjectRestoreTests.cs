using System.Runtime.CompilerServices;

internal static class StageCGeneratedObjectRestoreTests
{
    [ModuleInitializer]
    internal static void ValidateGeneratedObjectRestoreWiring()
    {
        string root = Directory.GetCurrentDirectory();
        string bridgePath = Path.Combine(root, "Scripts", "Main.V1020StageCBridge.cs");
        string restorePath = Path.Combine(root, "Scripts", "Main.V1031GeneratedObjectRestore.cs");
        if (!File.Exists(bridgePath) || !File.Exists(restorePath))
            throw new InvalidOperationException("TEST FAILED: generated-object restore source is missing");

        string bridge = File.ReadAllText(bridgePath);
        string restore = File.ReadAllText(restorePath);

        Assert(bridge.Contains("V1031RestoreAppliedStageCObjects(session);", StringComparison.Ordinal),
            "Stage-C reopen must invoke generated-object rehydration after durable session restore");
        Assert(restore.Contains("x.Status == CandidateStatus.Applied", StringComparison.Ordinal),
            "rehydration must project only durably applied generated candidates");
        Assert(restore.Contains("projectObject.ActiveMeshRevisionId", StringComparison.Ordinal),
            "rehydration must use the object's current durable active revision rather than the original candidate mesh");
        Assert(restore.Contains("V1020FindSceneObject(projectObject.Id)", StringComparison.Ordinal),
            "rehydration must detect an existing presentation and avoid duplicate scene objects");
        Assert(restore.Contains("_v1013ObjectIds[presentation.GetInstanceId()] = projectObject.Id;", StringComparison.Ordinal),
            "rehydrated presentation must recover stable Core object identity");
        Assert(restore.Contains("V1020ProjectObjectStateToScene(presentation, projectObject, reloadMesh: false);", StringComparison.Ordinal),
            "rehydrated presentation must project the durable transform from Core state");
        Assert(restore.Contains("_v1027ViewportSelection = StageCSelection.BindObject(session.Current, objectId);", StringComparison.Ordinal),
            "rehydrated generated object must recover revision-bound selection for editing continuity");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
