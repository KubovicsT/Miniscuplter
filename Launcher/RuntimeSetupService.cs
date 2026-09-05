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

        // Runtime repair can download multi-gigabyte PyTorch wheels and install many Python
        // packages. Keep a real console visible so users can see progress instead of staring
        // at a frozen-looking launcher. The child is still contained by the launcher Job Object.
        var psi = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            WorkingDirectory = Path.GetDirectoryName(script) ?? _settings.InstallRoot
        };
        psi.ArgumentList.Add("/c"); psi.ArgumentList.Add(script); psi.ArgumentList.Add("/quiet");
        psi.Environment["MINISCULPTER_ROOT"] = _settings.InstallRoot;
        psi.Environment["MINISCULPTER_DATA"] = _settings.DataRoot;
        using var process = OwnedChildProcessJob.Start(psi);
        await process.WaitForExitAsync();
        if (process.ExitCode == 2) throw new InvalidOperationException("Python 3.10 x64 is required for the current local-AI runtime. Install Python 3.10 x64, then click Repair AI Runtime again.");
        if (process.ExitCode != 0) throw new InvalidOperationException($"AI runtime setup exited with code {process.ExitCode}. See the setup console above for the failing command.");
        return "AI runtime repaired successfully. Xet/model download support and runtime dependencies were verified.";
    }
}
