using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Miniscuplter.Updater;

internal static class Program
{
    const long ExtractionSafetyBytes = 128L * 1024 * 1024;
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
    static string? _nestedDataRoot;

    static int Main(string[] args)
    {
        string? workRoot = null, stage = null, backup = null, parked = null, dataRoot = null;
        bool parkedNeedsRestore = false;
        bool managedNeedsRestore = false;
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

            string parent = Directory.GetParent(target)?.FullName ?? throw new InvalidOperationException("The application installation directory has no usable parent for transactional update staging.");
            long expandedBytes = GetExpandedSize(package);
            long requiredBytes = checked(expandedBytes + Math.Max(ExtractionSafetyBytes, expandedBytes / 10));
            EnsureFreeSpace(parent, requiredBytes, expandedBytes);

            // Keep extraction, rollback and parked runtime beside the installed application.
            // This keeps them off Windows TEMP and guarantees same-volume directory moves for
            // the existing app/runtime, so rollback does not duplicate multi-GB persistent data.
            workRoot = Path.Combine(parent, ".MiniscuplterUpdate_" + Guid.NewGuid().ToString("N"));
            stage = Path.Combine(workRoot, "stage");
            backup = Path.Combine(workRoot, "rollback");
            parked = Path.Combine(workRoot, "preserved");
            Directory.CreateDirectory(stage);
            Directory.CreateDirectory(backup);
            Directory.CreateDirectory(parked);

            ZipFile.ExtractToDirectory(package, stage, true);
            string source = NormalizePackageRoot(stage);
            ValidateReleasePackage(source, expectedVersion);

            parkedNeedsRestore = true;
            ParkPreservedNested(target, parked);

            try
            {
                // Move the old managed tree into rollback storage rather than copying it.
                // Because rollback is on the same volume, this is a metadata operation and
                // consumes essentially no additional disk space.
                MoveManagedTreeToBackup(target, backup);
                managedNeedsRestore = true;

                // The release is already extracted on the same volume. Move its managed files
                // into place so the extracted copy is consumed instead of duplicated.
                InstallManagedTreeFromStage(source, target);
                ValidateInstalledTree(target, expectedVersion);

                RestoreParkedNested(parked, target);
                parkedNeedsRestore = false;
                managedNeedsRestore = false;
            }
            catch
            {
                try { RemoveManagedTree(target); } catch { }
                if (managedNeedsRestore)
                {
                    RestoreManagedTreeFromBackup(backup, target);
                    managedNeedsRestore = false;
                }
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
            try
            {
                var map = Parse(args);
                string target = Path.GetFullPath(Require(map, "target"));
                if (managedNeedsRestore && backup != null)
                {
                    try { RemoveManagedTree(target); } catch { }
                    RestoreManagedTreeFromBackup(backup, target);
                    managedNeedsRestore = false;
                }
                if (parkedNeedsRestore && parked != null)
                {
                    RestoreParkedNested(parked, target);
                    parkedNeedsRestore = false;
                }
            }
            catch { }

            WriteError(dataRoot, ex);
            if (stage != null) TryDeleteDirectory(stage);
            // If rollback itself failed, keep workRoot/rollback as recovery material.
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
        _nestedDataRoot = null;
        string rel;
        try { rel = Path.GetRelativePath(target, dataRoot); }
        catch { return result; }
        if (rel == ".") throw new InvalidOperationException("The configured AI DataRoot cannot be the application installation root during self-update.");
        if (Path.IsPathRooted(rel) || rel.StartsWith(".." + Path.DirectorySeparatorChar) || rel.Equals("..", StringComparison.Ordinal)) return result;

        string top = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        if (top.Equals("App", StringComparison.OrdinalIgnoreCase) || top.Equals("ai_backend", StringComparison.OrdinalIgnoreCase))
        {
            string normalized = NormalizeRelative(rel);
            if (normalized.Equals("App", StringComparison.OrdinalIgnoreCase) || normalized.Equals("App/ai_backend", StringComparison.OrdinalIgnoreCase) || normalized.Equals("ai_backend", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The configured AI DataRoot overlaps the managed application/backend root. Move AI data to a dedicated subfolder before self-update.");
            _nestedDataRoot = rel;
        }
        else if (!string.IsNullOrWhiteSpace(top) && top != ".") result.Add(top);
        return result;
    }

    static IEnumerable<string> PreservedNestedPaths()
    {
        var paths = PreserveNested.ToList();
        if (!string.IsNullOrWhiteSpace(_nestedDataRoot))
        {
            string dynamicPath = _nestedDataRoot!;
            if (!paths.Any(existing => SameOrParent(existing, dynamicPath)))
            {
                paths.RemoveAll(existing => SameOrParent(dynamicPath, existing));
                paths.Add(dynamicPath);
            }
        }
        return paths.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    static string NormalizeRelative(string value) => value.Replace('\\', '/').Trim('/');
    static bool SameOrParent(string parent, string child)
    {
        string p = NormalizeRelative(parent), c = NormalizeRelative(child);
        return c.Equals(p, StringComparison.OrdinalIgnoreCase) || c.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase);
    }

    static bool VerifySha256(string path, string expected)
    {
        using var stream = File.OpenRead(path);
        string actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    static long GetExpandedSize(string package)
    {
        using var archive = ZipFile.OpenRead(package);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            total = checked(total + entry.Length);
        }
        if (total <= 0) throw new InvalidDataException("Update package contains no extractable files.");
        return total;
    }

    static long AvailableBytes(string path)
    {
        string root = Path.GetPathRoot(Path.GetFullPath(path)) ?? throw new InvalidOperationException("Could not resolve update staging drive.");
        return new DriveInfo(root).AvailableFreeSpace;
    }

    static void EnsureFreeSpace(string path, long requiredBytes, long expandedBytes)
    {
        long free = AvailableBytes(path);
        if (free >= requiredBytes) return;
        string root = Path.GetPathRoot(Path.GetFullPath(path)) ?? path;
        throw new IOException($"Not enough free space to stage the Miniscuplter update on {root}. The release expands to about {FormatBytes(expandedBytes)} and the updater requires about {FormatBytes(requiredBytes)} free including a safety margin; {FormatBytes(free)} is available. AI models, Python runtime and other preserved data are not duplicated.");
    }

    static string FormatBytes(long bytes)
    {
        double gib = Math.Max(0, bytes) / 1073741824.0;
        return gib >= 0.1 ? $"{gib:0.00} GiB" : $"{Math.Max(0, bytes) / 1048576.0:0} MiB";
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
                throw new InvalidDataException("Updated application failed post-move validation: " + relative);
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
        foreach (string relative in PreservedNestedPaths())
        {
            string source = Path.Combine(target, relative);
            if (!Directory.Exists(source)) continue;
            string destination = Path.Combine(parking, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
            MoveDirectoryWithRetry(source, destination);
        }
    }

    static void RestoreParkedNested(string parking, string target)
    {
        var restored = new List<(string Source, string Destination)>();
        try
        {
            foreach (string relative in PreservedNestedPaths())
            {
                string source = Path.Combine(parking, relative);
                if (!Directory.Exists(source)) continue;
                string destination = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
                MoveDirectoryWithRetry(source, destination);
                restored.Add((source, destination));
            }
        }
        catch
        {
            for (int i = restored.Count - 1; i >= 0; i--)
            {
                var item = restored[i];
                try
                {
                    if (Directory.Exists(item.Destination) && !Directory.Exists(item.Source))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(item.Source)!);
                        Directory.Move(item.Destination, item.Source);
                    }
                }
                catch { }
            }
            throw;
        }
    }

    static void MoveManagedTreeToBackup(string target, string backup)
    {
        if (!Directory.Exists(target)) return;
        var moved = new List<(string Source, string Destination, bool Directory)>();
        try
        {
            foreach (string file in Directory.GetFiles(target))
            {
                string rel = Path.GetFileName(file);
                if (IsPreservedTopLevel(rel)) continue;
                string destination = Path.Combine(backup, rel);
                File.Move(file, destination, true);
                moved.Add((file, destination, false));
            }
            foreach (string dir in Directory.GetDirectories(target))
            {
                string rel = Path.GetFileName(dir);
                if (IsPreservedTopLevel(rel)) continue;
                string destination = Path.Combine(backup, rel);
                MoveDirectoryWithRetry(dir, destination);
                moved.Add((dir, destination, true));
            }
        }
        catch
        {
            for (int i = moved.Count - 1; i >= 0; i--)
            {
                var item = moved[i];
                try
                {
                    if (item.Directory && Directory.Exists(item.Destination) && !Directory.Exists(item.Source)) Directory.Move(item.Destination, item.Source);
                    else if (!item.Directory && File.Exists(item.Destination) && !File.Exists(item.Source)) File.Move(item.Destination, item.Source, true);
                }
                catch { }
            }
            throw;
        }
    }

    static void RestoreManagedTreeFromBackup(string backup, string target)
    {
        if (!Directory.Exists(backup)) return;
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(backup))
        {
            string destination = Path.Combine(target, Path.GetFileName(file));
            if (File.Exists(destination)) DeleteFileWithRetry(destination);
            File.Move(file, destination, true);
        }
        foreach (string dir in Directory.GetDirectories(backup))
        {
            string destination = Path.Combine(target, Path.GetFileName(dir));
            if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
            MoveDirectoryWithRetry(dir, destination);
        }
    }

    static void InstallManagedTreeFromStage(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
        {
            string rel = Path.GetFileName(file);
            if (IsPreservedTopLevel(rel)) continue;
            string destination = Path.Combine(target, rel);
            if (File.Exists(destination)) DeleteFileWithRetry(destination);
            File.Move(file, destination, true);
        }
        foreach (string dir in Directory.GetDirectories(source))
        {
            string rel = Path.GetFileName(dir);
            if (IsPreservedTopLevel(rel)) continue;
            string destination = Path.Combine(target, rel);
            if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
            MoveDirectoryWithRetry(dir, destination);
        }
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

    static bool IsPreservedTopLevel(string rel)
    {
        string top = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return _preserveTop.Contains(top);
    }

    static void MoveDirectoryWithRetry(string source, string destination)
    {
        Exception? last = null;
        for (int i = 0; i < 20; i++)
        {
            try { Directory.Move(source, destination); return; }
            catch (IOException ex) { last = ex; Thread.Sleep(250); }
            catch (UnauthorizedAccessException ex) { last = ex; Thread.Sleep(250); }
        }
        throw new IOException($"Could not move directory {source} to {destination}", last);
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

    static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
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
