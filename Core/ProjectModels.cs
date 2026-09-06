using System.Collections.ObjectModel;

namespace Miniscuplter.Core;

public enum CandidateStatus
{
    Ready,
    Applied,
    Conflict,
    Discarded,
    Failed
}

public sealed record MeshRevision(
    RevisionId Id,
    ObjectId ObjectId,
    RevisionId? ParentRevisionId,
    string AssetPath,
    string Sha256,
    int VertexCount,
    int TriangleCount,
    string Provenance,
    DateTimeOffset CreatedUtc,
    string? TopologySignature = null);

public sealed record ProjectObject(
    ObjectId Id,
    string DisplayName,
    RevisionId ActiveMeshRevisionId,
    TransformState Transform,
    string Role = "mesh",
    bool Visible = true);

public sealed record ImageRevision(
    RevisionId Id,
    RevisionId? ParentRevisionId,
    string AssetPath,
    string Sha256,
    string Purpose,
    string Provenance,
    DateTimeOffset CreatedUtc);

public sealed record SelectionBinding(
    SelectionId Id,
    ObjectId ObjectId,
    RevisionId MeshRevisionId,
    string Kind,
    string DataAssetPath,
    DateTimeOffset CreatedUtc);

public sealed record RigRecord(
    RigId Id,
    ObjectId ObjectId,
    RevisionId RestMeshRevisionId,
    string DataAssetPath,
    DateTimeOffset CreatedUtc);

public sealed record AttachmentRecord(
    AttachmentId Id,
    ObjectId ParentObjectId,
    ObjectId ChildObjectId,
    string Socket,
    TransformState LocalTransform,
    DateTimeOffset CreatedUtc);

public sealed record CandidateRecord(
    CandidateId Id,
    ObjectId ObjectId,
    RevisionId InputRevisionId,
    RevisionId OutputRevisionId,
    string Kind,
    CandidateStatus Status,
    string Provenance,
    DateTimeOffset CreatedUtc,
    string? ConflictReason = null);

public sealed class ProjectState
{
    public const int CurrentSchemaVersion = 7;

    readonly Dictionary<ObjectId, ProjectObject> _objects;
    readonly Dictionary<RevisionId, MeshRevision> _meshRevisions;
    readonly Dictionary<RevisionId, ImageRevision> _imageRevisions;
    readonly Dictionary<SelectionId, SelectionBinding> _selections;
    readonly Dictionary<RigId, RigRecord> _rigs;
    readonly Dictionary<AttachmentId, AttachmentRecord> _attachments;
    readonly Dictionary<CandidateId, CandidateRecord> _candidates;
    readonly Dictionary<string, string> _metadata;

    public ProjectId ProjectId { get; }
    public string DisplayName { get; }
    public long RevisionNumber { get; }
    public IReadOnlyDictionary<ObjectId, ProjectObject> Objects { get; }
    public IReadOnlyDictionary<RevisionId, MeshRevision> MeshRevisions { get; }
    public IReadOnlyDictionary<RevisionId, ImageRevision> ImageRevisions { get; }
    public IReadOnlyDictionary<SelectionId, SelectionBinding> Selections { get; }
    public IReadOnlyDictionary<RigId, RigRecord> Rigs { get; }
    public IReadOnlyDictionary<AttachmentId, AttachmentRecord> Attachments { get; }
    public IReadOnlyDictionary<CandidateId, CandidateRecord> Candidates { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public ProjectState(
        ProjectId projectId,
        string displayName,
        long revisionNumber = 0,
        IEnumerable<ProjectObject>? objects = null,
        IEnumerable<MeshRevision>? meshRevisions = null,
        IEnumerable<ImageRevision>? imageRevisions = null,
        IEnumerable<SelectionBinding>? selections = null,
        IEnumerable<RigRecord>? rigs = null,
        IEnumerable<AttachmentRecord>? attachments = null,
        IEnumerable<CandidateRecord>? candidates = null,
        IEnumerable<KeyValuePair<string, string>>? metadata = null)
    {
        if (projectId.Value == Guid.Empty) throw new ArgumentException("Project ID cannot be empty.", nameof(projectId));
        ProjectId = projectId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Untitled" : displayName.Trim();
        RevisionNumber = Math.Max(0, revisionNumber);
        _objects = (objects ?? []).ToDictionary(x => x.Id);
        _meshRevisions = (meshRevisions ?? []).ToDictionary(x => x.Id);
        _imageRevisions = (imageRevisions ?? []).ToDictionary(x => x.Id);
        _selections = (selections ?? []).ToDictionary(x => x.Id);
        _rigs = (rigs ?? []).ToDictionary(x => x.Id);
        _attachments = (attachments ?? []).ToDictionary(x => x.Id);
        _candidates = (candidates ?? []).ToDictionary(x => x.Id);
        _metadata = new Dictionary<string, string>(metadata ?? [], StringComparer.OrdinalIgnoreCase);
        Objects = new ReadOnlyDictionary<ObjectId, ProjectObject>(_objects);
        MeshRevisions = new ReadOnlyDictionary<RevisionId, MeshRevision>(_meshRevisions);
        ImageRevisions = new ReadOnlyDictionary<RevisionId, ImageRevision>(_imageRevisions);
        Selections = new ReadOnlyDictionary<SelectionId, SelectionBinding>(_selections);
        Rigs = new ReadOnlyDictionary<RigId, RigRecord>(_rigs);
        Attachments = new ReadOnlyDictionary<AttachmentId, AttachmentRecord>(_attachments);
        Candidates = new ReadOnlyDictionary<CandidateId, CandidateRecord>(_candidates);
        Metadata = new ReadOnlyDictionary<string, string>(_metadata);
        Validate();
    }

    public static ProjectState Create(string displayName) => new(ProjectId.New(), displayName);

    public ProjectState WithRevisionNumber(long value) => Copy(revisionNumber: value);
    public ProjectState WithDisplayName(string value) => Copy(displayName: value);

    public ProjectState WithObject(ProjectObject value)
    {
        var next = new Dictionary<ObjectId, ProjectObject>(_objects) { [value.Id] = value };
        return Copy(objects: next.Values);
    }

    public ProjectState WithoutObject(ObjectId id)
    {
        var next = new Dictionary<ObjectId, ProjectObject>(_objects);
        next.Remove(id);
        var selections = _selections.Values.Where(x => x.ObjectId != id);
        var rigs = _rigs.Values.Where(x => x.ObjectId != id);
        var attachments = _attachments.Values.Where(x => x.ParentObjectId != id && x.ChildObjectId != id);
        var candidates = _candidates.Values.Where(x => x.ObjectId != id);
        return Copy(objects: next.Values, selections: selections, rigs: rigs, attachments: attachments, candidates: candidates);
    }

    public ProjectState WithMeshRevision(MeshRevision value)
    {
        var next = new Dictionary<RevisionId, MeshRevision>(_meshRevisions) { [value.Id] = value };
        return Copy(meshRevisions: next.Values);
    }

    public ProjectState WithImageRevision(ImageRevision value)
    {
        var next = new Dictionary<RevisionId, ImageRevision>(_imageRevisions) { [value.Id] = value };
        return Copy(imageRevisions: next.Values);
    }

    public ProjectState WithSelection(SelectionBinding value)
    {
        var next = new Dictionary<SelectionId, SelectionBinding>(_selections) { [value.Id] = value };
        return Copy(selections: next.Values);
    }

    public ProjectState WithRig(RigRecord value)
    {
        var next = new Dictionary<RigId, RigRecord>(_rigs) { [value.Id] = value };
        return Copy(rigs: next.Values);
    }

    public ProjectState WithAttachment(AttachmentRecord value)
    {
        var next = new Dictionary<AttachmentId, AttachmentRecord>(_attachments) { [value.Id] = value };
        return Copy(attachments: next.Values);
    }

    public ProjectState WithCandidate(CandidateRecord value)
    {
        var next = new Dictionary<CandidateId, CandidateRecord>(_candidates) { [value.Id] = value };
        return Copy(candidates: next.Values);
    }

    public ProjectState WithMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Metadata key is required.", nameof(key));
        var next = new Dictionary<string, string>(_metadata, StringComparer.OrdinalIgnoreCase) { [key.Trim()] = value ?? string.Empty };
        return Copy(metadata: next);
    }

    ProjectState Copy(
        string? displayName = null,
        long? revisionNumber = null,
        IEnumerable<ProjectObject>? objects = null,
        IEnumerable<MeshRevision>? meshRevisions = null,
        IEnumerable<ImageRevision>? imageRevisions = null,
        IEnumerable<SelectionBinding>? selections = null,
        IEnumerable<RigRecord>? rigs = null,
        IEnumerable<AttachmentRecord>? attachments = null,
        IEnumerable<CandidateRecord>? candidates = null,
        IEnumerable<KeyValuePair<string, string>>? metadata = null) =>
        new(ProjectId, displayName ?? DisplayName, revisionNumber ?? RevisionNumber,
            objects ?? _objects.Values,
            meshRevisions ?? _meshRevisions.Values,
            imageRevisions ?? _imageRevisions.Values,
            selections ?? _selections.Values,
            rigs ?? _rigs.Values,
            attachments ?? _attachments.Values,
            candidates ?? _candidates.Values,
            metadata ?? _metadata);

    public void Validate()
    {
        foreach (var revision in _meshRevisions.Values)
        {
            if (revision.Id.Value == Guid.Empty || revision.ObjectId.Value == Guid.Empty)
                throw new InvalidDataException("Mesh revision identity cannot be empty.");
            if (revision.VertexCount <= 0 || revision.TriangleCount <= 0)
                throw new InvalidDataException($"Mesh revision {revision.Id} has invalid counts.");
            if (string.IsNullOrWhiteSpace(revision.AssetPath) || string.IsNullOrWhiteSpace(revision.Sha256))
                throw new InvalidDataException($"Mesh revision {revision.Id} has no durable asset reference.");
        }

        foreach (var obj in _objects.Values)
        {
            obj.Transform.Validate();
            if (!_meshRevisions.TryGetValue(obj.ActiveMeshRevisionId, out var revision))
                throw new InvalidDataException($"Object {obj.Id} references missing mesh revision {obj.ActiveMeshRevisionId}.");
            if (revision.ObjectId != obj.Id)
                throw new InvalidDataException($"Object {obj.Id} references a mesh revision owned by another object.");
        }

        foreach (var selection in _selections.Values)
        {
            if (!_objects.ContainsKey(selection.ObjectId)) throw new InvalidDataException($"Selection {selection.Id} references a missing object.");
            if (!_meshRevisions.TryGetValue(selection.MeshRevisionId, out var revision) || revision.ObjectId != selection.ObjectId)
                throw new InvalidDataException($"Selection {selection.Id} is not bound to a valid revision of its object.");
        }

        foreach (var rig in _rigs.Values)
        {
            if (!_objects.ContainsKey(rig.ObjectId)) throw new InvalidDataException($"Rig {rig.Id} references a missing object.");
            if (!_meshRevisions.TryGetValue(rig.RestMeshRevisionId, out var revision) || revision.ObjectId != rig.ObjectId)
                throw new InvalidDataException($"Rig {rig.Id} rest mesh is not a revision of its object.");
        }

        foreach (var attachment in _attachments.Values)
        {
            attachment.LocalTransform.Validate();
            if (!_objects.ContainsKey(attachment.ParentObjectId) || !_objects.ContainsKey(attachment.ChildObjectId))
                throw new InvalidDataException($"Attachment {attachment.Id} references a missing object.");
        }

        foreach (var candidate in _candidates.Values)
        {
            if (!_objects.ContainsKey(candidate.ObjectId)) throw new InvalidDataException($"Candidate {candidate.Id} references a missing object.");
            if (!_meshRevisions.ContainsKey(candidate.InputRevisionId) || !_meshRevisions.ContainsKey(candidate.OutputRevisionId))
                throw new InvalidDataException($"Candidate {candidate.Id} references missing input/output revisions.");
        }
    }
}
