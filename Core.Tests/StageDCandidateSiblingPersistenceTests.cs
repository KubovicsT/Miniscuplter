using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class StageDCandidateSiblingPersistenceTests
{
    [ModuleInitializer]
    internal static void ValidateSiblingCandidatePersistence()
    {
        string root = Path.Combine(Path.GetTempPath(), "MiniscuplterStageDCandidateSiblingPersistenceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string projectPath = Path.Combine(root, "candidate-sibling-persistence" + ProjectStore.ProjectExtension);
            var store = new ProjectStore();
            var objectId = ObjectId.New();
            var candidateAId = CandidateId.New();
            var candidateBId = CandidateId.New();
            var mesh = new MeshData(
                new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 },
                new int[] { 0, 1, 2 });

            MeshRevision input = store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "candidate-sibling-input").GetAwaiter().GetResult();
            MeshRevision outputA = store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "candidate-sibling-output-a", input.Id).GetAwaiter().GetResult();
            MeshRevision outputB = store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "candidate-sibling-output-b", input.Id).GetAwaiter().GetResult();
            var now = DateTimeOffset.UtcNow;

            var state = new ProjectState(
                ProjectId.New(),
                "candidate-sibling-persistence",
                objects: new[] { new ProjectObject(objectId, "Candidate Sibling Target", input.Id, TransformState.Identity) },
                meshRevisions: new[] { input, outputA, outputB },
                candidates: new[]
                {
                    new CandidateRecord(candidateAId, objectId, input.Id, outputA.Id, "refinement", CandidateStatus.Ready, "candidate-sibling-a", now),
                    new CandidateRecord(candidateBId, objectId, input.Id, outputB.Id, "refinement", CandidateStatus.Ready, "candidate-sibling-b", now)
                });

            var session = new ProjectSession(state);
            CandidateApplyResult applied = session.ApplyCandidate(candidateAId);
            Assert(applied.Applied && !applied.Conflict,
                "primary candidate did not apply against its exact bound input revision");

            CandidateRecord sibling = session.Current.Candidates[candidateBId];
            Assert(sibling.Status == CandidateStatus.Conflict && !string.IsNullOrWhiteSpace(sibling.ConflictReason),
                "sibling candidate did not become a durable conflict after the competing candidate applied");

            store.SaveAsync(session.Current, projectPath).GetAwaiter().GetResult();
            ProjectState reopened = store.LoadAsync(projectPath).GetAwaiter().GetResult();
            var reopenedSession = new ProjectSession(reopened);

            Assert(!reopenedSession.IsDirty &&
                   reopenedSession.SavedRevisionNumber == reopenedSession.Current.RevisionNumber,
                "a valid persisted sibling conflict was spuriously repaired or marked dirty on reopen");
            Assert(reopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id,
                "save/reopen changed the applied candidate output revision");
            Assert(reopenedSession.Current.Candidates[candidateAId].Status == CandidateStatus.Applied,
                "save/reopen lost the applied candidate status");

            CandidateRecord reopenedSibling = reopenedSession.Current.Candidates[candidateBId];
            Assert(reopenedSibling.Status == CandidateStatus.Conflict,
                "save/reopen resurrected the invalidated sibling candidate as Ready");
            Assert(reopenedSibling.ConflictReason == sibling.ConflictReason,
                "save/reopen changed the sibling candidate conflict reason");
            Assert(reopenedSibling.InputRevisionId == input.Id && reopenedSibling.OutputRevisionId == outputB.Id,
                "save/reopen rewrote the sibling candidate dependency identity");

            long revisionBeforeRejectedApply = reopenedSession.Current.RevisionNumber;
            CandidateApplyResult rejected = reopenedSession.ApplyCandidate(candidateBId);
            Assert(!rejected.Applied && rejected.Conflict,
                "reopened invalidated sibling candidate did not fail closed on Apply");
            Assert(reopenedSession.Current.RevisionNumber == revisionBeforeRejectedApply &&
                   reopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id,
                "rejected sibling Apply mutated durable project state after save/reopen");
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
