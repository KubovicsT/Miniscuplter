namespace Miniscuplter.Core;

/// <summary>
/// UI-neutral durable attachment transaction seam. Godot may choose a parent/child pair and
/// local placement, but Core owns stable attachment identity, revision binding and committed history.
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

        var parentRevisionId = session.Current.Objects[parentObjectId].ActiveMeshRevisionId;
        var childRevisionId = session.Current.Objects[childObjectId].ActiveMeshRevisionId;
        var attachment = new AttachmentRecord(
            attachmentId,
            parentObjectId,
            childObjectId,
            socket.Trim(),
            localTransform,
            DateTimeOffset.UtcNow,
            parentRevisionId,
            childRevisionId,
            AttachmentBindingStatus.Current);
        session.Execute(
            $"{TransactionPrefix} create",
            state =>
            {
                ValidateIdentity(state, attachmentId, parentObjectId, childObjectId, socket, localTransform);
                if (state.Attachments.ContainsKey(attachmentId))
                    throw new InvalidOperationException($"Attachment {attachmentId} appeared before commit.");
                if (state.Attachments.Values.Any(x => x.ChildObjectId == childObjectId))
                    throw new InvalidOperationException($"Object {childObjectId} became attached before commit.");
                if (state.Objects[parentObjectId].ActiveMeshRevisionId != parentRevisionId ||
                    state.Objects[childObjectId].ActiveMeshRevisionId != childRevisionId)
                    throw new InvalidOperationException("Attachment object revisions changed before creation could commit.");
                return state.WithAttachment(attachment);
            },
            parentObjectId, childObjectId);
        return attachment;
    }

    public static AttachmentBindingStatus ResolveBindingStatus(ProjectState state, AttachmentRecord attachment)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(attachment);
        if (attachment.ParentMeshRevisionId is not { } parentRevisionId ||
            attachment.ChildMeshRevisionId is not { } childRevisionId ||
            attachment.BindingStatus == AttachmentBindingStatus.Stale)
            return AttachmentBindingStatus.Stale;
        if (!state.Objects.TryGetValue(attachment.ParentObjectId, out var parent) ||
            !state.Objects.TryGetValue(attachment.ChildObjectId, out var child))
            return AttachmentBindingStatus.Stale;
        if (parent.ActiveMeshRevisionId != parentRevisionId || child.ActiveMeshRevisionId != childRevisionId)
            return AttachmentBindingStatus.Stale;
        return attachment.BindingStatus;
    }

    public static bool IsAuthoritative(ProjectState state, AttachmentRecord attachment) =>
        ResolveBindingStatus(state, attachment) is AttachmentBindingStatus.Current or AttachmentBindingStatus.Rebound;

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
        EnsureAuthoritative(session.Current, expected);

        var updated = expected with { Socket = socket.Trim(), LocalTransform = localTransform };
        if (updated == expected) return expected;
        session.Execute(
            $"{TransactionPrefix} update",
            state =>
            {
                if (!state.Attachments.TryGetValue(expected.Id, out AttachmentRecord? persisted) || persisted != expected)
                    throw new InvalidOperationException("Attachment changed before the update transaction could commit.");
                ValidateIdentity(state, expected.Id, expected.ParentObjectId, expected.ChildObjectId, socket, localTransform);
                EnsureAuthoritative(state, expected);
                return state.WithAttachment(updated);
            },
            expected.ParentObjectId, expected.ChildObjectId);
        return updated;
    }

    public static AttachmentRecord RebindToCurrent(ProjectSession session, AttachmentRecord expected)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(expected);
        if (!session.Current.Attachments.TryGetValue(expected.Id, out AttachmentRecord? current) || current != expected)
            throw new InvalidOperationException("Attachment changed before the rebind could commit.");
        ValidateIdentity(session.Current, expected.Id, expected.ParentObjectId, expected.ChildObjectId, expected.Socket, expected.LocalTransform);
        var parentRevisionId = session.Current.Objects[expected.ParentObjectId].ActiveMeshRevisionId;
        var childRevisionId = session.Current.Objects[expected.ChildObjectId].ActiveMeshRevisionId;
        var rebound = expected with
        {
            ParentMeshRevisionId = parentRevisionId,
            ChildMeshRevisionId = childRevisionId,
            BindingStatus = AttachmentBindingStatus.Rebound
        };
        if (rebound == expected && IsAuthoritative(session.Current, expected)) return expected;

        session.Execute(
            $"{TransactionPrefix} rebind",
            state =>
            {
                if (!state.Attachments.TryGetValue(expected.Id, out AttachmentRecord? persisted) || persisted != expected)
                    throw new InvalidOperationException("Attachment changed before the rebind transaction could commit.");
                ValidateIdentity(state, expected.Id, expected.ParentObjectId, expected.ChildObjectId, expected.Socket, expected.LocalTransform);
                if (state.Objects[expected.ParentObjectId].ActiveMeshRevisionId != parentRevisionId ||
                    state.Objects[expected.ChildObjectId].ActiveMeshRevisionId != childRevisionId)
                    throw new InvalidOperationException("Attachment object revisions changed before rebind could commit.");
                return state.WithAttachment(rebound);
            },
            expected.ParentObjectId, expected.ChildObjectId);
        return rebound;
    }

    public static void RemoveIfCurrent(ProjectSession session, AttachmentRecord expected)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(expected);
        if (!session.Current.Attachments.TryGetValue(expected.Id, out AttachmentRecord? current) || current != expected)
            throw new InvalidOperationException("Attachment changed before the removal could commit.");

        session.Execute(
            $"{TransactionPrefix} remove",
            state =>
            {
                if (!state.Attachments.TryGetValue(expected.Id, out AttachmentRecord? persisted) || persisted != expected)
                    throw new InvalidOperationException("Attachment changed before the removal transaction could commit.");
                return state.WithoutAttachment(expected.Id);
            },
            expected.ParentObjectId, expected.ChildObjectId);
    }

    public static bool IsAttachmentTransaction(ProjectTransaction transaction) =>
        transaction != null && transaction.Label.StartsWith(TransactionPrefix, StringComparison.Ordinal);

    static void EnsureAuthoritative(ProjectState state, AttachmentRecord attachment)
    {
        if (!IsAuthoritative(state, attachment))
            throw new InvalidOperationException("Attachment is stale for the current parent/child mesh revisions and must be explicitly rebound or removed.");
    }

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
