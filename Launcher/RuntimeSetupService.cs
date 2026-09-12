using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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

        progress?.Report(new RuntimeSetupEvent(DateTimeOffset.Now, "verify", "Starting the repaired backend with its validated virtual-environment interpreter..."));
        await VerifyBackendHealthAsync(progress, cancellationToken);
        return "AI runtime repaired successfully. The exact repaired backend environment started and answered its health check.";
    }

    async Task VerifyBackendHealthAsync(IProgress<RuntimeSetupEvent>? progress, CancellationToken cancellationToken)
    {
        string[] backendCandidates =
        {
            Path.Combine(_settings.InstallRoot, "ai_backend"),
            Path.Combine(_settings.InstallRoot, "App", "ai_backend")
        };
        string? backendDir = Array.Find(backendCandidates, dir => File.Exists(Path.Combine(dir, "app.py")) && File.Exists(Path.Combine(dir, "serve.py")));
        if (backendDir == null)
            throw new InvalidOperationException("Repair completed, but the packaged AI backend server entry point could not be found for startup verification.");

        string app = Path.Combine(backendDir, "app.py");
        string server = Path.Combine(backendDir, "serve.py");
        string python = Path.Combine(backendDir, ".venv", "Scripts", "python.exe");
        if (!File.Exists(python))
            throw new InvalidOperationException($"Repair completed, but its validated interpreter is missing: {python}");

        int smokePort = ReserveLoopbackPort();
        string instanceToken = Guid.NewGuid().ToString("N");
        string expectedVersion = typeof(RuntimeSetupService).Assembly.GetName().Version?.ToString(3) ?? "";
        var psi = new ProcessStartInfo(python)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = backendDir
        };
        psi.ArgumentList.Add(server);
        psi.ArgumentList.Add("--host");
        psi.ArgumentList.Add("127.0.0.1");
        psi.ArgumentList.Add("--port");
        psi.ArgumentList.Add(smokePort.ToString());
        psi.ArgumentList.Add("--instance-token");
        psi.ArgumentList.Add(instanceToken);
        psi.Environment["MINISCULPTER_ROOT"] = _settings.InstallRoot;
        psi.Environment["MINISCULPTER_DATA"] = _settings.DataRoot;
        psi.Environment["MINISCULPTER_PARENT_PID"] = Environment.ProcessId.ToString();
        psi.Environment["PYTHONUNBUFFERED"] = "1";

        using var backend = OwnedChildProcessJob.Start(psi);
        Task<string> stdoutTask = backend.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = backend.StandardError.ReadToEndAsync();
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (backend.HasExited)
                {
                    string stderr = await stderrTask;
                    throw new InvalidOperationException($"Repaired AI backend exited before health validation (exit {backend.ExitCode}). Backend: {app}; interpreter: {python}; recent stderr: {Tail(stderr, 4000)}");
                }

                try
                {
                    using var response = await http.GetAsync($"http://127.0.0.1:{smokePort}/health", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
                        using var doc = JsonDocument.Parse(payload);
                        JsonElement root = doc.RootElement;
                        string version = root.TryGetProperty("version", out var versionNode) ? versionNode.GetString() ?? "" : "";
                        string token = root.TryGetProperty("instance_token", out var tokenNode) ? tokenNode.GetString() ?? "" : "";
                        if (version == expectedVersion && token == instanceToken)
                        {
                            progress?.Report(new RuntimeSetupEvent(DateTimeOffset.Now, "verify", $"Backend {version} health verified on isolated loopback port with interpreter {python}."));
                            return;
                        }
                    }
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }

                await Task.Delay(250, cancellationToken);
            }

            throw new TimeoutException($"Repaired AI backend did not answer health within 30 seconds. Expected backend version {expectedVersion}. Backend: {app}; interpreter: {python}; recent stderr: {Tail(await CompletedTextAsync(stderrTask), 4000)}");
        }
        finally
        {
            try { if (!backend.HasExited) backend.Kill(entireProcessTree: true); } catch { }
            try { await backend.WaitForExitAsync(); } catch { }
            try { await Task.WhenAll(stdoutTask, stderrTask); } catch { }
        }
    }

    static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    static async Task<string> CompletedTextAsync(Task<string> task)
    {
        if (task.IsCompleted) return await task;
        return "<backend still running; stderr stream not yet complete>";
    }

    static string Tail(string value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value)) return "<none captured>";
        string text = value.Trim();
        return text.Length <= maxChars ? text : text[^maxChars..];
    }
}
