using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class StageDSelectionDependencyTests
{
    [ModuleInitializer]
    internal static void ValidateSelectionDependencySeam()
    {
        ValidateDurableSelectionBridge();
        ValidateCandidateDependencyTransactions();
        ValidatePersistenceAndRevisionAdvanceAsync().GetAwaiter().GetResult();
    }

    static void ValidateDurableSelectionBridge()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Scripts", "Main.V1027ProtectedRegionAuthority.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: durable Smart Selection bridge is missing");
        string source = File.ReadAllText(path);
        Assert(source.Contains("StageCSelection.BindRevisionSelection", StringComparison.Ordinal),
            "Smart Selection is not persisted as a Core SelectionBinding");
        Assert(source.Contains("StageCSelection.IsCurrent", StringComparison.Ordinal),
            "Smart Selection does not detect stale revision-bound indices");
        Assert(source.Contains("ClearV096Selection(false)", StringComparison.Ordinal),
            "stale Smart Selection is not explicitly invalidated");
        Assert(source.Contains("V1020SaveSessionAsync", StringComparison.Ordinal),
            "durable Smart Selection binding is not saved through project persistence");
        Assert(source.Contains("V1027RestoreSmartSelection", StringComparison.Ordinal) &&
               source.Contains("Revision-bound Smart Selection restored from project history", StringComparison.Ordinal),
            "undo/redo history cannot restore the live protected-region selection from durable project state");
        Assert(source.Contains("ProjectStore.ResolveAsset", StringComparison.Ordinal) &&
               source.Contains("binding.DataAssetPath", StringComparison.Ordinal),
            "selection restoration does not resolve its durable asset through the project store");
    }

    static void ValidateCandidateDependencyTransactions()
    {
        var fixture = CreateCandidateFixture(outputDescendsFromInput: true, activeOnInput: true);
        var session = new ProjectSession(fixture.State);

        var applied = session.ApplyCandidate(fixture.CandidateId);
        Assert(applied.Applied && !applied.Conflict, "valid revision-bound candidate did not apply");
        Assert(session.Current.Objects[fixture.ObjectId].ActiveMeshRevisionId == fixture.OutputRevisionId,
            "candidate apply did not advance the target object to its exact output revision");
        Assert(session.Current.Candidates[fixture.CandidateId].Status == CandidateStatus.Applied,
            "candidate apply did not persist Applied status");

        session.Undo();
        Assert(session.Current.Objects[fixture.ObjectId].ActiveMeshRevisionId == fixture.InputRevisionId &&
               session.Current.Candidates[fixture.CandidateId].Status == CandidateStatus.Ready,
            "undo did not restore the exact candidate input state");
        session.Redo();
        Assert(session.Current.Objects[fixture.ObjectId].ActiveMeshRevisionId == fixture.OutputRevisionId &&
               session.Current.Candidates[fixture.CandidateId].Status == CandidateStatus.Applied,
            "redo did not restore the exact applied candidate state");

        session.Undo();
        var discarded = session.DiscardCandidate(fixture.CandidateId);
        Assert(!discarded.Applied && !discarded.Conflict &&
               session.Current.Candidates[fixture.CandidateId].Status == CandidateStatus.Discarded,
            "candidate discard was not transactional");
        session.Undo();
        Assert(session.Current.Candidates[fixture.CandidateId].Status == CandidateStatus.Ready,
            "undo did not restore a discarded candidate to Ready");
        session.Redo();
        Assert(session.Current.Candidates[fixture.CandidateId].Status == CandidateStatus.Discarded,
            "redo did not restore candidate discard");

        var stale = CreateCandidateFixture(outputDescendsFromInput: true, activeOnInput: false);
        var staleSession = new ProjectSession(stale.State);
        var staleResult = staleSession.ApplyCandidate(stale.CandidateId);
        Assert(!staleResult.Applied && staleResult.Conflict &&
               staleSession.Current.Candidates[stale.CandidateId].Status == CandidateStatus.Conflict,
            "candidate bound to a stale input revision was not preserved as a conflict");
        staleSession.Undo();
        Assert(staleSession.Current.Candidates[stale.CandidateId].Status == CandidateStatus.Ready,
            "undo did not restore stale candidate conflict marking");

        var invalidLineage = CreateCandidateFixture(outputDescendsFromInput: false, activeOnInput: true);
        var invalidSession = new ProjectSession(invalidLineage.State);
        var invalidResult = invalidSession.ApplyCandidate(invalidLineage.CandidateId);
        Assert(!invalidResult.Applied && invalidResult.Conflict &&
               invalidSession.Current.Candidates[invalidLineage.CandidateId].Status == CandidateStatus.Conflict,
            "candidate with unrelated output lineage was allowed to replace its input revision");
        Assert(invalidSession.Current.Candidates[invalidLineage.CandidateId].ConflictReason?.Contains("not descended", StringComparison.Ordinal) == true,
            "invalid candidate output lineage did not retain a diagnostic conflict reason");

        long conflictedRevision = invalidSession.Current.RevisionNumber;
        var repeatedConflict = invalidSession.ApplyCandidate(invalidLineage.CandidateId);
        Assert(!repeatedConflict.Applied && repeatedConflict.Conflict &&
               invalidSession.Current.RevisionNumber == conflictedRevision,
            "already-conflicted candidate was reprocessed or mutated again");
    }

    static async Task ValidatePersistenceAndRevisionAdvanceAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "MiniscuplterStageDSelectionDependencyTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string projectPath = Path.Combine(root, "dependency-roundtrip" + ProjectStore.ProjectExtension);
            var store = new ProjectStore();
            var objectId = ObjectId.New();
            var candidateId = CandidateId.New();
            var selectionId = SelectionId.New();
            var mesh = new MeshData(
                new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 },
                new int[] { 0, 1, 2 });

            MeshRevision input = await store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "dependency-roundtrip-input");
            MeshRevision output = await store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "dependency-roundtrip-output", input.Id);
            var layout = ProjectLayout.FromManifest(projectPath);
            string selectionRelativePath = $"data/{selectionId}.json";
            string selectionPath = ProjectStore.ResolveAsset(layout, selectionRelativePath);
            await File.WriteAllTextAsync(selectionPath, "{\"indices\":[0,1,2]}");

            var selection = new SelectionBinding(
                selectionId, objectId, input.Id, "protected-region", selectionRelativePath, DateTimeOffset.UtcNow);
            var candidate = new CandidateRecord(
                candidateId, objectId, input.Id, output.Id, "refinement", CandidateStatus.Ready,
                "dependency-roundtrip", DateTimeOffset.UtcNow);
            var state = new ProjectState(
                ProjectId.New(),
                "dependency-roundtrip",
                objects: new[] { new ProjectObject(objectId, "Dependency Roundtrip", input.Id, TransformState.Identity) },
                meshRevisions: new[] { input, output },
                selections: new[] { selection },
                candidates: new[] { candidate });

            await store.SaveAsync(state, projectPath);
            ProjectState reopened = await store.LoadAsync(projectPath);
            Assert(reopened.Objects[objectId].ActiveMeshRevisionId == input.Id,
                "save/reopen changed the active input revision before candidate application");
            Assert(reopened.Selections.TryGetValue(selectionId, out SelectionBinding? reopenedSelection) && reopenedSelection == selection,
                "save/reopen did not preserve the exact protected-region selection binding");
            Assert(reopened.Candidates.TryGetValue(candidateId, out CandidateRecord? reopenedCandidate) &&
                   reopenedCandidate.InputRevisionId == input.Id && reopenedCandidate.OutputRevisionId == output.Id &&
                   reopenedCandidate.Status == CandidateStatus.Ready,
                "save/reopen did not preserve the exact candidate dependency binding");
            Assert(StageCSelection.IsCurrent(reopened, reopenedSelection!),
                "reopened protected-region selection was not current on its bound input revision");

            var session = new ProjectSession(reopened);
            var result = session.ApplyCandidate(candidateId);
            Assert(result.Applied && !result.Conflict,
                "reopened candidate could not apply against its exact persisted input revision");
            Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == output.Id,
                "candidate application did not advance the reopened object to its output revision");
            Assert(!StageCSelection.IsCurrent(session.Current, session.Current.Selections[selectionId]),
                "revision advancement did not make the persisted protected-region selection stale");

            await store.SaveAsync(session.Current, projectPath);
            ProjectState advanced = await store.LoadAsync(projectPath);
            Assert(advanced.Objects[objectId].ActiveMeshRevisionId == output.Id &&
                   advanced.Candidates[candidateId].Status == CandidateStatus.Applied,
                "save/reopen lost the applied candidate or advanced object revision");
            Assert(advanced.Selections.ContainsKey(selectionId) &&
                   advanced.Selections[selectionId].MeshRevisionId == input.Id &&
                   !StageCSelection.IsCurrent(advanced, advanced.Selections[selectionId]),
                "save/reopen did not preserve the stale protected-region dependency after revision advancement");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    static CandidateFixture CreateCandidateFixture(bool outputDescendsFromInput, bool activeOnInput)
    {
        var projectId = ProjectId.New();
        var objectId = ObjectId.New();
        var inputRevisionId = RevisionId.New();
        var outputRevisionId = RevisionId.New();
        var alternateRevisionId = RevisionId.New();
        var candidateId = CandidateId.New();
        var now = DateTimeOffset.UtcNow;

        var input = new MeshRevision(
            inputRevisionId, objectId, null, "assets/input.meshbin", new string('a', 64),
            3, 1, "candidate-test-input", now);
        var output = new MeshRevision(
            outputRevisionId, objectId, outputDescendsFromInput ? inputRevisionId : null,
            "assets/output.meshbin", new string('b', 64), 3, 1, "candidate-test-output", now);
        var alternate = new MeshRevision(
            alternateRevisionId, objectId, inputRevisionId, "assets/alternate.meshbin", new string('c', 64),
            3, 1, "candidate-test-alternate", now);
        var obj = new ProjectObject(
            objectId, "Candidate Test", activeOnInput ? inputRevisionId : alternateRevisionId,
            TransformState.Identity);
        var candidate = new CandidateRecord(
            candidateId, objectId, inputRevisionId, outputRevisionId, "refinement",
            CandidateStatus.Ready, "candidate-test", now);
        var state = new ProjectState(
            projectId,
            "candidate-test",
            objects: new[] { obj },
            meshRevisions: new[] { input, output, alternate },
            candidates: new[] { candidate });

        return new CandidateFixture(state, objectId, inputRevisionId, outputRevisionId, candidateId);
    }

    readonly record struct CandidateFixture(
        ProjectState State,
        ObjectId ObjectId,
        RevisionId InputRevisionId,
        RevisionId OutputRevisionId,
        CandidateId CandidateId);

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
