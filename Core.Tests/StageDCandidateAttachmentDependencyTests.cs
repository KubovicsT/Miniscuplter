using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class StageDCandidateAttachmentDependencyTests
{
    [ModuleInitializer]
    internal static void ValidateCandidateAttachmentDependency()
    {
        var parentId = ObjectId.New();
        var childId = ObjectId.New();
        var parentInputId = RevisionId.New();
        var parentOutputId = RevisionId.New();
        var childRevisionId = RevisionId.New();
        var candidateId = CandidateId.New();
        var attachmentId = AttachmentId.New();
        var now = DateTimeOffset.UtcNow;

        var parentInput = new MeshRevision(parentInputId, parentId, null, "assets/parent-input.meshbin", new string('a', 64), 3, 1, "candidate-attachment-input", now);
        var parentOutput = new MeshRevision(parentOutputId, parentId, parentInputId, "assets/parent-output.meshbin", new string('b', 64), 3, 1, "candidate-attachment-output", now);
        var childRevision = new MeshRevision(childRevisionId, childId, null, "assets/child.meshbin", new string('c', 64), 3, 1, "candidate-attachment-child", now);
        var candidate = new CandidateRecord(candidateId, parentId, parentInputId, parentOutputId, "refinement", CandidateStatus.Ready, "candidate-attachment", now);
        var state = new ProjectState(
            ProjectId.New(),
            "candidate-attachment-dependency",
            objects: new[]
            {
                new ProjectObject(parentId, "Parent", parentInputId, TransformState.Identity),
                new ProjectObject(childId, "Child", childRevisionId, TransformState.Identity)
            },
            meshRevisions: new[] { parentInput, parentOutput, childRevision },
            candidates: new[] { candidate });
        var session = new ProjectSession(state);
        AttachmentRecord attachment = StageDAttachments.Create(session, attachmentId, parentId, childId, "mount", TransformState.Identity);
        Assert(StageDAttachments.ResolveBindingStatus(session.Current, attachment) == AttachmentBindingStatus.Current,
            "attachment was not current before candidate application");

        CandidateApplyResult applied = session.ApplyCandidate(candidateId);
        Assert(applied.Applied && !applied.Conflict, "candidate did not apply against its exact input revision");
        Assert(session.Current.Attachments[attachmentId].BindingStatus == AttachmentBindingStatus.Stale &&
               StageDAttachments.ResolveBindingStatus(session.Current, session.Current.Attachments[attachmentId]) == AttachmentBindingStatus.Stale,
            "candidate-driven parent revision advancement did not persist stale attachment semantics");

        session.Undo();
        Assert(session.Current.Objects[parentId].ActiveMeshRevisionId == parentInputId &&
               session.Current.Candidates[candidateId].Status == CandidateStatus.Ready &&
               StageDAttachments.ResolveBindingStatus(session.Current, session.Current.Attachments[attachmentId]) == AttachmentBindingStatus.Current,
            "undo did not restore the candidate-bound attachment to current semantics");

        session.Redo();
        Assert(session.Current.Objects[parentId].ActiveMeshRevisionId == parentOutputId &&
               session.Current.Candidates[candidateId].Status == CandidateStatus.Applied &&
               session.Current.Attachments[attachmentId].BindingStatus == AttachmentBindingStatus.Stale &&
               StageDAttachments.ResolveBindingStatus(session.Current, session.Current.Attachments[attachmentId]) == AttachmentBindingStatus.Stale,
            "redo did not restore stale attachment semantics after candidate revision advancement");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
