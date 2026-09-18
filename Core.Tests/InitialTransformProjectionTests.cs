using System.Runtime.CompilerServices;

internal static class InitialTransformProjectionTests
{
    [ModuleInitializer]
    internal static void ValidateGeneratedPresentationProjection()
    {
        string root = Directory.GetCurrentDirectory();
        string projectionPath = Path.Combine(root, "Scripts", "Main.V1036InitialTransformProjection.cs");
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        if (!File.Exists(projectionPath) || !File.Exists(installerPath))
            throw new InvalidOperationException("TEST FAILED: initial-transform projection source is missing");

        string projection = File.ReadAllText(projectionPath);
        string bridge = File.ReadAllText(Path.Combine(root, "Scripts", "Main.V1020StageCBridge.cs"));
        string installer = File.ReadAllText(installerPath);
        Assert(!projection.Contains("Timer", StringComparison.Ordinal) &&
               !projection.Contains("Timeout", StringComparison.Ordinal) &&
               !projection.Contains("V1036ProjectNewMappedPresentation", StringComparison.Ordinal),
            "generated transform projection still depends on scene polling");
        Assert(projection.Contains("void V1036ProjectMappedPresentation(MeshInstance3D presentation, ObjectId objectId)", StringComparison.Ordinal) &&
               projection.Contains("mappedObjectId != objectId", StringComparison.Ordinal),
            "event-driven projection does not verify the exact mapped presentation identity");
        Assert(projection.Contains("V1036EnsureGeneratedPresentationNormals(presentation);", StringComparison.Ordinal) &&
               projection.Contains("surface.GenerateNormals();", StringComparison.Ordinal),
            "Stage-C presentation does not repair the missing normal array before standard rendering");
        Assert(projection.Contains("V1020ProjectObjectStateToScene(presentation, projectObject, reloadMesh: false);", StringComparison.Ordinal),
            "new mapped presentation is not projected from durable Core transform state");
        Assert(projection.Contains("_v1036ObservedMappedPresentations.Add(instanceId);", StringComparison.Ordinal),
            "initial transform projection is not bounded to one pass per presentation");
        Assert(bridge.Contains("_v1013ObjectIds[_selected.GetInstanceId()] = _v1020PendingCandidate.OutputObjectId;\n                    V1036ProjectMappedPresentation(_selected, _v1020PendingCandidate.OutputObjectId);", StringComparison.Ordinal) &&
               bridge.Contains("_v1013ObjectIds[_selected.GetInstanceId()] = candidate.OutputObjectId;\n                    V1036ProjectMappedPresentation(_selected, candidate.OutputObjectId);", StringComparison.Ordinal),
            "Stage-C insertion paths do not project immediately after stable object mapping");
        Assert(!projection.Contains("_v1093DBusy =", StringComparison.Ordinal),
            "projection helper competes with the generation path for busy ownership");
        Assert(installer.Contains("main.InstallV1036InitialTransformProjection();", StringComparison.Ordinal),
            "initial transform projection is not composed by the installer");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
