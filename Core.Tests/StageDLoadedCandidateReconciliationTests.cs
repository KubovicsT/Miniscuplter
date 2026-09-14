using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class StageDLoadedCandidateReconciliationTests
{
    [ModuleInitializer]
    internal static void ValidateLoadedCandidateReconciliation()
    {
        var stale = CreateCandidateFixture(outputDescendsFromInput: true, activeOnInput: false);
        var staleSession = new ProjectSession(stale.State);
        Assert(
            staleSession.Current.Candidates[stale.CandidateId].Status == CandidateStatus.Conflict &&
            staleSession.Current.Candidates[stale.CandidateId].ConflictReason?.Contains("advanced", StringComparison.Ordinal) == true,
            "loaded candidate bound to a stale input revision remained Ready");
        Assert(staleSession.IsDirty && staleSession.Current.RevisionNumber == stale.State.RevisionNumber + 1,
            "load-time stale-candidate reconciliation was not marked as a durable unsaved repair");

        var invalidLineage = CreateCandidateFixture(outputDescendsFromInput: false, activeOnInput: true);
        var invalidSession = new ProjectSession(invalidLineage.State);
        Assert(
            invalidSession.Current.Candidates[invalidLineage.CandidateId].Status == CandidateStatus.Conflict &&
            invalidSession.Current.Candidates[invalidLineage.CandidateId].ConflictReason?.Contains("not descended", StringComparison.Ordinal) == true,
            "loaded candidate with invalid output lineage remained Ready");
        Assert(invalidSession.IsDirty,
            "load-time invalid-lineage reconciliation was not marked dirty for persistence");

        var valid = CreateCandidateFixture(outputDescendsFromInput: true, activeOnInput: true);
        var validSession = new ProjectSession(valid.State);
        Assert(validSession.Current.Candidates[valid.CandidateId].Status == CandidateStatus.Ready,
            "valid loaded candidate was incorrectly conflicted");
        Assert(!validSession.IsDirty && validSession.Current.RevisionNumber == valid.State.RevisionNumber,
            "unchanged valid load was incorrectly marked dirty");

        validSession.ReplaceFromLoad(stale.State);
        Assert(validSession.Current.Candidates[stale.CandidateId].Status == CandidateStatus.Conflict,
            "ReplaceFromLoad did not reconcile a stale Ready candidate");
        Assert(validSession.IsDirty && validSession.Current.RevisionNumber == stale.State.RevisionNumber + 1,
            "ReplaceFromLoad reconciliation repair was not marked dirty for persistence");
    }

    static CandidateFixture CreateCandidateFixture(bool outputDescendsFromInput, bool activeOnInput)
    {
        var objectId = ObjectId.New();
        var inputRevisionId = RevisionId.New();
        var outputRevisionId = RevisionId.New();
        var alternateRevisionId = RevisionId.New();
        var candidateId = CandidateId.New();
        var now = DateTimeOffset.UtcNow;

        var input = new MeshRevision(
            inputRevisionId, objectId, null, "assets/load-input.meshbin", new string('a', 64),
            3, 1, "loaded-candidate-input", now);
        var output = new MeshRevision(
            outputRevisionId, objectId, outputDescendsFromInput ? inputRevisionId : null,
            "assets/load-output.meshbin", new string('b', 64), 3, 1, "loaded-candidate-output", now);
        var alternate = new MeshRevision(
            alternateRevisionId, objectId, inputRevisionId, "assets/load-alternate.meshbin", new string('c', 64),
            3, 1, "loaded-candidate-alternate", now);
        var obj = new ProjectObject(
            objectId, "Loaded Candidate Test", activeOnInput ? inputRevisionId : alternateRevisionId, TransformState.Identity);
        var candidate = new CandidateRecord(
            candidateId, objectId, inputRevisionId, outputRevisionId, "refinement",
            CandidateStatus.Ready, "loaded-candidate-test", now);
        var state = new ProjectState(
            ProjectId.New(),
            "loaded-candidate-test",
            objects: new[] { obj },
            meshRevisions: new[] { input, output, alternate },
            candidates: new[] { candidate });

        return new CandidateFixture(state, candidateId);
    }

    readonly record struct CandidateFixture(ProjectState State, CandidateId CandidateId);

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
