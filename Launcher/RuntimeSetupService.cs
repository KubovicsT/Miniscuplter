using System.Diagnostics;
using System.Text;

namespace Miniscuplter.Launcher;

internal sealed record RuntimeSetupEvent(DateTimeOffset Timestamp, string Stream, string Message);

internal sealed class RuntimeSetupService
{
    readonly LauncherSettings _settings;
    public RuntimeSetupService(LauncherSettings settings) => _settings = settings;

    public async Task<string> RepairAsync(IProgress<RuntimeSetupEvent>? progress = null, CancellationToken cancellationToken = default)
    {
        string script = Path.Combine(_settings.InstallRoot, "setup_ai_backend.bat");
        if (!File.Exists(script))
        {
            string nested = Path.Combine(_settings.InstallRoot, "App", "setup_ai_backend.bat");
            if (File.Exists(nested)) script = nested;
        }
        if (!File.Exists(script)) throw new FileNotFoundException("AI runtime setup script is missing.", script);

        // Runtime repair can download multi-gigabyte PyTorch wheels and install many Python
        // packages. Stream the real command output into the launcher's progress dialog. Large
        // downloads are cached by setup_ai_backend.bat, so cancellation/retry remains resumable.
        var psi = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(script) ?? _settings.InstallRoot
        };
        psi.ArgumentList.Add("/d");
        psi.ArgumentList.Add("/s");
        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(script);
        psi.ArgumentList.Add("/quiet");
        psi.Environment["MINISCULPTER_ROOT"] = _settings.InstallRoot;
        psi.Environment["MINISCULPTER_DATA"] = _settings.DataRoot;
        psi.Environment["PYTHONUNBUFFERED"] = "1";

        using var process = OwnedChildProcessJob.Start(psi);

        async Task PumpAsync(StreamReader reader, string stream)
        {
            char[] buffer = new char[1024];
            var line = new StringBuilder();
            while (true)
            {
                int n = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (n <= 0) break;
                for (int i = 0; i < n; i++)
                {
                    char c = buffer[i];
                    if (c is '\r' or '\n')
                    {
                        if (line.Length == 0) continue;
                        progress?.Report(new RuntimeSetupEvent(DateTimeOffset.Now, stream, line.ToString()));
                        line.Clear();
                    }
                    else line.Append(c);
                }
            }
            if (line.Length > 0) progress?.Report(new RuntimeSetupEvent(DateTimeOffset.Now, stream, line.ToString()));
        }

        Task stdoutTask = PumpAsync(process.StandardOutput, "stdout");
        Task stderrTask = PumpAsync(process.StandardError, "stderr");
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            try { await process.WaitForExitAsync(); } catch { }
            await Task.WhenAll(stdoutTask, stderrTask);
            throw;
        }

        await Task.WhenAll(stdoutTask, stderrTask);
        cancellationToken.ThrowIfCancellationRequested();
        if (process.ExitCode == 2) throw new InvalidOperationException("Python 3.10 x64 is required for the current local-AI runtime. Install Python 3.10 x64, then click Repair AI Runtime again.");
        if (process.ExitCode != 0) throw new InvalidOperationException($"AI runtime setup exited with code {process.ExitCode}. See the setup log for the failing command.");
        return "AI runtime repaired successfully. Xet/model download support and runtime dependencies were verified.";
    }
}