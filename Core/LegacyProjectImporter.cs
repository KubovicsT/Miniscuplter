using System.Text;
using System.Text.Json;

namespace Miniscuplter.Core;

public sealed record MigrationResult(string DestinationProject, int SourceSchema, int ObjectCount, IReadOnlyList<string> Warnings, string MigrationLogPath);

public sealed class LegacyProjectImporter
{
    readonly ProjectStore _store;
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public LegacyProjectImporter(ProjectStore? store = null) => _store = store ?? new ProjectStore();

    public async Task<MigrationResult> ImportAsync(string legacyProjectPath, string destinationProjectPath, CancellationToken cancellationToken = default)
    {
        string source = Path.GetFullPath(legacyProjectPath);
        string destination = Path.GetFullPath(destinationProjectPath);
        if (!File.Exists(source)) throw new FileNotFoundException("Legacy project does not exist.", source);
        if (source.Equals(destination, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Legacy import always writes a new project copy; destination cannot be the original project file.");
        if (File.Exists(destination)) throw new IOException("Migration destination already exists; refusing to overwrite it.");

        var layout = ProjectLayout.FromManifest(destination);
        if (Directory.Exists(layout.AssetsRoot) && Directory.EnumerateFileSystemEntries(layout.AssetsRoot).Any())
            throw new IOException("Migration destination asset directory is not empty; choose a fresh destination.");

        string sourceText = await File.ReadAllTextAsync(source, cancellationToken);
        using var document = JsonDocument.Parse(sourceText);
        JsonElement root = document.RootElement;
        int schema = ReadInt(root, "Version");
        if (schema is < 1 or > 6) throw new InvalidDataException($"Legacy importer supports schema 1–6, not {schema}.");
        JsonElement objectsElement = FindProperty(root, "Objects") ?? throw new InvalidDataException("Legacy project has no Objects array.");
        if (objectsElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Legacy Objects value is not an array.");

        string legacyAssetsRoot = Path.Combine(Path.GetDirectoryName(source)!, Path.GetFileNameWithoutExtension(source) + "_assets");
        string legacyAssetsPrefix = Path.GetFullPath(legacyAssetsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var warnings = new List<string>();
        var state = ProjectState.Create(Path.GetFileNameWithoutExtension(destination))
            .WithMetadata("migration.source_schema", schema.ToString())
            .WithMetadata("migration.source_name", Path.GetFileName(source))
            .WithMetadata("migration.imported_utc", DateTimeOffset.UtcNow.ToString("O"));

        try
        {
            layout.EnsureDirectories();
            string migrationDir = Path.Combine(layout.DataDirectory, "migration");
            Directory.CreateDirectory(migrationDir);
            string preservedManifest = Path.Combine(migrationDir, "legacy_manifest.json");
            await WriteDurableCopyAsync(preservedManifest, sourceText, cancellationToken);
            state = state.WithMetadata("migration.legacy_manifest", "data/migration/legacy_manifest.json");

            int objectCount = 0;
            foreach (JsonElement item in objectsElement.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                string name = ReadString(item, "Name") ?? $"Object {objectCount + 1}";
                string meshRef = ReadString(item, "Mesh") ?? throw new InvalidDataException($"Legacy object '{name}' has no mesh asset reference.");
                string normalized = meshRef.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(normalized)) throw new InvalidDataException($"Legacy object '{name}' contains an absolute mesh asset reference.");
                string meshPath = Path.GetFullPath(Path.Combine(legacyAssetsRoot, normalized));
                if (!meshPath.StartsWith(legacyAssetsPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Legacy object '{name}' mesh reference escapes the legacy asset directory.");
                if (!File.Exists(meshPath)) throw new FileNotFoundException($"Legacy mesh asset for '{name}' is missing.", meshPath);

                MeshData mesh = LegacyStlReader.ReadBinary(meshPath);
                ObjectId objectId = ObjectId.New();
                var revision = await _store.CreateMeshRevisionAsync(destination, objectId, mesh, $"legacy-v{schema}:{meshRef.Replace('\\','/')}", cancellationToken: cancellationToken);
                state = state.WithMeshRevision(revision);
                var transform = new TransformState(ReadVec3(item, "Position", Vec3.Zero), ReadVec3(item, "Rotation", Vec3.Zero), ReadVec3(item, "Scale", Vec3.One));
                string role = ReadString(item, "Role") ?? "mesh";
                state = state.WithObject(new ProjectObject(objectId, name, revision.Id, transform, role, true));
                objectCount++;
            }

            foreach (string legacySection in new[] { "AiLayers", "Rigs", "Sockets", "Attachments", "SculptMasks" })
            {
                var section = FindProperty(root, legacySection);
                if (section is { ValueKind: JsonValueKind.Array } array && array.GetArrayLength() > 0)
                    warnings.Add($"{legacySection}: {array.GetArrayLength()} legacy record(s) preserved verbatim in the migration payload; semantic adapter will be attached during the Stage-B bridge.");
            }

            await _store.SaveAsync(state, destination, cancellationToken);
            string logPath = Path.Combine(migrationDir, "migration_log.json");
            string logJson = JsonSerializer.Serialize(new
            {
                source = source,
                source_schema = schema,
                destination,
                destination_schema = ProjectState.CurrentSchemaVersion,
                imported_utc = DateTimeOffset.UtcNow,
                objects = objectCount,
                warnings
            }, JsonOptions);
            await WriteDurableCopyAsync(logPath, logJson, cancellationToken);
            return new MigrationResult(destination, schema, objectCount, warnings, logPath);
        }
        catch
        {
            try { if (File.Exists(destination)) File.Delete(destination); } catch { }
            try { if (Directory.Exists(layout.AssetsRoot)) Directory.Delete(layout.AssetsRoot, recursive: true); } catch { }
            throw;
        }
    }

    static async Task WriteDurableCopyAsync(string path, string content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 64 * 1024, useAsync: true))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 64 * 1024, leaveOpen: true))
            {
                await writer.WriteAsync(content.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(true);
            }
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            throw;
        }
    }

    static JsonElement? FindProperty(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in element.EnumerateObject())
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        return null;
    }

    static int ReadInt(JsonElement element, string name)
    {
        var value = FindProperty(element, name) ?? throw new InvalidDataException($"Legacy project is missing '{name}'.");
        if (!value.TryGetInt32(out int result)) throw new InvalidDataException($"Legacy field '{name}' is not an integer.");
        return result;
    }

    static string? ReadString(JsonElement element, string name)
    {
        var value = FindProperty(element, name);
        return value is { ValueKind: JsonValueKind.String } ? value.Value.GetString() : null;
    }

    static Vec3 ReadVec3(JsonElement element, string name, Vec3 fallback)
    {
        var value = FindProperty(element, name);
        if (value is not { ValueKind: JsonValueKind.Array } array || array.GetArrayLength() < 3) return fallback;
        var values = array.EnumerateArray().Take(3).Select(x => x.GetSingle()).ToArray();
        var result = new Vec3(values[0], values[1], values[2]);
        if (!result.IsFinite) throw new InvalidDataException($"Legacy transform '{name}' contains non-finite values.");
        return result;
    }
}

public static class LegacyStlReader
{
    public static MeshData ReadBinary(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: false);
        if (stream.Length < 84) throw new InvalidDataException("Legacy STL is too short to be a binary STL.");
        _ = reader.ReadBytes(80);
        uint triangleCount = reader.ReadUInt32();
        if (triangleCount == 0 || triangleCount > 100_000_000) throw new InvalidDataException("Legacy STL declares an invalid triangle count.");
        long expected = 84L + triangleCount * 50L;
        if (stream.Length != expected)
            throw new InvalidDataException("Legacy project mesh is not a valid binary STL produced by Miniscuplter; migration leaves the original untouched.");
        if (triangleCount > int.MaxValue / 9) throw new InvalidDataException("Legacy STL is too large to migrate safely.");
        var positions = new float[checked((int)triangleCount * 9)];
        var indices = new int[checked((int)triangleCount * 3)];
        int p = 0, vi = 0;
        for (uint triangle = 0; triangle < triangleCount; triangle++)
        {
            _ = reader.ReadSingle(); _ = reader.ReadSingle(); _ = reader.ReadSingle();
            for (int vertex = 0; vertex < 3; vertex++)
            {
                positions[p++] = reader.ReadSingle();
                positions[p++] = reader.ReadSingle();
                positions[p++] = reader.ReadSingle();
                indices[vi] = vi;
                vi++;
            }
            _ = reader.ReadUInt16();
        }
        return new MeshData(positions, indices);
    }
}
