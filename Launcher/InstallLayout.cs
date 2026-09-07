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

        string dataRoot = Path.GetFullPath(s.DataRoot);
        string roaming = Path.Combine(dataRoot, "System", "AppData", "Roaming");
        string local = Path.Combine(dataRoot, "System", "AppData", "Local");
        string temp = Path.Combine(dataRoot, "Temp");
        string cache = Path.Combine(dataRoot, "Cache");
        string hf = Path.Combine(cache, "HuggingFace");
        string torch = Path.Combine(cache, "Torch");
        string pip = Path.Combine(cache, "Pip");
        string pycache = Path.Combine(cache, "PythonBytecode");
        foreach (string path in new[] { dataRoot, roaming, local, temp, cache, hf, torch, pip, pycache }) Directory.CreateDirectory(path);

        CopyLegacyGodotUserData(roaming);

        psi.Environment["MINISCULPTER_ROOT"] = s.InstallRoot;
        psi.Environment["MINISCULPTER_DATA"] = dataRoot;
        psi.Environment["MINISCULPTER_LAUNCHER"] = "1";

        // Godot normally resolves user:// beneath %APPDATA% and many libraries use Windows TEMP
        // or per-user caches by default. Redirect only the editor process and its children so all
        // Miniscuplter working data remains under the user-selected DataRoot/AIData location.
        psi.Environment["APPDATA"] = roaming;
        psi.Environment["LOCALAPPDATA"] = local;
        psi.Environment["TEMP"] = temp;
        psi.Environment["TMP"] = temp;
        psi.Environment["HF_HOME"] = hf;
        psi.Environment["HUGGINGFACE_HUB_CACHE"] = Path.Combine(hf, "hub");
        psi.Environment["TRANSFORMERS_CACHE"] = Path.Combine(hf, "transformers");
        psi.Environment["TORCH_HOME"] = torch;
        psi.Environment["PIP_CACHE_DIR"] = pip;
        psi.Environment["XDG_CACHE_HOME"] = cache;
        psi.Environment["PYTHONPYCACHEPREFIX"] = pycache;
        return psi;
    }

    static void CopyLegacyGodotUserData(string newRoamingRoot)
    {
        // Preserve existing presets, recovery data and generated assets when moving Godot's
        // user:// root away from C:. This is copy-only: the previous AppData tree is intentionally
        // left as a safety backup and the new editor no longer writes to it.
        try
        {
            string oldRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(oldRoaming)) return;
            string source = Path.Combine(oldRoaming, "Godot", "app_userdata", "Miniscuplter");
            string destination = Path.Combine(newRoamingRoot, "Godot", "app_userdata", "Miniscuplter");
            if (!Directory.Exists(source)) return;
            if (Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)) return;
            CopyDirectoryMissing(source, destination);
        }
        catch
        {
            // Migration failure must never prevent the editor from launching. Existing data stays
            // untouched at the old path and the new contained user:// tree can still be created.
        }
    }

    static void CopyDirectoryMissing(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, dir)));
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (!File.Exists(target)) File.Copy(file, target, overwrite: false);
        }
    }
}
