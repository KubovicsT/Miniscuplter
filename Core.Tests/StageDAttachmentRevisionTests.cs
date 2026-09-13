using Miniscuplter.Core;

internal static class StageDAttachmentRevisionTests
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }

    static MeshData Tetra(float scale) => new(
        new float[] { 0,0,0, scale,0,0, 0,scale,0, 0,0,scale },
        new int[] { 0,2,1, 0,1,3, 0,3,2, 1,2,3 });

    public static async Task RunAsync(string root)
    {
        var store = new ProjectStore();
        string projectPath = Path.Combine(root, "attachment-revisions.msculpt2");
        var parentId = ObjectId.New();
        var childId = ObjectId.New();
        var parentV1 = await store.CreateMeshRevisionAsync(projectPath, parentId, Tetra(1f), "attachment-test:parent-v1");
        var childV1 = await store.CreateMeshRevisionAsync(projectPath, childId, Tetra(0.5f), "attachment-test:child-v1");
        var state = ProjectState.Create("Attachment revisions")
            .WithMeshRevision(parentV1)
            .WithMeshRevision(childV1)
            .WithObject(new ProjectObject(parentId, "Parent", parentV1.Id, TransformState.Identity))
            .WithObject(new ProjectObject(childId, "Child", childV1.Id, TransformState.Identity));
        var session = new ProjectSession(state);
        var attachment = StageDAttachments.Create(session, AttachmentId.New(), parentId, childId, "mount", TransformState.Identity);
        Assert(attachment.ParentMeshRevisionId == parentV1.Id && attachment.ChildMeshRevisionId == childV1.Id,
            "new attachment did not bind exact active mesh revisions");
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, attachment) == AttachmentBindingStatus.Current,
            "new attachment was not authoritative");

        var parentV2 = await store.CreateMeshRevisionAsync(projectPath, parentId, Tetra(1.1f), "attachment-test:parent-v2", parentV1.Id);
        session.Execute("Advance parent mesh", current => current
            .WithMeshRevision(parentV2)
            .WithObject(current.Objects[parentId] with { ActiveMeshRevisionId = parentV2.Id }), parentId);
        var persisted = session.Current.Attachments[attachment.Id];
        Assert(persisted == attachment, "revision advance rewrote or deleted durable attachment state");
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, persisted) == AttachmentBindingStatus.Stale,
            "attachment did not become stale after parent revision advance");

        bool staleUpdateRejected = false;
        try { StageDAttachments.UpdateIfCurrent(session, persisted, "other", TransformState.Identity); }
        catch (InvalidOperationException) { staleUpdateRejected = true; }
        Assert(staleUpdateRejected, "stale attachment remained authoritative for update");

        await store.SaveAsync(session.Current, projectPath);
        var loaded = await store.LoadAsync(projectPath);
        var loadedAttachment = loaded.Attachments[attachment.Id];
        Assert(StageDAttachments.ResolveBindingStatus(loaded, loadedAttachment) == AttachmentBindingStatus.Stale,
            "save/reopen lost stale attachment semantics");
        Assert(loadedAttachment.ParentMeshRevisionId == parentV1.Id,
            "save/reopen silently transferred the stale parent binding");

        var rebound = StageDAttachments.RebindToCurrent(session, persisted);
        Assert(rebound.ParentMeshRevisionId == parentV2.Id && rebound.ChildMeshRevisionId == childV1.Id,
            "explicit rebind did not capture current mesh revisions");
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, rebound) == AttachmentBindingStatus.Rebound,
            "explicit rebind did not restore authoritative state");
        session.Undo();
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, session.Current.Attachments[attachment.Id]) == AttachmentBindingStatus.Stale,
            "undo did not restore stale attachment binding");
        session.Redo();
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, session.Current.Attachments[attachment.Id]) == AttachmentBindingStatus.Rebound,
            "redo did not restore rebound attachment binding");
    }
}
