using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Miniscuplter.Launcher;

internal sealed record LauncherHardware(string Cpu, int LogicalProcessors, double RamGb, string? Gpu, int VramMb, bool NvidiaCudaCapable, string RecommendedPreset);

internal static class HardwareProbe
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    public static LauncherHardware Detect()
    {
        string cpu = ReadCpuName();
        double ram = ReadRamGb();
        (string? gpu, int vram) = ReadNvidiaGpu();
        string preset = vram switch
        {
            <= 0 => "Low (CPU / no NVIDIA CUDA detected)",
            <= 4096 => "Low",
            <= 8192 => "Medium",
            <= 12288 => "High",
            _ => "Ultra"
        };
        return new LauncherHardware(cpu, Environment.ProcessorCount, ram, gpu, vram, vram > 0, preset);
    }

    static string ReadCpuName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            string? value = key?.GetValue("ProcessorNameString")?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        catch { }
        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU";
    }

    static double ReadRamGb()
    {
        try
        {
            var s = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (GlobalMemoryStatusEx(ref s)) return Math.Round(s.ullTotalPhys / 1024d / 1024d / 1024d, 1);
        }
        catch { }
        return 0;
    }

    static (string? gpu, int vramMb) ReadNvidiaGpu()
    {
        try
        {
            var psi = new ProcessStartInfo("nvidia-smi", "--query-gpu=name,memory.total --format=csv,noheader,nounits")
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process == null || !process.WaitForExit(5000) || process.ExitCode != 0) return (null, 0);
            string? line = process.StandardOutput.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) return (null, 0);
            int comma = line.LastIndexOf(',');
            if (comma < 0) return (line.Trim(), 0);
            string name = line[..comma].Trim();
            return int.TryParse(line[(comma + 1)..].Trim(), out int mb) ? (name, mb) : (name, 0);
        }
        catch { return (null, 0); }
    }
}
