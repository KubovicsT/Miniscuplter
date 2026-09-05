using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Miniscuplter.Launcher;

internal static class OwnedChildProcessJob
{
    const uint JobObjectExtendedLimitInformation = 9;
    const uint JobObjectLimitKillOnJobClose = 0x00002000;
    static readonly object Gate = new();
    static IntPtr _job;
    static bool _initialized;

    public static void Initialize()
    {
        lock (Gate)
        {
            if (_initialized) return;
            _initialized = true;
            if (!OperatingSystem.IsWindows()) return;

            _job = CreateJobObject(IntPtr.Zero, null);
            if (_job == IntPtr.Zero) return;

            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            info.BasicLimitInformation.LimitFlags = JobObjectLimitKillOnJobClose;
            int length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            IntPtr buffer = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(info, buffer, false);
                if (!SetInformationJobObject(_job, JobObjectExtendedLimitInformation, buffer, (uint)length))
                {
                    CloseHandle(_job);
                    _job = IntPtr.Zero;
                }
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
    }

    public static Process Start(ProcessStartInfo startInfo)
    {
        Initialize();
        if (OperatingSystem.IsWindows() && _job == IntPtr.Zero)
            throw new InvalidOperationException("Windows child-process containment could not be initialized. Miniscuplter will not start unmanaged helper processes because they could remain running after the launcher closes.");

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {startInfo.FileName}.");
        if (!OperatingSystem.IsWindows()) return process;

        lock (Gate)
        {
            bool assigned = false;
            try { assigned = _job != IntPtr.Zero && AssignProcessToJobObject(_job, process.Handle); }
            catch { }
            if (!assigned)
            {
                int error = Marshal.GetLastWin32Error();
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
                try { process.WaitForExit(3000); } catch { }
                process.Dispose();
                throw new InvalidOperationException($"Could not attach helper process to the Miniscuplter lifetime job (Windows error {error}). The helper was terminated to prevent an orphaned process.");
            }
        }
        return process;
    }

    public static void Dispose()
    {
        lock (Gate)
        {
            if (_job != IntPtr.Zero)
            {
                CloseHandle(_job);
                _job = IntPtr.Zero;
            }
        }
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
