using System.Diagnostics;
using System.IO.Compression;

namespace Miniscuplter.Updater;

internal static class Program
{
    static readonly string[] PreserveTopLevel = { "AIData", "Projects", "PartsLibrary", "Exports", "UserData", "launcher.settings.json" };
    static readonly string[] PreserveNested = { Path.Combine("App", "ai_backend", ".venv") };

    static int Main(string[] args)
    {
        string? stage = null;
        string? backup = null;
        try
        {
            var map = Parse(args);
            string package = Path.GetFullPath(Require(map, "package"));
            string target = Path.GetFullPath(Require(map, "target"));
            int waitPid = map.TryGetValue("wait-pid", out var p) && int.TryParse(p, out var pid) ? pid : -1;
            string restart = map.TryGetValue("restart", out var r) ? Path.GetFullPath(r) : Path.Combine(target, "Miniscuplter.Launcher.exe");

            if (waitPid > 0) WaitForExit(waitPid);
            EnsureEditorClosed();
            if (!File.Exists(package)) throw new FileNotFoundException("Update package not found", package);
            Directory.CreateDirectory(target);

            stage = Path.Combine(Path.GetTempPath(), "MiniscuplterUpdate_" + Guid.NewGuid().ToString("N"));
            backup = Path.Combine(Path.GetTempPath(), "MiniscuplterBackup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage); Directory.CreateDirectory(backup);
            ZipFile.ExtractToDirectory(package, stage, true);
            string source = NormalizePackageRoot(stage);
            ValidateReleasePackage(source);

            BackupManagedTree(target, backup);
            try
            {
                RemoveManagedTree(target);
                CopyTree(source, target);
                RestorePreservedNested(backup, target);
                ValidateInstalledTree(target);
            }
            catch
            {
                // A failed update must never strand a half-copied application. Remove whatever
                // the failed update wrote, then restore the complete pre-update program tree.
                try { RemoveManagedTree(target); } catch { }
                RestoreBackup(backup, target);
                throw;
            }

            TryDelete(package);
            TryDeleteDirectory(stage);
            TryDeleteDirectory(backup);
            if (File.Exists(restart))
                Process.Start(new ProcessStartInfo(restart) { WorkingDirectory = Path.GetDirectoryName(restart) ?? target, UseShellExecute = true });
            ScheduleSelfDelete();
            return 0;
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "MiniscuplterUpdater.error.txt"), ex.ToString()); } catch { }
            if (stage != null) TryDeleteDirectory(stage);
            // Keep the backup on disk if rollback itself failed; it is more useful than deleting recovery data.
            return 1;
        }
    }

    static Dictionary<string,string> Parse(string[] args)
    {
        var map = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
            if (args[i].StartsWith("--") && i + 1 < args.Length) map[args[i][2..]] = args[++i];
        return map;
    }

    static string Require(Dictionary<string,string> map, string key) => map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Missing --" + key);

    static void WaitForExit(int pid)
    {
        try { using var p = Process.GetProcessById(pid); if (!p.WaitForExit(120000)) throw new TimeoutException("Launcher did not exit in time for update."); }
        catch (ArgumentException) { }
    }

    static void EnsureEditorClosed()
    {
        var running = Process.GetProcessesByName("Miniscuplter").Where(p => p.Id != Environment.ProcessId).ToArray();
        try
        {
            if (running.Any(p => !p.HasExited)) throw new InvalidOperationException("The Miniscuplter editor is still running. Close it before applying the application update.");
        }
        finally { foreach (var p in running) p.Dispose(); }
    }

    static string NormalizePackageRoot(string stage)
    {
        string[] dirs = Directory.GetDirectories(stage); string[] files = Directory.GetFiles(stage);
        return files.Length == 0 && dirs.Length == 1 ? dirs[0] : stage;
    }

    static void ValidateReleasePackage(string source)
    {
        string[] required = {
            "Miniscuplter.Launcher.exe",
            "Miniscuplter.Updater.exe",
            Path.Combine("App", "Miniscuplter.exe"),
            Path.Combine("App", "ai_backend", "app.py"),
            Path.Combine("App", "ai_backend", "launcher_bridge.py"),
            "setup_ai_backend.bat"
        };
        foreach (string relative in required)
        {
            string path = Path.Combine(source, relative);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidDataException("Update package is incomplete; missing required file: " + relative);
        }
    }

    static void ValidateInstalledTree(string target)
    {
        string[] required = {
            "Miniscuplter.Launcher.exe", "Miniscuplter.Updater.exe",
            Path.Combine("App", "Miniscuplter.exe"), Path.Combine("App", "ai_backend", "app.py")
        };
        foreach (string relative in required)
        {
            string path = Path.Combine(target, relative);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidDataException("Updated application failed post-copy validation: " + relative);
        }
    }

    static void BackupManagedTree(string target, string backup)
    {
        if (!Directory.Exists(target)) return;
        foreach (string file in Directory.GetFiles(target))
        {
            string rel = Path.GetFileName(file); if (IsPreservedTopLevel(rel)) continue;
            File.Copy(file, Path.Combine(backup, rel), true);
        }
        foreach (string dir in Directory.GetDirectories(target))
        {
            string rel = Path.GetFileName(dir); if (IsPreservedTopLevel(rel)) continue;
            CopyDirectory(dir, Path.Combine(backup, rel));
        }
    }

    static void RestorePreservedNested(string backup, string target)
    {
        foreach (string relative in PreserveNested)
        {
            string source = Path.Combine(backup, relative);
            if (!Directory.Exists(source)) continue;
            string destination = Path.Combine(target, relative);
            if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
            CopyDirectory(source, destination);
        }
    }

    static void RestoreBackup(string backup, string target)
    {
        if (!Directory.Exists(backup)) return;
        CopyDirectory(backup, target);
    }

    static void RemoveManagedTree(string target)
    {
        if (!Directory.Exists(target)) return;
        foreach (string file in Directory.GetFiles(target))
        {
            string rel = Path.GetFileName(file); if (IsPreservedTopLevel(rel)) continue;
            DeleteFileWithRetry(file);
        }
        foreach (string dir in Directory.GetDirectories(target))
        {
            string rel = Path.GetFileName(dir); if (IsPreservedTopLevel(rel)) continue;
            DeleteDirectoryWithRetry(dir);
        }
    }

    static void CopyTree(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(source, dir); if (IsPreservedTopLevel(rel)) continue;
            Directory.CreateDirectory(Path.Combine(target, rel));
        }
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(source, file); if (IsPreservedTopLevel(rel)) continue;
            string dest = Path.Combine(target, rel); Directory.CreateDirectory(Path.GetDirectoryName(dest)!); CopyWithRetry(file, dest);
        }
    }

    static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, dir)));
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string dest = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!); File.Copy(file, dest, true);
        }
    }

    static bool IsPreservedTopLevel(string rel)
    {
        string top = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return PreserveTopLevel.Any(p => top.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    static void CopyWithRetry(string source, string dest)
    {
        Exception? last = null;
        for (int i = 0; i < 20; i++)
        {
            try { File.Copy(source, dest, true); return; }
            catch (IOException ex) { last = ex; Thread.Sleep(250); }
            catch (UnauthorizedAccessException ex) { last = ex; Thread.Sleep(250); }
        }
        throw new IOException("Could not replace " + dest, last);
    }

    static void DeleteFileWithRetry(string path)
    {
        Exception? last = null;
        for (int i = 0; i < 20; i++)
        {
            try { File.SetAttributes(path, FileAttributes.Normal); File.Delete(path); return; }
            catch (IOException ex) { last = ex; Thread.Sleep(250); }
            catch (UnauthorizedAccessException ex) { last = ex; Thread.Sleep(250); }
        }
        throw new IOException("Could not remove old application file " + path, last);
    }

    static void DeleteDirectoryWithRetry(string path)
    {
        Exception? last = null;
        for (int i = 0; i < 20; i++)
        {
            try { Directory.Delete(path, true); return; }
            catch (IOException ex) { last = ex; Thread.Sleep(250); }
            catch (UnauthorizedAccessException ex) { last = ex; Thread.Sleep(250); }
        }
        throw new IOException("Could not remove old application directory " + path, last);
    }

    static void TryDelete(string path) { try { File.Delete(path); } catch { } }
    static void TryDeleteDirectory(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { } }

    static void ScheduleSelfDelete()
    {
        try
        {
            string self = Environment.ProcessPath ?? "";
            if (string.IsNullOrWhiteSpace(self) || !File.Exists(self)) return;
            var psi = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add("/c"); psi.ArgumentList.Add($"ping 127.0.0.1 -n 3 > nul & del /f /q \"{self}\"");
            Process.Start(psi);
        }
        catch { }
    }
}
