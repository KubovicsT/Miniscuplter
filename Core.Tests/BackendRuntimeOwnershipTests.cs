using System.Runtime.CompilerServices;

internal static class BackendRuntimeOwnershipTests
{
    [ModuleInitializer]
    internal static void ValidateBackendRuntimeOwnership()
    {
        string root = Directory.GetCurrentDirectory();
        string backendLauncherPath = Path.Combine(root, "Scripts", "BackendLauncher.cs");
        string repairPath = Path.Combine(root, "Launcher", "RuntimeSetupService.cs");
        if (!File.Exists(backendLauncherPath) || !File.Exists(repairPath))
            throw new InvalidOperationException("TEST FAILED: backend runtime ownership source files are missing");

        string backend = File.ReadAllText(backendLauncherPath);
        string repair = File.ReadAllText(repairPath);

        Assert(backend.Contains("Path.Combine(Path.GetDirectoryName(app)!, \".venv\", \"Scripts\", \"python.exe\")", StringComparison.Ordinal),
            "editor backend launcher does not use the repaired backend-local virtual environment");
        Assert(!backend.Contains("Path.Combine(root, \"Runtime\", \"Python\", \"python.exe\")", StringComparison.Ordinal),
            "editor backend launcher still prefers the unrelated embedded Runtime/Python interpreter");
        Assert(!backend.Contains("?? \"python\"", StringComparison.Ordinal),
            "editor backend launcher still silently falls back to arbitrary PATH Python");
        Assert(backend.Contains("recent stderr", StringComparison.Ordinal) && backend.Contains("interpreter:", StringComparison.Ordinal),
            "backend startup failure does not expose interpreter/path/stderr diagnostics");

        Assert(repair.Contains("VerifyBackendHealthAsync", StringComparison.Ordinal),
            "Repair AI Runtime does not validate backend startup health");
        Assert(repair.Contains("Path.Combine(backendDir, \".venv\", \"Scripts\", \"python.exe\")", StringComparison.Ordinal),
            "Repair health validation does not use the same backend-local virtual environment");
        Assert(repair.Contains("http://127.0.0.1:7868/health", StringComparison.Ordinal),
            "Repair success does not require a real backend health response");
        Assert(repair.Contains("backend.Kill(entireProcessTree: true)", StringComparison.Ordinal),
            "Repair health probe does not clean up its temporary backend process tree");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
