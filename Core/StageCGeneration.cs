using System.Globalization;
using System.Text.Json;

namespace Miniscuplter.Core;

/// <summary>
/// Stable identity for a Stage-C image-to-mesh generation request. This is intentionally
/// independent from the transport/backend job ID so the project can preserve provenance even
/// if the local worker is restarted while the broader Job Broker is still being migrated.
/// </summary>
public readonly record struct GenerationJobId(Guid Value) : IStrongId
{
    public static GenerationJobId New() => new(Guid.NewGuid());
    public static GenerationJobId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}

public sealed record GenerationJobBinding(
    GenerationJobId JobId,
    ProjectId ProjectId,
    long InputProjectRevisionNumber,
    RevisionId InputImageRevisionId,
    ObjectId OutputObjectId,
    DateTimeOffset CreatedUtc);

public sealed record ImageToMeshCandidateState(
    CandidateId Id,
    GenerationJobId JobId,
    RevisionId InputImageRevisionId,
    ObjectId OutputObjectId,
    RevisionId OutputMeshRevisionId,
    CandidateStatus Status,
    string Provider,
    string Provenance,
    DateTimeOffset CreatedUtc,
    string? ConflictReason = null);

/// <summary>
/// Stage-C bridge that moves the first reliable 2D -> 3D slice onto ProjectState/ProjectSession.
///
/// The current schema-7 manifest already persists arbitrary project metadata. To keep v1.0.19
/// project files readable while this vertical slice is proven, baseline/candidate descriptors are
/// stored behind this strongly-typed Core API instead of introducing a breaking manifest schema
/// bump mid-migration. Mesh/image payloads remain normal durable Core revisions. Once the Job
/// Broker/project migration is authoritative, these descriptors can become first-class manifest
/// collections without changing callers of this API.
/// </summary>
public static class StageCGeneration
{
    const string BaselineKey = "stagec.acceptedBaselineImageRevisionId";
    const string CandidatesKey = "stagec.imageToMeshCandidates.v1";
    const int MaxCandidates = 2048;
    static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    sealed class CandidateDto
    {
        public string Id { get; set; } = "";
        public string JobId { get; set; } = "";
        public string InputImageRevisionId { get; set; } = "";
        public string OutputObjectId { get; set; } = "";
        public string OutputMeshRevisionId { get; set; } = "";
        public string Status { get; set; } = CandidateStatus.Ready.ToString();
        public string Provider { get; set; } = "";
        public string Provenance { get; set; } = "";
        public DateTimeOffset CreatedUtc { get; set; }
        public string? ConflictReason { get; set; }
    }

    public static RevisionId? AcceptedBaseline(ProjectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Metadata.TryGetValue(BaselineKey, out string? raw) || string.IsNullOrWhiteSpace(raw))
            return null;
        RevisionId id;
        try { id = RevisionId.Parse(raw); }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            throw new InvalidDataException("Accepted 2D baseline metadata contains an invalid revision identity.", ex);
        }
        if (!state.ImageRevisions.ContainsKey(id))
            throw new InvalidDataException($"Accepted 2D baseline references missing image revision {id}.");
        return id;
    }

    public static ProjectTransaction AcceptBaseline(ProjectSession session, RevisionId imageRevisionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.Current.ImageRevisions.ContainsKey(imageRevisionId))
            throw new InvalidOperationException($"Image revision {imageRevisionId} does not exist in this project.");
        return session.Execute(
            "Accept 2D baseline",
            state => state.WithMetadata(BaselineKey, imageRevisionId.ToString()));
    }

    public static GenerationJobBinding BeginImageToMesh(ProjectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        RevisionId baseline = AcceptedBaseline(state)
            ?? throw new InvalidOperationException("Accept a 2D baseline before starting 3D generation.");
        return new GenerationJobBinding(
            GenerationJobId.New(),
            state.ProjectId,
            state.RevisionNumber,
            baseline,
            ObjectId.New(),
            DateTimeOffset.UtcNow);
    }

    public static ImageToMeshCandidateState RegisterResult(
        ProjectSession session,
        GenerationJobBinding binding,
        MeshRevision outputRevision,
        string provider,
        string provenance)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (binding.JobId.Value == Guid.Empty || binding.ProjectId.Value == Guid.Empty || binding.OutputObjectId.Value == Guid.Empty)
            throw new ArgumentException("Generation binding contains an empty identity.", nameof(binding));
        if (session.Current.ProjectId != binding.ProjectId)
            throw new InvalidOperationException("Generation result belongs to a different project.");
        if (!session.Current.ImageRevisions.ContainsKey(binding.InputImageRevisionId))
            throw new InvalidOperationException("Generation input image revision no longer exists in this project.");
        if (outputRevision.ObjectId != binding.OutputObjectId)
            throw new InvalidOperationException("Generated mesh revision is owned by a different object identity than the job binding.");
        if (session.Current.MeshRevisions.ContainsKey(outputRevision.Id))
            throw new InvalidOperationException("Generated mesh revision is already registered in this project.");
        if (session.Current.Objects.ContainsKey(binding.OutputObjectId))
            throw new InvalidOperationException("Generation output object identity is already in use.");

        string providerName = string.IsNullOrWhiteSpace(provider) ? "unknown" : provider.Trim();
        string provenanceValue = string.IsNullOrWhiteSpace(provenance) ? "image-to-3d" : provenance.Trim();
        RevisionId? currentBaseline = AcceptedBaseline(session.Current);
        bool stale = currentBaseline != binding.InputImageRevisionId;
        var candidate = new ImageToMeshCandidateState(
            CandidateId.New(),
            binding.JobId,
            binding.InputImageRevisionId,
            binding.OutputObjectId,
            outputRevision.Id,
            stale ? CandidateStatus.Conflict : CandidateStatus.Ready,
            providerName,
            provenanceValue,
            DateTimeOffset.UtcNow,
            stale ? $"Accepted 2D baseline advanced from {binding.InputImageRevisionId} to {currentBaseline?.ToString() ?? "none"} while generation was running." : null);

        var candidates = ReadCandidates(session.Current).ToList();
        if (candidates.Count >= MaxCandidates)
            throw new InvalidOperationException($"Project already contains the Stage-C candidate safety limit of {MaxCandidates} entries.");
        candidates.Add(candidate);
        session.Execute(
            stale ? "Preserve stale 3D generation as conflict" : "Register generated 3D candidate",
            state => state.WithMeshRevision(outputRevision).WithMetadata(CandidatesKey, SerializeCandidates(candidates)),
            binding.OutputObjectId);
        return candidate;
    }

    public static CandidateApplyResult ApplyCandidate(ProjectSession session, CandidateId candidateId, string displayName = "Generated model")
    {
        ArgumentNullException.ThrowIfNull(session);
        var candidates = ReadCandidates(session.Current).ToList();
        int index = candidates.FindIndex(x => x.Id == candidateId);
        if (index < 0)
            return new CandidateApplyResult(false, false, "Generated 3D candidate does not exist.", session.Current);
        var candidate = candidates[index];
        if (candidate.Status is CandidateStatus.Applied or CandidateStatus.Discarded)
            return new CandidateApplyResult(false, false, $"Candidate is already {candidate.Status.ToString().ToLowerInvariant()}.", session.Current);

        RevisionId? currentBaseline = AcceptedBaseline(session.Current);
        if (currentBaseline != candidate.InputImageRevisionId)
            return MarkConflict(
                session,
                candidates,
                index,
                candidate,
                $"Accepted 2D baseline advanced from {candidate.InputImageRevisionId} to {currentBaseline?.ToString() ?? "none"}.");
        if (!session.Current.MeshRevisions.TryGetValue(candidate.OutputMeshRevisionId, out var output) || output.ObjectId != candidate.OutputObjectId)
            return MarkConflict(session, candidates, index, candidate, "Generated mesh revision is missing or belongs to another object.");
        if (session.Current.Objects.ContainsKey(candidate.OutputObjectId))
            return MarkConflict(session, candidates, index, candidate, "Generated object identity is already occupied by newer project state.");

        string label = string.IsNullOrWhiteSpace(displayName) ? "Generated model" : displayName.Trim();
        var applied = candidate with { Status = CandidateStatus.Applied, ConflictReason = null };
        candidates[index] = applied;
        session.Execute(
            "Apply generated 3D candidate",
            state => state
                .WithObject(new ProjectObject(candidate.OutputObjectId, label, candidate.OutputMeshRevisionId, TransformState.Identity))
                .WithMetadata(CandidatesKey, SerializeCandidates(candidates)),
            candidate.OutputObjectId);
        return new CandidateApplyResult(true, false, "Generated 3D candidate applied transactionally.", session.Current);
    }

    public static CandidateApplyResult DiscardCandidate(ProjectSession session, CandidateId candidateId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var candidates = ReadCandidates(session.Current).ToList();
        int index = candidates.FindIndex(x => x.Id == candidateId);
        if (index < 0)
            return new CandidateApplyResult(false, false, "Generated 3D candidate does not exist.", session.Current);
        var candidate = candidates[index];
        if (candidate.Status == CandidateStatus.Applied)
            return new CandidateApplyResult(false, false, "Applied candidates cannot be discarded without undoing the apply transaction.", session.Current);
        if (candidate.Status == CandidateStatus.Discarded)
            return new CandidateApplyResult(false, false, "Candidate is already discarded.", session.Current);
        candidates[index] = candidate with { Status = CandidateStatus.Discarded, ConflictReason = candidate.ConflictReason };
        session.Execute(
            "Discard generated 3D candidate",
            state => state.WithMetadata(CandidatesKey, SerializeCandidates(candidates)),
            candidate.OutputObjectId);
        return new CandidateApplyResult(false, false, "Generated 3D candidate discarded; its immutable mesh revision remains available for provenance/recovery.", session.Current);
    }

    public static IReadOnlyList<ImageToMeshCandidateState> ReadCandidates(ProjectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Metadata.TryGetValue(CandidatesKey, out string? json) || string.IsNullOrWhiteSpace(json))
            return Array.Empty<ImageToMeshCandidateState>();
        List<CandidateDto>? rows;
        try { rows = JsonSerializer.Deserialize<List<CandidateDto>>(json, JsonOptions); }
        catch (JsonException ex) { throw new InvalidDataException("Stage-C candidate metadata is invalid JSON.", ex); }
        if (rows == null || rows.Count > MaxCandidates)
            throw new InvalidDataException("Stage-C candidate metadata exceeds its safe collection limit or is null.");

        var result = new List<ImageToMeshCandidateState>(rows.Count);
        var ids = new HashSet<CandidateId>();
        foreach (var row in rows)
        {
            try
            {
                var candidate = new ImageToMeshCandidateState(
                    CandidateId.Parse(row.Id),
                    GenerationJobId.Parse(row.JobId),
                    RevisionId.Parse(row.InputImageRevisionId),
                    ObjectId.Parse(row.OutputObjectId),
                    RevisionId.Parse(row.OutputMeshRevisionId),
                    Enum.TryParse<CandidateStatus>(row.Status, true, out var status) ? status : CandidateStatus.Failed,
                    row.Provider?.Trim() ?? "",
                    row.Provenance?.Trim() ?? "",
                    row.CreatedUtc,
                    row.ConflictReason);
                ValidateCandidate(state, candidate);
                if (!ids.Add(candidate.Id))
                    throw new InvalidDataException($"Duplicate Stage-C candidate identity {candidate.Id}.");
                result.Add(candidate);
            }
            catch (InvalidDataException) { throw; }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                throw new InvalidDataException("Stage-C candidate metadata contains an invalid identity.", ex);
            }
        }
        return result;
    }

    static CandidateApplyResult MarkConflict(
        ProjectSession session,
        List<ImageToMeshCandidateState> candidates,
        int index,
        ImageToMeshCandidateState candidate,
        string reason)
    {
        candidates[index] = candidate with { Status = CandidateStatus.Conflict, ConflictReason = reason };
        session.Execute(
            "Mark stale generated 3D candidate as conflict",
            state => state.WithMetadata(CandidatesKey, SerializeCandidates(candidates)),
            candidate.OutputObjectId);
        return new CandidateApplyResult(false, true, "Generated result is stale/conflicting and was preserved instead of overwriting newer work.", session.Current);
    }

    static void ValidateCandidate(ProjectState state, ImageToMeshCandidateState candidate)
    {
        if (candidate.Id.Value == Guid.Empty || candidate.JobId.Value == Guid.Empty || candidate.OutputObjectId.Value == Guid.Empty)
            throw new InvalidDataException("Stage-C candidate contains an empty identity.");
        if (!state.ImageRevisions.ContainsKey(candidate.InputImageRevisionId))
            throw new InvalidDataException($"Stage-C candidate {candidate.Id} references missing input image revision {candidate.InputImageRevisionId}.");
        if (!state.MeshRevisions.TryGetValue(candidate.OutputMeshRevisionId, out var output) || output.ObjectId != candidate.OutputObjectId)
            throw new InvalidDataException($"Stage-C candidate {candidate.Id} references an invalid output mesh revision.");
        if (string.IsNullOrWhiteSpace(candidate.Provider) || string.IsNullOrWhiteSpace(candidate.Provenance))
            throw new InvalidDataException($"Stage-C candidate {candidate.Id} is missing provider/provenance.");
        if (candidate.Status == CandidateStatus.Applied)
        {
            if (!state.Objects.TryGetValue(candidate.OutputObjectId, out var obj))
                throw new InvalidDataException($"Applied Stage-C candidate {candidate.Id} has no generated object.");
            if (!ActiveLineageContains(state, obj, candidate.OutputMeshRevisionId))
                throw new InvalidDataException($"Applied Stage-C candidate {candidate.Id} is not in the active revision lineage of its generated object.");
        }
    }

    static bool ActiveLineageContains(ProjectState state, ProjectObject obj, RevisionId ancestor)
    {
        var visited = new HashSet<RevisionId>();
        RevisionId cursor = obj.ActiveMeshRevisionId;
        while (visited.Add(cursor) && state.MeshRevisions.TryGetValue(cursor, out var revision) && revision.ObjectId == obj.Id)
        {
            if (cursor == ancestor) return true;
            if (revision.ParentRevisionId is not { } parent) return false;
            cursor = parent;
        }
        return false;
    }

    static string SerializeCandidates(IEnumerable<ImageToMeshCandidateState> values)
    {
        var rows = values.Select(x => new CandidateDto
        {
            Id = x.Id.ToString(),
            JobId = x.JobId.ToString(),
            InputImageRevisionId = x.InputImageRevisionId.ToString(),
            OutputObjectId = x.OutputObjectId.ToString(),
            OutputMeshRevisionId = x.OutputMeshRevisionId.ToString(),
            Status = x.Status.ToString(),
            Provider = x.Provider,
            Provenance = x.Provenance,
            CreatedUtc = x.CreatedUtc,
            ConflictReason = x.ConflictReason
        }).ToList();
        return JsonSerializer.Serialize(rows, JsonOptions);
    }
}
