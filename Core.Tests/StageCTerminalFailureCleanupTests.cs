using Miniscuplter.Core;
using System.Runtime.CompilerServices;

internal static class StageCTerminalFailureCleanupTests
{
    [ModuleInitializer]
    internal static void ValidateTerminalFailureCleanup()
    {
        ValidateBridgeWiring();
        ValidateDurableExactEnvelopeRetirement().GetAwaiter().GetResult();
    }

    static void ValidateBridgeWiring()
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

    static async Task ValidateDurableExactEnvelopeRetirement()
    {
        string root = Path.Combine(Path.GetTempPath(), "miniscuplter-stagec-terminal-failure-cleanup");
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        Directory.CreateDirectory(root);
        try
        {
            string projectPath = Path.Combine(root, "terminal-failure.msculpt2");
            string sourcePath = Path.Combine(root, "baseline.png");
            await File.WriteAllBytesAsync(sourcePath, new byte[] { 1, 2, 3, 4 });

            var store = new ProjectStore();
            var baseline = await StageCAssetStore.CreateImageRevisionAsync(
                projectPath, sourcePath, "3d-baseline", "terminal-failure-test", null);
            var session = new ProjectSession(ProjectState.Create("Terminal failure cleanup").WithImageRevision(baseline));
            StageCGeneration.AcceptBaseline(session, baseline.Id);

            var failed = StageCGeneration.BeginImageToMesh(session);
            var survivor = StageCGeneration.BeginImageToMesh(session);
            await store.SaveAsync(session.Current, projectPath);

            Assert(StageCGeneration.AbandonGenerationJob(session, failed),
                "terminal failure cleanup did not retire the failed envelope");
            await store.SaveAsync(session.Current, projectPath);

            ProjectState reloaded = await store.LoadAsync(projectPath);
            var jobs = StageCGeneration.ReadGenerationJobs(reloaded);
            Assert(!jobs.Any(x => x.JobId == failed.JobId),
                "failed generation envelope survived durable save/reload");
            Assert(jobs.Count(x => x.JobId == survivor.JobId) == 1,
                "terminal failure cleanup retired or duplicated an unrelated envelope");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
