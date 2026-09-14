using System.Runtime.CompilerServices;

internal static class ProtectedRegionPresentationSafetyTests
{
    [ModuleInitializer]
    internal static void ValidateProtectedRegionPresentationFailsClosed()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Scripts", "Main.V1027ProtectedRegionAuthority.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: protected-region authority bridge is missing");

        string source = File.ReadAllText(path);
        string normalizedSource = source.Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert(source.Contains("liveObjectId == null || liveObjectId == existing.ObjectId", StringComparison.Ordinal),
            "stale protected-region weights are not cleared when stable live object identity cannot be proven");
        Assert(source.Contains("_v1027DurableSmartSelection = null;", StringComparison.Ordinal),
            "stale durable protected-region presentation binding is retained after invalidation");
        Assert(source.Contains("_v1027FailedSelectionRestore = null;", StringComparison.Ordinal),
            "stale restore-failure presentation state is retained after revision invalidation");
        Assert(source.Contains("_v096Selection.AsSpan().SequenceEqual(weights)", StringComparison.Ordinal),
            "Smart Selection persistence can acknowledge a stale snapshot as if it contained newer live weights");
        Assert(normalizedSource.Contains("if (_v096Selection != null)\n                V1027ReconcileDurableSmartSelection();", StringComparison.Ordinal),
            "Smart Selection changes made during an in-flight persistence operation are not re-queued afterward");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
