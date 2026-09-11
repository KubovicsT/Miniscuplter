using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class BackendLauncher : Node
{
    const uint JobObjectExtendedLimitInformation = 9;
    const uint JobObjectLimitKillOnJobClose = 0x00002000;
    const int RecentStderrLimit = 20;

    readonly object _processLock = new();
    readonly HttpClient _readinessHttp = new() { Timeout = TimeSpan.FromSeconds(2) };
    readonly object _logLock = new();
    readonly Queue<string> _recentStderr = new();
    Process? _backend;
    IntPtr _job = IntPtr.Zero;
    string? _backendPath;
    string? _pythonPath;
    string? _instanceToken;

    public override void _Ready()
    {
        StartBackend();
        _ = ReportInitialReadinessAsync();
    }

    /// <summary>
    /// Cancelling an HttpClient request does not stop a synchronous FastAPI inference handler.
    /// Miniscuplter owns the local backend process tree, so a user cancellation restarts that
    /// process tree to guarantee the abandoned CUDA/model job is actually gone before another
    /// job is allowed to start.
    /// </summary>
    public async Task RestartAsync()
    {
        lock (_processLock)
        {
            ShutdownBackendLocked();
            StartBackendLocked();
        }
        await WaitForBackendReadyAsync(TimeSpan.FromSeconds(30));
    }

    public bool IsRunning
    {
        get
        {
            lock (_processLock)
            {
                try { return _backend != null && !_backend.HasExited; }
                catch { return false; }
            }
        }
    }

    void StartBackend()
    {
        lock (_processLock) StartBackendLocked();
    }

    void StartBackendLocked()
    {
        if (_backend != null)
        {
            try { if (!_backend.HasExited) return; }
            catch { }
            try { _backend.Dispose(); } catch { }
            _backend = null;
        }

        string root = Environment.GetEnvironmentVariable("MINISCULPTER_ROOT") ?? "";
        if (string.IsNullOrWhiteSpace(root))
            root = AppDataRoot.InstallRoot;

        string[] backendCandidates =
        {
            Path.Combine(root, "ai_backend"),
            Path.Combine(root, "App", "ai_backend"),
            ProjectSettings.GlobalizePath("res://ai_backend")
        };
        string? backendDir = Array.Find(backendCandidates, dir => File.Exists(Path.Combine(dir, "app.py")) && File.Exists(Path.Combine(dir, "serve.py")));
        if (backendDir == null)
        {
            GD.Print("AI backend files were not found; editor remains usable without AI.");
            return;
        }
        string app = Path.Combine(backendDir, "app.py");
        string server = Path.Combine(backendDir, "serve.py");

        // Repair AI Runtime creates and validates the virtual environment beside app.py.
        // Launch that exact environment. Do not prefer a separate embedded/system Python or
        // silently fall back to PATH, because that can make Repair report success while the
        // editor starts the backend with a different, unvalidated interpreter.
        string python = Path.Combine(backendDir, ".venv", "Scripts", "python.exe");
        _backendPath = Path.GetFullPath(app);
        _pythonPath = Path.GetFullPath(python);
        lock (_logLock) _recentStderr.Clear();

        if (!File.Exists(python))
        {
            GD.PrintErr($"AI backend runtime is not repaired. Backend: {_backendPath}; expected interpreter: {_pythonPath}. Use Repair AI Runtime in Miniscuplter Launcher.");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo(python)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(app) ?? root
            };
            _instanceToken = Guid.NewGuid().ToString("N");
            psi.ArgumentList.Add(server);
            psi.ArgumentList.Add("--host");
            psi.ArgumentList.Add("127.0.0.1");
            psi.ArgumentList.Add("--port");
            psi.ArgumentList.Add("7868");
            psi.ArgumentList.Add("--instance-token");
            psi.ArgumentList.Add(_instanceToken);
            psi.Environment["MINISCULPTER_ROOT"] = root;
            AppDataRoot.ApplyEnvironment(psi.Environment);
            psi.Environment["MINISCULPTER_PARENT_PID"] = Environment.ProcessId.ToString();

            _backend = Process.Start(psi);
            if (_backend == null)
            {
                GD.PrintErr(BuildBackendFailure("Could not start the AI backend process."));
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                _job = CreateKillOnCloseJob();
                if (_job == IntPtr.Zero || !AssignProcessToJobObject(_job, _backend.Handle))
                {
                    int error = Marshal.GetLastWin32Error();
                    GD.PrintErr($"AI backend lifetime containment failed (Windows error {error}). The backend is being terminated rather than leaving an unmanaged process behind.");
                    ShutdownBackendLocked();
                    return;
                }
            }

            AttachOutputLogging(_backend);
            GD.Print($"AI backend launched (PID {_backend.Id}). Backend: {_backendPath}; interpreter: {_pythonPath}.");
        }
        catch (Exception ex)
        {
            GD.PrintErr(BuildBackendFailure("AI backend auto-launch failed: " + ex.Message));
            ShutdownBackendLocked();
        }
    }

    async Task ReportInitialReadinessAsync()
    {
        try
        {
            await WaitForBackendReadyAsync(TimeSpan.FromSeconds(30));
            GD.Print($"AI backend readiness confirmed. Backend: {_backendPath}; interpreter: {_pythonPath}.");
        }
        catch (Exception ex)
        {
            GD.PrintErr("AI backend readiness failed: " + ex.Message);
        }
    }

    async Task WaitForBackendReadyAsync(TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (!IsRunning)
                throw new InvalidOperationException(BuildBackendFailure("The AI backend process exited before becoming healthy."));

            try
            {
                using var response = await _readinessHttp.GetAsync("http://127.0.0.1:7868/health");
                if (response.IsSuccessStatusCode)
                {
                    string payload = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(payload);
                    var root = doc.RootElement;
                    string version = root.TryGetProperty("version", out var versionNode) ? versionNode.GetString() ?? "" : "";
                    string token = root.TryGetProperty("instance_token", out var tokenNode) ? tokenNode.GetString() ?? "" : "";
                    if (version == "1.0.26" && !string.IsNullOrWhiteSpace(_instanceToken) && token == _instanceToken)
                        return;
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }

            await Task.Delay(250);
        }
        throw new TimeoutException(BuildBackendFailure("The AI backend did not become healthy within the startup timeout."));
    }

    string BuildBackendFailure(string reason)
    {
        string exit = "running/unknown";
        try
        {
            if (_backend != null && _backend.HasExited) exit = _backend.ExitCode.ToString();
        }
        catch { }

        string stderr;
        lock (_logLock)
            stderr = _recentStderr.Count == 0 ? "<none captured>" : string.Join(" | ", _recentStderr);
        return $"{reason} Backend: {_backendPath ?? "<unresolved>"}; interpreter: {_pythonPath ?? "<unresolved>"}; exit: {exit}; recent stderr: {stderr}";
    }

    void AttachOutputLogging(Process process)
    {
        process.OutputDataReceived += (_, args) => WriteBackendLog("stdout", args.Data);
        process.ErrorDataReceived += (_, args) => WriteBackendLog("stderr", args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    void WriteBackendLog(string stream, string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        try
        {
            string log = AppDataRoot.Resolve("Logs/backend.log");
            lock (_logLock)
            {
                if (stream.Equals("stderr", StringComparison.OrdinalIgnoreCase))
                {
                    _recentStderr.Enqueue(line);
                    while (_recentStderr.Count > RecentStderrLimit) _recentStderr.Dequeue();
                }
                File.AppendAllText(log, $"{DateTime.UtcNow:O} [{stream}] {line}{Environment.NewLine}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("Could not write backend log: " + ex.Message);
        }
    }

    public override void _ExitTree()
    {
        lock (_processLock) ShutdownBackendLocked();
    }

    void ShutdownBackendLocked()
    {
        // On Windows, closing a JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE job kills the backend
        // and every subprocess it created, including specialist model runtimes. The job
        // handle is also closed automatically if the editor process crashes or is force-killed.
        if (_job != IntPtr.Zero)
        {
            try { CloseHandle(_job); } catch { }
            _job = IntPtr.Zero;
        }

        if (_backend != null)
        {
            try
            {
                if (!_backend.HasExited)
                {
                    _backend.Kill(entireProcessTree: true);
                    _backend.WaitForExit(5000);
                }
            }
            catch { }
            finally
            {
                _backend.Dispose();
                _backend = null;
            }
        }
    }

    static IntPtr CreateKillOnCloseJob()
    {
        IntPtr job = CreateJobObject(IntPtr.Zero, null);
        if (job == IntPtr.Zero) return IntPtr.Zero;

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = JobObjectLimitKillOnJobClose;
        int length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(info, buffer, false);
            if (!SetInformationJobObject(job, JobObjectExtendedLimitInformation, buffer, (uint)length))
            {
                CloseHandle(job);
                return IntPtr.Zero;
            }
            return job;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetInformationJobObject(IntPtr hJob, uint infoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr hObject);
}
