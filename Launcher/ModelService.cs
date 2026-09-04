using System.Diagnostics;
using System.Text.Json;

namespace Miniscuplter.Launcher;

internal sealed record HardwareSnapshot(string? Gpu, int VramMb, bool CudaAvailable, string RecommendedProfile, string Platform, string Python);
internal sealed record ModelSnapshot(string Id, string Name, string Kind, bool Installed, bool UpdateAvailable, string? InstalledRevision, string? RemoteRevision, double EstimatedGb, string Description, string? Path, string? UpdateError);
internal sealed record ModelStatusSnapshot(HardwareSnapshot Hardware, List<ModelSnapshot> Models, string DataRoot, double FreeGb, double TotalGb);

internal sealed class ModelService
{
    readonly LauncherSettings _settings;
    readonly string? _python;
    readonly string _bridge;

    public ModelService(LauncherSettings settings)
    {
        _settings = settings;
        _python = InstallLayout.ResolvePython(settings);
        _bridge = Path.Combine(InstallLayout.ResolveBackendRoot(settings), "launcher_bridge.py");
    }

    public bool IsAvailable => _python != null && File.Exists(_bridge);
    public string AvailabilityMessage => _python == null ? "Python runtime was not found." : !File.Exists(_bridge) ? $"Launcher bridge was not found: {_bridge}" : "Ready";

    async Task<JsonDocument> RunAsync(params string[] args)
    {
        if (_python == null) throw new InvalidOperationException("Python runtime is not installed or could not be located.");
        if (!File.Exists(_bridge)) throw new FileNotFoundException("AI launcher bridge is missing.", _bridge);
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
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the AI model manager.");
        string stdoutTask = await process.StandardOutput.ReadToEndAsync();
        string stderrTask = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            string detail = string.IsNullOrWhiteSpace(stderrTask) ? stdoutTask : stderrTask;
            throw new InvalidOperationException(detail.Trim());
        }
        string text = stdoutTask.Trim();
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Model manager returned an empty response.");
        return JsonDocument.Parse(text);
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

    public async Task InstallAsync(string id) { using var _ = await RunAsync("install", id); }
    public async Task RemoveAsync(string id) { using var _ = await RunAsync("remove", id); }
    public async Task UpdateAsync(string id) { using var _ = await RunAsync("update", id); }
}
