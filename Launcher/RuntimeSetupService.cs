using System.Diagnostics;

namespace Miniscuplter.Launcher;

internal sealed class RuntimeSetupService
{
    readonly LauncherSettings _settings;
    public RuntimeSetupService(LauncherSettings settings) => _settings = settings;

    public async Task<string> RepairAsync()
    {
        string script = Path.Combine(_settings.InstallRoot, "setup_ai_backend.bat");
        if (!File.Exists(script))
        {
            string nested = Path.Combine(_settings.InstallRoot, "App", "setup_ai_backend.bat");
            if (File.Exists(nested)) script = nested;
        }
        if (!File.Exists(script)) throw new FileNotFoundException("AI runtime setup script is missing.", script);

        var psi = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(script) ?? _settings.InstallRoot
        };
        psi.ArgumentList.Add("/c"); psi.ArgumentList.Add(script); psi.ArgumentList.Add("/quiet");
        psi.Environment["MINISCULPTER_ROOT"] = _settings.InstallRoot;
        psi.Environment["MINISCULPTER_DATA"] = _settings.DataRoot;
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start AI runtime setup.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(); Task<string> stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        string output = await stdout; string error = await stderr;
        if (process.ExitCode == 2) throw new InvalidOperationException("Python 3.10 x64 is required for the current local-AI runtime. Install Python 3.10 x64, then click Repair AI Runtime again.");
        if (process.ExitCode != 0) throw new InvalidOperationException((string.IsNullOrWhiteSpace(error) ? output : error).Trim());
        return string.IsNullOrWhiteSpace(output) ? "AI runtime repaired successfully." : output.Trim().Split('\n').LastOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim() ?? "AI runtime repaired successfully.";
    }
}
