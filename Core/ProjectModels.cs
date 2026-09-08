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
        ValidateCollectionSizes();

        foreach (var pair in _objects)
        {
            RequireId(pair.Key.Value, "project object");
            if (pair.Key != pair.Value.Id)
                throw new InvalidDataException("Project object dictionary key does not match the embedded object identity.");
            if (string.IsNullOrWhiteSpace(pair.Value.DisplayName))
                throw new InvalidDataException($"Project object {pair.Key} has no display name.");
            pair.Value.Transform.Validate();
            if (!_meshRevisions.TryGetValue(pair.Value.ActiveMeshRevisionId, out var activeRevision))
                throw new InvalidDataException($"Object {pair.Key} references missing mesh revision {pair.Value.ActiveMeshRevisionId}.");
            if (activeRevision.ObjectId != pair.Key)
                throw new InvalidDataException($"Object {pair.Key} references a mesh revision owned by another object.");
        }

        foreach (var pair in _meshRevisions)
        {
            RequireId(pair.Key.Value, "mesh revision");
            var revision = pair.Value;
            if (pair.Key != revision.Id || revision.ObjectId.Value == Guid.Empty)
                throw new InvalidDataException("Mesh revision dictionary identity is invalid.");
            if (revision.VertexCount <= 0 || revision.TriangleCount <= 0 ||
                revision.VertexCount > 100_000_000 || revision.TriangleCount > 100_000_000)
                throw new InvalidDataException($"Mesh revision {revision.Id} has invalid counts.");
            ValidateAssetReference(revision.AssetPath, $"mesh revision {revision.Id}");
            ValidateSha256(revision.Sha256, $"mesh revision {revision.Id}");
            if (revision.ParentRevisionId is { } parent)
            {
                if (!_meshRevisions.TryGetValue(parent, out var parentRevision))
                    throw new InvalidDataException($"Mesh revision {revision.Id} references missing parent revision {parent}.");
                if (parentRevision.ObjectId != revision.ObjectId)
                    throw new InvalidDataException($"Mesh revision {revision.Id} references a parent revision owned by another object.");
            }
        }

        foreach (var revision in _meshRevisions.Values)
        {
            var visited = new HashSet<RevisionId>();
            var cursor = revision;
            while (cursor.ParentRevisionId is { } parent)
            {
                if (!visited.Add(cursor.Id))
                    throw new InvalidDataException($"Mesh revision lineage contains a cycle at {cursor.Id}.");
                cursor = _meshRevisions[parent];
            }
        }

        foreach (var pair in _imageRevisions)
        {
            RequireId(pair.Key.Value, "image revision");
            var revision = pair.Value;
            if (pair.Key != revision.Id)
                throw new InvalidDataException("Image revision dictionary identity is invalid.");
            ValidateAssetReference(revision.AssetPath, $"image revision {revision.Id}");
            ValidateSha256(revision.Sha256, $"image revision {revision.Id}");
            if (revision.ParentRevisionId is { } parent && !_imageRevisions.ContainsKey(parent))
                throw new InvalidDataException($"Image revision {revision.Id} references missing parent image revision {parent}.");
        }

        foreach (var pair in _selections)
        {
            RequireId(pair.Key.Value, "selection");
            var selection = pair.Value;
            if (pair.Key != selection.Id || selection.ObjectId.Value == Guid.Empty)
                throw new InvalidDataException("Selection dictionary identity is invalid.");
            if (!_objects.ContainsKey(selection.ObjectId))
                throw new InvalidDataException($"Selection {selection.Id} references a missing object.");
            if (!_meshRevisions.TryGetValue(selection.MeshRevisionId, out var selectionRevision) ||
                selectionRevision.ObjectId != selection.ObjectId)
                throw new InvalidDataException($"Selection {selection.Id} is not bound to a valid revision of its object.");
            if (string.IsNullOrWhiteSpace(selection.Kind))
                throw new InvalidDataException($"Selection {selection.Id} has no kind.");
            ValidateAssetReference(selection.DataAssetPath, $"selection {selection.Id}");
        }

        foreach (var pair in _rigs)
        {
            RequireId(pair.Key.Value, "rig");
            var rig = pair.Value;
            if (pair.Key != rig.Id || rig.ObjectId.Value == Guid.Empty)
                throw new InvalidDataException("Rig dictionary identity is invalid.");
            if (!_objects.ContainsKey(rig.ObjectId))
                throw new InvalidDataException($"Rig {rig.Id} references a missing object.");
            if (!_meshRevisions.TryGetValue(rig.RestMeshRevisionId, out var restRevision) ||
                restRevision.ObjectId != rig.ObjectId)
                throw new InvalidDataException($"Rig {rig.Id} rest mesh is not a revision of its object.");
            ValidateAssetReference(rig.DataAssetPath, $"rig {rig.Id}");
        }

        foreach (var pair in _attachments)
        {
            RequireId(pair.Key.Value, "attachment");
            var attachment = pair.Value;
            if (pair.Key != attachment.Id || attachment.ParentObjectId.Value == Guid.Empty || attachment.ChildObjectId.Value == Guid.Empty)
                throw new InvalidDataException("Attachment dictionary identity is invalid.");
            attachment.LocalTransform.Validate();
            if (attachment.ParentObjectId == attachment.ChildObjectId)
                throw new InvalidDataException($"Attachment {attachment.Id} cannot attach an object to itself.");
            if (!_objects.ContainsKey(attachment.ParentObjectId) || !_objects.ContainsKey(attachment.ChildObjectId))
                throw new InvalidDataException($"Attachment {attachment.Id} references a missing object.");
            if (string.IsNullOrWhiteSpace(attachment.Socket))
                throw new InvalidDataException($"Attachment {attachment.Id} has no socket name.");
        }

        foreach (var pair in _candidates)
        {
            RequireId(pair.Key.Value, "candidate");
            var candidate = pair.Value;
            if (pair.Key != candidate.Id || candidate.ObjectId.Value == Guid.Empty)
                throw new InvalidDataException("Candidate dictionary identity is invalid.");
            if (!_objects.ContainsKey(candidate.ObjectId))
                throw new InvalidDataException($"Candidate {candidate.Id} references a missing object.");
            if (!_meshRevisions.TryGetValue(candidate.InputRevisionId, out var input) ||
                !_meshRevisions.TryGetValue(candidate.OutputRevisionId, out var output))
                throw new InvalidDataException($"Candidate {candidate.Id} references missing input/output revisions.");
            if (input.ObjectId != candidate.ObjectId || output.ObjectId != candidate.ObjectId)
                throw new InvalidDataException($"Candidate {candidate.Id} references revisions from another object.");
            if (candidate.InputRevisionId == candidate.OutputRevisionId)
                throw new InvalidDataException($"Candidate {candidate.Id} input and output revisions must differ.");
            if (string.IsNullOrWhiteSpace(candidate.Kind) || string.IsNullOrWhiteSpace(candidate.Provenance))
                throw new InvalidDataException($"Candidate {candidate.Id} is missing kind or provenance.");
        }

        foreach (var key in _metadata.Keys)
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidDataException("Project metadata contains an empty key.");
    }

    void ValidateCollectionSizes()
    {
        if (_objects.Count > 100_000 || _meshRevisions.Count > 500_000 || _imageRevisions.Count > 500_000 ||
            _selections.Count > 500_000 || _rigs.Count > 100_000 || _attachments.Count > 500_000 ||
            _candidates.Count > 500_000)
            throw new InvalidDataException("Project graph exceeds the safe in-memory collection limits.");
    }

    static void RequireId(Guid value, string kind)
    {
        if (value == Guid.Empty) throw new InvalidDataException($"{kind} identity cannot be empty.");
    }

    static void ValidateSha256(string value, string kind)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 ||
            value.Any(c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))))
            throw new InvalidDataException($"{kind} does not contain a valid SHA-256 digest.");
    }

    static void ValidateAssetReference(string value, string kind)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"{kind} has no durable asset reference.");
        string normalized = value.Replace('\\', '/').Trim();
        if (Path.IsPathRooted(normalized) || normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.Equals("..", StringComparison.Ordinal) || normalized.StartsWith("../", StringComparison.Ordinal) ||
            normalized.Contains("/../", StringComparison.Ordinal) || normalized.EndsWith("/..", StringComparison.Ordinal) ||
            (normalized.Length >= 3 && char.IsLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/'))
            throw new InvalidDataException($"{kind} asset reference must stay relative to the project asset directory.");
    }
}
