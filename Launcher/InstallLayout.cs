using System.Diagnostics;
using System.Text.Json;

namespace Miniscuplter.Launcher;

internal sealed class LauncherSettings
{
    public string InstallRoot { get; set; } = "";
    public string AppExecutable { get; set; } = "App\\Miniscuplter.exe";
    public string DataRoot { get; set; } = "AIData";
    public bool CheckApplicationUpdates { get; set; } = true;
    public bool CheckModelUpdates { get; set; } = true;
    public string ReleaseRepository { get; set; } = "KubovicsT/Miniscuplter";
}

internal static class InstallLayout
{
    public static string LauncherDirectory => AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    public static string SettingsPath => Path.Combine(LauncherDirectory, "launcher.settings.json");

    public static LauncherSettings Load()
    {
        LauncherSettings settings;
        try { settings = File.Exists(SettingsPath) ? JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(SettingsPath)) ?? new LauncherSettings() : new LauncherSettings(); }
        catch { settings = new LauncherSettings(); }
        if (string.IsNullOrWhiteSpace(settings.InstallRoot)) settings.InstallRoot = LauncherDirectory;
        if (!Path.IsPathRooted(settings.InstallRoot)) settings.InstallRoot = Path.GetFullPath(Path.Combine(LauncherDirectory, settings.InstallRoot));
        if (!Path.IsPathRooted(settings.DataRoot)) settings.DataRoot = Path.Combine(settings.InstallRoot, settings.DataRoot);
        return settings;
    }

    public static void Save(LauncherSettings settings)
    {
        Directory.CreateDirectory(LauncherDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static string ResolveApp(LauncherSettings s)
    {
        string configured = Path.IsPathRooted(s.AppExecutable) ? s.AppExecutable : Path.Combine(s.InstallRoot, s.AppExecutable);
        if (File.Exists(configured)) return configured;
        string[] candidates = { Path.Combine(s.InstallRoot, "App", "Miniscuplter.exe"), Path.Combine(s.InstallRoot, "Miniscuplter.exe"), Path.Combine(s.InstallRoot, "Miniscuplter.console.exe") };
        return candidates.FirstOrDefault(File.Exists) ?? configured;
    }

    public static string ResolveBackendRoot(LauncherSettings s)
    {
        string nested = Path.Combine(s.InstallRoot, "App", "ai_backend");
        if (Directory.Exists(nested)) return nested;
        string root = Path.Combine(s.InstallRoot, "ai_backend");
        return Directory.Exists(root) ? root : nested;
    }

    public static string? ResolvePython(LauncherSettings s)
    {
        string[] candidates = {
            Path.Combine(s.InstallRoot, "Runtime", "Python", "python.exe"),
            Path.Combine(s.InstallRoot, "ai_backend", ".venv", "Scripts", "python.exe"),
            Path.Combine(s.InstallRoot, "App", "ai_backend", ".venv", "Scripts", "python.exe"),
            Path.Combine(s.InstallRoot, ".venv", "Scripts", "python.exe")
        };
        foreach (string p in candidates) if (File.Exists(p)) return p;
        foreach (string name in new[] { "python.exe", "python", "py.exe", "py" })
        {
            try
            {
                var psi = new ProcessStartInfo(name, "--version") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using var p = Process.Start(psi); if (p == null) continue; if (!p.WaitForExit(2500)) { try { p.Kill(); } catch { } continue; }
                if (p.ExitCode == 0) return name;
            }
            catch { }
        }
        return null;
    }

    public static ProcessStartInfo CreateAppStartInfo(LauncherSettings s)
    {
        string exe = ResolveApp(s);
        var psi = new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) ?? s.InstallRoot, UseShellExecute = false };
        psi.Environment["MINISCULPTER_ROOT"] = s.InstallRoot;
        psi.Environment["MINISCULPTER_DATA"] = s.DataRoot;
        psi.Environment["MINISCULPTER_LAUNCHER"] = "1";
        return psi;
    }
}
