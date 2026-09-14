using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class StageDCandidateConflictPersistenceTests
{
    [ModuleInitializer]
    internal static void ValidateCandidateConflictPersistence()
    {
        string root = Path.Combine(Path.GetTempPath(), "MiniscuplterStageDCandidateConflictPersistenceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string projectPath = Path.Combine(root, "candidate-conflict" + ProjectStore.ProjectExtension);
            var store = new ProjectStore();
            var objectId = ObjectId.New();
            var candidateId = CandidateId.New();
            var mesh = new MeshData(
                new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 },
                new int[] { 0, 1, 2 });

            MeshRevision input = store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "candidate-input").GetAwaiter().GetResult();
            MeshRevision output = store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "candidate-output", input.Id).GetAwaiter().GetResult();
            MeshRevision alternate = store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "candidate-alternate", input.Id).GetAwaiter().GetResult();

            var state = new ProjectState(
                ProjectId.New(),
                "candidate-conflict-persistence",
                objects: new[] { new ProjectObject(objectId, "Candidate Target", alternate.Id, TransformState.Identity) },
                meshRevisions: new[] { input, output, alternate },
                candidates: new[]
                {
                    new CandidateRecord(
                        candidateId,
                        objectId,
                        input.Id,
                        output.Id,
                        "refinement",
                        CandidateStatus.Ready,
                        "candidate-conflict-persistence",
                        DateTimeOffset.UtcNow)
                });

            var session = new ProjectSession(state);
            CandidateRecord conflicted = session.Current.Candidates[candidateId];
            Assert(conflicted.Status == CandidateStatus.Conflict,
                "stale Ready candidate was not reconciled to Conflict before persistence");
            Assert(!string.IsNullOrWhiteSpace(conflicted.ConflictReason),
                "reconciled candidate conflict did not retain its diagnostic reason");

            store.SaveAsync(session.Current, projectPath).GetAwaiter().GetResult();
            ProjectState reopened = store.LoadAsync(projectPath).GetAwaiter().GetResult();
            var reopenedSession = new ProjectSession(reopened);
            CandidateRecord reopenedCandidate = reopenedSession.Current.Candidates[candidateId];

            Assert(reopenedCandidate.Status == CandidateStatus.Conflict,
                "save/reopen resurrected a conflicted candidate as Ready");
            Assert(reopenedCandidate.ConflictReason == conflicted.ConflictReason,
                "save/reopen changed the durable candidate conflict reason");

            long revisionBeforeApply = reopenedSession.Current.RevisionNumber;
            CandidateApplyResult apply = reopenedSession.ApplyCandidate(candidateId);
            Assert(!apply.Applied && apply.Conflict,
                "reopened conflicted candidate did not fail closed on Apply");
            Assert(reopenedSession.Current.RevisionNumber == revisionBeforeApply &&
                   reopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == alternate.Id,
                "reopened conflicted candidate mutated project state during rejected Apply");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
