using System.Runtime.CompilerServices;

internal static class StageCTerminalFailureCleanupTests
{
    [ModuleInitializer]
    internal static void ValidateTerminalFailureCleanup()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "Main.V1020StageCBridge.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: Stage-C bridge source is missing");

        string source = File.ReadAllText(path);
        Assert(source.Contains("var failedBinding = _v1020GenerationBinding;", StringComparison.Ordinal),
            "terminal generation failure must capture the exact live binding before finally clears it");
        Assert(source.Contains("StageCGeneration.AbandonGenerationJob(session, failedBinding)", StringComparison.Ordinal),
            "terminal generation failure must retire its exact durable envelope through Core authority");
        Assert(source.Contains("if (StageCGeneration.AbandonGenerationJob(session, failedBinding))", StringComparison.Ordinal)
            && source.Contains("await V1020SaveSessionAsync();", StringComparison.Ordinal),
            "terminal generation failure must persist durable envelope retirement");
        Assert(source.Contains("Durable generation cleanup also failed:", StringComparison.Ordinal),
            "cleanup failure must be surfaced without replacing the original generation error");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
