using System.Security.Cryptography;

namespace Miniscuplter.Core;

/// <summary>
/// Durable asset helper for the Stage-C compatibility bridge. Images are copied into the
/// project-owned asset directory before they can become an accepted baseline revision.
/// </summary>
public static class StageCAssetStore
{
    static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp"
    };

    public static async Task<ImageRevision> CreateImageRevisionAsync(
        string projectPath,
        string sourcePath,
        string purpose,
        string provenance,
        RevisionId? parentRevisionId = null,
        CancellationToken cancellationToken = default)
    {
        string source = Path.GetFullPath(sourcePath);
        if (!File.Exists(source)) throw new FileNotFoundException("Accepted 2D source image does not exist.", source);
        var sourceInfo = new FileInfo(source);
        if (sourceInfo.Length <= 0 || sourceInfo.Length > ProjectStore.MaxImageAssetBytes)
            throw new InvalidDataException($"Accepted image size {sourceInfo.Length:N0} bytes is outside the safe project limit.");

        string extension = Path.GetExtension(source).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
            throw new InvalidDataException($"Unsupported Stage-C image format '{extension}'.");

        var layout = ProjectLayout.FromManifest(projectPath);
        layout.EnsureDirectories();
        RevisionId id = RevisionId.New();
        string relative = $"images/{id}{extension}";
        string destination = ProjectStore.ResolveAsset(layout, relative);
        string temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true))
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true))
            {
                await input.CopyToAsync(output, 64 * 1024, cancellationToken);
                await output.FlushAsync(cancellationToken);
                output.Flush(flushToDisk: true);
            }
            File.Move(temp, destination, overwrite: false);
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            throw;
        }

        string sha256;
        await using (var stream = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true))
        {
            using var hash = SHA256.Create();
            byte[] digest = await hash.ComputeHashAsync(stream, cancellationToken);
            sha256 = Convert.ToHexString(digest).ToLowerInvariant();
        }

        return new ImageRevision(
            id,
            parentRevisionId,
            relative,
            sha256,
            string.IsNullOrWhiteSpace(purpose) ? "3d-baseline" : purpose.Trim(),
            string.IsNullOrWhiteSpace(provenance) ? "editor-stagec" : provenance.Trim(),
            DateTimeOffset.UtcNow);
    }

    public static string ResolveImagePath(string projectPath, ImageRevision revision)
        => ProjectStore.ResolveAsset(ProjectLayout.FromManifest(projectPath), revision.AssetPath);
}
