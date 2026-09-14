using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class StageDCandidateAttachmentPersistenceTests
{
    [ModuleInitializer]
    internal static void ValidateCandidateAttachmentPersistence()
    {
        string root = Path.Combine(Path.GetTempPath(), "MiniscuplterStageDCandidateAttachmentPersistenceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string projectPath = Path.Combine(root, "candidate-attachment" + ProjectStore.ProjectExtension);
            var store = new ProjectStore();
            var parentId = ObjectId.New();
            var childId = ObjectId.New();
            var candidateId = CandidateId.New();
            var attachmentId = AttachmentId.New();
            var mesh = new MeshData(
                new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 },
                new int[] { 0, 1, 2 });

            MeshRevision parentInput = store.CreateMeshRevisionAsync(projectPath, parentId, mesh, "candidate-attachment-input").GetAwaiter().GetResult();
            MeshRevision parentOutput = store.CreateMeshRevisionAsync(projectPath, parentId, mesh, "candidate-attachment-output", parentInput.Id).GetAwaiter().GetResult();
            MeshRevision childRevision = store.CreateMeshRevisionAsync(projectPath, childId, mesh, "candidate-attachment-child").GetAwaiter().GetResult();
            var candidate = new CandidateRecord(
                candidateId,
                parentId,
                parentInput.Id,
                parentOutput.Id,
                "refinement",
                CandidateStatus.Ready,
                "candidate-attachment-persistence",
                DateTimeOffset.UtcNow);
            var state = new ProjectState(
                ProjectId.New(),
                "candidate-attachment-persistence",
                objects: new[]
                {
                    new ProjectObject(parentId, "Parent", parentInput.Id, TransformState.Identity),
                    new ProjectObject(childId, "Child", childRevision.Id, TransformState.Identity)
                },
                meshRevisions: new[] { parentInput, parentOutput, childRevision },
                candidates: new[] { candidate });
            var session = new ProjectSession(state);
            StageDAttachments.Create(session, attachmentId, parentId, childId, "mount", TransformState.Identity);

            CandidateApplyResult applied = session.ApplyCandidate(candidateId);
            Assert(applied.Applied && !applied.Conflict,
                "candidate did not apply before persistence");
            Assert(session.Current.Attachments[attachmentId].BindingStatus == AttachmentBindingStatus.Stale,
                "candidate application did not persist stale attachment status before save");

            store.SaveAsync(session.Current, projectPath).GetAwaiter().GetResult();
            ProjectState reopened = store.LoadAsync(projectPath).GetAwaiter().GetResult();
            var reopenedSession = new ProjectSession(reopened);
            AttachmentRecord reopenedAttachment = reopenedSession.Current.Attachments[attachmentId];

            Assert(reopenedSession.Current.Objects[parentId].ActiveMeshRevisionId == parentOutput.Id,
                "save/reopen did not preserve the candidate-applied parent revision");
            Assert(reopenedSession.Current.Candidates[candidateId].Status == CandidateStatus.Applied,
                "save/reopen did not preserve the applied candidate state");
            Assert(reopenedAttachment.ParentMeshRevisionId == parentInput.Id &&
                   reopenedAttachment.ChildMeshRevisionId == childRevision.Id,
                "save/reopen silently transferred the attachment revision binding");
            Assert(reopenedAttachment.BindingStatus == AttachmentBindingStatus.Stale &&
                   StageDAttachments.ResolveBindingStatus(reopenedSession.Current, reopenedAttachment) == AttachmentBindingStatus.Stale,
                "save/reopen resurrected a candidate-invalidated attachment as current");
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
