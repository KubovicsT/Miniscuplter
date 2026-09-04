using System.Diagnostics;
using System.IO.Compression;

namespace Miniscuplter.Updater;

internal static class Program
{
    static readonly string[] PreserveTopLevel = { "AIData", "Projects", "PartsLibrary", "Exports", "UserData", "launcher.settings.json" };

    static int Main(string[] args)
    {
        try
        {
            var map = Parse(args);
            string package = Require(map, "package");
            string target = Require(map, "target");
            int waitPid = map.TryGetValue("wait-pid", out var p) && int.TryParse(p, out var pid) ? pid : -1;
            string restart = map.TryGetValue("restart", out var r) ? r : Path.Combine(target, "Miniscuplter.Launcher.exe");
            if (waitPid > 0) WaitForExit(waitPid);
            if (!File.Exists(package)) throw new FileNotFoundException("Update package not found", package);
            Directory.CreateDirectory(target);
            string stage = Path.Combine(Path.GetTempPath(), "MiniscuplterUpdate_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);
            ZipFile.ExtractToDirectory(package, stage, true);
            string source = NormalizePackageRoot(stage);
            CopyTree(source, target);
            try { File.Delete(package); } catch { }
            try { Directory.Delete(stage, true); } catch { }
            if (File.Exists(restart)) Process.Start(new ProcessStartInfo(restart) { WorkingDirectory = Path.GetDirectoryName(restart) ?? target, UseShellExecute = true });
            return 0;
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "MiniscuplterUpdater.error.txt"), ex.ToString()); } catch { }
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

    static string NormalizePackageRoot(string stage)
    {
        string[] dirs = Directory.GetDirectories(stage); string[] files = Directory.GetFiles(stage);
        return files.Length == 0 && dirs.Length == 1 ? dirs[0] : stage;
    }

    static void CopyTree(string source, string target)
    {
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(source, dir);
            if (IsPreserved(rel)) continue;
            Directory.CreateDirectory(Path.Combine(target, rel));
        }
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(source, file);
            if (IsPreserved(rel)) continue;
            string dest = Path.Combine(target, rel); Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            CopyWithRetry(file, dest);
        }
    }

    static bool IsPreserved(string rel)
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
}
