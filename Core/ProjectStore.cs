using System.Text.Json;

namespace Miniscuplter.Core;

public sealed record ProjectLayout(string ManifestPath, string AssetsRoot, string MeshDirectory, string ImageDirectory, string DataDirectory, string RecoveryDirectory)
{
    public static ProjectLayout FromManifest(string projectPath)
    {
        string manifest = Path.GetFullPath(projectPath);
        string parent = Path.GetDirectoryName(manifest) ?? throw new InvalidOperationException("Project path has no parent directory.");
        string name = Path.GetFileNameWithoutExtension(manifest);
        string assets = Path.Combine(parent, name + "_assets_v7");
        return new ProjectLayout(
            manifest,
            assets,
            Path.Combine(assets, "meshes"),
            Path.Combine(assets, "images"),
            Path.Combine(assets, "data"),
            Path.Combine(assets, "recovery"));
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath)!);
        Directory.CreateDirectory(AssetsRoot);
        Directory.CreateDirectory(MeshDirectory);
        Directory.CreateDirectory(ImageDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(RecoveryDirectory);
    }
}

public sealed class ProjectStore
{
    public const string ProjectExtension = ".msculpt2";
    public const int RecoveryCheckpointLimit = 5;
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public async Task<MeshRevision> CreateMeshRevisionAsync(
        string projectPath,
        ObjectId objectId,
        MeshData mesh,
        string provenance,
        RevisionId? parentRevisionId = null,
        CancellationToken cancellationToken = default)
    {
        if (objectId.Value == Guid.Empty) throw new ArgumentException("Object ID cannot be empty.", nameof(objectId));
        var layout = ProjectLayout.FromManifest(projectPath);
        layout.EnsureDirectories();
        RevisionId revisionId = RevisionId.New();
        string relative = $"meshes/{revisionId}.msh";
        string destination = ResolveAsset(layout, relative);
        string hash = await MeshBinaryCodec.WriteAtomicAsync(destination, mesh, cancellationToken);
        return new MeshRevision(
            revisionId,
            objectId,
            parentRevisionId,
            relative,
            hash,
            mesh.VertexCount,
            mesh.TriangleCount,
            string.IsNullOrWhiteSpace(provenance) ? "unknown" : provenance.Trim(),
            DateTimeOffset.UtcNow);
    }

    public async Task SaveAsync(ProjectState state, string projectPath, CancellationToken cancellationToken = default)
    {
        state.Validate();
        var layout = ProjectLayout.FromManifest(projectPath);
        layout.EnsureDirectories();
        await VerifyDurableAssetsAsync(state, layout, cancellationToken);
        var manifest = ToManifest(state);
        string json = JsonSerializer.Serialize(manifest, JsonOptions);
        _ = JsonSerializer.Deserialize<ProjectManifest>(json, JsonOptions) ?? throw new InvalidDataException("Generated project manifest failed JSON round-trip validation.");

        string temp = layout.ManifestPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await WriteDurableTextAsync(temp, json, cancellationToken);
            if (File.Exists(layout.ManifestPath))
            {
                string backup = layout.ManifestPath + ".bak";
                File.Copy(layout.ManifestPath, backup, overwrite: true);
            }
            File.Move(temp, layout.ManifestPath, overwrite: true);
            await WriteCheckpointAsync(layout, json, state.RevisionNumber, cancellationToken);
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            throw;
        }
    }

    public async Task<ProjectState> LoadAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var layout = ProjectLayout.FromManifest(projectPath);
        if (!File.Exists(layout.ManifestPath)) throw new FileNotFoundException("Project manifest does not exist.", layout.ManifestPath);
        string json = await File.ReadAllTextAsync(layout.ManifestPath, cancellationToken);
        var manifest = JsonSerializer.Deserialize<ProjectManifest>(json, JsonOptions) ?? throw new InvalidDataException("Project manifest JSON is invalid.");
        if (manifest.SchemaVersion != ProjectState.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported project schema {manifest.SchemaVersion}; expected {ProjectState.CurrentSchemaVersion}.");
        var state = FromManifest(manifest);
        state.Validate();
        await VerifyDurableAssetsAsync(state, layout, cancellationToken);
        return state;
    }

    async Task VerifyDurableAssetsAsync(ProjectState state, ProjectLayout layout, CancellationToken cancellationToken)
    {
        foreach (var revision in state.MeshRevisions.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string asset = ResolveAsset(layout, revision.AssetPath);
            if (!File.Exists(asset)) throw new FileNotFoundException($"Mesh revision {revision.Id} is missing its durable asset.", asset);
            var data = MeshBinaryCodec.Read(asset);
            if (data.VertexCount != revision.VertexCount || data.TriangleCount != revision.TriangleCount)
                throw new InvalidDataException($"Mesh revision {revision.Id} buffer counts do not match the manifest.");
            string hash = await MeshBinaryCodec.ComputeSha256Async(asset, cancellationToken);
            if (!hash.Equals(revision.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Mesh revision {revision.Id} failed SHA-256 verification.");
        }
    }

    static async Task WriteDurableTextAsync(string path, string content, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 64 * 1024, useAsync: true);
        await using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 64 * 1024, leaveOpen: true);
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
        await stream.FlushAsync(cancellationToken);
        stream.Flush(flushToDisk: true);
    }

    static async Task WriteCheckpointAsync(ProjectLayout layout, string json, long revisionNumber, CancellationToken cancellationToken)
    {
        string checkpoint = Path.Combine(layout.RecoveryDirectory, $"manifest_{revisionNumber:D12}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json");
        await WriteDurableTextAsync(checkpoint, json, cancellationToken);
        var old = Directory.EnumerateFiles(layout.RecoveryDirectory, "manifest_*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Skip(RecoveryCheckpointLimit)
            .ToArray();
        foreach (string file in old) { try { File.Delete(file); } catch { } }
    }

    public static string ResolveAsset(ProjectLayout layout, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) throw new InvalidDataException("Project asset reference is empty.");
        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized)) throw new InvalidDataException("Project asset reference must be relative.");
        string root = Path.GetFullPath(layout.AssetsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine(layout.AssetsRoot, normalized));
        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Project asset reference escapes the project asset directory.");
        return candidate;
    }

    static ProjectManifest ToManifest(ProjectState state) => new()
    {
        SchemaVersion = ProjectState.CurrentSchemaVersion,
        ProjectId = state.ProjectId.ToString(),
        DisplayName = state.DisplayName,
        RevisionNumber = state.RevisionNumber,
        Objects = state.Objects.Values.Select(x => new ObjectDto
        {
            Id = x.Id.ToString(), DisplayName = x.DisplayName, ActiveMeshRevisionId = x.ActiveMeshRevisionId.ToString(),
            Position = [x.Transform.Position.X, x.Transform.Position.Y, x.Transform.Position.Z],
            Rotation = [x.Transform.RotationEuler.X, x.Transform.RotationEuler.Y, x.Transform.RotationEuler.Z],
            Scale = [x.Transform.Scale.X, x.Transform.Scale.Y, x.Transform.Scale.Z], Role = x.Role, Visible = x.Visible
        }).ToList(),
        MeshRevisions = state.MeshRevisions.Values.Select(x => new MeshRevisionDto
        {
            Id = x.Id.ToString(), ObjectId = x.ObjectId.ToString(), ParentRevisionId = x.ParentRevisionId?.ToString(), AssetPath = x.AssetPath,
            Sha256 = x.Sha256, VertexCount = x.VertexCount, TriangleCount = x.TriangleCount, Provenance = x.Provenance,
            CreatedUtc = x.CreatedUtc, TopologySignature = x.TopologySignature
        }).ToList(),
        ImageRevisions = state.ImageRevisions.Values.Select(x => new ImageRevisionDto
        {
            Id = x.Id.ToString(), ParentRevisionId = x.ParentRevisionId?.ToString(), AssetPath = x.AssetPath, Sha256 = x.Sha256,
            Purpose = x.Purpose, Provenance = x.Provenance, CreatedUtc = x.CreatedUtc
        }).ToList(),
        Selections = state.Selections.Values.Select(x => new SelectionDto
        {
            Id = x.Id.ToString(), ObjectId = x.ObjectId.ToString(), MeshRevisionId = x.MeshRevisionId.ToString(), Kind = x.Kind,
            DataAssetPath = x.DataAssetPath, CreatedUtc = x.CreatedUtc
        }).ToList(),
        Rigs = state.Rigs.Values.Select(x => new RigDto
        {
            Id = x.Id.ToString(), ObjectId = x.ObjectId.ToString(), RestMeshRevisionId = x.RestMeshRevisionId.ToString(),
            DataAssetPath = x.DataAssetPath, CreatedUtc = x.CreatedUtc
        }).ToList(),
        Attachments = state.Attachments.Values.Select(x => new AttachmentDto
        {
            Id = x.Id.ToString(), ParentObjectId = x.ParentObjectId.ToString(), ChildObjectId = x.ChildObjectId.ToString(), Socket = x.Socket,
            Position = [x.LocalTransform.Position.X, x.LocalTransform.Position.Y, x.LocalTransform.Position.Z],
            Rotation = [x.LocalTransform.RotationEuler.X, x.LocalTransform.RotationEuler.Y, x.LocalTransform.RotationEuler.Z],
            Scale = [x.LocalTransform.Scale.X, x.LocalTransform.Scale.Y, x.LocalTransform.Scale.Z], CreatedUtc = x.CreatedUtc
        }).ToList(),
        Candidates = state.Candidates.Values.Select(x => new CandidateDto
        {
            Id = x.Id.ToString(), ObjectId = x.ObjectId.ToString(), InputRevisionId = x.InputRevisionId.ToString(), OutputRevisionId = x.OutputRevisionId.ToString(),
            Kind = x.Kind, Status = x.Status.ToString(), Provenance = x.Provenance, CreatedUtc = x.CreatedUtc, ConflictReason = x.ConflictReason
        }).ToList(),
        Metadata = new Dictionary<string, string>(state.Metadata, StringComparer.OrdinalIgnoreCase)
    };

    static ProjectState FromManifest(ProjectManifest manifest)
    {
        var meshes = manifest.MeshRevisions.Select(x => new MeshRevision(
            RevisionId.Parse(x.Id), ObjectId.Parse(x.ObjectId), ParseRevision(x.ParentRevisionId), x.AssetPath, x.Sha256,
            x.VertexCount, x.TriangleCount, x.Provenance, x.CreatedUtc, x.TopologySignature)).ToArray();
        var objects = manifest.Objects.Select(x => new ProjectObject(
            ObjectId.Parse(x.Id), x.DisplayName, RevisionId.Parse(x.ActiveMeshRevisionId),
            ParseTransform(x.Position, x.Rotation, x.Scale), x.Role, x.Visible)).ToArray();
        var images = manifest.ImageRevisions.Select(x => new ImageRevision(
            RevisionId.Parse(x.Id), ParseRevision(x.ParentRevisionId), x.AssetPath, x.Sha256, x.Purpose, x.Provenance, x.CreatedUtc)).ToArray();
        var selections = manifest.Selections.Select(x => new SelectionBinding(
            SelectionId.Parse(x.Id), ObjectId.Parse(x.ObjectId), RevisionId.Parse(x.MeshRevisionId), x.Kind, x.DataAssetPath, x.CreatedUtc)).ToArray();
        var rigs = manifest.Rigs.Select(x => new RigRecord(
            RigId.Parse(x.Id), ObjectId.Parse(x.ObjectId), RevisionId.Parse(x.RestMeshRevisionId), x.DataAssetPath, x.CreatedUtc)).ToArray();
        var attachments = manifest.Attachments.Select(x => new AttachmentRecord(
            AttachmentId.Parse(x.Id), ObjectId.Parse(x.ParentObjectId), ObjectId.Parse(x.ChildObjectId), x.Socket,
            ParseTransform(x.Position, x.Rotation, x.Scale), x.CreatedUtc)).ToArray();
        var candidates = manifest.Candidates.Select(x => new CandidateRecord(
            CandidateId.Parse(x.Id), ObjectId.Parse(x.ObjectId), RevisionId.Parse(x.InputRevisionId), RevisionId.Parse(x.OutputRevisionId),
            x.Kind, Enum.TryParse<CandidateStatus>(x.Status, true, out var status) ? status : CandidateStatus.Failed,
            x.Provenance, x.CreatedUtc, x.ConflictReason)).ToArray();
        return new ProjectState(ProjectId.Parse(manifest.ProjectId), manifest.DisplayName, manifest.RevisionNumber,
            objects, meshes, images, selections, rigs, attachments, candidates, manifest.Metadata);
    }

    static RevisionId? ParseRevision(string? value) => string.IsNullOrWhiteSpace(value) ? null : RevisionId.Parse(value);

    static TransformState ParseTransform(float[] position, float[] rotation, float[] scale)
    {
        if (position.Length != 3 || rotation.Length != 3 || scale.Length != 3) throw new InvalidDataException("Manifest transform arrays must each contain three values.");
        return new TransformState(new Vec3(position[0], position[1], position[2]), new Vec3(rotation[0], rotation[1], rotation[2]), new Vec3(scale[0], scale[1], scale[2]));
    }

    sealed class ProjectManifest
    {
        public int SchemaVersion { get; set; }
        public string ProjectId { get; set; } = "";
        public string DisplayName { get; set; } = "Untitled";
        public long RevisionNumber { get; set; }
        public List<ObjectDto> Objects { get; set; } = [];
        public List<MeshRevisionDto> MeshRevisions { get; set; } = [];
        public List<ImageRevisionDto> ImageRevisions { get; set; } = [];
        public List<SelectionDto> Selections { get; set; } = [];
        public List<RigDto> Rigs { get; set; } = [];
        public List<AttachmentDto> Attachments { get; set; } = [];
        public List<CandidateDto> Candidates { get; set; } = [];
        public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    sealed class ObjectDto { public string Id { get; set; } = ""; public string DisplayName { get; set; } = ""; public string ActiveMeshRevisionId { get; set; } = ""; public float[] Position { get; set; } = [0,0,0]; public float[] Rotation { get; set; } = [0,0,0]; public float[] Scale { get; set; } = [1,1,1]; public string Role { get; set; } = "mesh"; public bool Visible { get; set; } = true; }
    sealed class MeshRevisionDto { public string Id { get; set; } = ""; public string ObjectId { get; set; } = ""; public string? ParentRevisionId { get; set; } public string AssetPath { get; set; } = ""; public string Sha256 { get; set; } = ""; public int VertexCount { get; set; } public int TriangleCount { get; set; } public string Provenance { get; set; } = ""; public DateTimeOffset CreatedUtc { get; set; } public string? TopologySignature { get; set; } }
    sealed class ImageRevisionDto { public string Id { get; set; } = ""; public string? ParentRevisionId { get; set; } public string AssetPath { get; set; } = ""; public string Sha256 { get; set; } = ""; public string Purpose { get; set; } = ""; public string Provenance { get; set; } = ""; public DateTimeOffset CreatedUtc { get; set; } }
    sealed class SelectionDto { public string Id { get; set; } = ""; public string ObjectId { get; set; } = ""; public string MeshRevisionId { get; set; } = ""; public string Kind { get; set; } = ""; public string DataAssetPath { get; set; } = ""; public DateTimeOffset CreatedUtc { get; set; } }
    sealed class RigDto { public string Id { get; set; } = ""; public string ObjectId { get; set; } = ""; public string RestMeshRevisionId { get; set; } = ""; public string DataAssetPath { get; set; } = ""; public DateTimeOffset CreatedUtc { get; set; } }
    sealed class AttachmentDto { public string Id { get; set; } = ""; public string ParentObjectId { get; set; } = ""; public string ChildObjectId { get; set; } = ""; public string Socket { get; set; } = ""; public float[] Position { get; set; } = [0,0,0]; public float[] Rotation { get; set; } = [0,0,0]; public float[] Scale { get; set; } = [1,1,1]; public DateTimeOffset CreatedUtc { get; set; } }
    sealed class CandidateDto { public string Id { get; set; } = ""; public string ObjectId { get; set; } = ""; public string InputRevisionId { get; set; } = ""; public string OutputRevisionId { get; set; } = ""; public string Kind { get; set; } = ""; public string Status { get; set; } = "Ready"; public string Provenance { get; set; } = ""; public DateTimeOffset CreatedUtc { get; set; } public string? ConflictReason { get; set; } }
}
