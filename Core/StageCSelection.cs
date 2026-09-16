namespace Miniscuplter.Core;

/// <summary>
/// Stable whole-object selection identity for the bounded Stage-D viewport selection seam.
/// Godot may discover a hit, but durable project object/revision identity determines what is selected.
/// Whole-object selection can explicitly transfer to a newer active revision because it contains
/// no topology indices; component/region selections remain bound to their exact mesh revision.
/// </summary>
public readonly record struct ObjectSelectionRef(ObjectId ObjectId, RevisionId MeshRevisionId);

public static class StageCSelection
{
    public const string TransactionPrefix = "Stage-C selection:";

    public static ObjectSelectionRef BindObject(ProjectState state, ObjectId objectId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Objects.TryGetValue(objectId, out ProjectObject? obj))
            throw new InvalidOperationException($"Cannot select missing object {objectId}.");
        return new ObjectSelectionRef(objectId, obj.ActiveMeshRevisionId);
    }

    public static bool IsCurrent(ProjectState state, ObjectSelectionRef selection)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Objects.TryGetValue(selection.ObjectId, out ProjectObject? obj) &&
               obj.ActiveMeshRevisionId == selection.MeshRevisionId;
    }

    public static ObjectSelectionRef RebindWholeObject(ProjectState state, ObjectSelectionRef selection)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Objects.TryGetValue(selection.ObjectId, out ProjectObject? obj))
            throw new InvalidOperationException($"Selected object {selection.ObjectId} no longer exists.");
        return new ObjectSelectionRef(selection.ObjectId, obj.ActiveMeshRevisionId);
    }

    public static SelectionBinding BindRevisionSelection(
        ProjectSession session,
        SelectionId selectionId,
        ObjectId objectId,
        RevisionId meshRevisionId,
        string kind,
        string dataAssetPath)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (selectionId.Value == Guid.Empty)
            throw new ArgumentException("Selection ID cannot be empty.", nameof(selectionId));
        if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? obj))
            throw new InvalidOperationException($"Cannot bind selection to missing object {objectId}.");
        if (obj.ActiveMeshRevisionId != meshRevisionId)
            throw new InvalidOperationException("Revision-dependent selection is stale before it can be bound.");
        if (!session.Current.MeshRevisions.TryGetValue(meshRevisionId, out MeshRevision? revision) || revision.ObjectId != objectId)
            throw new InvalidDataException("Revision-dependent selection does not target a valid mesh revision of its object.");
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("Selection kind is required.", nameof(kind));
        if (string.IsNullOrWhiteSpace(dataAssetPath))
            throw new ArgumentException("Selection data asset path is required.", nameof(dataAssetPath));

        var binding = new SelectionBinding(
            selectionId,
            objectId,
            meshRevisionId,
            kind.Trim(),
            dataAssetPath.Replace('\\', '/'),
            DateTimeOffset.UtcNow);
        session.Execute(
            $"{TransactionPrefix} bind {binding.Kind}",
            state =>
            {
                if (!state.Objects.TryGetValue(objectId, out ProjectObject? current) || current.ActiveMeshRevisionId != meshRevisionId)
                    throw new InvalidOperationException("Object revision advanced before selection binding could commit.");
                return state.WithSelection(binding);
            },
            objectId);
        return binding;
    }

    public static int RemoveRevisionSelections(ProjectSession session, ObjectId objectId, string kind)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (objectId.Value == Guid.Empty)
            throw new ArgumentException("Object ID cannot be empty.", nameof(objectId));
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("Selection kind is required.", nameof(kind));

        string normalizedKind = kind.Trim();
        SelectionId[] ids = session.Current.Selections.Values
            .Where(selection => selection.ObjectId == objectId &&
                                string.Equals(selection.Kind, normalizedKind, StringComparison.Ordinal))
            .Select(selection => selection.Id)
            .ToArray();
        if (ids.Length == 0)
            return 0;

        var remove = ids.ToHashSet();
        session.Execute(
            $"{TransactionPrefix} clear {normalizedKind}",
            state => WithoutSelections(state, remove),
            objectId);
        return ids.Length;
    }

    public static bool RemoveRevisionSelection(ProjectSession session, SelectionBinding expected)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(expected);
        if (!session.Current.Selections.TryGetValue(expected.Id, out SelectionBinding? current) ||
            current != expected)
            return false;

        session.Execute(
            $"{TransactionPrefix} clear {expected.Kind}",
            state =>
            {
                if (!state.Selections.TryGetValue(expected.Id, out SelectionBinding? persisted) ||
                    persisted != expected)
                    throw new InvalidOperationException("Revision selection changed before clear could commit.");
                return WithoutSelections(state, new HashSet<SelectionId> { expected.Id });
            },
            expected.ObjectId);
        return true;
    }

    static ProjectState WithoutSelections(ProjectState state, IReadOnlySet<SelectionId> selectionIds) =>
        new(
            state.ProjectId,
            state.DisplayName,
            state.RevisionNumber,
            state.Objects.Values,
            state.MeshRevisions.Values,
            state.ImageRevisions.Values,
            state.Selections.Values.Where(selection => !selectionIds.Contains(selection.Id)),
            state.Rigs.Values,
            state.Attachments.Values,
            state.Candidates.Values,
            state.Metadata);

    public static bool IsSelectionTransaction(ProjectTransaction transaction) =>
        transaction != null && transaction.Label.StartsWith(TransactionPrefix, StringComparison.Ordinal);

    public static bool IsCurrent(ProjectState state, SelectionBinding selection)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(selection);
        return state.Selections.TryGetValue(selection.Id, out SelectionBinding? persisted) &&
               persisted == selection &&
               state.Objects.TryGetValue(selection.ObjectId, out ProjectObject? obj) &&
               obj.ActiveMeshRevisionId == selection.MeshRevisionId;
    }
}
