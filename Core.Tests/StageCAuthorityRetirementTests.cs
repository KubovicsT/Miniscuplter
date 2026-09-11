using System.Runtime.CompilerServices;

internal static class StageCAuthorityRetirementTests
{
    [ModuleInitializer]
    internal static void ValidateStageCGenerationAuthority()
    {
        string root = Directory.GetCurrentDirectory();
        string legacyPath = Path.Combine(root, "Scripts", "Main.V109Experience.cs");
        string stageCPath = Path.Combine(root, "Scripts", "Main.V1020StageCBridge.cs");
        if (!File.Exists(legacyPath) || !File.Exists(stageCPath))
            throw new InvalidOperationException("TEST FAILED: Stage-C authority source files are missing");

        string legacy = File.ReadAllText(legacyPath);
        string stageC = File.ReadAllText(stageCPath);

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
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
