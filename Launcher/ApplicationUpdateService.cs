using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
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
        Sha256 is { Length: 64 } digest && digest.All(Uri.IsHexDigit) && AssetSize > 0;
}

internal sealed class ApplicationUpdateService
{
    const string AssetName = "Miniscuplter-win-x64.zip";
    const string DigestAssetName = "Miniscuplter-win-x64.zip.sha256";
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(30) };
    readonly LauncherSettings _settings;

    public ApplicationUpdateService(LauncherSettings settings)
    {
        _settings = settings;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Miniscuplter-Launcher/1.0.6");
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

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
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
                    digestUrl = asset.TryGetProperty("browser_download_url", out var due) ? due.GetString() : null;
            }
        }

        if (sha256 == null && !string.IsNullOrWhiteSpace(digestUrl))
        {
            try
            {
                string text = await _http.GetStringAsync(digestUrl, cancellationToken);
                var match = Regex.Match(text, @"(?i)\b[0-9a-f]{64}\b");
                if (match.Success) sha256 = NormalizeSha256(match.Value);
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
            try { return processes.Any(p => p.Id != Environment.ProcessId && !p.HasExited); }
            finally { foreach (var p in processes) p.Dispose(); }
        }
        catch { return false; }
    }

    public async Task<string> DownloadPackageAsync(AppUpdateInfo info, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!info.Installable)
            throw new InvalidOperationException("The newest release is not safely installable: the exact Windows ZIP, its byte size, and a SHA-256 digest are all required.");

        string cache = Path.Combine(_settings.DataRoot, "update-cache");
        Directory.CreateDirectory(cache);
        string safeVersion = Regex.Replace(info.LatestVersion, @"[^0-9A-Za-z._-]", "_");
        string partial = Path.Combine(cache, $"Miniscuplter-{safeVersion}-win-x64.zip.partial");
        string final = Path.Combine(cache, $"Miniscuplter-{safeVersion}-win-x64.zip");

        if (File.Exists(final))
        {
            if (await VerifyPackageFileAsync(final, info, cancellationToken)) { progress?.Report(100); return final; }
            TryDelete(final);
        }

        for (int attempt = 0; attempt < 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long existing = File.Exists(partial) ? new FileInfo(partial).Length : 0;
            if (existing < 0 || existing > info.AssetSize) { TryDelete(partial); existing = 0; }
            if (existing == info.AssetSize && await VerifyPackageFileAsync(partial, info, cancellationToken))
            {
                File.Move(partial, final, true); progress?.Report(100); CleanupOldUpdateCache(cache, final); return final;
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
            if (!resumed) existing = 0;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(partial, resumed ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 128, useAsync: true);
            byte[] buffer = new byte[1024 * 128];
            long written = existing;
            while (true)
            {
                int n = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (n <= 0) break;
                await output.WriteAsync(buffer.AsMemory(0, n), cancellationToken);
                written += n;
                progress?.Report((int)Math.Clamp(written * 100L / info.AssetSize, 0, 99));
            }
            await output.FlushAsync(cancellationToken);

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

    public void StartStagedUpdate(string package, AppUpdateInfo info)
    {
        if (!info.Installable || string.IsNullOrWhiteSpace(info.Sha256))
            throw new InvalidOperationException("Cannot start an unverified application update.");
        string installedUpdater = FindUpdater();
        if (!File.Exists(installedUpdater)) throw new FileNotFoundException("Miniscuplter updater executable is missing.", installedUpdater);
        string tempUpdater = Path.Combine(Path.GetTempPath(), $"Miniscuplter.Updater.{Guid.NewGuid():N}.exe");
        File.Copy(installedUpdater, tempUpdater, true);

        string launcher = Environment.ProcessPath ?? Path.Combine(_settings.InstallRoot, "Miniscuplter.Launcher.exe");
        var psi = new ProcessStartInfo(tempUpdater) { UseShellExecute = true, WorkingDirectory = Path.GetTempPath() };
        psi.ArgumentList.Add("--package"); psi.ArgumentList.Add(package);
        psi.ArgumentList.Add("--target"); psi.ArgumentList.Add(_settings.InstallRoot);
        psi.ArgumentList.Add("--data-root"); psi.ArgumentList.Add(_settings.DataRoot);
        psi.ArgumentList.Add("--version"); psi.ArgumentList.Add(info.LatestVersion);
        psi.ArgumentList.Add("--sha256"); psi.ArgumentList.Add(info.Sha256);
        psi.ArgumentList.Add("--wait-pid"); psi.ArgumentList.Add(Environment.ProcessId.ToString());
        psi.ArgumentList.Add("--restart"); psi.ArgumentList.Add(launcher);
        _ = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the staged updater.");
    }

    async Task<bool> VerifyPackageFileAsync(string path, AppUpdateInfo info, CancellationToken cancellationToken)
    {
        if (!File.Exists(path) || new FileInfo(path).Length != info.AssetSize || string.IsNullOrWhiteSpace(info.Sha256)) return false;
        string actual = await ComputeSha256Async(path, cancellationToken);
        return actual.Equals(info.Sha256, StringComparison.OrdinalIgnoreCase);
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

    static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

    string FindUpdater()
    {
        string[] candidates = { Path.Combine(_settings.InstallRoot, "Miniscuplter.Updater.exe"), Path.Combine(_settings.InstallRoot, "Updater", "Miniscuplter.Updater.exe"), Path.Combine(AppContext.BaseDirectory, "Miniscuplter.Updater.exe") };
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
