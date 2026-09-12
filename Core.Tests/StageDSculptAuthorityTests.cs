using System.Runtime.CompilerServices;

internal static class StageDSculptAuthorityTests
{
    [ModuleInitializer]
    internal static void ValidateRevisionBoundSculptWiring()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Scripts", "Main.V1027SelectionAuthority.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: Stage-D sculpt authority source is missing");

        string source = File.ReadAllText(path);
        Assert(source.Contains("StageCSelection.BindObject", StringComparison.Ordinal),
            "sculpt gesture does not capture stable object/revision identity");
        Assert(source.Contains("StageCSelection.IsCurrent", StringComparison.Ordinal),
            "sculpt commit does not reject stale revision-bound selection state");
        Assert(source.Contains("selection.ObjectId", StringComparison.Ordinal) &&
               source.Contains("selection.MeshRevisionId", StringComparison.Ordinal),
            "sculpt commit does not consume exact object and mesh-revision identity");
        Assert(source.Contains("StageCEditing.CommitMeshRevision", StringComparison.Ordinal),
            "sculpt commit bypasses transactional Core mesh-revision history");
        Assert(source.Contains("V1020RestoreMappedObjectFromCurrentState", StringComparison.Ordinal),
            "failed sculpt commit does not restore Godot presentation from durable Core state");
        Assert(source.Contains("_v1020SculptGestureActive = false", StringComparison.Ordinal),
            "legacy sculpt persistence remains able to double-commit the same stroke");
        Assert(source.Contains("StageCSelection.RebindWholeObject", StringComparison.Ordinal),
            "successful sculpt revision does not explicitly transfer whole-object selection identity");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
