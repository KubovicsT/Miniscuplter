namespace Miniscuplter.Core;

public sealed record StageCCleanupBinding(
    ProjectId ProjectId,
    long InputProjectRevisionNumber,
    ObjectId ObjectId,
    RevisionId InputMeshRevisionId);

/// <summary>
/// Stable Core contract for advancing an already-applied Stage-C object through cleanup.
/// Geometry backends may materialize temporary STL inputs/outputs, but project authority remains
/// the exact ObjectId + immutable MeshRevision lineage represented here.
/// </summary>
public static class StageCCleanup
{
    public static StageCCleanupBinding Begin(ProjectState state, ObjectId objectId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Objects.TryGetValue(objectId, out var obj))
            throw new InvalidOperationException($"Project object {objectId} does not exist.");
        if (!state.MeshRevisions.TryGetValue(obj.ActiveMeshRevisionId, out var revision) || revision.ObjectId != objectId)
            throw new InvalidDataException($"Project object {objectId} has an invalid active mesh revision.");
        return new StageCCleanupBinding(state.ProjectId, state.RevisionNumber, objectId, revision.Id);
    }

    public static ProjectTransaction ApplyResult(
        ProjectSession session,
        StageCCleanupBinding binding,
        MeshRevision outputRevision)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(outputRevision);

        ProjectState state = session.Current;
        if (binding.ProjectId != state.ProjectId)
            throw new InvalidOperationException("Cleanup result belongs to a different project.");
        if (!state.Objects.TryGetValue(binding.ObjectId, out var obj))
            throw new InvalidOperationException("Cleanup target object no longer exists.");
        if (obj.ActiveMeshRevisionId != binding.InputMeshRevisionId)
            throw new InvalidOperationException(
                $"Cleanup result is stale. Object {binding.ObjectId} advanced from revision {binding.InputMeshRevisionId} to {obj.ActiveMeshRevisionId} while cleanup was running.");
        if (!state.MeshRevisions.TryGetValue(binding.InputMeshRevisionId, out var input) || input.ObjectId != binding.ObjectId)
            throw new InvalidOperationException("Cleanup input revision is missing or belongs to another object.");
        if (state.MeshRevisions.ContainsKey(outputRevision.Id))
            throw new InvalidOperationException("Cleanup output revision is already registered in this project.");
        if (outputRevision.ObjectId != binding.ObjectId)
            throw new InvalidOperationException("Cleanup output revision belongs to another object.");
        if (outputRevision.ParentRevisionId != binding.InputMeshRevisionId)
            throw new InvalidOperationException("Cleanup output revision must directly descend from the exact cleaned input revision.");

        return session.Execute(
            "Apply cleaned mesh revision",
            current => current
                .WithMeshRevision(outputRevision)
                .WithObject(obj with { ActiveMeshRevisionId = outputRevision.Id }),
            binding.ObjectId);
    }

    public static MeshRevision ResolveExportRevision(ProjectState state, ObjectId objectId, RevisionId revisionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Objects.TryGetValue(objectId, out var obj))
            throw new InvalidOperationException($"Project object {objectId} does not exist.");
        if (obj.ActiveMeshRevisionId != revisionId)
            throw new InvalidOperationException(
                $"Export scope is stale. Object {objectId} currently points to revision {obj.ActiveMeshRevisionId}, not requested revision {revisionId}.");
        if (!state.MeshRevisions.TryGetValue(revisionId, out var revision) || revision.ObjectId != objectId)
            throw new InvalidOperationException("Export revision is missing or belongs to another object.");
        return revision;
    }
}
