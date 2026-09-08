using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Miniscuplter.Launcher;

internal sealed record AppUpdateInfo(
    bool Available,
    string CurrentVersion,
    string LatestVersion,
    string? DownloadUrl,
    string? Sha256,
    long AssetSize,
    string? ReleasePage,
    string? Notes)
{
    public bool Installable => Available && !string.IsNullOrWhiteSpace(DownloadUrl) &&
        Sha256 is { Length: 64 } digest && digest.All(Uri.IsHexDigit) &&
        AssetSize is > 0 and <= ApplicationUpdateService.MaxDownloadBytes;
}

internal sealed class ApplicationUpdateService
{
    const string AssetName = "Miniscuplter-win-x64.zip";
    const string DigestAssetName = "Miniscuplter-win-x64.zip.sha256";
    const long UpdateSafetyBytes = 64L * 1024 * 1024;
    internal const long MaxDownloadBytes = 8L * 1024 * 1024 * 1024;
    const long MaxReleaseMetadataBytes = 4L * 1024 * 1024;
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(30) };
    readonly LauncherSettings _settings;
    readonly SemaphoreSlim _downloadGate = new(1, 1);

    public ApplicationUpdateService(LauncherSettings settings)
    {
        _settings = settings;
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"Miniscuplter-Launcher/{version}");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public async Task<AppUpdateInfo> CheckAsync(CancellationToken cancellationToken = default)
    {
        string current = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        string endpoint = $"https://api.github.com/repos/{_settings.ReleaseRepository}/releases?per_page=100";
        using var response = await _http.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new AppUpdateInfo(false, current, current, null, null, 0, null, "No published release is available yet.");
            throw new InvalidOperationException($"Application update check failed: HTTP {(int)response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(await ReadTextAsync(response.Content, MaxReleaseMetadataBytes, cancellationToken));
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("GitHub returned an invalid release list.");

        JsonElement? newest = null;
        string latest = current;
        foreach (var release in doc.RootElement.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draft) && draft.GetBoolean()) continue;
            if (release.TryGetProperty("prerelease", out var prerelease) && prerelease.GetBoolean()) continue;
            string tag = release.TryGetProperty("tag_name", out var te) ? te.GetString() ?? "" : "";
            string candidate = Normalize(tag);
            if (!Version.TryParse(candidate, out _)) continue;
            if (newest == null || CompareVersions(candidate, latest) > 0)
            {
                newest = release.Clone();
                latest = candidate;
            }
        }

        if (newest == null)
            return new AppUpdateInfo(false, current, current, null, null, 0, null, "No stable Miniscuplter release is published yet.");

        var root = newest.Value;
        string? page = root.TryGetProperty("html_url", out var he) ? he.GetString() : null;
        string? notes = root.TryGetProperty("body", out var be) && be.ValueKind != JsonValueKind.Null ? be.GetString() : null;
        string? download = null, sha256 = null, digestUrl = null;
        long assetSize = 0;

        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.TryGetProperty("name", out var ne) ? ne.GetString() ?? "" : "";
                if (name.Equals(AssetName, StringComparison.OrdinalIgnoreCase))
                {
                    download = asset.TryGetProperty("browser_download_url", out var ue) ? ue.GetString() : null;
                    assetSize = asset.TryGetProperty("size", out var se) && se.TryGetInt64(out long parsedSize) ? parsedSize : 0;
                    if (asset.TryGetProperty("digest", out var de) && de.ValueKind == JsonValueKind.String)
                    {
                        string digest = de.GetString() ?? "";
                        if (digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                            sha256 = NormalizeSha256(digest[7..]);
                    }
                }
                else if (name.Equals(DigestAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    digestUrl = asset.TryGetProperty("browser_download_url", out var due) ? due.GetString() : null;
                }
            }
        }

        if (!IsTrustedDownloadUrl(download))
            download = null;
        if (!IsTrustedDownloadUrl(digestUrl))
            digestUrl = null;

        if (sha256 == null && !string.IsNullOrWhiteSpace(digestUrl))
        {
            try
            {
                using var digestResponse = await _http.GetAsync(digestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                digestResponse.EnsureSuccessStatusCode();
                string text = await ReadTextAsync(digestResponse.Content, MaxReleaseMetadataBytes, cancellationToken);
                sha256 = ParseDigestFile(text);
            }
            catch { }
        }

        bool available = CompareVersions(latest, current) > 0;
        return new AppUpdateInfo(available, current, latest, download, sha256, assetSize, page, notes);
    }

    public bool IsMainApplicationRunning()
    {
        try
        {
            string app = InstallLayout.ResolveApp(_settings);
            string processName = Path.GetFileNameWithoutExtension(app);
            var processes = Process.GetProcessesByName(processName);
            string target = Path.GetFullPath(app);
            try
            {
                foreach (var process in processes)
                {
                    if (process.Id == Environment.ProcessId || process.HasExited) continue;
                    try
                    {
                        string? executable = process.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(executable) &&
                            Path.GetFullPath(executable).Equals(target, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    catch
                    {
                        // A same-named process whose executable cannot be inspected is treated
                        // conservatively as running so an update cannot race another install.
                        return true;
                    }
                }
                return false;
            }
            finally { foreach (var p in processes) p.Dispose(); }
        }
        catch { return false; }
    }

    public async Task<string> DownloadPackageAsync(AppUpdateInfo info, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await _downloadGate.WaitAsync(cancellationToken);
        try
        {
            return await DownloadPackageCoreAsync(info, progress, cancellationToken);
        }
        finally
        {
            _downloadGate.Release();
        }
    }

    async Task<string> DownloadPackageCoreAsync(AppUpdateInfo info, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        if (!info.Installable || !IsTrustedDownloadUrl(info.DownloadUrl))
            throw new InvalidOperationException("The newest release is not safely installable: it needs a trusted HTTPS GitHub ZIP URL, a bounded byte size, and a SHA-256 digest.");

        string safeVersion = Regex.Replace(info.LatestVersion, @"[^0-9A-Za-z._-]", "_");
        string cache = await ResolveUpdateCacheAsync(info, safeVersion, cancellationToken);
        Directory.CreateDirectory(cache);
        string partial = Path.Combine(cache, $"Miniscuplter-{safeVersion}-win-x64.zip.partial");
        string final = Path.Combine(cache, $"Miniscuplter-{safeVersion}-win-x64.zip");

        if (File.Exists(final))
        {
            if (await VerifyPackageFileAsync(final, info, cancellationToken))
            {
                progress?.Report(100);
                return final;
            }
            TryDelete(final);
        }

        for (int attempt = 0; attempt < 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long existing = File.Exists(partial) ? new FileInfo(partial).Length : 0;
            if (existing < 0 || existing > info.AssetSize)
            {
                TryDelete(partial);
                existing = 0;
            }

            EnsureFreeSpace(cache, Math.Max(0, info.AssetSize - existing) + UpdateSafetyBytes, "application update download");
            if (existing == info.AssetSize && await VerifyPackageFileAsync(partial, info, cancellationToken))
            {
                File.Move(partial, final, true);
                progress?.Report(100);
                CleanupOldUpdateCache(cache, final);
                return final;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, info.DownloadUrl);
            if (existing > 0) request.Headers.Range = new RangeHeaderValue(existing, null);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                TryDelete(partial);
                continue;
            }
            response.EnsureSuccessStatusCode();

            bool resumed = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;
            if (resumed)
            {
                long? rangeStart = response.Content.Headers.ContentRange?.From;
                long? responseBytes = response.Content.Headers.ContentLength;
                if (rangeStart != existing ||
                    responseBytes is long returnedBytes && returnedBytes > info.AssetSize - existing)
                {
                    TryDelete(partial);
                    continue;
                }
            }
            else
            {
                if (existing == 0 && response.StatusCode == HttpStatusCode.PartialContent &&
                    response.Content.Headers.ContentRange?.From is long unexpectedStart && unexpectedStart != 0)
                {
                    TryDelete(partial);
                    throw new InvalidDataException("The update server returned an unexpected byte range.");
                }
                existing = 0;
            }

            if (response.Content.Headers.ContentLength is long contentLength &&
                contentLength > info.AssetSize - existing)
            {
                TryDelete(partial);
                throw new InvalidDataException("The update server returned more data than the published asset size.");
            }

            long written = existing;

            // Keep network/file handles in an inner scope so verification and replacement happen
            // only after every writer has been disposed.
            {
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = new FileStream(
                    partial,
                    resumed ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    1024 * 128,
                    useAsync: true);

                byte[] buffer = new byte[1024 * 128];
                while (true)
                {
                    int n = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (n <= 0) break;
                    if (written > info.AssetSize - n)
                    {
                        TryDelete(partial);
                        throw new InvalidDataException("The update server exceeded the published asset size.");
                    }
                    await output.WriteAsync(buffer.AsMemory(0, n), cancellationToken);
                    written += n;
                    progress?.Report((int)Math.Clamp(written * 100L / info.AssetSize, 0, 99));
                }
                // Windows cannot reliably hash a file while another writer still owns it.
                // Flush the async stream and then force the durable handle flush before verification.
                await output.FlushAsync(cancellationToken);
                output.Flush(flushToDisk: true);
            }

            if (new FileInfo(partial).Length != info.AssetSize)
                throw new InvalidDataException($"Application update download is incomplete ({new FileInfo(partial).Length:N0} of {info.AssetSize:N0} bytes). Reopen the launcher to resume it.");

            if (!await VerifyPackageFileAsync(partial, info, cancellationToken))
            {
                TryDelete(partial);
                throw new InvalidDataException("Downloaded application update failed SHA-256 verification. The corrupt cache entry was discarded.");
            }

            File.Move(partial, final, true);
            progress?.Report(100);
            CleanupOldUpdateCache(cache, final);
            return final;
        }

        throw new InvalidOperationException("Could not resume the application update download safely.");
    }

    public async Task StartStagedUpdateAsync(string package, AppUpdateInfo info, CancellationToken cancellationToken = default)
    {
        if (!info.Installable || string.IsNullOrWhiteSpace(info.Sha256))
            throw new InvalidOperationException("Cannot start an unverified application update.");

        string packagePath = EnsurePackagePath(package);
        if (!await VerifyPackageFileAsync(packagePath, info, cancellationToken))
            throw new InvalidDataException("The cached application update is no longer a verified release package.");

        string installedUpdater = FindUpdater();
        if (!File.Exists(installedUpdater))
            throw new FileNotFoundException("Miniscuplter updater executable is missing.", installedUpdater);

        string updaterRoot = Path.Combine(UpdateCacheRoot(), "updater");
        Directory.CreateDirectory(updaterRoot);
        RejectReparsePoints(DataRootPath(), updaterRoot);
        string stagedUpdater = Path.Combine(updaterRoot, $"Miniscuplter.Updater.{Guid.NewGuid():N}.exe");
        File.Copy(installedUpdater, stagedUpdater, true);

        string launcher = Environment.ProcessPath ?? Path.Combine(_settings.InstallRoot, "Miniscuplter.Launcher.exe");
        var psi = new ProcessStartInfo(stagedUpdater) { UseShellExecute = true, WorkingDirectory = updaterRoot };
        psi.ArgumentList.Add("--package"); psi.ArgumentList.Add(packagePath);
        psi.ArgumentList.Add("--target"); psi.ArgumentList.Add(_settings.InstallRoot);
        psi.ArgumentList.Add("--data-root"); psi.ArgumentList.Add(DataRootPath());
        psi.ArgumentList.Add("--version"); psi.ArgumentList.Add(info.LatestVersion);
        psi.ArgumentList.Add("--sha256"); psi.ArgumentList.Add(info.Sha256);
        psi.ArgumentList.Add("--wait-pid"); psi.ArgumentList.Add(Environment.ProcessId.ToString());
        psi.ArgumentList.Add("--restart"); psi.ArgumentList.Add(launcher);
        _ = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the staged updater.");
    }

    async Task<string> ResolveUpdateCacheAsync(AppUpdateInfo info, string safeVersion, CancellationToken cancellationToken)
    {
        var candidates = UpdateCacheCandidates().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        string fileName = $"Miniscuplter-{safeVersion}-win-x64.zip";
        string partialName = fileName + ".partial";

        foreach (string candidate in candidates)
        {
            string final = Path.Combine(candidate, fileName);
            if (File.Exists(final) && await VerifyPackageFileAsync(final, info, cancellationToken)) return candidate;
        }

        var resumable = candidates
            .Select(path => (Path: path, Existing: SafeLength(Path.Combine(path, partialName))))
            .Where(x => x.Existing > 0 && x.Existing <= info.AssetSize)
            .OrderByDescending(x => x.Existing);

        foreach (var item in resumable)
        {
            long needed = Math.Max(0, info.AssetSize - item.Existing) + UpdateSafetyBytes;
            if (AvailableBytes(item.Path) >= needed) return item.Path;
        }

        long freshRequired = info.AssetSize + UpdateSafetyBytes;
        foreach (string candidate in candidates)
            if (AvailableBytes(candidate) >= freshRequired) return candidate;

        string details = string.Join(Environment.NewLine,
            candidates.Select(path => $"  {path} — {FormatBytes(AvailableBytes(path))} free"));
        throw new IOException($"There is not enough free space in any safe Miniscuplter update cache location. Need about {FormatBytes(freshRequired)} free for the verified release download.{Environment.NewLine}{details}");
    }

    IEnumerable<string> UpdateCacheCandidates()
    {
        yield return UpdateCacheRoot();
    }

    string DataRootPath()
    {
        if (string.IsNullOrWhiteSpace(_settings.DataRoot))
            throw new InvalidOperationException("The Miniscuplter data root is not configured.");
        string root = Path.GetFullPath(_settings.DataRoot);
        Directory.CreateDirectory(root);
        RejectReparsePoints(root, root);
        return root;
    }

    string UpdateCacheRoot()
    {
        string root = Path.Combine(DataRootPath(), "update-cache");
        Directory.CreateDirectory(root);
        RejectReparsePoints(DataRootPath(), root);
        return root;
    }

    string EnsurePackagePath(string package)
    {
        if (string.IsNullOrWhiteSpace(package))
            throw new InvalidDataException("An update package path is required.");
        string cache = UpdateCacheRoot();
        string full = Path.GetFullPath(package);
        if (!IsWithin(cache, full) ||
            !(full.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
              full.EndsWith(".zip.partial", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Update packages must remain in the persistent Miniscuplter data update cache.");
        RejectReparsePoints(DataRootPath(), full);
        return full;
    }

    static long SafeLength(string path)
    {
        try { return File.Exists(path) ? new FileInfo(path).Length : 0; }
        catch { return 0; }
    }

    static long AvailableBytes(string path)
    {
        try
        {
            string full = Path.GetFullPath(path);
            string? root = Path.GetPathRoot(full);
            if (string.IsNullOrWhiteSpace(root)) return 0;
            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch { return 0; }
    }

    static void EnsureFreeSpace(string path, long requiredBytes, string operation)
    {
        long free = AvailableBytes(path);
        if (free < requiredBytes)
            throw new IOException($"Not enough free space for {operation} at {path}. Need about {FormatBytes(requiredBytes)}, but only {FormatBytes(free)} is available.");
    }

    static string FormatBytes(long bytes)
    {
        double gib = Math.Max(0, bytes) / 1073741824.0;
        return gib >= 0.1 ? $"{gib:0.00} GiB" : $"{Math.Max(0, bytes) / 1048576.0:0} MiB";
    }

    async Task<bool> VerifyPackageFileAsync(string path, AppUpdateInfo info, CancellationToken cancellationToken)
    {
        try
        {
            string safePath = EnsurePackagePath(path);
            if (!File.Exists(safePath) || new FileInfo(safePath).Length != info.AssetSize || string.IsNullOrWhiteSpace(info.Sha256))
                return false;
            string actual = await ComputeSha256Async(safePath, cancellationToken);
            return actual.Equals(info.Sha256, StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (InvalidDataException) { return false; }
    }

    static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, useAsync: true);
        byte[] buffer = new byte[1024 * 1024];
        while (true)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (n <= 0) break;
            hash.AppendData(buffer, 0, n);
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    static void CleanupOldUpdateCache(string cache, string keep)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(cache, "Miniscuplter-*-win-x64.zip*"))
                if (!Path.GetFullPath(file).Equals(Path.GetFullPath(keep), StringComparison.OrdinalIgnoreCase)) TryDelete(file);
        }
        catch { }
    }

    static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    static bool IsTrustedDownloadUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            return false;
        return uri.Port is -1 or 443;
    }

    static async Task<string> ReadTextAsync(HttpContent content, long maxBytes, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is long length && length > maxBytes)
            throw new InvalidDataException("The GitHub response is larger than the allowed metadata limit.");

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var builder = new StringBuilder();
        char[] buffer = new char[8192];
        while (true)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read <= 0) break;
            builder.Append(buffer, 0, read);
            if (builder.Length > maxBytes)
                throw new InvalidDataException("The GitHub response is larger than the allowed metadata limit.");
        }
        return builder.ToString();
    }

    static string? ParseDigestFile(string text)
    {
        foreach (string rawLine in text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();
            Match match = Regex.Match(line, @"^(?<hash>[0-9a-fA-F]{64})\s+(?:\*?)(?<name>\S+)\s*$");
            if (!match.Success) continue;
            string name = match.Groups["name"].Value.Replace('\\', '/');
            string fileName = name[(name.LastIndexOf('/') + 1)..];
            if (fileName.Equals(AssetName, StringComparison.OrdinalIgnoreCase))
                return NormalizeSha256(match.Groups["hash"].Value);
        }
        return null;
    }

    static bool IsWithin(string root, string candidate)
    {
        string relative;
        try { relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(candidate)); }
        catch { return false; }
        return !Path.IsPathRooted(relative) &&
            relative != ".." &&
            !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }

    static void RejectReparsePoints(string root, string candidate)
    {
        string rootFull = Path.GetFullPath(root);
        string candidateFull = Path.GetFullPath(candidate);
        if (!IsWithin(rootFull, candidateFull))
            throw new InvalidDataException("An updater path escapes the persistent Miniscuplter data root.");

        DirectoryInfo? current = new DirectoryInfo(rootFull);
        while (current != null)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Refusing a reparse point in the update data path: {current.FullName}");
            current = current.Parent;
        }

        string relative = Path.GetRelativePath(rootFull, candidateFull);
        current = new DirectoryInfo(rootFull);
        foreach (string part in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            current = new DirectoryInfo(Path.Combine(current.FullName, part));
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Refusing a reparse point in the update data path: {current.FullName}");
        }
    }

    string FindUpdater()
    {
        string[] candidates =
        {
            Path.Combine(_settings.InstallRoot, "Miniscuplter.Updater.exe"),
            Path.Combine(_settings.InstallRoot, "Updater", "Miniscuplter.Updater.exe"),
            Path.Combine(AppContext.BaseDirectory, "Miniscuplter.Updater.exe")
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    static string? NormalizeSha256(string value)
    {
        string digest = value.Trim().ToLowerInvariant();
        return digest.Length == 64 && digest.All(Uri.IsHexDigit) ? digest : null;
    }

    static string Normalize(string value) => value.Trim().TrimStart('v', 'V').Split('-', '+')[0];

    static int CompareVersions(string a, string b)
    {
        if (!Version.TryParse(Normalize(a), out var av)) av = new Version(0, 0, 0);
        if (!Version.TryParse(Normalize(b), out var bv)) bv = new Version(0, 0, 0);
        return av.CompareTo(bv);
    }
}
