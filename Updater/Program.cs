using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Miniscuplter.Updater;

internal static class Program
{
    const long ExtractionSafetyBytes = 128L * 1024 * 1024;
    const long MaxPackageBytes = 8L * 1024 * 1024 * 1024;
    const long MaxExpandedBytes = 16L * 1024 * 1024 * 1024;
    const int MaxArchiveEntries = 100000;
    const int MaxArgumentLength = 4096;
    const long MaxJournalBytes = 1024 * 1024;
    const int LauncherHealthTimeoutMs = 30000;

    static readonly string[] DefaultPreserveTopLevel =
    {
        "AIData", "Runtime", "Projects", "PartsLibrary", "Exports", "UserData", "launcher.settings.json"
    };

    static readonly string[] PreserveNested =
    {
        Path.Combine("App", "ai_backend", ".venv"),
        Path.Combine("App", "ai_backend", ".runtime-cache"),
        Path.Combine("App", "ai_backend", "data"),
        Path.Combine("ai_backend", ".venv"),
        Path.Combine("ai_backend", ".runtime-cache"),
        Path.Combine("ai_backend", "data")
    };

    // Only these paths are owned by the application updater. Unknown top-level files/folders
    // are left alone instead of being guessed to be disposable application content.
    static readonly string[] ManagedTopLevel =
    {
        "Miniscuplter.Launcher.exe",
        "Miniscuplter.Updater.exe",
        "setup_ai_backend.bat",
        "release.json",
        "App",
        "ai_backend"
    };

    static HashSet<string> _preserveTop = new(DefaultPreserveTopLevel, StringComparer.OrdinalIgnoreCase);
    static string? _nestedDataRoot;

    sealed class UpdateJournal
    {
        public int Schema { get; set; } = 1;
        public string Target { get; set; } = "";
        public string ExpectedVersion { get; set; } = "";
        public string Backup { get; set; } = "";
        public string Parked { get; set; } = "";
        public string Phase { get; set; } = "prepared";
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    }

    static int Main(string[] args)
    {
        string? workRoot = null, stage = null, backup = null, parked = null, dataRoot = null;
        string? target = null, restart = null;
        bool backupComplete = false;
        bool installStarted = false;
        bool preservedRestored = false;

        try
        {
            using var updaterMutex = new Mutex(true, "Local\\MiniscuplterUpdater", out bool mutexCreated);
            if (!mutexCreated)
                throw new InvalidOperationException("Another Miniscuplter updater transaction is already running.");

            var map = Parse(args);
            target = Path.GetFullPath(Require(map, "target"));
            dataRoot = Path.GetFullPath(Require(map, "data-root"));
            ValidateDataRoot(dataRoot);
            ValidateTargetRoot(target, dataRoot);
            string package = ValidatePackagePath(Require(map, "package"), dataRoot);
            string expectedVersion = NormalizeVersion(Require(map, "version"));
            if (!Version.TryParse(expectedVersion, out _))
                throw new ArgumentException("--version must be a valid semantic version.");
            string expectedSha256 = RequireSha256(map);
            int waitPid = map.TryGetValue("wait-pid", out var p) && int.TryParse(p, out var pid) ? pid : -1;
            restart = map.TryGetValue("restart", out var r) ? ValidateRestartPath(r, target) : Path.Combine(target, "Miniscuplter.Launcher.exe");

            if (!File.Exists(package)) throw new FileNotFoundException("Update package not found", package);
            if (!VerifySha256(package, expectedSha256))
                throw new InvalidDataException("Update package SHA-256 does not match the release digest. No installed files were changed.");

            _preserveTop = BuildPreserveSet(target, dataRoot);
            ValidateManagedPaths(target);

            if (waitPid > 0) WaitForExit(waitPid);
            EnsureEditorClosed(target);

            string parent = Directory.GetParent(target)?.FullName
                ?? throw new InvalidOperationException("The application installation directory has no usable parent for transactional update staging.");
            RejectReparsePoints(parent, target);

            RecoverInterruptedTransactions(parent, target);

            long expandedBytes = GetExpandedSize(package);
            long requiredBytes = checked(expandedBytes + Math.Max(ExtractionSafetyBytes, expandedBytes / 10));
            EnsureFreeSpace(parent, requiredBytes, expandedBytes);

            workRoot = Path.Combine(parent, ".MiniscuplterUpdate_" + Guid.NewGuid().ToString("N"));
            stage = Path.Combine(workRoot, "stage");
            backup = Path.Combine(workRoot, "rollback");
            parked = Path.Combine(workRoot, "preserved");
            Directory.CreateDirectory(stage);
            Directory.CreateDirectory(backup);
            Directory.CreateDirectory(parked);
            WriteJournal(workRoot, target, expectedVersion, backup, parked, "prepared");

            ZipFile.ExtractToDirectory(package, stage, true);
            ValidateExtractedTree(stage);
            string source = NormalizePackageRoot(stage);
            ValidateReleasePackage(source, expectedVersion);
            WriteJournal(workRoot, target, expectedVersion, backup, parked, "extracted");

            ParkPreservedNested(target, parked);
            WriteJournal(workRoot, target, expectedVersion, backup, parked, "preserved-parked");

            try
            {
                MoveManagedTreeToBackup(target, backup);
                backupComplete = true;
                WriteJournal(workRoot, target, expectedVersion, backup, parked, "backup-complete");

                installStarted = true;
                InstallManagedTreeFromStage(source, target);
                ValidateInstalledTree(target, expectedVersion);
                WriteJournal(workRoot, target, expectedVersion, backup, parked, "new-installed");

                RestoreParkedNested(parked, target);
                preservedRestored = true;
                WriteJournal(workRoot, target, expectedVersion, backup, parked, "preserved-restored");

                VerifyLauncherStartup(restart, target, workRoot);
                WriteJournal(workRoot, target, expectedVersion, backup, parked, "launcher-healthy");

                backupComplete = false;
                installStarted = false;
                preservedRestored = false;
            }
            catch
            {
                RollbackManagedUpdate(target, backup, parked, backupComplete, installStarted, preservedRestored);
                backupComplete = false;
                installStarted = false;
                preservedRestored = false;
                TryRestartRestoredLauncher(restart, target);
                throw;
            }

            WriteJournal(workRoot, target, expectedVersion, backup, parked, "committed");
            TryDelete(package);
            TryDeleteDirectory(workRoot);
            ScheduleSelfDelete();
            return 0;
        }
        catch (Exception ex)
        {
            try
            {
                if (target != null && backup != null && parked != null && (backupComplete || HasManagedContent(backup)))
                {
                    RollbackManagedUpdate(target, backup, parked, backupComplete, installStarted, preservedRestored);
                    TryRestartRestoredLauncher(restart, target);
                }
                else if (target != null && parked != null && HasParkedContent(parked))
                {
                    RestoreParkedNested(parked, target);
                }
            }
            catch { }

            WriteError(dataRoot, ex, workRoot);
            if (stage != null) TryDeleteDirectory(stage);
            // Keep rollback/preserved data when the transaction did not fully commit.
            return 1;
        }
    }

    static Dictionary<string, string> Parse(string[] args)
    {
        if (args.Length > 64)
            throw new ArgumentException("Too many updater arguments.");
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length)
                throw new ArgumentException("Updater arguments must be named --key value pairs.");
            string key = args[i][2..];
            if (key.Length == 0 || !map.TryAdd(key, args[++i]))
                throw new ArgumentException("Duplicate or empty updater argument: " + key);
        }
        return map;
    }

    static string Require(Dictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Missing --" + key);
        if (value.Length > MaxArgumentLength)
            throw new ArgumentException("--" + key + " is too long.");
        return value;
    }

    static string RequireSha256(Dictionary<string, string> map)
    {
        string value = Require(map, "sha256").Trim().ToLowerInvariant();
        if (value.Length != 64 || !value.All(Uri.IsHexDigit))
            throw new ArgumentException("--sha256 must be a 64-character SHA-256 digest");
        return value;
    }

    static void WaitForExit(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            if (!p.WaitForExit(120000)) throw new TimeoutException("Launcher did not exit in time for update.");
        }
        catch (ArgumentException) { }
    }

    static void EnsureEditorClosed(string target)
    {
        string targetFull = Path.GetFullPath(target);
        string[] expected = {
            Path.Combine(targetFull, "App", "Miniscuplter.exe"),
            Path.Combine(targetFull, "Miniscuplter.exe"),
            Path.Combine(targetFull, "App", "Miniscuplter.console.exe")
        };
        foreach (string processName in new[] { "Miniscuplter", "Miniscuplter.console" })
        {
            var running = Process.GetProcessesByName(processName).Where(p => p.Id != Environment.ProcessId).ToArray();
            try
            {
                foreach (var process in running)
                {
                    if (process.HasExited) continue;
                    try
                    {
                        string? executable = process.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(executable) &&
                            expected.Any(path => Path.GetFullPath(executable).Equals(path, StringComparison.OrdinalIgnoreCase)))
                            throw new InvalidOperationException("The Miniscuplter editor is still running. Close it before applying the application update.");
                    }
                    catch (InvalidOperationException) { throw; }
                    catch
                    {
                        throw new InvalidOperationException("A Miniscuplter editor process could not be identified safely. Close it before applying the application update.");
                    }
                }
            }
            finally
            {
                foreach (var process in running) process.Dispose();
            }
        }
    }

    static HashSet<string> BuildPreserveSet(string target, string dataRoot)
    {
        var result = new HashSet<string>(DefaultPreserveTopLevel, StringComparer.OrdinalIgnoreCase);
        _nestedDataRoot = null;
        string rel;
        try { rel = Path.GetRelativePath(target, dataRoot); }
        catch { return result; }
        if (rel == ".")
            throw new InvalidOperationException("The configured AI DataRoot cannot be the application installation root during self-update.");
        if (Path.IsPathRooted(rel) || rel.StartsWith(".." + Path.DirectorySeparatorChar) || rel.Equals("..", StringComparison.Ordinal))
            return result;

        string top = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        if (top.Equals("App", StringComparison.OrdinalIgnoreCase) || top.Equals("ai_backend", StringComparison.OrdinalIgnoreCase))
        {
            string normalized = NormalizeRelative(rel);
            if (normalized.Equals("App", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("App/ai_backend", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("ai_backend", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The configured AI DataRoot overlaps the managed application/backend root. Move AI data to a dedicated subfolder before self-update.");
            _nestedDataRoot = rel;
        }
        else if (!string.IsNullOrWhiteSpace(top) && top != ".")
        {
            result.Add(top);
        }
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
        if (!File.Exists(path))
            return false;
        var info = new FileInfo(path);
        if (info.Length <= 0 || info.Length > MaxPackageBytes)
            throw new InvalidDataException("Update package is empty or exceeds the maximum package size.");
        using var stream = File.OpenRead(path);
        string actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    static long GetExpandedSize(string package)
    {
        using var archive = ZipFile.OpenRead(package);
        if (new FileInfo(package).Length > MaxPackageBytes)
            throw new InvalidDataException("Update package exceeds the maximum compressed size.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        if (archive.Entries.Count > MaxArchiveEntries)
            throw new InvalidDataException("Update package contains too many archive entries.");

        foreach (var entry in archive.Entries)
        {
            string normalized = ValidateArchiveEntryName(entry.FullName);
            if (!names.Add(normalized))
                throw new InvalidDataException("Update package contains duplicate archive paths.");
            if (IsArchiveSymlink(entry))
                throw new InvalidDataException("Update package contains a symbolic-link entry, which is not allowed.");

            if (entry.Name.Length == 0) continue;
            if (entry.Length < 0)
                throw new InvalidDataException("Update package contains an invalid archive entry size.");
            total = checked(total + entry.Length);
            if (total > MaxExpandedBytes)
                throw new InvalidDataException("Update package expands beyond the maximum allowed size.");
        }

        if (total <= 0) throw new InvalidDataException("Update package contains no extractable files.");
        return total;
    }

    static string ValidateArchiveEntryName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxArgumentLength || value.Contains('\0'))
            throw new InvalidDataException("Update package contains an invalid archive path.");
        string normalized = value.Replace('\\', '/').TrimEnd('/');
        if (normalized.Length == 0 || normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.StartsWith("//", StringComparison.Ordinal) ||
            (normalized.Length > 1 && normalized[1] == ':'))
            throw new InvalidDataException("Update package contains an absolute archive path.");

        foreach (string segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment is "." or ".." || segment.Contains(':'))
                throw new InvalidDataException("Update package contains a traversal or alternate-stream path.");
        }
        return normalized;
    }

    static bool IsArchiveSymlink(ZipArchiveEntry entry)
    {
        int unixMode = (entry.ExternalAttributes >> 16) & 0xF000;
        return unixMode == 0xA000;
    }

    static void ValidateExtractedTree(string root)
    {
        if (!Directory.Exists(root))
            throw new InvalidDataException("Update package extraction did not produce a directory.");
        RejectReparsePoints(root, root);
        var pending = new Stack<string>();
        pending.Push(root);
        int count = 0;
        long total = 0;
        while (pending.Count > 0)
        {
            string current = pending.Pop();
            foreach (string path in Directory.GetFileSystemEntries(current))
            {
                if (++count > MaxArchiveEntries)
                    throw new InvalidDataException("Extracted update contains too many filesystem entries.");
                RejectReparsePoints(root, path);
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Extracted update contains a reparse point.");
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pending.Push(path);
                    continue;
                }

                long length = new FileInfo(path).Length;
                total = checked(total + length);
                if (total > MaxExpandedBytes)
                    throw new InvalidDataException("Extracted update exceeds the maximum allowed size.");
            }
        }
    }

    static long AvailableBytes(string path)
    {
        string root = Path.GetPathRoot(Path.GetFullPath(path))
            ?? throw new InvalidOperationException("Could not resolve update staging drive.");
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
        string[] dirs = Directory.GetDirectories(stage);
        string[] files = Directory.GetFiles(stage);
        return files.Length == 0 && dirs.Length == 1 ? dirs[0] : stage;
    }

    static void RequireReleaseFile(string root, string relative, string description)
    {
        string path = Path.Combine(root, relative);
        RejectReparsePoints(root, path);
        if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.Directory) != 0 ||
            new FileInfo(path).Length == 0)
            throw new InvalidDataException("Update package is incomplete; missing required file: " + description);
    }

    static void ValidateReleasePackage(string source, string expectedVersion)
    {
        string[] required =
        {
            "Miniscuplter.Launcher.exe",
            "Miniscuplter.Updater.exe",
            Path.Combine("App", "Miniscuplter.exe"),
            Path.Combine("App", "ai_backend", "app.py"),
            Path.Combine("App", "ai_backend", "launcher_bridge.py"),
            "setup_ai_backend.bat",
            "release.json"
        };
        foreach (string relative in required) RequireReleaseFile(source, relative, relative);
        ValidateReleaseManifest(Path.Combine(source, "release.json"), expectedVersion);
    }

    static void ValidateInstalledTree(string target, string expectedVersion)
    {
        string[] required =
        {
            "Miniscuplter.Launcher.exe",
            "Miniscuplter.Updater.exe",
            Path.Combine("App", "Miniscuplter.exe"),
            Path.Combine("App", "ai_backend", "app.py"),
            "release.json"
        };
        foreach (string relative in required) RequireReleaseFile(target, relative, relative);
        ValidateReleaseManifest(Path.Combine(target, "release.json"), expectedVersion);
    }

    static void ValidateReleaseManifest(string path, string expectedVersion)
    {
        if (!File.Exists(path) || new FileInfo(path).Length > MaxJournalBytes)
            throw new InvalidDataException("The release manifest is missing or too large.");
        using var doc = JsonDocument.Parse(File.ReadAllBytes(path), new JsonDocumentOptions { MaxDepth = 16 });
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("The release manifest is not a JSON object.");
        string version = doc.RootElement.TryGetProperty("version", out var value)
            ? NormalizeVersion(value.GetString() ?? "")
            : "";
        if (!version.Equals(expectedVersion, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Update package version mismatch. Expected " + expectedVersion + ", package contains " + version + ".");
        string asset = doc.RootElement.TryGetProperty("asset", out var ae) ? ae.GetString() ?? "" : "";
        if (!asset.Equals("Miniscuplter-win-x64.zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Update package manifest does not identify the expected Windows asset.");
    }

    static string NormalizeVersion(string value) => value.Trim().TrimStart('v', 'V').Split('-', '+')[0];

    static void ParkPreservedNested(string target, string parking)
    {
        Directory.CreateDirectory(parking);
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
        if (!Directory.Exists(parking)) return;
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
        Directory.CreateDirectory(backup);
        var moved = new List<(string Source, string Destination, bool Directory)>();
        try
        {
            foreach (string rel in ManagedTopLevel)
            {
                if (_preserveTop.Contains(rel)) continue;
                string source = Path.Combine(target, rel);
                string destination = Path.Combine(backup, rel);
                if (File.Exists(source))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Move(source, destination, true);
                    moved.Add((source, destination, false));
                }
                else if (Directory.Exists(source))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    MoveDirectoryWithRetry(source, destination);
                    moved.Add((source, destination, true));
                }
            }
        }
        catch
        {
            // Best effort immediate restoration. Any item that cannot be restored remains in
            // rollback storage and the caller's transaction recovery will retry without first
            // deleting unrelated/unmoved target content.
            for (int i = moved.Count - 1; i >= 0; i--)
            {
                var item = moved[i];
                try
                {
                    if (item.Directory && Directory.Exists(item.Destination) && !Directory.Exists(item.Source))
                        Directory.Move(item.Destination, item.Source);
                    else if (!item.Directory && File.Exists(item.Destination) && !File.Exists(item.Source))
                        File.Move(item.Destination, item.Source, true);
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
        foreach (string rel in ManagedTopLevel)
        {
            string source = Path.Combine(backup, rel);
            string destination = Path.Combine(target, rel);
            if (File.Exists(source))
            {
                if (File.Exists(destination)) DeleteFileWithRetry(destination);
                File.Move(source, destination, true);
            }
            else if (Directory.Exists(source))
            {
                if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
                MoveDirectoryWithRetry(source, destination);
            }
        }
    }

    static void InstallManagedTreeFromStage(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string rel in ManagedTopLevel)
        {
            if (_preserveTop.Contains(rel)) continue;
            string sourcePath = Path.Combine(source, rel);
            string destination = Path.Combine(target, rel);
            if (File.Exists(sourcePath))
            {
                if (File.Exists(destination)) DeleteFileWithRetry(destination);
                File.Move(sourcePath, destination, true);
            }
            else if (Directory.Exists(sourcePath))
            {
                if (Directory.Exists(destination)) DeleteDirectoryWithRetry(destination);
                MoveDirectoryWithRetry(sourcePath, destination);
            }
        }
    }

    static void RemoveManagedTree(string target)
    {
        if (!Directory.Exists(target)) return;
        foreach (string rel in ManagedTopLevel)
        {
            if (_preserveTop.Contains(rel)) continue;
            string path = Path.Combine(target, rel);
            if (File.Exists(path)) DeleteFileWithRetry(path);
            else if (Directory.Exists(path)) DeleteDirectoryWithRetry(path);
        }
    }

    static bool HasManagedContent(string backup)
    {
        if (!Directory.Exists(backup)) return false;
        return ManagedTopLevel.Any(rel => File.Exists(Path.Combine(backup, rel)) || Directory.Exists(Path.Combine(backup, rel)));
    }

    static bool HasParkedContent(string parking)
    {
        if (!Directory.Exists(parking)) return false;
        return PreservedNestedPaths().Any(rel => Directory.Exists(Path.Combine(parking, rel)));
    }

    static void RollbackManagedUpdate(string target, string backup, string parked, bool backupComplete, bool installStarted, bool preservedRestored)
    {
        if (preservedRestored)
        {
            // Move expensive/runtime data out of the failed new App tree before deleting it.
            ParkPreservedNested(target, parked);
        }

        if (backupComplete)
        {
            if (installStarted) RemoveManagedTree(target);
            RestoreManagedTreeFromBackup(backup, target);
        }
        else if (HasManagedContent(backup))
        {
            // Backup failed part-way. Do NOT remove the target: unmoved old files may still be
            // the only good copy. Restore only the entries that actually reached rollback.
            RestoreManagedTreeFromBackup(backup, target);
        }

        if (HasParkedContent(parked)) RestoreParkedNested(parked, target);
    }

    static void VerifyLauncherStartup(string launcher, string target, string workRoot)
    {
        if (!File.Exists(launcher)) throw new FileNotFoundException("Updated launcher is missing before startup validation.", launcher);
        RejectReparsePoints(target, launcher);
        string token = Path.Combine(workRoot, "launcher-healthy.token");
        TryDelete(token);

        var psi = new ProcessStartInfo(launcher)
        {
            WorkingDirectory = Path.GetDirectoryName(launcher) ?? target,
            UseShellExecute = true
        };
        psi.ArgumentList.Add("--update-health-token");
        psi.ArgumentList.Add(token);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the updated launcher for health validation.");

        try
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < LauncherHealthTimeoutMs)
            {
                if (File.Exists(token) && new FileInfo(token).Length > 0) return;
                if (process.HasExited)
                    throw new InvalidOperationException("The updated launcher exited before confirming startup health (exit code " + process.ExitCode + "). The previous application will be restored.");
                Thread.Sleep(250);
            }
            throw new TimeoutException("The updated launcher did not confirm a healthy startup within 30 seconds. The previous application will be restored.");
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    process.WaitForExit(5000);
                }
            }
            catch { }
        }
    }

    static void TryRestartRestoredLauncher(string? launcher, string target)
    {
        try
        {
            string path = !string.IsNullOrWhiteSpace(launcher) ? launcher : Path.Combine(target, "Miniscuplter.Launcher.exe");
            if (File.Exists(path))
                Process.Start(new ProcessStartInfo(path) { WorkingDirectory = Path.GetDirectoryName(path) ?? target, UseShellExecute = true });
        }
        catch { }
    }

    static void RecoverInterruptedTransactions(string parent, string target)
    {
        foreach (string work in Directory.EnumerateDirectories(parent, ".MiniscuplterUpdate_*", SearchOption.TopDirectoryOnly))
        {
            string journalPath = Path.Combine(work, "update-journal.json");
            if (!File.Exists(journalPath)) continue;
            try
            {
                RejectReparsePoints(parent, work);
                RejectReparsePoints(work, journalPath);
                string json = ReadBoundedText(journalPath, MaxJournalBytes);
                var journal = JsonSerializer.Deserialize<UpdateJournal>(json, new JsonSerializerOptions { MaxDepth = 16 });
                if (journal == null || journal.Schema != 1 ||
                    string.IsNullOrWhiteSpace(journal.ExpectedVersion) ||
                    !Version.TryParse(NormalizeVersion(journal.ExpectedVersion), out _) ||
                    !new[] { "prepared", "extracted", "preserved-parked", "backup-complete", "new-installed", "preserved-restored", "launcher-healthy", "committed" }.Contains(journal.Phase, StringComparer.OrdinalIgnoreCase) ||
                    !Path.GetFullPath(journal.Target).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                    continue;

                string backup = Path.GetFullPath(journal.Backup);
                string parked = Path.GetFullPath(journal.Parked);
                if (!IsWithin(work, backup) || !IsWithin(work, parked) ||
                    backup.Equals(parked, StringComparison.OrdinalIgnoreCase))
                    continue;
                RejectReparsePoints(work, backup);
                RejectReparsePoints(work, parked);

                if (journal.Phase.Equals("committed", StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteDirectory(work);
                    continue;
                }

                bool backupKnownComplete = journal.Phase.Equals("backup-complete", StringComparison.OrdinalIgnoreCase) ||
                                           journal.Phase.Equals("new-installed", StringComparison.OrdinalIgnoreCase) ||
                                           journal.Phase.Equals("preserved-restored", StringComparison.OrdinalIgnoreCase) ||
                                           journal.Phase.Equals("launcher-healthy", StringComparison.OrdinalIgnoreCase);

                if (HasManagedContent(backup))
                {
                    if (backupKnownComplete)
                    {
                        if (Directory.Exists(target))
                        {
                            try { ParkPreservedNested(target, parked); } catch { }
                            try { RemoveManagedTree(target); } catch { }
                        }
                    }
                    RestoreManagedTreeFromBackup(backup, target);
                }
                if (HasParkedContent(parked)) RestoreParkedNested(parked, target);
                TryDeleteDirectory(work);
            }
            catch
            {
                // Leave invalid or partially inaccessible recovery material intact for manual
                // recovery instead of deleting a path that was not fully validated.
            }
        }
    }

    static string ReadBoundedText(string path, long maxBytes)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > maxBytes)
            throw new InvalidDataException("Recovery metadata is missing or too large.");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var builder = new StringBuilder();
        char[] buffer = new char[8192];
        while (true)
        {
            int read = reader.Read(buffer, 0, buffer.Length);
            if (read <= 0) break;
            builder.Append(buffer, 0, read);
            if (builder.Length > maxBytes)
                throw new InvalidDataException("Recovery metadata is too large.");
        }
        return builder.ToString();
    }

    static void WriteJournal(string workRoot, string target, string expectedVersion, string backup, string parked, string phase)
    {
        RejectReparsePoints(workRoot, workRoot);
        string path = Path.Combine(workRoot, "update-journal.json");
        RejectReparsePoints(workRoot, path);
        var journal = new UpdateJournal
        {
            Target = Path.GetFullPath(target),
            ExpectedVersion = expectedVersion,
            Backup = Path.GetFullPath(backup),
            Parked = Path.GetFullPath(parked),
            Phase = phase,
            UpdatedUtc = DateTime.UtcNow
        };
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(journal, new JsonSerializerOptions { WriteIndented = true });
        string temp = Path.Combine(workRoot, ".update-journal-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temp, path, true);
        }
        finally { TryDelete(temp); }
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
            try
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
                return;
            }
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

    static void WriteError(string? dataRoot, Exception ex, string? workRoot)
    {
        string detail = ex + (string.IsNullOrWhiteSpace(workRoot) ? "" : Environment.NewLine + "Recovery work directory: " + workRoot);
        string? destination = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(dataRoot))
            {
                string root = Path.GetFullPath(dataRoot);
                string folder = Path.Combine(root, "update-cache");
                Directory.CreateDirectory(folder);
                RejectReparsePoints(root, folder);
                destination = Path.Combine(folder, "MiniscuplterUpdater.error.txt");
                RejectReparsePoints(root, destination);
            }
        }
        catch { }

        try
        {
            if (destination == null)
            {
                string root = Path.GetFullPath(AppContext.BaseDirectory);
                string folder = Path.Combine(root, ".MiniscuplterUpdateErrors");
                Directory.CreateDirectory(folder);
                RejectReparsePoints(root, folder);
                destination = Path.Combine(folder, "MiniscuplterUpdater.error.txt");
                RejectReparsePoints(root, destination);
            }
            File.WriteAllText(destination, detail);
        }
        catch { }
    }

    static bool IsWithin(string root, string candidate)
    {
        try
        {
            string relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(candidate));
            return !Path.IsPathRooted(relative) &&
                (relative == "." || (relative != ".." &&
                !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal)));
        }
        catch { return false; }
    }

    static void RejectReparsePoints(string root, string candidate)
    {
        string rootFull = Path.GetFullPath(root);
        string candidateFull = Path.GetFullPath(candidate);
        if (!IsWithin(rootFull, candidateFull))
            throw new InvalidDataException("Updater path escapes its validated root.");

        static void CheckNode(string path)
        {
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Refusing a reparse point in updater storage: " + path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }

        DirectoryInfo? current = new DirectoryInfo(rootFull);
        while (current != null)
        {
            CheckNode(current.FullName);
            current = current.Parent;
        }
        CheckNode(candidateFull);

        string relative = Path.GetRelativePath(rootFull, candidateFull);
        string currentPath = rootFull;
        foreach (string part in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, part);
            CheckNode(currentPath);
        }
    }

    static void ValidateDataRoot(string root)
    {
        Directory.CreateDirectory(root);
        RejectReparsePoints(root, root);
    }

    static void ValidateTargetRoot(string target, string dataRoot)
    {
        string fullTarget = Path.GetFullPath(target);
        string volume = Path.GetPathRoot(fullTarget) ?? "";
        if (fullTarget.Equals(volume, StringComparison.OrdinalIgnoreCase) ||
            IsWithin(dataRoot, fullTarget) ||
            fullTarget.Equals(Path.GetFullPath(dataRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The application installation directory is not a safe updater target.");
        Directory.CreateDirectory(fullTarget);
        RejectReparsePoints(fullTarget, fullTarget);
    }

    static string ValidatePackagePath(string package, string dataRoot)
    {
        string cache = Path.Combine(Path.GetFullPath(dataRoot), "update-cache");
        Directory.CreateDirectory(cache);
        RejectReparsePoints(dataRoot, cache);
        string full = Path.GetFullPath(package);
        if (!IsWithin(cache, full) ||
            !(full.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
              full.EndsWith(".zip.partial", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The update package must be inside the persistent Miniscuplter data update cache.");
        RejectReparsePoints(dataRoot, full);
        return full;
    }

    static string ValidateRestartPath(string value, string target)
    {
        string full = Path.GetFullPath(value);
        if (!IsWithin(target, full) ||
            !Path.GetFileName(full).Equals("Miniscuplter.Launcher.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The updater restart path must be the installed Miniscuplter launcher.");
        RejectReparsePoints(target, full);
        return full;
    }

    static void ValidateManagedPaths(string root)
    {
        foreach (string relative in ManagedTopLevel.Concat(PreservedNestedPaths()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string path = Path.Combine(root, relative);
            if (File.Exists(path) || Directory.Exists(path))
                RejectReparsePoints(root, path);
        }
    }

    static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
    }

    static void ScheduleSelfDelete()
    {
        try
        {
            string self = Environment.ProcessPath ?? "";
            if (string.IsNullOrWhiteSpace(self) || !File.Exists(self)) return;
            var psi = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add($"ping 127.0.0.1 -n 3 > nul & del /f /q \"{self}\"");
            Process.Start(psi);
        }
        catch { }
    }
}
