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
        var attachment = StageDAttachments.Create(session, AttachmentId.New(), parentId, childId, "mount", TransformState.Identity, "detail-part");
        Assert(attachment.ParentMeshRevisionId == parentV1.Id && attachment.ChildMeshRevisionId == childV1.Id,
            "new attachment did not bind exact active mesh revisions");
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, attachment) == AttachmentBindingStatus.Current,
            "new attachment was not authoritative");
        Assert(attachment.PartLibraryId == "detail-part",
            "new attachment did not retain durable part-library identity");

        var parentV2 = await store.CreateMeshRevisionAsync(projectPath, parentId, Tetra(1.1f), "attachment-test:parent-v2", parentV1.Id);
        session.Execute("Advance parent mesh", current => current
            .WithMeshRevision(parentV2)
            .WithObject(current.Objects[parentId] with { ActiveMeshRevisionId = parentV2.Id }), parentId);
        var persisted = session.Current.Attachments[attachment.Id];
        Assert(persisted.ParentMeshRevisionId == parentV1.Id && persisted.ChildMeshRevisionId == childV1.Id,
            "revision advance silently transferred durable attachment revision bindings");
        Assert(persisted.BindingStatus == AttachmentBindingStatus.Stale,
            "revision advance did not persist the stale attachment status");
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, persisted) == AttachmentBindingStatus.Stale,
            "attachment did not become stale after parent revision advance");

        bool staleUpdateRejected = false;
        try { StageDAttachments.UpdateIfCurrent(session, persisted, "other", TransformState.Identity); }
        catch (InvalidOperationException) { staleUpdateRejected = true; }
        Assert(staleUpdateRejected, "stale attachment remained authoritative for update");

        await store.SaveAsync(session.Current, projectPath);
        var loaded = await store.LoadAsync(projectPath);
        var loadedAttachment = loaded.Attachments[attachment.Id];
        Assert(loadedAttachment.BindingStatus == AttachmentBindingStatus.Stale,
            "save/reopen lost the durable stale attachment status");
        Assert(StageDAttachments.ResolveBindingStatus(loaded, loadedAttachment) == AttachmentBindingStatus.Stale,
            "save/reopen lost stale attachment semantics");
        Assert(loadedAttachment.ParentMeshRevisionId == parentV1.Id,
            "save/reopen silently transferred the stale parent binding");
        Assert(loadedAttachment.PartLibraryId == "detail-part",
            "save/reopen lost durable part-library identity");

        string legacyManifest = string.Join(Environment.NewLine,
            (await File.ReadAllLinesAsync(projectPath))
                .Where(line => !line.Contains("\"PartLibraryId\"", StringComparison.Ordinal)))
            .Replace("\"SchemaVersion\": 8", "\"SchemaVersion\": 7", StringComparison.Ordinal);
        await File.WriteAllTextAsync(projectPath, legacyManifest);
        var migratedV7 = await store.LoadAsync(projectPath);
        Assert(migratedV7.Attachments[attachment.Id].PartLibraryId == null,
            "schema-7 migration invented attachment part-library identity");
        await store.SaveAsync(migratedV7, projectPath);
        string migratedManifest = await File.ReadAllTextAsync(projectPath);
        Assert(migratedManifest.Contains("\"SchemaVersion\": 8", StringComparison.Ordinal),
            "schema-7 project was not upgraded on save");
        Assert(migratedManifest.Contains("\"PartLibraryId\": null", StringComparison.Ordinal),
            "schema-7 attachment migration did not emit the schema-8 compatibility field");

        var legacyCurrent = loaded.WithAttachment(loadedAttachment with { BindingStatus = AttachmentBindingStatus.Current });
        var reconciledSession = new ProjectSession(legacyCurrent);
        Assert(reconciledSession.Current.Attachments[attachment.Id].BindingStatus == AttachmentBindingStatus.Stale,
            "session construction did not reconcile a loaded stale attachment marked current");
        Assert(reconciledSession.IsDirty && reconciledSession.Current.RevisionNumber == legacyCurrent.RevisionNumber + 1,
            "session construction attachment reconciliation was not marked as a durable unsaved repair");
        var replacementSession = new ProjectSession(state);
        replacementSession.ReplaceFromLoad(legacyCurrent);
        Assert(replacementSession.Current.Attachments[attachment.Id].BindingStatus == AttachmentBindingStatus.Stale,
            "ReplaceFromLoad did not reconcile a stale attachment marked current");
        Assert(replacementSession.IsDirty && replacementSession.Current.RevisionNumber == legacyCurrent.RevisionNumber + 1,
            "ReplaceFromLoad attachment reconciliation was not marked dirty for persistence");

        var rebound = StageDAttachments.RebindToCurrent(session, persisted);
        Assert(rebound.ParentMeshRevisionId == parentV2.Id && rebound.ChildMeshRevisionId == childV1.Id,
            "explicit rebind did not capture current mesh revisions");
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, rebound) == AttachmentBindingStatus.Rebound,
            "explicit rebind did not restore authoritative state");
        session.Undo();
        Assert(session.Current.Attachments[attachment.Id].BindingStatus == AttachmentBindingStatus.Stale,
            "undo did not restore durable stale attachment status");
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, session.Current.Attachments[attachment.Id]) == AttachmentBindingStatus.Stale,
            "undo did not restore stale attachment binding");
        session.Redo();
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, session.Current.Attachments[attachment.Id]) == AttachmentBindingStatus.Rebound,
            "redo did not restore rebound attachment binding");

        await store.SaveAsync(session.Current, projectPath);
        var reopenedRebound = await store.LoadAsync(projectPath);
        var persistedRebound = reopenedRebound.Attachments[attachment.Id];
        Assert(persistedRebound.BindingStatus == AttachmentBindingStatus.Rebound &&
               persistedRebound.ParentMeshRevisionId == parentV2.Id &&
               persistedRebound.ChildMeshRevisionId == childV1.Id &&
               persistedRebound.PartLibraryId == "detail-part",
            "save/reopen did not preserve the exact explicit rebind state");
        Assert(StageDAttachments.ResolveBindingStatus(reopenedRebound, persistedRebound) == AttachmentBindingStatus.Rebound,
            "save/reopen did not preserve authoritative rebound semantics");

        var childV2 = await store.CreateMeshRevisionAsync(projectPath, childId, Tetra(0.6f), "attachment-test:child-v2", childV1.Id);
        session.Execute("Advance child mesh", current => current
            .WithMeshRevision(childV2)
            .WithObject(current.Objects[childId] with { ActiveMeshRevisionId = childV2.Id }), childId);
        var childStale = session.Current.Attachments[attachment.Id];
        Assert(childStale.ParentMeshRevisionId == parentV2.Id && childStale.ChildMeshRevisionId == childV1.Id,
            "child revision advance silently transferred durable attachment revision bindings");
        Assert(childStale.BindingStatus == AttachmentBindingStatus.Stale &&
               StageDAttachments.ResolveBindingStatus(session.Current, childStale) == AttachmentBindingStatus.Stale,
            "attachment did not persist stale state after child revision advance");
        bool childStaleUpdateRejected = false;
        try { StageDAttachments.UpdateIfCurrent(session, childStale, "other-child", TransformState.Identity); }
        catch (InvalidOperationException) { childStaleUpdateRejected = true; }
        Assert(childStaleUpdateRejected,
            "child-revision-stale attachment remained authoritative for update");

        var staleMarkedRebound = session.Current.WithAttachment(childStale with { BindingStatus = AttachmentBindingStatus.Rebound });
        var reboundReconciledSession = new ProjectSession(staleMarkedRebound);
        Assert(reboundReconciledSession.Current.Attachments[attachment.Id].BindingStatus == AttachmentBindingStatus.Stale,
            "session construction did not fail closed a revision-stale attachment marked rebound");
        Assert(reboundReconciledSession.IsDirty && reboundReconciledSession.Current.RevisionNumber == staleMarkedRebound.RevisionNumber + 1,
            "stale Rebound load reconciliation was not marked as a durable unsaved repair");
        var reboundReplacementSession = new ProjectSession(state);
        reboundReplacementSession.ReplaceFromLoad(staleMarkedRebound);
        Assert(reboundReplacementSession.Current.Attachments[attachment.Id].BindingStatus == AttachmentBindingStatus.Stale,
            "ReplaceFromLoad did not fail closed a revision-stale attachment marked rebound");
        Assert(reboundReplacementSession.IsDirty && reboundReplacementSession.Current.RevisionNumber == staleMarkedRebound.RevisionNumber + 1,
            "ReplaceFromLoad stale Rebound reconciliation was not marked dirty for persistence");
    }
}
