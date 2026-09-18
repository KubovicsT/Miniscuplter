using System.Runtime.CompilerServices;

internal static class JobCancellationBoundaryTests
{
    [ModuleInitializer]
    internal static void ValidateCancellationBoundaryComposition()
    {
        string root = Directory.GetCurrentDirectory();
        string clientPath = Path.Combine(root, "Scripts", "AIClient.cs");
        string recoveryPath = Path.Combine(root, "Scripts", "Main.V1013CancellationRecovery.cs");
        string progressPath = Path.Combine(root, "ai_backend", "job_progress.py");
        string journalPath = Path.Combine(root, "ai_backend", "job_journal.py");
        foreach (string path in new[] { clientPath, recoveryPath, progressPath, journalPath })
            if (!File.Exists(path)) throw new InvalidOperationException("TEST FAILED: cancellation boundary source missing: " + path);

        string client = File.ReadAllText(clientPath);
        string recovery = File.ReadAllText(recoveryPath);
        string progress = File.ReadAllText(progressPath);
        string journal = File.ReadAllText(journalPath);

        int request = client.IndexOf("/job-progress/{Uri.EscapeDataString(jobId)}/cancel", StringComparison.Ordinal);
        int restart = client.IndexOf("await recovery();", StringComparison.Ordinal);
        Assert(request >= 0 && restart > request && client.Contains("TimeSpan.FromSeconds(2)", StringComparison.Ordinal),
            "editor cancellation does not persist a bounded request before terminating the backend");
        Assert(recovery.Contains("await launcher.RestartAsync();", StringComparison.Ordinal),
            "cancellation recovery no longer establishes an owned backend process boundary");
        Assert(progress.Contains("entry[\"state\"] = \"cancelling\"", StringComparison.Ordinal) &&
               progress.Contains("_mark_cancelled_locked", StringComparison.Ordinal),
            "backend cancellation request and acknowledged terminal state are not distinct");
        Assert(journal.Contains("was_cancelling", StringComparison.Ordinal) &&
               journal.Contains("else \"interrupted\"", StringComparison.Ordinal),
            "startup reconciliation cannot distinguish requested cancellation from interrupted work");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
