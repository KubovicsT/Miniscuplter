using System.Runtime.CompilerServices;

internal static class StageDSculptAuthorityTests
{
    [ModuleInitializer]
    internal static void ValidateRevisionBoundSculptWiring()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Scripts", "Main.V1027SelectionAuthority.cs");
        string editingPath = Path.Combine(root, "Scripts", "Main.V1020StageCEditing.cs");
        if (!File.Exists(path) || !File.Exists(editingPath))
            throw new InvalidOperationException("TEST FAILED: Stage-D sculpt authority sources are missing");

        string source = File.ReadAllText(path);
        string editing = File.ReadAllText(editingPath);
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
        Assert(!source.Contains("_v1020SculptGestureActive", StringComparison.Ordinal) &&
               !editing.Contains("_v1020SculptGestureActive", StringComparison.Ordinal) &&
               !editing.Contains("V1020CommitSculptStrokeAsync", StringComparison.Ordinal) &&
               !editing.Contains("_v1020SculptObjectId", StringComparison.Ordinal) &&
               !editing.Contains("_v1020SculptInputRevisionId", StringComparison.Ordinal),
            "mapped sculpt persistence must have one durable commit owner instead of relying on handler-order suppression");
        Assert(source.Contains("V1027CommitRevisionBoundSculptStrokeAsync", StringComparison.Ordinal) &&
               source.Contains("V1020RemoveDuplicatedLegacySculptUndo", StringComparison.Ordinal),
            "revision-bound sculpt authority must retain Core commit and legacy presentation-history cleanup");
        Assert(source.Contains("StageCSelection.RebindWholeObject", StringComparison.Ordinal),
            "successful sculpt revision does not explicitly transfer whole-object selection identity");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
