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
        string installer = File.ReadAllText(installerPath);
        Assert(projection.Contains("if (_v1093DBusy || _v1020StageCSession == null) return;", StringComparison.Ordinal),
            "generated transform projection must wait for the generation busy boundary");
        Assert(projection.Contains("V1020ProjectObjectStateToScene(presentation, projectObject, reloadMesh: false);", StringComparison.Ordinal),
            "new mapped presentation is not projected from durable Core transform state");
        Assert(projection.Contains("_v1036ObservedMappedPresentations.Add(instanceId);", StringComparison.Ordinal),
            "initial transform projection is not bounded to one pass per presentation");
        Assert(installer.Contains("main.InstallV1036InitialTransformProjection();", StringComparison.Ordinal),
            "initial transform projection is not composed by the installer");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
