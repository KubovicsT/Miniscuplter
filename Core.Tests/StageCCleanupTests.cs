using Miniscuplter.Core;
using System.Runtime.CompilerServices;

internal static class StageCCleanupTests
{
    [ModuleInitializer]
    internal static void ValidateCleanupAndExportScope() => Run();

    internal static void Run()
    {
        var objectId = ObjectId.New();
        var otherObjectId = ObjectId.New();
        var input = Revision(objectId, null, "assets/input.mesh", "cleanup-input");
        var other = Revision(otherObjectId, null, "assets/other.mesh", "cleanup-other");
        var state = ProjectState.Create("Cleanup scope")
            .WithMeshRevision(input)
            .WithMeshRevision(other)
            .WithObject(new ProjectObject(objectId, "Body", input.Id, TransformState.Identity))
            .WithObject(new ProjectObject(otherObjectId, "Other", other.Id, TransformState.Identity));
        var session = new ProjectSession(state);

        StageCCleanupBinding binding = StageCCleanup.Begin(session.Current, objectId);
        Assert(binding.ProjectId == session.Current.ProjectId, "cleanup binding lost project identity");
        Assert(binding.ObjectId == objectId && binding.InputMeshRevisionId == input.Id,
            "cleanup binding did not capture exact object/revision scope");

        var output = Revision(objectId, input.Id, "assets/clean.mesh", "cleanup-output");
        ProjectTransaction tx = StageCCleanup.ApplyResult(session, binding, output);
        Assert(tx.AffectedObjectIds.Count == 1 && tx.AffectedObjectIds[0] == objectId,
            "cleanup transaction lost target object identity");
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == output.Id,
            "cleanup transaction did not advance active revision");
        Assert(session.Current.MeshRevisions.ContainsKey(output.Id),
            "cleanup output revision was not registered transactionally");

        session.Undo();
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == input.Id,
            "cleanup undo did not restore the source revision");
        Assert(!session.Current.MeshRevisions.ContainsKey(output.Id),
            "cleanup undo did not restore the complete pre-cleanup state");
        session.Redo();
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == output.Id,
            "cleanup redo did not restore the cleaned revision");

        MeshRevision export = StageCCleanup.ResolveExportRevision(session.Current, objectId, output.Id);
        Assert(export.Id == output.Id && export.ObjectId == objectId,
            "export scope did not resolve exact current object/revision");
        AssertThrows(() => StageCCleanup.ResolveExportRevision(session.Current, objectId, input.Id),
            "stale export revision was accepted");
        AssertThrows(() => StageCCleanup.ResolveExportRevision(session.Current, objectId, other.Id),
            "cross-object export revision was accepted");

        var staleSession = new ProjectSession(state);
        StageCCleanupBinding stale = StageCCleanup.Begin(staleSession.Current, objectId);
        var intervening = Revision(objectId, input.Id, "assets/intervening.mesh", "intervening");
        staleSession.Execute("Advance during cleanup", current => current
            .WithMeshRevision(intervening)
            .WithObject(current.Objects[objectId] with { ActiveMeshRevisionId = intervening.Id }), objectId);
        var staleOutput = Revision(objectId, input.Id, "assets/stale-output.mesh", "stale-output");
        AssertThrows(() => StageCCleanup.ApplyResult(staleSession, stale, staleOutput),
            "stale cleanup result overwrote a newer active revision");

        var wrongParent = Revision(objectId, null, "assets/wrong-parent.mesh", "wrong-parent");
        var freshSession = new ProjectSession(state);
        StageCCleanupBinding fresh = StageCCleanup.Begin(freshSession.Current, objectId);
        AssertThrows(() => StageCCleanup.ApplyResult(freshSession, fresh, wrongParent),
            "cleanup output without exact parent lineage was accepted");

        var wrongObjectOutput = Revision(otherObjectId, input.Id, "assets/wrong-object.mesh", "wrong-object");
        AssertThrows(() => StageCCleanup.ApplyResult(freshSession, fresh, wrongObjectOutput),
            "cleanup output belonging to another object was accepted");

        ValidatePresentationWiring();
    }

    static void ValidatePresentationWiring()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "Main.V1020StageCCleanupExport.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: Stage-C cleanup/export bridge source is missing");
        string source = File.ReadAllText(path);
        Assert(source.Contains("StageCCleanup.Begin(session.Current, objectId)", StringComparison.Ordinal),
            "repair path must bind exact durable object/revision scope before cleanup");
        Assert(source.Contains("StageCCleanup.ApplyResult(session, binding, outputRevision);", StringComparison.Ordinal)
            && source.Contains("await V1020SaveSessionAsync();", StringComparison.Ordinal),
            "repair path must transactionally apply and durably save cleanup before presentation publication");
        Assert(source.IndexOf("await V1020SaveSessionAsync();", StringComparison.Ordinal)
               < source.IndexOf("target.Mesh = repaired;", StringComparison.Ordinal),
            "repair presentation is published before durable cleanup save");
        Assert(source.Contains("StageCCleanup.ResolveExportRevision(session.Current, objectId, obj.ActiveMeshRevisionId)", StringComparison.Ordinal),
            "export must resolve the exact durable active object/revision");
        Assert(source.Contains("ProjectStore.ResolveAsset", StringComparison.Ordinal)
            && source.Contains("V1020GodotTransform(obj.Transform)", StringComparison.Ordinal),
            "export must derive mesh and transform from durable project state");
        Assert(source.Contains("V1020ValidateExportMesh(verification)", StringComparison.Ordinal)
            && source.Contains("File.Move(temp, full, true);", StringComparison.Ordinal),
            "export must verify temporary STL before replacing destination");
    }

    static MeshRevision Revision(ObjectId objectId, RevisionId? parent, string path, string provenance) => new(
        RevisionId.New(), objectId, parent, path, new string('a', 64), 4, 4,
        provenance, DateTimeOffset.UtcNow);

    static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("TEST FAILED: " + message);
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
