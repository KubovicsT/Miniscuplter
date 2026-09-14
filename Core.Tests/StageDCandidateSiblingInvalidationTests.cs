using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class StageDCandidateSiblingInvalidationTests
{
    [ModuleInitializer]
    internal static void ValidateSiblingCandidateInvalidation()
    {
        var objectId = ObjectId.New();
        var inputRevisionId = RevisionId.New();
        var outputARevisionId = RevisionId.New();
        var outputBRevisionId = RevisionId.New();
        var candidateAId = CandidateId.New();
        var candidateBId = CandidateId.New();
        var now = DateTimeOffset.UtcNow;

        var input = new MeshRevision(
            inputRevisionId, objectId, null, "assets/sibling-input.meshbin", new string('a', 64),
            3, 1, "candidate-sibling-input", now);
        var outputA = new MeshRevision(
            outputARevisionId, objectId, inputRevisionId, "assets/sibling-output-a.meshbin", new string('b', 64),
            3, 1, "candidate-sibling-output-a", now);
        var outputB = new MeshRevision(
            outputBRevisionId, objectId, inputRevisionId, "assets/sibling-output-b.meshbin", new string('c', 64),
            3, 1, "candidate-sibling-output-b", now);
        var candidateA = new CandidateRecord(
            candidateAId, objectId, inputRevisionId, outputARevisionId, "refinement",
            CandidateStatus.Ready, "candidate-sibling-a", now);
        var candidateB = new CandidateRecord(
            candidateBId, objectId, inputRevisionId, outputBRevisionId, "refinement",
            CandidateStatus.Ready, "candidate-sibling-b", now);
        var state = new ProjectState(
            ProjectId.New(),
            "candidate-sibling-invalidation",
            objects: new[]
            {
                new ProjectObject(objectId, "Candidate Sibling Test", inputRevisionId, TransformState.Identity)
            },
            meshRevisions: new[] { input, outputA, outputB },
            candidates: new[] { candidateA, candidateB });
        var session = new ProjectSession(state);

        CandidateApplyResult applied = session.ApplyCandidate(candidateAId);
        Assert(applied.Applied && !applied.Conflict,
            "primary candidate did not apply against its exact bound input revision");
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == outputARevisionId,
            "candidate apply did not advance the object to the selected output revision");
        Assert(session.Current.Candidates[candidateAId].Status == CandidateStatus.Applied,
            "applied candidate did not persist Applied status");

        CandidateRecord siblingAfterApply = session.Current.Candidates[candidateBId];
        Assert(siblingAfterApply.Status == CandidateStatus.Conflict &&
               siblingAfterApply.ConflictReason?.Contains("advanced", StringComparison.Ordinal) == true,
            "sibling candidate bound to the displaced input revision remained Ready");
        Assert(siblingAfterApply.InputRevisionId == inputRevisionId &&
               siblingAfterApply.OutputRevisionId == outputBRevisionId,
            "sibling invalidation rewrote its exact dependency identity instead of preserving the conflict");
        Assert(session.UndoTransactions.Count == 1 &&
               session.Current.RevisionNumber == state.RevisionNumber + 1,
            "candidate apply and sibling invalidation were not committed as one atomic project transaction");

        session.Undo();
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == inputRevisionId &&
               session.Current.Candidates[candidateAId].Status == CandidateStatus.Ready &&
               session.Current.Candidates[candidateBId].Status == CandidateStatus.Ready,
            "undo did not restore the exact competing-candidate input state");

        session.Redo();
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == outputARevisionId &&
               session.Current.Candidates[candidateAId].Status == CandidateStatus.Applied &&
               session.Current.Candidates[candidateBId].Status == CandidateStatus.Conflict,
            "redo did not restore the applied candidate plus stale sibling conflict atomically");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
