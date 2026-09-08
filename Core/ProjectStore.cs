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
        ProjectStore.RejectReparsePoints(AssetsRoot, AssetsRoot);
    }
}

public sealed record ProjectLoadResult(
    ProjectState State,
    string SourcePath,
    bool Recovered,
    IReadOnlyList<string> Warnings);

public sealed class ProjectStore
{
    public const string ProjectExtension = ".msculpt2";
    public const int RecoveryCheckpointLimit = 5;
    public const long MaxManifestBytes = 32L * 1024 * 1024;
    public const long MaxMeshAssetBytes = 512L * 1024 * 1024;
    public const long MaxImageAssetBytes = 512L * 1024 * 1024;
    public const long MaxDataAssetBytes = 64L * 1024 * 1024;
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        MaxDepth = 64
    };
    readonly SemaphoreSlim _saveGate = new(1, 1);

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
        if (state == null) throw new ArgumentNullException(nameof(state));
        await _saveGate.WaitAsync(cancellationToken);
        try
        {
            state.Validate();
            var layout = ProjectLayout.FromManifest(projectPath);
            layout.EnsureDirectories();
            await VerifyDurableAssetsAsync(state, layout, cancellationToken);

            var manifest = ToManifest(state);
            string json = JsonSerializer.Serialize(manifest, JsonOptions);
            ValidateManifestRoundTrip(json);

            string temp = layout.ManifestPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                // The recovery copy is durable before the primary manifest is replaced. If a
                // checkpoint cannot be written, the last known-good primary remains untouched.
                await WriteDurableTextAsync(temp, json, cancellationToken);
                await WriteCheckpointAsync(layout, json, state.RevisionNumber, cancellationToken);
                if (File.Exists(layout.ManifestPath))
                    await CopyDurableFileAsync(layout.ManifestPath, layout.ManifestPath + ".bak", cancellationToken);
                File.Move(temp, layout.ManifestPath, overwrite: true);
            }
            catch
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                throw;
            }
        }
        finally
        {
            _saveGate.Release();
        }
    }

    public async Task<ProjectState> LoadAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var layout = ProjectLayout.FromManifest(projectPath);
        return await LoadStateAsync(layout.ManifestPath, layout, cancellationToken);
    }

    public async Task<ProjectLoadResult> LoadWithRecoveryAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var layout = ProjectLayout.FromManifest(projectPath);
        var candidates = new List<string>();
        void Add(string path)
        {
            string full = Path.GetFullPath(path);
            if (!candidates.Contains(full, StringComparer.OrdinalIgnoreCase)) candidates.Add(full);
        }

        Add(layout.ManifestPath);
        Add(layout.ManifestPath + ".bak");
        if (Directory.Exists(layout.RecoveryDirectory))
        {
            foreach (string checkpoint in Directory.EnumerateFiles(layout.RecoveryDirectory, "manifest_*.json", SearchOption.TopDirectoryOnly)
                         .OrderByDescending(File.GetLastWriteTimeUtc)
                         .Take(RecoveryCheckpointLimit))
                Add(checkpoint);
        }

        var failures = new List<string>();
        foreach (string candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                ProjectState state = await LoadStateAsync(candidate, layout, cancellationToken);
                bool recovered = !candidate.Equals(layout.ManifestPath, StringComparison.OrdinalIgnoreCase);
                var warnings = recovered
                    ? new[] { $"Primary manifest was unavailable or invalid; loaded a verified recovery copy from {candidate}." }
                    : Array.Empty<string>();
                return new ProjectLoadResult(state, candidate, recovered, warnings);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(candidate)}: {ex.Message}");
            }
        }

        string detail = failures.Count == 0 ? "No manifest or recovery checkpoint exists." : string.Join(" | ", failures);
        throw new InvalidDataException($"No valid Miniscuplter project manifest could be loaded. {detail}");
    }

    async Task<ProjectState> LoadStateAsync(string path, ProjectLayout layout, CancellationToken cancellationToken)
    {
        string full = Path.GetFullPath(path);
        if (!File.Exists(full)) throw new FileNotFoundException("Project manifest does not exist.", full);
        var info = new FileInfo(full);
        if (info.Length <= 0 || info.Length > MaxManifestBytes)
            throw new InvalidDataException($"Project manifest size {info.Length:N0} bytes is outside the safe limit of {MaxManifestBytes:N0} bytes.");

        string json = await File.ReadAllTextAsync(full, cancellationToken);
        using (JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 }))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Project manifest root must be a JSON object.");
        }

        var manifest = JsonSerializer.Deserialize<ProjectManifest>(json, JsonOptions)
            ?? throw new InvalidDataException("Project manifest JSON is invalid.");
        EnsureManifestCollections(manifest);
        if (manifest.SchemaVersion != ProjectState.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported project schema {manifest.SchemaVersion}; expected {ProjectState.CurrentSchemaVersion}.");

        var state = FromManifest(manifest);
        state.Validate();
        await VerifyDurableAssetsAsync(state, layout, cancellationToken);
        return state;
    }

    static void EnsureManifestCollections(ProjectManifest manifest)
    {
        if (manifest.Objects == null || manifest.MeshRevisions == null || manifest.ImageRevisions == null ||
            manifest.Selections == null || manifest.Rigs == null || manifest.Attachments == null ||
            manifest.Candidates == null)
            throw new InvalidDataException("Project manifest contains a null graph collection.");
        manifest.Metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    static void ValidateManifestRoundTrip(string json)
    {
        using (JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 }))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Generated project manifest root is not a JSON object.");
        }
        var roundTrip = JsonSerializer.Deserialize<ProjectManifest>(json, JsonOptions)
            ?? throw new InvalidDataException("Generated project manifest failed JSON round-trip validation.");
        EnsureManifestCollections(roundTrip);
        if (roundTrip.SchemaVersion != ProjectState.CurrentSchemaVersion)
            throw new InvalidDataException("Generated project manifest schema is not current.");
    }

    async Task VerifyDurableAssetsAsync(ProjectState state, ProjectLayout layout, CancellationToken cancellationToken)
    {
        foreach (var revision in state.MeshRevisions.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string asset = ResolveAsset(layout, revision.AssetPath);
            EnsureDurableFile(asset, MaxMeshAssetBytes, $"Mesh revision {revision.Id}");
            var data = MeshBinaryCodec.Read(asset);
            if (data.VertexCount != revision.VertexCount || data.TriangleCount != revision.TriangleCount)
                throw new InvalidDataException($"Mesh revision {revision.Id} buffer counts do not match the manifest.");
            string hash = await MeshBinaryCodec.ComputeSha256Async(asset, cancellationToken);
            if (!hash.Equals(revision.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Mesh revision {revision.Id} failed SHA-256 verification.");
        }

        foreach (var revision in state.ImageRevisions.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string asset = ResolveAsset(layout, revision.AssetPath);
            EnsureDurableFile(asset, MaxImageAssetBytes, $"Image revision {revision.Id}");
            string hash = await MeshBinaryCodec.ComputeSha256Async(asset, cancellationToken);
            if (!hash.Equals(revision.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Image revision {revision.Id} failed SHA-256 verification.");
        }

        foreach (var selection in state.Selections.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureDurableFile(ResolveAsset(layout, selection.DataAssetPath), MaxDataAssetBytes, $"Selection {selection.Id}");
        }

        foreach (var rig in state.Rigs.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureDurableFile(ResolveAsset(layout, rig.DataAssetPath), MaxDataAssetBytes, $"Rig {rig.Id}");
        }
    }

    static void EnsureDurableFile(string path, long maxBytes, string label)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"{label} is missing its durable asset.", path);
        var info = new FileInfo(path);
        if (info.Length <= 0 || info.Length > maxBytes)
            throw new InvalidDataException($"{label} asset size {info.Length:N0} bytes is outside the safe limit of {maxBytes:N0} bytes.");
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

    static async Task CopyDurableFileAsync(string source, string destination, CancellationToken cancellationToken)
    {
        string temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true))
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
            {
                await input.CopyToAsync(output, 64 * 1024, cancellationToken);
                await output.FlushAsync(cancellationToken);
                output.Flush(flushToDisk: true);
            }
            File.Move(temp, destination, overwrite: true);
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            throw;
        }
    }

    static async Task<string> WriteCheckpointAsync(ProjectLayout layout, string json, long revisionNumber, CancellationToken cancellationToken)
    {
        string checkpoint = Path.Combine(layout.RecoveryDirectory, $"manifest_{revisionNumber:D12}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.json");
        await WriteDurableTextAsync(checkpoint, json, cancellationToken);
        var old = Directory.EnumerateFiles(layout.RecoveryDirectory, "manifest_*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Skip(RecoveryCheckpointLimit)
            .ToArray();
        foreach (string file in old) { try { File.Delete(file); } catch { } }
        return checkpoint;
    }

    public static string ResolveAsset(ProjectLayout layout, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) throw new InvalidDataException("Project asset reference is empty.");
        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).Trim();
        if (Path.IsPathRooted(normalized) || normalized.Equals(".", StringComparison.Ordinal) ||
            normalized.Equals("..", StringComparison.Ordinal) || normalized.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Project asset reference must be relative to the project asset directory.");

        string root = Path.GetFullPath(layout.AssetsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string rootPrefix = root + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine(root, normalized));
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Project asset reference escapes the project asset directory.");
        RejectReparsePoints(root, candidate);
        return candidate;
    }

    internal static void RejectReparsePoints(string root, string candidate)
    {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullCandidate = Path.GetFullPath(candidate);
        string prefix = fullRoot + Path.DirectorySeparatorChar;
        if (!fullCandidate.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) &&
            !fullCandidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Project path escapes its expected root.");

        CheckReparse(fullRoot);
        string relative = Path.GetRelativePath(fullRoot, fullCandidate);
        if (relative.Equals(".", StringComparison.Ordinal)) return;
        string current = fullRoot;
        foreach (string part in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (string.IsNullOrWhiteSpace(part) || part.Equals(".", StringComparison.Ordinal)) continue;
            current = Path.Combine(current, part);
            if (!TryGetAttributes(current, out var attributes)) continue;
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Project path contains a reparse point or symlink: {current}");
        }
    }

    static void CheckReparse(string path)
    {
        if (TryGetAttributes(path, out var attributes) && (attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException($"Project path contains a reparse point or symlink: {path}");
    }

    static bool TryGetAttributes(string path, out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException) { attributes = default; return false; }
        catch (DirectoryNotFoundException) { attributes = default; return false; }
        catch (IOException) { attributes = default; return false; }
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
