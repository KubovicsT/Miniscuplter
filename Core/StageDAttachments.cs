namespace Miniscuplter.Core;

/// <summary>
/// UI-neutral durable attachment transaction seam. Godot may choose a parent/child pair and
/// local placement, but Core owns stable attachment identity and committed history.
/// </summary>
public static class StageDAttachments
{
    public const string TransactionPrefix = "Stage-D attachment:";

    public static AttachmentRecord Create(
        ProjectSession session,
        AttachmentId attachmentId,
        ObjectId parentObjectId,
        ObjectId childObjectId,
        string socket,
        TransformState localTransform)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateIdentity(session.Current, attachmentId, parentObjectId, childObjectId, socket, localTransform);
        if (session.Current.Attachments.ContainsKey(attachmentId))
            throw new InvalidOperationException($"Attachment {attachmentId} already exists.");
        if (session.Current.Attachments.Values.Any(x => x.ChildObjectId == childObjectId))
            throw new InvalidOperationException($"Object {childObjectId} is already attached.");

        var attachment = new AttachmentRecord(
            attachmentId, parentObjectId, childObjectId, socket.Trim(), localTransform, DateTimeOffset.UtcNow);
        session.Execute(
            $"{TransactionPrefix} create",
            state =>
            {
                ValidateIdentity(state, attachmentId, parentObjectId, childObjectId, socket, localTransform);
                if (state.Attachments.ContainsKey(attachmentId))
                    throw new InvalidOperationException($"Attachment {attachmentId} appeared before commit.");
                if (state.Attachments.Values.Any(x => x.ChildObjectId == childObjectId))
                    throw new InvalidOperationException($"Object {childObjectId} became attached before commit.");
                return state.WithAttachment(attachment);
            },
            parentObjectId, childObjectId);
        return attachment;
    }

    public static AttachmentRecord UpdateIfCurrent(
        ProjectSession session,
        AttachmentRecord expected,
        string socket,
        TransformState localTransform)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(expected);
        ValidateIdentity(session.Current, expected.Id, expected.ParentObjectId, expected.ChildObjectId, socket, localTransform);
        if (!session.Current.Attachments.TryGetValue(expected.Id, out AttachmentRecord? current) || current != expected)
            throw new InvalidOperationException("Attachment changed before the update could commit.");

        var updated = expected with { Socket = socket.Trim(), LocalTransform = localTransform };
        if (updated == expected) return expected;
        session.Execute(
            $"{TransactionPrefix} update",
            state =>
            {
                if (!state.Attachments.TryGetValue(expected.Id, out AttachmentRecord? persisted) || persisted != expected)
                    throw new InvalidOperationException("Attachment changed before the update transaction could commit.");
                ValidateIdentity(state, expected.Id, expected.ParentObjectId, expected.ChildObjectId, socket, localTransform);
                return state.WithAttachment(updated);
            },
            expected.ParentObjectId, expected.ChildObjectId);
        return updated;
    }

    public static bool IsAttachmentTransaction(ProjectTransaction transaction) =>
        transaction != null && transaction.Label.StartsWith(TransactionPrefix, StringComparison.Ordinal);

    static void ValidateIdentity(
        ProjectState state,
        AttachmentId attachmentId,
        ObjectId parentObjectId,
        ObjectId childObjectId,
        string socket,
        TransformState localTransform)
    {
        if (attachmentId.Value == Guid.Empty)
            throw new ArgumentException("Attachment ID cannot be empty.", nameof(attachmentId));
        if (parentObjectId.Value == Guid.Empty || childObjectId.Value == Guid.Empty)
            throw new ArgumentException("Attachment object IDs cannot be empty.");
        if (parentObjectId == childObjectId)
            throw new InvalidDataException("An object cannot be attached to itself.");
        if (!state.Objects.ContainsKey(parentObjectId) || !state.Objects.ContainsKey(childObjectId))
            throw new InvalidOperationException("Attachment parent and child must both exist in the current project.");
        if (string.IsNullOrWhiteSpace(socket))
            throw new ArgumentException("Attachment socket is required.", nameof(socket));
        localTransform.Validate();
    }
}
