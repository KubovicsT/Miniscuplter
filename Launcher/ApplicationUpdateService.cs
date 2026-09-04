using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Miniscuplter.Launcher;

internal sealed record AppUpdateInfo(bool Available, string CurrentVersion, string LatestVersion, string? DownloadUrl, string? Sha256, string? ReleasePage, string? Notes);

internal sealed class ApplicationUpdateService
{
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(30) };
    readonly LauncherSettings _settings;

    public ApplicationUpdateService(LauncherSettings settings)
    {
        _settings = settings;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Miniscuplter-Launcher/1.0.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public async Task<AppUpdateInfo> CheckAsync()
    {
        string current = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        string endpoint = $"https://api.github.com/repos/{_settings.ReleaseRepository}/releases/latest";
        using var response = await _http.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode == 404) return new AppUpdateInfo(false, current, current, null, null, null, "No published release is available yet.");
            throw new InvalidOperationException($"Application update check failed: HTTP {(int)response.StatusCode}");
        }
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        string tag = root.TryGetProperty("tag_name", out var te) ? te.GetString() ?? "0.0.0" : "0.0.0";
        string latest = Normalize(tag);
        string? page = root.TryGetProperty("html_url", out var he) ? he.GetString() : null;
        string? notes = root.TryGetProperty("body", out var be) && be.ValueKind != JsonValueKind.Null ? be.GetString() : null;
        string? download = null, sha256 = null;
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.TryGetProperty("name", out var ne) ? ne.GetString() ?? "" : "";
                if (!name.Equals("Miniscuplter-win-x64.zip", StringComparison.OrdinalIgnoreCase)) continue;
                download = asset.TryGetProperty("browser_download_url", out var ue) ? ue.GetString() : null;
                if (asset.TryGetProperty("digest", out var de) && de.ValueKind == JsonValueKind.String)
                {
                    string digest = de.GetString() ?? "";
                    if (digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) && digest.Length > 7)
                        sha256 = digest[7..].Trim().ToLowerInvariant();
                }
                break;
            }
        }
        bool available = CompareVersions(latest, current) > 0;
        return new AppUpdateInfo(available, current, latest, download, sha256, page, notes);
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

    public async Task<string> DownloadPackageAsync(AppUpdateInfo info, IProgress<int>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(info.DownloadUrl)) throw new InvalidOperationException("The latest release has no Miniscuplter-win-x64.zip asset. The launcher will not substitute an unrelated ZIP; use the release installer/page instead.");
        string path = Path.Combine(Path.GetTempPath(), $"Miniscuplter-{info.LatestVersion}-{Guid.NewGuid():N}.zip");
        using var response = await _http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        long total = response.Content.Headers.ContentLength ?? -1;
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = File.Create(path);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[1024 * 128]; long read = 0; int n;
        while ((n = await input.ReadAsync(buffer)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, n));
            hash.AppendData(buffer, 0, n); read += n;
            if (total > 0) progress?.Report((int)Math.Clamp(read * 100 / total, 0, 100));
        }
        await output.FlushAsync();
        string actual = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(info.Sha256) && !actual.Equals(info.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(path); } catch { }
            throw new InvalidDataException("Downloaded application update failed SHA-256 verification. The update was discarded.");
        }
        if (new FileInfo(path).Length == 0) { File.Delete(path); throw new InvalidDataException("Downloaded application update is empty."); }
        progress?.Report(100); return path;
    }

    public void StartStagedUpdate(string package)
    {
        string installedUpdater = FindUpdater();
        if (!File.Exists(installedUpdater)) throw new FileNotFoundException("Miniscuplter updater executable is missing.", installedUpdater);
        string tempUpdater = Path.Combine(Path.GetTempPath(), $"Miniscuplter.Updater.{Guid.NewGuid():N}.exe");
        File.Copy(installedUpdater, tempUpdater, true);

        string launcher = Environment.ProcessPath ?? Path.Combine(_settings.InstallRoot, "Miniscuplter.Launcher.exe");
        var psi = new ProcessStartInfo(tempUpdater) { UseShellExecute = true, WorkingDirectory = Path.GetTempPath() };
        psi.ArgumentList.Add("--package"); psi.ArgumentList.Add(package);
        psi.ArgumentList.Add("--target"); psi.ArgumentList.Add(_settings.InstallRoot);
        psi.ArgumentList.Add("--wait-pid"); psi.ArgumentList.Add(Environment.ProcessId.ToString());
        psi.ArgumentList.Add("--restart"); psi.ArgumentList.Add(launcher);
        _ = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the staged updater.");
    }

    string FindUpdater()
    {
        string[] candidates = { Path.Combine(_settings.InstallRoot, "Miniscuplter.Updater.exe"), Path.Combine(_settings.InstallRoot, "Updater", "Miniscuplter.Updater.exe"), Path.Combine(AppContext.BaseDirectory, "Miniscuplter.Updater.exe") };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    static string Normalize(string value) => value.Trim().TrimStart('v', 'V').Split('-', '+')[0];
    static int CompareVersions(string a, string b)
    {
        if (!Version.TryParse(Normalize(a), out var av)) av = new Version(0, 0, 0);
        if (!Version.TryParse(Normalize(b), out var bv)) bv = new Version(0, 0, 0);
        return av.CompareTo(bv);
    }
}
