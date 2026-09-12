using Miniscuplter.Core;
using System.Runtime.CompilerServices;

internal static class StageDSelectionDependencyTests
{
    [ModuleInitializer]
    internal static void ValidateSelectionDependencySeam()
    {
        Task.Run(RunCoreRoundTripAsync).GetAwaiter().GetResult();
        ValidateProductionWiring();
    }

    static async Task RunCoreRoundTripAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "miniscuplter-stage-d-selection-tests");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);
        try
        {
            string projectPath = Path.Combine(root, "selection.msculpt2");
            var store = new ProjectStore();
            ObjectId objectId = ObjectId.New();
            MeshRevision original = await store.CreateMeshRevisionAsync(projectPath, objectId, Tetra(), "test:original");
            var session = new ProjectSession(ProjectState.Create("selection dependency")
                .WithMeshRevision(original)
                .WithObject(new ProjectObject(objectId, "body", original.Id, TransformState.Identity)));

            SelectionId selectionId = SelectionId.New();
            ProjectLayout layout = ProjectLayout.FromManifest(projectPath);
            layout.EnsureDirectories();
            string relative = $"data/selection_{selectionId}.json";
            await File.WriteAllTextAsync(ProjectStore.ResolveAsset(layout, relative), "{\"weights\":[1,0,1,0]}");
            SelectionBinding binding = StageCSelection.BindRevisionSelection(
                session, selectionId, objectId, original.Id, "smart-select-vertex-weights", relative);
            Assert(StageCSelection.IsCurrent(session.Current, binding), "fresh durable selection is not current");

            await store.SaveAsync(session.Current, projectPath);
            ProjectState reloaded = await store.LoadAsync(projectPath);
            Assert(reloaded.Selections.TryGetValue(selectionId, out SelectionBinding? persisted) && persisted == binding,
                "durable selection binding did not survive save/reload");
            Assert(StageCSelection.IsCurrent(reloaded, binding), "reloaded selection binding is not current");

            MeshRevision edited = await store.CreateMeshRevisionAsync(projectPath, objectId, Tetra(1.1f), "test:edit", original.Id);
            StageCEditing.CommitMeshRevision(session, objectId, original.Id, edited, "selection invalidation edit");
            Assert(!StageCSelection.IsCurrent(session.Current, binding),
                "revision-dependent selection silently survived a mesh revision change");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    static void ValidateProductionWiring()
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
    }

    static MeshData Tetra(float scale = 1f) => new(
        new float[] { 0,0,0, scale,0,0, 0,scale,0, 0,0,scale },
        new int[] { 0,2,1, 0,1,3, 0,3,2, 1,2,3 });

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
