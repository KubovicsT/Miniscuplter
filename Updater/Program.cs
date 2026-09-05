using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Miniscuplter.Updater;

internal static class Program
{
    static readonly string[] DefaultPreserveTopLevel = { "AIData", "Runtime", "Projects", "PartsLibrary", "Exports", "UserData", "launcher.settings.json" };
    static readonly string[] PreserveNested = {
        Path.Combine("App", "ai_backend", ".venv"),
        Path.Combine("App", "ai_backend", ".runtime-cache"),
        Path.Combine("App", "ai_backend", "data"),
        Path.Combine("ai_backend", ".venv"),
        Path.Combine("ai_backend", ".runtime-cache"),
        Path.Combine("ai_backend", "data")
    };

    static HashSet<string> _preserveTop = new(DefaultPreserveTopLevel, StringComparer.OrdinalIgnoreCase);

    static int Main(string[] args)
    {
        string? workRoot = null, stage = null, backup = null, parked = null, dataRoot = null;
        bool parkedNeedsRestore = false;
        try
        {
            var map = Parse(args);
            string package = Path.GetFullPath(Require(map, "package"));
            string target = Path.GetFullPath(Require(map, "target"));
            dataRoot = Path.GetFullPath(Require(map, "data-root"));
            string expectedVersion = NormalizeVersion(Require(map, "version"));
            string expectedSha256 = RequireSha256(map);
            int waitPid = map.TryGetValue("wait-pid", out var p) && int.TryParse(p, out var pid) ? pid : -1;
            string restart = map.TryGetValue("restart", out var r) ? Path.GetFullPath(r) : Path.Combine(target, "Miniscuplter.Launcher.exe");

            if (!File.Exists(package)) throw new FileNotFoundException("Update package not found", package);
            if (!VerifySha256(package, expectedSha256)) throw new InvalidDataException("Update package SHA-256 does not match the release digest. No installed files were changed.");
            Directory.CreateDirectory(target);
            _preserveTop = BuildPreserveSet(target, dataRoot);

            if (waitPid > 0) WaitForExit(waitPid);
            EnsureEditorClosed();

            string parent = Directory.GetParent(target)?.FullName ?? Path.GetTempPath();
            workRoot = Path.Combine(parent, ".MiniscuplterUpdate_" + Guid.NewGuid().ToString("N"));
            stage = Path.Combine(workRoot, "stage");
            backup = Path.Combine(workRoot, "backup");
            parked = Path.Combine(workRoot, "preserved");
            Directory.CreateDirectory(stage); Directory.CreateDirectory(backup); Directory.CreateDirectory(parked);

            ZipFile.ExtractToDirectory(package, stage, true);
            string source = NormalizePackageRoot(stage);
            ValidateReleasePackage(source, expectedVersion);

            ParkPreservedNested(target, parked);
            parkedNeedsRestore = true;
            BackupManagedTree(target, backup);
            try
            {
                RemoveManagedTree(target);
                CopyTree(source, target);
                RestoreParkedNested(parked, target);
                parkedNeedsRestore = false;
                ValidateInstalledTree(target, expectedVersion);
            }
            catch
            {
                try { RemoveManagedTree(target); } catch { }
                RestoreBackup(backup, target);
                if (parkedNeedsRestore)
                {
                    RestoreParkedNested(parked, target);
                    parkedNeedsRestore = false;
                }
                throw;
            }

            TryDelete(package);
            TryDeleteDirectory(workRoot);
            if (File.Exists(restart))
                Process.Start(new ProcessStartInfo(restart) { WorkingDirectory = Path.GetDirectoryName(restart) ?? target, UseShellExecute = true });
            ScheduleSelfDelete();
            return 0;
        }
        catch (Exception ex)
        {
            if (parkedNeedsRestore && parked != null)
            {
                try
                {
                    var map = Parse(args);
                    string target = Path.GetFullPath(Require(map, "target"));
                    RestoreParkedNested(parked, target);
                }
                catch { }
            }
            WriteError(dataRoot, ex);
            if (stage != null) TryDeleteDirectory(stage);
            // If rollback failed, leave workRoot/backup in place as recovery material.
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

    static string RequireSha256(Dictionary<string,string> map)
    {
        string value = Require(map, "sha256").Trim().ToLowerInvariant();
        if (value.Length != 64 || !value.All(Uri.IsHexDigit)) throw new ArgumentException("--sha256 must be a 64-character SHA-256 digest");
        return value;
    }

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

    static HashSet<string> BuildPreserveSet(string target, string dataRoot)
    {
        var result = new HashSet<string>(DefaultPreserveTopLevel, StringComparer.OrdinalIgnoreCase);
        try
        {
            string rel = Path.GetRelativePath(target, dataRoot);
            if (rel != "." && !rel.StartsWith(".." + Path.DirectorySeparatorChar) && !Path.IsPathRooted(rel))
            {
                string top = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                if (!string.IsNullOrWhiteSpace(top) && top != ".") result.Add(top);
            }
        }
        catch { }
        return result;
    }

    static bool VerifySha256(string path, string expected)
    {
        using var stream = File.OpenRead(path);
        string actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    static string NormalizePackageRoot(string stage)
    {
        string[] dirs = Directory.GetDirectories(stage); string[] files = Directory.GetFiles(stage);
        return files.Length == 0 && dirs.Length == 1 ? dirs[0] : stage;
    }

    static void ValidateReleasePackage(string source, string expectedVersion)
    {
        string[] required = {
            "Miniscuplter.Launcher.exe",
            "Miniscuplter.Updater.exe",
            Path.Combine("App", "Miniscuplter.exe"),
            Path.Combine("App", "ai_backend", "app.py"),
            Path.Combine("App", "ai_backend", "launcher_bridge.py"),
            "setup_ai_backend.bat",
            "release.json"
        };
        foreach (string relative in required)
        {
            string path = Path.Combine(source, relative);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidDataException("Update package is incomplete; missing required file: " + relative);
        }
        ValidateReleaseManifest(Path.Combine(source, "release.json"), expectedVersion);
    }

    static void ValidateInstalledTree(string target, string expectedVersion)
    {
        string[] required = {
            "Miniscuplter.Launcher.exe", "Miniscuplter.Updater.exe",
            Path.Combine("App", "Miniscuplter.exe"), Path.Combine("App", "ai_backend", "app.py"), "release.json"
        };
        foreach (string relative in required)
        {
            string path = Path.Combine(target, relative);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidDataException("Updated application failed post-copy validation: " + relative);
        }
        ValidateReleaseManifest(Path.Combine(target, "release.json"), expectedVersion);
    }

    static void ValidateReleaseManifest(string path, string expectedVersion)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        string version = doc.RootElement.TryGetProperty("version", out var value) ? NormalizeVersion(value.GetString() ?? "") : "";
        if (!version.Equals(expectedVersion, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Update package version mismatch. Expected {expectedVersion}, package contains {version}.");
        string asset = doc.RootElement.TryGetProperty("asset", out var ae) ? ae.GetString() ?? "" : "";
        if (!asset.Equals("Miniscuplter-win-x64.zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Update package manifest does not identify the expected Windows asset.");
    }

    static string NormalizeVersion(string value) => value.Trim().TrimStart('v', 'V').Split('-', '+')[0];

    static void ParkPreservedNested(string target, string parking)
    {
        foreach (string relative in PreserveNested)
        {
            string source = Path.Combine(target, relative);
            if (!Directory.Exists(source)) continue;
            string destination = Path.Combine(parking, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
            Directory.Move(source, destination);
        }
    }

    static void RestoreParkedNested(string parking, string target)
    {
        foreach (string relative in PreserveNested)
        {
            string source = Path.Combine(parking, relative);
            if (!Directory.Exists(source)) continue;
            string destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
            try { Directory.Move(source, destination); }
            catch (IOException) { CopyDirectory(source, destination); DeleteDirectoryWithRetry(source); }
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
        return _preserveTop.Contains(top);
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

    static void WriteError(string? dataRoot, Exception ex)
    {
        try
        {
            string folder = !string.IsNullOrWhiteSpace(dataRoot) ? Path.Combine(dataRoot, "update-cache") : Path.GetTempPath();
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "MiniscuplterUpdater.error.txt"), ex.ToString());
        }
        catch
        {
            try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "MiniscuplterUpdater.error.txt"), ex.ToString()); } catch { }
        }
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
