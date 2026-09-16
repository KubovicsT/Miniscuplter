using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class StageDSelectionDependencyTests
{
    [ModuleInitializer]
    internal static void ValidateSelectionDependencySeam()
    {
        ValidateDurableSelectionBridge();
        ValidateCandidateDependencyTransactions();
        ValidatePersistenceAndRevisionAdvance();
        ValidateSelectionTransactions();
        ValidateAttachmentTransactions();
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
        Assert(staleSession.Current.Candidates[stale.CandidateId].Status == CandidateStatus.Conflict,
            "loaded candidate bound to a stale input revision was not reconciled as a conflict");
        long staleRevision = staleSession.Current.RevisionNumber;
        var staleResult = staleSession.ApplyCandidate(stale.CandidateId);
        Assert(!staleResult.Applied && staleResult.Conflict &&
               staleSession.Current.Candidates[stale.CandidateId].Status == CandidateStatus.Conflict &&
               staleSession.Current.RevisionNumber == staleRevision,
            "already-conflicted stale candidate was reprocessed or mutated again");

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

    static void ValidatePersistenceAndRevisionAdvance()
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

            MeshRevision input = store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "dependency-roundtrip-input").GetAwaiter().GetResult();
            MeshRevision output = store.CreateMeshRevisionAsync(projectPath, objectId, mesh, "dependency-roundtrip-output", input.Id).GetAwaiter().GetResult();
            var layout = ProjectLayout.FromManifest(projectPath);
            string selectionRelativePath = $"data/{selectionId}.json";
            string selectionPath = ProjectStore.ResolveAsset(layout, selectionRelativePath);
            File.WriteAllText(selectionPath, "{\"indices\":[0,1,2]}");

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

            store.SaveAsync(state, projectPath).GetAwaiter().GetResult();
            ProjectState reopened = store.LoadAsync(projectPath).GetAwaiter().GetResult();
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

            store.SaveAsync(session.Current, projectPath).GetAwaiter().GetResult();
            session.Undo();
            Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == input.Id &&
                   StageCSelection.IsCurrent(session.Current, session.Current.Selections[selectionId]),
                "undo did not restore the revision-bound protected-region selection to current semantics");
            session.Redo();
            Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == output.Id &&
                   !StageCSelection.IsCurrent(session.Current, session.Current.Selections[selectionId]),
                "redo did not restore stale protected-region semantics after revision advancement");


            ProjectState advanced = store.LoadAsync(projectPath).GetAwaiter().GetResult();
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

    static void ValidateSelectionTransactions()
    {
        var objectId = ObjectId.New();
        var revisionId = RevisionId.New();
        var now = DateTimeOffset.UtcNow;
        var revision = new MeshRevision(
            revisionId, objectId, null, "assets/selection.meshbin", new string('f', 64),
            3, 1, "selection-history", now);
        var state = new ProjectState(
            ProjectId.New(),
            "selection-history",
            objects: new[] { new ProjectObject(objectId, "Selection History", revisionId, TransformState.Identity) },
            meshRevisions: new[] { revision });
        var session = new ProjectSession(state);
        var selectionId = SelectionId.New();

        SelectionBinding binding = StageCSelection.BindRevisionSelection(
            session, selectionId, objectId, revisionId, "smart-select-vertex-weights", "data/selection-history.json");
        Assert(StageCSelection.IsSelectionTransaction(session.UndoTransactions.First()),
            "selection bind was not recorded as a selection transaction");

        int historyCount = session.UndoTransactions.Count;
        bool duplicateIdentityRejected = false;
        try
        {
            StageCSelection.BindRevisionSelection(
                session, selectionId, objectId, revisionId, binding.Kind, "data/selection-overwrite.json");
        }
        catch (InvalidOperationException) { duplicateIdentityRejected = true; }
        Assert(duplicateIdentityRejected &&
               session.Current.Selections[selectionId] == binding &&
               session.UndoTransactions.Count == historyCount,
            "duplicate stable selection identity overwrote durable state or history");

        session.Undo();
        Assert(!session.Current.Selections.ContainsKey(selectionId),
            "undo did not remove a newly bound revision selection");
        session.Redo();
        Assert(session.Current.Selections[selectionId] == binding,
            "redo did not restore the exact revision selection binding");

        Assert(StageCSelection.RemoveRevisionSelection(session, binding),
            "selection clear did not remove the exact durable binding");
        Assert(StageCSelection.IsSelectionTransaction(session.UndoTransactions.First()),
            "selection clear was not recorded as a selection transaction");
        session.Undo();
        Assert(session.Current.Selections[selectionId] == binding,
            "undo did not restore the cleared revision selection");
        session.Redo();
        Assert(!session.Current.Selections.ContainsKey(selectionId),
            "redo did not restore the revision selection clear");

        SelectionBinding replacement = StageCSelection.BindRevisionSelection(
            session, SelectionId.New(), objectId, revisionId, binding.Kind, "data/selection-replacement.json");
        Assert(!StageCSelection.RemoveRevisionSelection(session, binding) &&
               session.Current.Selections[replacement.Id] == replacement,
            "stale selection clear removed a newer replacement binding");
    }

    static void ValidateAttachmentTransactions()
    {
        var parentId = ObjectId.New();
        var childId = ObjectId.New();
        var parentRevisionId = RevisionId.New();
        var childRevisionId = RevisionId.New();
        var now = DateTimeOffset.UtcNow;
        var parentRevision = new MeshRevision(parentRevisionId, parentId, null, "assets/parent.meshbin", new string('d', 64), 3, 1, "attachment-parent", now);
        var childRevision = new MeshRevision(childRevisionId, childId, null, "assets/child.meshbin", new string('e', 64), 3, 1, "attachment-child", now);
        var state = new ProjectState(
            ProjectId.New(),
            "attachment-transactions",
            objects: new[]
            {
                new ProjectObject(parentId, "Parent", parentRevisionId, TransformState.Identity),
                new ProjectObject(childId, "Child", childRevisionId, TransformState.Identity)
            },
            meshRevisions: new[] { parentRevision, childRevision });
        var session = new ProjectSession(state);
        var attachmentId = AttachmentId.New();

        AttachmentRecord created = StageDAttachments.Create(
            session, attachmentId, parentId, childId, "surface", TransformState.Identity);
        Assert(session.Current.Attachments.TryGetValue(attachmentId, out AttachmentRecord? stored) && stored == created,
            "Core attachment create did not persist stable identity");
        Assert(StageDAttachments.IsAttachmentTransaction(session.UndoTransactions.First()),
            "attachment create was not recorded as an attachment transaction");

        session.Undo();
        Assert(!session.Current.Attachments.ContainsKey(attachmentId),
            "undo did not remove a newly created attachment");
        session.Redo();
        Assert(session.Current.Attachments[attachmentId] == created,
            "redo did not restore a newly created attachment");

        var moved = new TransformState(new Vec3(1, 2, 3), new Vec3(0, 15, 0), Vec3.One);
        AttachmentRecord updated = StageDAttachments.UpdateIfCurrent(session, created, "edge", moved);
        Assert(session.Current.Attachments[attachmentId] == updated && updated.LocalTransform == moved && updated.Socket == "edge",
            "Core attachment update did not commit local placement and socket atomically");

        bool staleUpdateRejected = false;
        try { StageDAttachments.UpdateIfCurrent(session, created, "stale", TransformState.Identity); }
        catch (InvalidOperationException) { staleUpdateRejected = true; }
        Assert(staleUpdateRejected, "stale attachment update was allowed to overwrite newer attachment state");

        StageDAttachments.RemoveIfCurrent(session, updated);
        Assert(!session.Current.Attachments.ContainsKey(attachmentId),
            "Core attachment remove did not detach the stable attachment identity");
        session.Undo();
        Assert(session.Current.Attachments[attachmentId] == updated,
            "undo did not restore the removed attachment");
        session.Redo();
        Assert(!session.Current.Attachments.ContainsKey(attachmentId),
            "redo did not restore attachment removal");

        bool selfAttachmentRejected = false;
        try { StageDAttachments.Create(new ProjectSession(state), AttachmentId.New(), parentId, parentId, "self", TransformState.Identity); }
        catch (InvalidDataException) { selfAttachmentRejected = true; }
        Assert(selfAttachmentRejected, "self attachment was not rejected by Core authority");

        string root = Path.Combine(Path.GetTempPath(), "MiniscuplterStageDAttachmentTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string projectPath = Path.Combine(root, "attachments" + ProjectStore.ProjectExtension);
            var store = new ProjectStore();
            var mesh = new MeshData(
                new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 },
                new int[] { 0, 1, 2 });
            MeshRevision durableParent = store.CreateMeshRevisionAsync(projectPath, parentId, mesh, "attachment-parent").GetAwaiter().GetResult();
            MeshRevision durableChild = store.CreateMeshRevisionAsync(projectPath, childId, mesh, "attachment-child").GetAwaiter().GetResult();
            var durableState = new ProjectState(
                ProjectId.New(),
                "attachment-persistence",
                objects: new[]
                {
                    new ProjectObject(parentId, "Parent", durableParent.Id, TransformState.Identity),
                    new ProjectObject(childId, "Child", durableChild.Id, TransformState.Identity)
                },
                meshRevisions: new[] { durableParent, durableChild });
            var persistSession = new ProjectSession(durableState);
            AttachmentRecord persisted = StageDAttachments.Create(
                persistSession, AttachmentId.New(), parentId, childId, "persisted", moved);
            store.SaveAsync(persistSession.Current, projectPath).GetAwaiter().GetResult();
            ProjectState reopened = store.LoadAsync(projectPath).GetAwaiter().GetResult();
            Assert(reopened.Attachments.TryGetValue(persisted.Id, out AttachmentRecord? reopenedAttachment) && reopenedAttachment == persisted,
                "save/reopen did not preserve the exact stable attachment record");

            MeshRevision advancedChild = store.CreateMeshRevisionAsync(
                projectPath, childId, mesh, "attachment-child-advanced", durableChild.Id).GetAwaiter().GetResult();
            persistSession.Execute(
                "advance attached child revision",
                currentState => currentState
                    .WithMeshRevision(advancedChild)
                    .WithObject(currentState.Objects[childId] with { ActiveMeshRevisionId = advancedChild.Id }),
                childId);
            AttachmentRecord stale = persistSession.Current.Attachments[persisted.Id];
            Assert(!StageDAttachments.IsAuthoritative(persistSession.Current, stale),
                "attachment did not become stale after its child mesh revision advanced");
            StageDAttachments.RemoveIfCurrent(persistSession, stale);
            Assert(!persistSession.Current.Attachments.ContainsKey(stale.Id),
                "exact stale attachment record was not removable through Core history");
            store.SaveAsync(persistSession.Current, projectPath).GetAwaiter().GetResult();
            ProjectState detached = store.LoadAsync(projectPath).GetAwaiter().GetResult();
            Assert(!detached.Attachments.ContainsKey(stale.Id),
                "save/reopen resurrected a detached stale attachment record");
            persistSession.Undo();
            Assert(persistSession.Current.Attachments.TryGetValue(stale.Id, out AttachmentRecord? restoredStale) &&
                   !StageDAttachments.IsAuthoritative(persistSession.Current, restoredStale),
                "undo did not restore the exact stale attachment record without promoting it to current authority");
            persistSession.Redo();
            Assert(!persistSession.Current.Attachments.ContainsKey(stale.Id),
                "redo did not restore stale attachment removal");
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
