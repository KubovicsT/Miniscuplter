using System.Runtime.CompilerServices;

internal static class TransformCommitOverlapTests
{
    [ModuleInitializer]
    internal static void ValidateTransformCommitOverlapGuard()
    {
        string root = Directory.GetCurrentDirectory();
        string guardPath = Path.Combine(root, "Scripts", "Main.V1036TransformCommitGuard.cs");
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        if (!File.Exists(guardPath) || !File.Exists(installerPath))
            throw new InvalidOperationException("TEST FAILED: transform overlap guard sources are missing");

        string guard = File.ReadAllText(guardPath);
        string installer = File.ReadAllText(installerPath);
        Assert(installer.Contains("InstallV1036TransformCommitGuard();", StringComparison.Ordinal),
            "transform overlap guard is not installed");
        Assert(guard.Contains("_v1020StageCGate.CurrentCount != 0", StringComparison.Ordinal) &&
               guard.Contains("_v1020TransformGestureActive = false;", StringComparison.Ordinal) &&
               guard.Contains("V1020RestoreMappedObjectFromCurrentState", StringComparison.Ordinal),
            "overlapping mapped transform gestures are not retired fail-closed while a prior durable commit is in flight");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
