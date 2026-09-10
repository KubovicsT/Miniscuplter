using Miniscuplter.Core;

internal static class StageCGenerationTests
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }

    static MeshData Tetra(float scale = 1f) => new(
        new float[]
        {
            0,0,0,
            scale,0,0,
            0,scale,0,
            0,0,scale
        },
        new int[]
        {
            0,2,1,
            0,1,3,
            0,3,2,
            1,2,3
        });

    static async Task<ImageRevision> CreateImageRevisionAsync(string projectPath, string name, RevisionId? parent = null)
    {
        string source = Path.Combine(Path.GetDirectoryName(projectPath)!, $"source-{name}.png");
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("fake-png-payload-" + name + "-" + Guid.NewGuid().ToString("N"));
        await File.WriteAllBytesAsync(source, payload);
        var revision = await StageCAssetStore.CreateImageRevisionAsync(projectPath, source, "3d-baseline", "unit-test:" + name, parent);
        string durable = StageCAssetStore.ResolveImagePath(projectPath, revision);
        Assert(File.Exists(durable), "accepted image was not copied into durable project storage");
        Assert(!Path.GetFullPath(durable).Equals(Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase), "accepted image revision still points at external/source storage");
        Assert(await File.ReadAllBytesAsync(durable) is var copied && copied.SequenceEqual(payload), "durable image revision payload changed during copy");
        File.Delete(source);
        Assert(File.Exists(durable), "durable accepted image disappeared when the original source was removed");
        return revision;
    }

    public static async Task RunAsync(string root)
    {
        var store = new ProjectStore();
        string projectPath = Path.Combine(root, "stagec.msculpt2");
        var firstImage = await CreateImageRevisionAsync(projectPath, "first");
        var secondImage = await CreateImageRevisionAsync(projectPath, "second", firstImage.Id);
        var state = ProjectState.Create("Stage C")
            .WithImageRevision(firstImage)
            .WithImageRevision(secondImage);
        var session = new ProjectSession(state);

        StageCGeneration.AcceptBaseline(session, firstImage.Id);
        Assert(StageCGeneration.AcceptedBaseline(session.Current) == firstImage.Id, "accepted baseline was not stored as a stable image revision");
        var staleJob = StageCGeneration.BeginImageToMesh(session.Current);
        Assert(staleJob.ProjectId == session.Current.ProjectId, "generation job did not capture project identity");
        Assert(staleJob.InputImageRevisionId == firstImage.Id, "generation job did not capture immutable baseline revision");
        Assert(staleJob.InputProjectRevisionNumber == session.Current.RevisionNumber, "generation job did not capture project revision number");

        StageCGeneration.AcceptBaseline(session, secondImage.Id);
        var staleMesh = await store.CreateMeshRevisionAsync(projectPath, staleJob.OutputObjectId, Tetra(), "unit-test:stale-generated");
        var staleCandidate = StageCGeneration.RegisterResult(session, staleJob, staleMesh, "sf3d", "unit-test:stale-result");
        Assert(staleCandidate.Status == CandidateStatus.Conflict, "stale baseline result should be preserved as a conflict");
        Assert(!session.Current.Objects.ContainsKey(staleJob.OutputObjectId), "stale generation result silently created/replaced an active object");
        Assert(StageCGeneration.ReadCandidates(session.Current).Single(x => x.Id == staleCandidate.Id).ConflictReason != null, "stale candidate did not retain conflict reason");

        var currentJob = StageCGeneration.BeginImageToMesh(session.Current);
        var generatedMesh = await store.CreateMeshRevisionAsync(projectPath, currentJob.OutputObjectId, Tetra(1.25f), "unit-test:generated");
        var ready = StageCGeneration.RegisterResult(session, currentJob, generatedMesh, "sf3d", "unit-test:ready-result");
        Assert(ready.Status == CandidateStatus.Ready, "current baseline generation should be ready for review");
        Assert(!session.Current.Objects.ContainsKey(currentJob.OutputObjectId), "ready candidate became active before explicit apply");

        long beforeApplyRevision = session.Current.RevisionNumber;
        var applied = StageCGeneration.ApplyCandidate(session, ready.Id, "Generated body");
        Assert(applied.Applied && !applied.Conflict, "ready generated candidate was not applied");
        Assert(session.Current.Objects[currentJob.OutputObjectId].ActiveMeshRevisionId == generatedMesh.Id, "applied object does not point at generated immutable mesh revision");
        Assert(StageCGeneration.ReadCandidates(session.Current).Single(x => x.Id == ready.Id).Status == CandidateStatus.Applied, "candidate apply status was not recorded");
        Assert(session.Current.RevisionNumber == beforeApplyRevision + 1, "candidate apply was not one project transaction");

        session.Undo();
        Assert(!session.Current.Objects.ContainsKey(currentJob.OutputObjectId), "undo did not remove the explicitly applied generated object");
        Assert(StageCGeneration.ReadCandidates(session.Current).Single(x => x.Id == ready.Id).Status == CandidateStatus.Ready, "undo did not restore candidate review state");
        session.Redo();
        Assert(session.Current.Objects.ContainsKey(currentJob.OutputObjectId), "redo did not restore generated object apply");

        // Cleanup is a child revision of the exact active generated revision. The generated source
        // remains immutable and export scope must fail closed if a caller asks for the old revision.
        var cleanupBinding = StageCCleanup.Begin(session.Current, currentJob.OutputObjectId);
        Assert(cleanupBinding.InputMeshRevisionId == generatedMesh.Id, "cleanup did not bind the exact active generated revision");
        var cleanedMesh = await store.CreateMeshRevisionAsync(
            projectPath,
            currentJob.OutputObjectId,
            Tetra(1.10f),
            "unit-test:cleanup",
            cleanupBinding.InputMeshRevisionId);
        long beforeCleanupRevision = session.Current.RevisionNumber;
        StageCCleanup.ApplyResult(session, cleanupBinding, cleanedMesh);
        Assert(session.Current.RevisionNumber == beforeCleanupRevision + 1, "cleanup apply was not one project transaction");
        Assert(session.Current.Objects[currentJob.OutputObjectId].ActiveMeshRevisionId == cleanedMesh.Id, "cleanup did not advance the same object to its new revision");
        Assert(session.Current.MeshRevisions.ContainsKey(generatedMesh.Id), "cleanup removed or overwrote the generated source revision");
        Assert(session.Current.MeshRevisions[cleanedMesh.Id].ParentRevisionId == generatedMesh.Id, "cleanup revision lost its generated parent lineage");
        bool staleExportRejected = false;
        try { _ = StageCCleanup.ResolveExportRevision(session.Current, currentJob.OutputObjectId, generatedMesh.Id); }
        catch (InvalidOperationException) { staleExportRejected = true; }
        Assert(staleExportRejected, "export scope accepted a stale/non-active revision");
        Assert(StageCCleanup.ResolveExportRevision(session.Current, currentJob.OutputObjectId, cleanedMesh.Id).Id == cleanedMesh.Id, "export scope did not resolve the exact active cleanup revision");

        // A result bound before another edit must not overwrite the newer active revision.
        var staleCleanupBinding = StageCCleanup.Begin(session.Current, currentJob.OutputObjectId);
        var newerRevision = await store.CreateMeshRevisionAsync(projectPath, currentJob.OutputObjectId, Tetra(1.05f), "unit-test:newer-edit", cleanedMesh.Id);
        StageCCleanup.ApplyResult(session, staleCleanupBinding, newerRevision);
        var lateCleanupRevision = await store.CreateMeshRevisionAsync(projectPath, currentJob.OutputObjectId, Tetra(.95f), "unit-test:late-cleanup", cleanedMesh.Id);
        bool staleCleanupRejected = false;
        try { StageCCleanup.ApplyResult(session, staleCleanupBinding, lateCleanupRevision); }
        catch (InvalidOperationException) { staleCleanupRejected = true; }
        Assert(staleCleanupRejected, "stale cleanup result overwrote a newer active revision");
        Assert(session.Current.Objects[currentJob.OutputObjectId].ActiveMeshRevisionId == newerRevision.Id, "stale cleanup changed the active object revision");

        await store.SaveAsync(session.Current, projectPath);
        var loaded = await store.LoadAsync(projectPath);
        Assert(StageCGeneration.AcceptedBaseline(loaded) == secondImage.Id, "accepted baseline did not survive project save/reload");
        var loadedCandidates = StageCGeneration.ReadCandidates(loaded);
        Assert(loadedCandidates.Count == 2, "generated candidate provenance did not survive save/reload");
        Assert(loadedCandidates.Single(x => x.Id == staleCandidate.Id).Status == CandidateStatus.Conflict, "stale conflict state did not survive save/reload");
        Assert(loadedCandidates.Single(x => x.Id == ready.Id).Status == CandidateStatus.Applied, "applied candidate state did not survive save/reload");
        Assert(loaded.MeshRevisions.ContainsKey(generatedMesh.Id) && loaded.MeshRevisions.ContainsKey(cleanedMesh.Id), "cleanup lineage did not survive save/reload");
        Assert(loaded.Objects[currentJob.OutputObjectId].ActiveMeshRevisionId == newerRevision.Id, "active post-cleanup revision did not survive save/reload");

        var recoveringSession = new ProjectSession(loaded);
        recoveringSession.Execute("Unsaved edit before simulated failure", current =>
            current.WithMetadata("save_failure_probe", "must-not-survive"));
        Assert(recoveringSession.IsDirty && recoveringSession.CanUndo, "save-failure fixture did not create unsaved state");
        bool saveFailureSurfaced = false;
        try
        {
            await recoveringSession.SaveRecoveringAsync(
                _ => Task.FromException(new IOException("simulated durable save failure")),
                () => Task.FromResult(loaded));
        }
        catch (IOException ex)
        {
            saveFailureSurfaced = ex.Message.Contains("restored", StringComparison.OrdinalIgnoreCase);
        }
        Assert(saveFailureSurfaced, "save failure was not surfaced after rollback");
        Assert(recoveringSession.Current.RevisionNumber == loaded.RevisionNumber, "failed save left in-memory revision ahead of durable state");
        Assert(!recoveringSession.Current.Metadata.ContainsKey("save_failure_probe"), "failed save left unsaved metadata authoritative in memory");
        Assert(!recoveringSession.IsDirty, "recovered session should be aligned with its durable revision");
        Assert(!recoveringSession.CanUndo && !recoveringSession.CanRedo, "rollback to durable state retained invalid pre-failure history");

        var mismatchSession = new ProjectSession(loaded);
        mismatchSession.Execute("Unsaved edit before identity mismatch", current =>
            current.WithMetadata("identity_mismatch_probe", "pending"));
        ProjectState foreign = ProjectState.Create("Foreign recovery project");
        bool foreignRecoveryRejected = false;
        try
        {
            await mismatchSession.SaveRecoveringAsync(
                _ => Task.FromException(new IOException("simulated save failure")),
                () => Task.FromResult(foreign));
        }
        catch (AggregateException)
        {
            foreignRecoveryRejected = true;
        }
        Assert(foreignRecoveryRejected, "save recovery accepted a different project identity");
        Assert(mismatchSession.Current.ProjectId == loaded.ProjectId, "failed recovery replaced the session with a foreign project");
        Assert(mismatchSession.IsDirty, "failed recovery falsely marked unresolved in-memory state as durable");
    }
}
