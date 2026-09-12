using System.Runtime.CompilerServices;

internal static class StageDSelectionDependencyTests
{
    [ModuleInitializer]
    internal static void ValidateSelectionDependencySeam()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Scripts", "Main.V1027ProtectedRegionAuthority.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: durable Smart Selection bridge is missing");
        string source = File.ReadAllText(path);
        Assert(source.Contains("StageCSelection.BindRevisionSelection", StringComparison.Ordinal),
            "Smart Selection is not persisted as a Core SelectionBinding");
        Assert(source.Contains("StageCSelection.IsCurrent", StringComparison.Ordinal),
            "Smart Selection does not detect stale revision-bound indices");
        Assert(source.Contains("ClearV096Selection(false)", StringComparison.Ordinal),
            "stale Smart Selection is not explicitly invalidated");
        Assert(source.Contains("V1020SaveSessionAsync", StringComparison.Ordinal),
            "durable Smart Selection binding is not saved through project persistence");
        Assert(source.Contains("V1027RestoreSmartSelection", StringComparison.Ordinal) &&
               source.Contains("Revision-bound Smart Selection restored from project history", StringComparison.Ordinal),
            "undo/redo history cannot restore the live protected-region selection from durable project state");
        Assert(source.Contains("ProjectStore.ResolveAsset", StringComparison.Ordinal) &&
               source.Contains("binding.DataAssetPath", StringComparison.Ordinal),
            "selection restoration does not resolve its durable asset through the project store");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
