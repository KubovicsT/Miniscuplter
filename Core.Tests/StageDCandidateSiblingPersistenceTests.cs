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

            string conflictReason = reopenedSession.Current.Candidates[candidateBId].ConflictReason!;
            CandidateApplyResult discarded = reopenedSession.DiscardCandidate(candidateBId);
            Assert(!discarded.Applied && !discarded.Conflict,
                "explicit discard of the reopened sibling conflict did not succeed transactionally");
            Assert(reopenedSession.Current.Candidates[candidateBId].Status == CandidateStatus.Discarded &&
                   reopenedSession.Current.Candidates[candidateBId].ConflictReason is null,
                "explicit discard did not retire the sibling conflict and diagnostic reason");
            Assert(reopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id,
                "discarding the sibling conflict changed the already-applied candidate output");
            Assert(reopenedSession.IsDirty && reopenedSession.CanUndo,
                "discarding the sibling conflict did not create an undoable dirty transaction");

            reopenedSession.Undo();
            Assert(reopenedSession.Current.Candidates[candidateBId].Status == CandidateStatus.Conflict &&
                   reopenedSession.Current.Candidates[candidateBId].ConflictReason == conflictReason &&
                   reopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id,
                "undo did not restore the exact persisted sibling conflict without disturbing the applied output");

            reopenedSession.Redo();
            Assert(reopenedSession.Current.Candidates[candidateBId].Status == CandidateStatus.Discarded &&
                   reopenedSession.Current.Candidates[candidateBId].ConflictReason is null &&
                   reopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id,
                "redo did not restore the discarded sibling state without disturbing the applied output");

            store.SaveAsync(reopenedSession.Current, projectPath).GetAwaiter().GetResult();
            ProjectState discardedReopened = store.LoadAsync(projectPath).GetAwaiter().GetResult();
            var discardedReopenedSession = new ProjectSession(discardedReopened);
            Assert(!discardedReopenedSession.IsDirty &&
                   discardedReopenedSession.Current.Candidates[candidateBId].Status == CandidateStatus.Discarded &&
                   discardedReopenedSession.Current.Candidates[candidateBId].ConflictReason is null,
                "discarded sibling conflict did not reopen as clean durable state");
            Assert(discardedReopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id &&
                   discardedReopenedSession.Current.Candidates[candidateAId].Status == CandidateStatus.Applied,
                "save/reopen after sibling discard disturbed the already-applied candidate result");

            long discardedRevisionBeforeRejectedApply = discardedReopenedSession.Current.RevisionNumber;
            CandidateApplyResult discardedApply = discardedReopenedSession.ApplyCandidate(candidateBId);
            Assert(!discardedApply.Applied && !discardedApply.Conflict,
                "reopened discarded sibling candidate did not remain terminal on Apply");
            Assert(discardedReopenedSession.Current.RevisionNumber == discardedRevisionBeforeRejectedApply &&
                   discardedReopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id &&
                   discardedReopenedSession.Current.Candidates[candidateBId].Status == CandidateStatus.Discarded,
                "Apply against a reopened discarded sibling candidate mutated durable project state");

            CandidateApplyResult repeatedDiscard = discardedReopenedSession.DiscardCandidate(candidateBId);
            Assert(!repeatedDiscard.Applied && !repeatedDiscard.Conflict,
                "repeated discard of a reopened discarded sibling did not remain a terminal no-op");
            Assert(discardedReopenedSession.Current.RevisionNumber == discardedRevisionBeforeRejectedApply &&
                   !discardedReopenedSession.IsDirty &&
                   !discardedReopenedSession.CanUndo &&
                   discardedReopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id &&
                   discardedReopenedSession.Current.Candidates[candidateBId].Status == CandidateStatus.Discarded,
                "repeated discard of a reopened discarded sibling created phantom history or mutated durable state");

            CandidateApplyResult appliedDiscard = discardedReopenedSession.DiscardCandidate(candidateAId);
            Assert(!appliedDiscard.Applied && appliedDiscard.Conflict,
                "reopened applied candidate did not fail closed when discard was attempted");
            Assert(discardedReopenedSession.Current.RevisionNumber == discardedRevisionBeforeRejectedApply &&
                   !discardedReopenedSession.IsDirty &&
                   !discardedReopenedSession.CanUndo &&
                   discardedReopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id &&
                   discardedReopenedSession.Current.Candidates[candidateAId].Status == CandidateStatus.Applied &&
                   discardedReopenedSession.Current.Candidates[candidateBId].Status == CandidateStatus.Discarded,
                "discard against a reopened applied candidate created phantom history or disturbed terminal sibling state");

            CandidateApplyResult repeatedApply = discardedReopenedSession.ApplyCandidate(candidateAId);
            Assert(!repeatedApply.Applied && repeatedApply.Conflict,
                "reopened applied candidate did not remain terminal when Apply was attempted again");
            Assert(discardedReopenedSession.Current.RevisionNumber == discardedRevisionBeforeRejectedApply &&
                   !discardedReopenedSession.IsDirty &&
                   !discardedReopenedSession.CanUndo &&
                   discardedReopenedSession.Current.Objects[objectId].ActiveMeshRevisionId == outputA.Id &&
                   discardedReopenedSession.Current.Candidates[candidateAId].Status == CandidateStatus.Applied &&
                   discardedReopenedSession.Current.Candidates[candidateBId].Status == CandidateStatus.Discarded,
                "Apply against a reopened applied candidate created phantom history or disturbed terminal sibling state");
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