using Godot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Miniscuplter;

/// <summary>
/// Single authoritative location for all Miniscuplter-generated data, model state,
/// runtime caches, logs and temporary work. The launcher supplies MINISCULPTER_DATA
/// in normal packaged use; the fallback also works when the editor is run directly.
/// </summary>
public static class AppDataRoot
{
    static readonly object Gate = new();
    static string? _root;
    static string? _installRoot;

    public static string InstallRoot
    {
        get
        {
            lock (Gate)
            {
                _installRoot ??= ResolveInstallRoot();
                return _installRoot;
            }
        }
    }

    public static string Root
    {
        get
        {
            lock (Gate)
            {
                if (_root == null)
                {
                    string configured = Environment.GetEnvironmentVariable("MINISCULPTER_DATA") ?? "";
                    _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
                        ? Path.Combine(InstallRoot, "AIData")
                        : configured);
                    Directory.CreateDirectory(_root);
                }
                return _root;
            }
        }
    }

    public static string Resolve(string relativePath)
    {
        string root = Root;
        string candidate = Path.IsPathRooted(relativePath)
            ? Path.GetFullPath(relativePath)
            : Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        bool inside = candidate.Equals(root, GetComparison())
            || candidate.StartsWith(prefix, GetComparison());
        if (!inside)
            throw new InvalidOperationException($"Path escapes the Miniscuplter data root: {relativePath}");

        string? parent = Path.GetDirectoryName(candidate);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
        return candidate;
    }

    public static string DirectoryFor(string relativePath)
    {
        string path = Resolve(relativePath);
        Directory.CreateDirectory(path);
        return path;
    }

    public static void ApplyEnvironment(IDictionary<string, string> environment)
    {
        string data = Root;
        string cache = DirectoryFor("Cache");
        string temp = DirectoryFor("Temp");
        string appData = DirectoryFor(Path.Combine("System", "AppData", "Roaming"));
        string localAppData = DirectoryFor(Path.Combine("System", "AppData", "Local"));

        environment["MINISCULPTER_ROOT"] = InstallRoot;
        environment["MINISCULPTER_DATA"] = data;
        environment["APPDATA"] = appData;
        environment["LOCALAPPDATA"] = localAppData;
        environment["TEMP"] = temp;
        environment["TMP"] = temp;
        environment["HF_HOME"] = Path.Combine(cache, "HuggingFace");
        environment["HUGGINGFACE_HUB_CACHE"] = Path.Combine(cache, "HuggingFace", "hub");
        environment["TRANSFORMERS_CACHE"] = Path.Combine(cache, "HuggingFace", "transformers");
        environment["TORCH_HOME"] = Path.Combine(cache, "Torch");
        environment["PIP_CACHE_DIR"] = Path.Combine(cache, "Pip");
        environment["XDG_CACHE_HOME"] = cache;
        environment["PYTHONPYCACHEPREFIX"] = Path.Combine(cache, "PythonBytecode");

        foreach (string path in new[]
        {
            environment["HF_HOME"], environment["HUGGINGFACE_HUB_CACHE"],
            environment["TRANSFORMERS_CACHE"], environment["TORCH_HOME"],
            environment["PIP_CACHE_DIR"], environment["XDG_CACHE_HOME"],
            environment["PYTHONPYCACHEPREFIX"]
        })
            Directory.CreateDirectory(path);
    }

    static string ResolveInstallRoot()
    {
        string projectRoot = ProjectSettings.GlobalizePath("res://");
        if (Directory.Exists(Path.Combine(projectRoot, "Scripts")))
            return Path.GetFullPath(projectRoot);

        string executable = OS.GetExecutablePath();
        string appDirectory = string.IsNullOrWhiteSpace(executable)
            ? projectRoot
            : Path.GetDirectoryName(executable) ?? projectRoot;
        if (Path.GetFileName(appDirectory).Equals("App", StringComparison.OrdinalIgnoreCase))
            appDirectory = Directory.GetParent(appDirectory)?.FullName ?? appDirectory;
        return Path.GetFullPath(appDirectory);
    }

    static StringComparison GetComparison() =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
