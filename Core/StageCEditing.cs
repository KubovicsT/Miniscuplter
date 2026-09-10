namespace Miniscuplter.Core;

/// <summary>
/// Narrow Stage-C editing contract used while the legacy Godot tools are migrated onto
/// authoritative project state. It intentionally covers only committed transforms and one
/// committed mesh-edit path; broader sculpt/tool architecture remains outside v1.0.20 scope.
/// </summary>
public static class StageCEditing
{
    public const string TransactionPrefix = "Stage-C edit:";

    public static bool SetTransform(
        ProjectSession session,
        ObjectId objectId,
        TransformState transform,
        string operation = "transform")
    {
        ArgumentNullException.ThrowIfNull(session);
        transform.Validate();
        if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? obj))
            throw new InvalidOperationException($"Stage-C object {objectId} does not exist.");
        if (obj.Transform == transform) return false;

        session.Execute(
            $"{TransactionPrefix} {NormalizeOperation(operation)}",
            state =>
            {
                ProjectObject current = state.Objects[objectId];
                return state.WithObject(current with { Transform = transform });
            },
            objectId);
        return true;
    }

    public static void CommitMeshRevision(
        ProjectSession session,
        ObjectId objectId,
        RevisionId expectedInputRevisionId,
        MeshRevision outputRevision,
        string operation = "sculpt stroke")
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(outputRevision);
        if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? obj))
            throw new InvalidOperationException($"Stage-C object {objectId} does not exist.");
        if (obj.ActiveMeshRevisionId != expectedInputRevisionId)
            throw new InvalidOperationException(
                $"Stage-C edit is stale: object {objectId} advanced from {expectedInputRevisionId} to {obj.ActiveMeshRevisionId}.");
        if (outputRevision.ObjectId != objectId)
            throw new InvalidDataException("Stage-C edit output belongs to a different object.");
        if (outputRevision.ParentRevisionId != expectedInputRevisionId)
            throw new InvalidDataException("Stage-C edit output must directly descend from the exact edited revision.");
        if (session.Current.MeshRevisions.ContainsKey(outputRevision.Id))
            throw new InvalidDataException("Stage-C edit output revision already exists in project state.");

        session.Execute(
            $"{TransactionPrefix} {NormalizeOperation(operation)}",
            state =>
            {
                ProjectObject current = state.Objects[objectId];
                if (current.ActiveMeshRevisionId != expectedInputRevisionId)
                    throw new InvalidOperationException("Stage-C object advanced before the edit transaction could commit.");
                return state
                    .WithMeshRevision(outputRevision)
                    .WithObject(current with { ActiveMeshRevisionId = outputRevision.Id });
            },
            objectId);
    }

    public static bool IsEditingTransaction(ProjectTransaction transaction) =>
        transaction != null && transaction.Label.StartsWith(TransactionPrefix, StringComparison.Ordinal);

    static string NormalizeOperation(string operation) =>
        string.IsNullOrWhiteSpace(operation) ? "edit" : operation.Trim();
}
