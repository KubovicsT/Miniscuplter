using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    sealed partial class ResourceSparkline : Control
    {
        readonly Queue<float> _samples = new();
        public ResourceSparkline()
        {
            CustomMinimumSize = new Vector2(110, 28);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public void Push(float value)
        {
            _samples.Enqueue(Math.Clamp(value, 0f, 100f));
            while (_samples.Count > 60) _samples.Dequeue();
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_samples.Count < 2 || Size.X <= 1 || Size.Y <= 1) return;
            var values = _samples.ToArray();
            float step = Size.X / Math.Max(1, values.Length - 1);
            Vector2 previous = new(0, Size.Y - (values[0] / 100f) * Size.Y);
            for (int i = 1; i < values.Length; i++)
            {
                Vector2 current = new(i * step, Size.Y - (values[i] / 100f) * Size.Y);
                DrawLine(previous, current, new Color(0.72f, 0.76f, 0.82f), 1.25f, true);
                previous = current;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    Timer? _resourceTelemetryTimer;
    Label? _resourceJobLabel;
    Label? _resourceGpuLabel;
    Label? _resourceVramLabel;
    Label? _resourceRamLabel;
    Label? _resourceCpuLabel;
    Label? _resourcePeakLabel;
    ResourceSparkline? _resourceGpuGraph;
    ResourceSparkline? _resourceVramGraph;
    ResourceSparkline? _resourceRamGraph;
    ResourceSparkline? _resourceCpuGraph;
    bool _resourceSampleBusy;
    int _resourceGpuFailureCount;
    TimeSpan _resourceLastCpuTime;
    DateTime _resourceLastCpuSampleUtc;
    bool _resourceWasJobBusy;
    float _resourcePeakGpu;
    float _resourcePeakVramMb;
    float _resourcePeakRamPercent;
    float _resourcePeakGpuTemp;

    public void InstallResourceTelemetry()
    {
        if (_resourceTelemetryTimer != null) return;
        if (FindChild("AI Command Console", true, false) is not Control console || console.GetParent() is not VBoxContainer root) return;

        var panel = new VBoxContainer
        {
            Name = "Resource Telemetry",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = "Local, low-overhead resource sampling. Unsupported GPU sensors are omitted rather than estimated."
        };
        panel.AddThemeConstantOverride("separation", 2);

        _resourceJobLabel = new Label { Text = "Resources · idle", TooltipText = "Active provider, current AI stage and elapsed time." };
        panel.AddChild(_resourceJobLabel);

        var grid = new GridContainer { Columns = 4, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        (_resourceGpuLabel, _resourceGpuGraph) = AddResourceMetric(grid, "GPU", "GPU utilization from NVIDIA telemetry when available.");
        (_resourceVramLabel, _resourceVramGraph) = AddResourceMetric(grid, "VRAM", "Dedicated GPU memory use normalized against dedicated VRAM capacity.");
        (_resourceRamLabel, _resourceRamGraph) = AddResourceMetric(grid, "RAM", "System physical-memory utilization on Windows.");
        (_resourceCpuLabel, _resourceCpuGraph) = AddResourceMetric(grid, "App CPU", "Miniscuplter process CPU utilization across all logical processors.");
        panel.AddChild(grid);

        _resourcePeakLabel = new Label
        {
            Text = "Observed peaks: —",
            TooltipText = "Peak values observed by this panel during the current AI job; these are sampled observations, not guaranteed hardware maxima."
        };
        panel.AddChild(_resourcePeakLabel);

        root.AddChild(panel);
        root.MoveChild(panel, Math.Min(console.GetIndex() + 1, root.GetChildCount() - 1));

        using var process = Process.GetCurrentProcess();
        _resourceLastCpuTime = process.TotalProcessorTime;
        _resourceLastCpuSampleUtc = DateTime.UtcNow;

        _resourceTelemetryTimer = new Timer { WaitTime = 1.0, OneShot = false };
        _resourceTelemetryTimer.Timeout += () => _ = SampleResourceTelemetryAsync();
        AddChild(_resourceTelemetryTimer);
        _resourceTelemetryTimer.Start();
        _ = SampleResourceTelemetryAsync();
    }

    static (Label Label, ResourceSparkline Graph) AddResourceMetric(Container parent, string title, string tooltip)
    {
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, TooltipText = tooltip };
        var label = new Label { Text = title + ": —", TooltipText = tooltip };
        var graph = new ResourceSparkline { TooltipText = tooltip };
        box.AddChild(label);
        box.AddChild(graph);
        parent.AddChild(box);
        return (label, graph);
    }

    async Task SampleResourceTelemetryAsync()
    {
        if (_resourceSampleBusy) return;
        _resourceSampleBusy = true;
        try
        {
            var cpu = SampleAppCpu();
            var ram = SampleSystemRam();
            var gpu = _resourceGpuFailureCount >= 3 ? null : await SampleNvidiaGpuAsync();

            if (gpu == null && _resourceGpuFailureCount < 3) _resourceGpuFailureCount++;
            else if (gpu != null) _resourceGpuFailureCount = 0;

            if (cpu.HasValue)
            {
                if (_resourceCpuLabel != null) _resourceCpuLabel.Text = $"App CPU: {cpu.Value:0}%";
                _resourceCpuGraph?.Push(cpu.Value);
            }

            if (ram.HasValue)
            {
                if (_resourceRamLabel != null) _resourceRamLabel.Text = $"RAM: {ram.Value.Percent:0}% · {ram.Value.UsedGb:0.0}/{ram.Value.TotalGb:0.0} GB";
                _resourceRamGraph?.Push(ram.Value.Percent);
            }

            if (gpu.HasValue)
            {
                float vramPercent = gpu.Value.TotalVramMb > 0 ? gpu.Value.UsedVramMb / gpu.Value.TotalVramMb * 100f : 0f;
                if (_resourceGpuLabel != null) _resourceGpuLabel.Text = $"GPU: {gpu.Value.Utilization:0}% · {gpu.Value.Temperature:0}°C";
                if (_resourceVramLabel != null) _resourceVramLabel.Text = $"VRAM: {gpu.Value.UsedVramMb / 1024f:0.0}/{gpu.Value.TotalVramMb / 1024f:0.0} GB";
                _resourceGpuGraph?.Push(gpu.Value.Utilization);
                _resourceVramGraph?.Push(vramPercent);
            }
            else if (_resourceGpuFailureCount >= 3)
            {
                if (_resourceGpuLabel != null) _resourceGpuLabel.Text = "GPU: unavailable";
                if (_resourceVramLabel != null) _resourceVramLabel.Text = "VRAM: unavailable";
            }

            UpdateResourceJobSummary(cpu, ram, gpu);
        }
        catch
        {
            // Telemetry must never interfere with modeling or inference.
        }
        finally
        {
            _resourceSampleBusy = false;
        }
    }

    float? SampleAppCpu()
    {
        using var process = Process.GetCurrentProcess();
        var now = DateTime.UtcNow;
        var cpu = process.TotalProcessorTime;
        double elapsedMs = (now - _resourceLastCpuSampleUtc).TotalMilliseconds;
        double cpuMs = (cpu - _resourceLastCpuTime).TotalMilliseconds;
        _resourceLastCpuTime = cpu;
        _resourceLastCpuSampleUtc = now;
        if (elapsedMs <= 0) return null;
        return (float)Math.Clamp(cpuMs / (elapsedMs * Math.Max(1, Environment.ProcessorCount)) * 100.0, 0.0, 100.0);
    }

    static (float Percent, float UsedGb, float TotalGb)? SampleSystemRam()
    {
        if (!OperatingSystem.IsWindows()) return null;
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status) || status.TotalPhysical == 0) return null;
        ulong used = status.TotalPhysical - status.AvailablePhysical;
        return (status.MemoryLoad, used / 1073741824f, status.TotalPhysical / 1073741824f);
    }

    readonly record struct NvidiaSample(float Utilization, float UsedVramMb, float TotalVramMb, float Temperature);

    static async Task<NvidiaSample?> SampleNvidiaGpuAsync()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=utilization.gpu,memory.used,memory.total,temperature.gpu --format=csv,noheader,nounits",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            if (!process.Start()) return null;
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task waitTask = process.WaitForExitAsync();
            if (await Task.WhenAny(waitTask, Task.Delay(1500)) != waitTask)
            {
                try { process.Kill(true); } catch { }
                return null;
            }
            string output = (await outputTask).Trim();
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output)) return null;
            string first = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            string[] parts = first.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 4) return null;
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float utilization)) return null;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float used)) return null;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float total)) return null;
            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float temperature)) return null;
            return new NvidiaSample(utilization, used, total, temperature);
        }
        catch
        {
            return null;
        }
    }

    void UpdateResourceJobSummary(float? cpu, (float Percent, float UsedGb, float TotalGb)? ram, NvidiaSample? gpu)
    {
        bool busy = _v1093DBusy || _v108AiBusy || _v1017ImageBusy;
        if (busy && !_resourceWasJobBusy)
        {
            _resourcePeakGpu = 0;
            _resourcePeakVramMb = 0;
            _resourcePeakRamPercent = 0;
            _resourcePeakGpuTemp = 0;
        }
        _resourceWasJobBusy = busy;

        string provider = "idle";
        string stage = "idle";
        DateTime started = DateTime.UtcNow;
        if (_v1093DBusy)
        {
            provider = string.IsNullOrWhiteSpace(_v1093DProvider) ? "3D" : _v1093DProvider;
            stage = CompactStage(_v1093DStatus?.Text);
            started = _v1093DStarted;
        }
        else if (_v108AiBusy)
        {
            provider = string.IsNullOrWhiteSpace(_v108AiProvider) ? "2D" : _v108AiProvider;
            stage = CompactStage(_v108AiJobStatus?.Text);
            started = _v108AiStarted;
        }
        else if (_v1017ImageBusy)
        {
            provider = "image-edit";
            stage = CompactStage(_v1017ImageJobStatus?.Text);
            started = _v1017ImageStarted;
        }

        if (_resourceJobLabel != null)
            _resourceJobLabel.Text = busy
                ? $"Resources · {provider} · {stage} · {(DateTime.UtcNow - started).TotalSeconds:0}s"
                : "Resources · idle";

        if (!busy) return;
        if (gpu.HasValue)
        {
            _resourcePeakGpu = Math.Max(_resourcePeakGpu, gpu.Value.Utilization);
            _resourcePeakVramMb = Math.Max(_resourcePeakVramMb, gpu.Value.UsedVramMb);
            _resourcePeakGpuTemp = Math.Max(_resourcePeakGpuTemp, gpu.Value.Temperature);
        }
        if (ram.HasValue) _resourcePeakRamPercent = Math.Max(_resourcePeakRamPercent, ram.Value.Percent);

        if (_resourcePeakLabel != null)
        {
            var parts = new List<string>();
            if (_resourcePeakGpu > 0) parts.Add($"GPU {_resourcePeakGpu:0}%");
            if (_resourcePeakVramMb > 0) parts.Add($"VRAM {_resourcePeakVramMb / 1024f:0.0} GB");
            if (_resourcePeakRamPercent > 0) parts.Add($"RAM {_resourcePeakRamPercent:0}%");
            if (_resourcePeakGpuTemp > 0) parts.Add($"GPU {_resourcePeakGpuTemp:0}°C");
            _resourcePeakLabel.Text = parts.Count == 0 ? "Observed peaks: —" : "Observed peaks: " + string.Join(" · ", parts);
        }
    }

    static string CompactStage(string? text)
    {
        string value = (text ?? "working").Trim();
        int colon = value.IndexOf(':');
        if (colon >= 0 && colon + 1 < value.Length) value = value[(colon + 1)..].Trim();
        return value.Length <= 64 ? value : value[..61] + "…";
    }
}
