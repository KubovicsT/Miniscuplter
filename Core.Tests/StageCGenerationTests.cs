using System.Security.Cryptography;
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
        var layout = ProjectLayout.FromManifest(projectPath);
        layout.EnsureDirectories();
        var id = RevisionId.New();
        string relative = $"images/{id}.png";
        string path = ProjectStore.ResolveAsset(layout, relative);
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("fake-png-payload-" + name + "-" + id);
        await File.WriteAllBytesAsync(path, payload);
        string hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        return new ImageRevision(id, parent, relative, hash, "3d-baseline", "unit-test:" + name, DateTimeOffset.UtcNow);
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

        // User advances the accepted baseline while inference is still running.
        StageCGeneration.AcceptBaseline(session, secondImage.Id);
        var staleMesh = await store.CreateMeshRevisionAsync(projectPath, staleJob.OutputObjectId, Tetra(), "unit-test:stale-generated");
        var staleCandidate = StageCGeneration.RegisterResult(session, staleJob, staleMesh, "sf3d", "unit-test:stale-result");
        Assert(staleCandidate.Status == CandidateStatus.Conflict, "stale baseline result should be preserved as a conflict");
        Assert(!session.Current.Objects.ContainsKey(staleJob.OutputObjectId), "stale generation result silently created/replaced an active object");
        Assert(StageCGeneration.ReadCandidates(session.Current).Single(x => x.Id == staleCandidate.Id).ConflictReason != null, "stale candidate did not retain conflict reason");

        // A job from the current accepted baseline may become a ready candidate, but must not
        // become visible/active project state until the explicit transactional apply.
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

        await store.SaveAsync(session.Current, projectPath);
        var loaded = await store.LoadAsync(projectPath);
        Assert(StageCGeneration.AcceptedBaseline(loaded) == secondImage.Id, "accepted baseline did not survive project save/reload");
        var loadedCandidates = StageCGeneration.ReadCandidates(loaded);
        Assert(loadedCandidates.Count == 2, "generated candidate provenance did not survive save/reload");
        Assert(loadedCandidates.Single(x => x.Id == staleCandidate.Id).Status == CandidateStatus.Conflict, "stale conflict state did not survive save/reload");
        Assert(loadedCandidates.Single(x => x.Id == ready.Id).Status == CandidateStatus.Applied, "applied candidate state did not survive save/reload");
    }
}
