using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Miniscuplter.Launcher;

internal sealed record HardwareSnapshot(string? Gpu, int VramMb, bool CudaAvailable, string RecommendedProfile, string Platform, string Python);
internal sealed record ModelSnapshot(string Id, string Name, string Kind, bool Installed, bool UpdateAvailable, string? InstalledRevision, string? RemoteRevision, double EstimatedGb, string Description, string? Path, string? UpdateError);
internal sealed record ModelStatusSnapshot(HardwareSnapshot Hardware, List<ModelSnapshot> Models, string DataRoot, double FreeGb, double TotalGb);
internal sealed record ModelOperationEvent(DateTimeOffset Timestamp, string Stream, string Message);

internal sealed class ModelService
{
    readonly LauncherSettings _settings;
    readonly string? _python;
    readonly string _bridge;
    readonly bool _runtimeCurrent;
    readonly string _runtimeMessage;

    public ModelService(LauncherSettings settings)
    {
        _settings = settings;
        _python = InstallLayout.ResolvePython(settings);
        _bridge = Path.Combine(InstallLayout.ResolveBackendRoot(settings), "launcher_bridge.py");
        (_runtimeCurrent, _runtimeMessage) = CheckRuntimeFingerprint();
    }

    public bool IsAvailable => _python != null && File.Exists(_bridge) && _runtimeCurrent;
    public string AvailabilityMessage => _python == null ? "Python runtime was not found. Click Repair AI Runtime."
        : !File.Exists(_bridge) ? $"Launcher bridge was not found: {_bridge}"
        : !_runtimeCurrent ? _runtimeMessage
        : "Ready";

    (bool Current, string Message) CheckRuntimeFingerprint()
    {
        try
        {
            string backend = InstallLayout.ResolveBackendRoot(_settings);
            string requirements = Path.Combine(backend, "requirements.txt");
            string setup = Path.Combine(_settings.InstallRoot, "setup_ai_backend.bat");
            string marker = Path.Combine(backend, ".venv", "miniscuplter_runtime.sha256");
            if (!File.Exists(requirements) || !File.Exists(setup))
                return (false, "AI runtime source files are incomplete. Repair or reinstall Miniscuplter.");
            if (!File.Exists(marker))
                return (false, "AI runtime has no v1.0 compatibility fingerprint. Click Repair AI Runtime before managing or using local models.");

            byte[] a = File.ReadAllBytes(requirements), b = File.ReadAllBytes(setup);
            byte[] combined = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, combined, 0, a.Length); Buffer.BlockCopy(b, 0, combined, a.Length, b.Length);
            string expected = Convert.ToHexString(SHA256.HashData(combined)).ToLowerInvariant();
            string installed = File.ReadAllText(marker).Trim().ToLowerInvariant();
            return expected.Equals(installed, StringComparison.OrdinalIgnoreCase)
                ? (true, "Ready")
                : (false, "AI runtime belongs to a different application/backend revision. Click Repair AI Runtime to update dependencies; downloaded model weights are preserved.");
        }
        catch (Exception ex)
        {
            return (false, "AI runtime compatibility could not be verified. Click Repair AI Runtime. " + ex.Message);
        }
    }

    void EnsureEditorClosedForMutation()
    {
        try
        {
            string app = InstallLayout.ResolveApp(_settings);
            string processName = Path.GetFileNameWithoutExtension(app);
            var processes = Process.GetProcessesByName(processName);
            try
            {
                if (processes.Any(p => p.Id != Environment.ProcessId && !p.HasExited))
                    throw new InvalidOperationException("Close the Miniscuplter editor before installing, removing, or updating AI models. This prevents changing model files while the backend may be using them.");
            }
            finally { foreach (var p in processes) p.Dispose(); }
        }
        catch (InvalidOperationException) { throw; }
        catch
        {
            // If process enumeration itself is unavailable, let the model manager's transactional
            // file operations provide the final safety boundary rather than blocking the launcher.
        }
    }

    ProcessStartInfo CreateBridgeStartInfo(params string[] args)
    {
        if (_python == null) throw new InvalidOperationException("Python runtime is not installed or could not be located.");
        if (!File.Exists(_bridge)) throw new FileNotFoundException("AI launcher bridge is missing.", _bridge);
        if (!_runtimeCurrent) throw new InvalidOperationException(_runtimeMessage);
        var psi = new ProcessStartInfo(_python)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(_bridge)!
        };
        psi.ArgumentList.Add(_bridge);
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment["MINISCULPTER_ROOT"] = _settings.InstallRoot;
        psi.Environment["MINISCULPTER_DATA"] = _settings.DataRoot;
        psi.Environment["PYTHONUNBUFFERED"] = "1";
        return psi;
    }

    async Task<JsonDocument> RunAsync(params string[] args)
    {
        var psi = CreateBridgeStartInfo(args);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the AI model manager.");

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(process.WaitForExitAsync(), stdoutTask, stderrTask);
        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            string detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException(detail.Trim());
        }
        string text = LastJsonLine(stdout);
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Model manager returned an empty response.");
        return JsonDocument.Parse(text);
    }

    public async Task RunModelOperationAsync(string action, string id, IProgress<ModelOperationEvent>? progress = null, CancellationToken cancellationToken = default)
    {
        if (action is not ("install" or "remove" or "update")) throw new ArgumentOutOfRangeException(nameof(action));
        EnsureEditorClosedForMutation();
        var psi = CreateBridgeStartInfo(action, id);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the AI model manager.");
        var stdoutLines = new List<string>();
        var stderrLines = new List<string>();
        object gate = new();

        async Task PumpAsync(StreamReader reader, string stream, List<string> sink)
        {
            char[] buffer = new char[1024];
            var line = new StringBuilder();
            while (true)
            {
                int n = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (n <= 0) break;
                for (int i = 0; i < n; i++)
                {
                    char c = buffer[i];
                    if (c == '\r' || c == '\n')
                    {
                        if (line.Length == 0) continue;
                        string text = line.ToString(); line.Clear();
                        lock (gate) { sink.Add(text); if (sink.Count > 250) sink.RemoveRange(0, sink.Count - 250); }
                        if (!LooksLikeResultJson(text)) progress?.Report(new ModelOperationEvent(DateTimeOffset.Now, stream, text));
                    }
                    else line.Append(c);
                }
            }
            if (line.Length > 0)
            {
                string text = line.ToString();
                lock (gate) { sink.Add(text); }
                if (!LooksLikeResultJson(text)) progress?.Report(new ModelOperationEvent(DateTimeOffset.Now, stream, text));
            }
        }

        Task stdoutTask = PumpAsync(process.StandardOutput, "stdout", stdoutLines);
        Task stderrTask = PumpAsync(process.StandardError, "stderr", stderrLines);
        try
        {
            await Task.WhenAll(process.WaitForExitAsync(cancellationToken), stdoutTask, stderrTask);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            throw;
        }

        if (process.ExitCode != 0)
        {
            string detail;
            lock (gate)
            {
                IEnumerable<string> source = stderrLines.Count > 0 ? stderrLines : stdoutLines;
                detail = string.Join(Environment.NewLine, source.TakeLast(60));
            }
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail) ? $"Model manager exited with code {process.ExitCode}." : detail.Trim());
        }
    }

    static bool LooksLikeResultJson(string line)
    {
        string t = line.Trim();
        return t.StartsWith('{') && t.EndsWith('}');
    }

    static string LastJsonLine(string stdout)
    {
        foreach (string line in stdout.Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Reverse())
        {
            if (LooksLikeResultJson(line)) return line;
        }
        return stdout.Trim();
    }

    public async Task<ModelStatusSnapshot> GetStatusAsync(bool checkUpdates)
    {
        using var doc = await RunAsync("status", checkUpdates ? "--updates" : "--no-updates");
        var root = doc.RootElement;
        var hw = root.GetProperty("hardware");
        var hardware = new HardwareSnapshot(
            hw.TryGetProperty("gpu", out var gp) && gp.ValueKind != JsonValueKind.Null ? gp.GetString() : null,
            hw.TryGetProperty("vram_mb", out var vm) ? vm.GetInt32() : 0,
            hw.TryGetProperty("cuda_available", out var ca) && ca.GetBoolean(),
            hw.TryGetProperty("recommended_profile", out var rp) ? rp.GetString() ?? "unknown" : "unknown",
            hw.TryGetProperty("platform", out var pf) ? pf.GetString() ?? "unknown" : "unknown",
            hw.TryGetProperty("python", out var py) ? py.GetString() ?? "unknown" : "unknown");
        var models = new List<ModelSnapshot>();
        foreach (var e in root.GetProperty("components").EnumerateArray())
        {
            models.Add(new ModelSnapshot(
                e.GetProperty("id").GetString() ?? "unknown",
                e.GetProperty("name").GetString() ?? "AI model",
                e.TryGetProperty("kind", out var kind) ? kind.GetString() ?? "unknown" : "unknown",
                e.TryGetProperty("installed", out var ins) && ins.GetBoolean(),
                e.TryGetProperty("update_available", out var up) && up.GetBoolean(),
                e.TryGetProperty("installed_revision", out var ir) && ir.ValueKind != JsonValueKind.Null ? ir.GetString() : null,
                e.TryGetProperty("remote_revision", out var rr) && rr.ValueKind != JsonValueKind.Null ? rr.GetString() : null,
                e.TryGetProperty("estimated_gb", out var gb) ? gb.GetDouble() : 0,
                e.TryGetProperty("description", out var de) ? de.GetString() ?? "" : "",
                e.TryGetProperty("path", out var pa) && pa.ValueKind != JsonValueKind.Null ? pa.GetString() : null,
                e.TryGetProperty("update_error", out var ue) && ue.ValueKind != JsonValueKind.Null ? ue.GetString() : null));
        }
        var disk = root.GetProperty("disk");
        return new ModelStatusSnapshot(hardware, models, root.GetProperty("data_root").GetString() ?? _settings.DataRoot,
            disk.TryGetProperty("free_gb", out var f) ? f.GetDouble() : 0,
            disk.TryGetProperty("total_gb", out var t) ? t.GetDouble() : 0);
    }

    public Task InstallAsync(string id) => RunModelOperationAsync("install", id);
    public Task RemoveAsync(string id) => RunModelOperationAsync("remove", id);
    public Task UpdateAsync(string id) => RunModelOperationAsync("update", id);
}
