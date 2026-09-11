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

        Assert(backend.Contains("Path.Combine(backendDir, \".venv\", \"Scripts\", \"python.exe\")", StringComparison.Ordinal),
            "editor backend launcher does not use the repaired backend-local virtual environment");
        Assert(backend.Contains("serve.py", StringComparison.Ordinal) &&
               backend.Contains("--instance-token", StringComparison.Ordinal) &&
               backend.Contains("instance_token", StringComparison.Ordinal),
            "editor backend launcher does not use the canonical instance-bound server contract");
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
        Assert(repair.Contains("ReserveLoopbackPort", StringComparison.Ordinal) &&
               repair.Contains("serve.py", StringComparison.Ordinal) &&
               repair.Contains("--instance-token", StringComparison.Ordinal) &&
               repair.Contains("instance_token", StringComparison.Ordinal),
            "Repair success does not require its own isolated instance-bound backend health response");
        Assert(repair.Contains("backend.Kill(entireProcessTree: true)", StringComparison.Ordinal),
            "Repair health probe does not clean up its temporary backend process tree");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
