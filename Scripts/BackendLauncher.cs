using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Miniscuplter;

public partial class BackendLauncher : Node
{
    const uint JobObjectExtendedLimitInformation = 9;
    const uint JobObjectLimitKillOnJobClose = 0x00002000;

    Process? _backend;
    IntPtr _job = IntPtr.Zero;

    public override void _Ready()
    {
        string root = Environment.GetEnvironmentVariable("MINISCULPTER_ROOT") ?? "";
        if (string.IsNullOrWhiteSpace(root))
        {
            string exe = OS.GetExecutablePath();
            root = string.IsNullOrWhiteSpace(exe) ? ProjectSettings.GlobalizePath("res://") : Path.GetDirectoryName(exe) ?? ProjectSettings.GlobalizePath("res://");
        }

        string[] backendCandidates =
        {
            Path.Combine(root, "ai_backend", "app.py"),
            Path.Combine(root, "App", "ai_backend", "app.py"),
            ProjectSettings.GlobalizePath("res://ai_backend/app.py")
        };
        string? app = Array.Find(backendCandidates, File.Exists);
        if (app == null) { GD.Print("AI backend files were not found; editor remains usable without AI."); return; }

        string[] pythonCandidates =
        {
            Path.Combine(root, "Runtime", "Python", "python.exe"),
            Path.Combine(root, ".venv", "Scripts", "python.exe"),
            Path.Combine(root, "App", ".venv", "Scripts", "python.exe"),
            Path.Combine(Path.GetDirectoryName(app)!, ".venv", "Scripts", "python.exe")
        };
        string python = Array.Find(pythonCandidates, File.Exists) ?? "python";

        try
        {
            var psi = new ProcessStartInfo(python)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(app) ?? root
            };
            psi.ArgumentList.Add(app);
            psi.Environment["MINISCULPTER_ROOT"] = root;
            string? data = Environment.GetEnvironmentVariable("MINISCULPTER_DATA");
            if (!string.IsNullOrWhiteSpace(data)) psi.Environment["MINISCULPTER_DATA"] = data;
            psi.Environment["MINISCULPTER_PARENT_PID"] = Environment.ProcessId.ToString();

            _backend = Process.Start(psi);
            if (_backend == null)
            {
                GD.Print("Could not auto-launch AI backend; editor will remain usable without AI.");
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                _job = CreateKillOnCloseJob();
                if (_job == IntPtr.Zero || !AssignProcessToJobObject(_job, _backend.Handle))
                {
                    int error = Marshal.GetLastWin32Error();
                    GD.PrintErr($"AI backend lifetime containment failed (Windows error {error}). The backend is being terminated rather than leaving an unmanaged process behind.");
                    ShutdownBackend();
                    return;
                }
            }

            GD.Print($"AI backend launched (PID {_backend.Id}) from {app}.");
        }
        catch (Exception ex)
        {
            GD.PrintErr("AI backend auto-launch failed: " + ex.Message);
            ShutdownBackend();
        }
    }

    public override void _ExitTree() => ShutdownBackend();

    void ShutdownBackend()
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
