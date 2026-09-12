using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

internal static class RuntimeRepairHealthProtocolTests
{
    [ModuleInitializer]
    internal static void ValidateRuntimeRepairHealthProtocol()
    {
        string root = Directory.GetCurrentDirectory();
        string servicePath = Path.Combine(root, "Launcher", "RuntimeSetupService.cs");
        string launcherProjectPath = Path.Combine(root, "Launcher", "Miniscuplter.Launcher.csproj");
        string backendPath = Path.Combine(root, "ai_backend", "app.py");
        if (!File.Exists(servicePath) || !File.Exists(launcherProjectPath) || !File.Exists(backendPath))
            throw new InvalidOperationException("TEST FAILED: runtime repair/version source files are missing");

        string service = File.ReadAllText(servicePath);
        string launcherProject = File.ReadAllText(launcherProjectPath);
        string backend = File.ReadAllText(backendPath);

        Assert(service.Contains("typeof(RuntimeSetupService).Assembly.GetName().Version?.ToString(3)", StringComparison.Ordinal),
            "Runtime Repair does not derive the expected backend version from the packaged launcher assembly");
        Assert(service.Contains("version == expectedVersion && token == instanceToken", StringComparison.Ordinal),
            "Runtime Repair health acceptance is not bound to both packaged version and isolated instance identity");
        Assert(!service.Contains("version == \"1.0.26\"", StringComparison.Ordinal),
            "Runtime Repair still contains the stale v1.0.26 backend health gate");

        Match launcherVersion = Regex.Match(launcherProject, @"<Version>([^<]+)</Version>");
        Match backendVersion = Regex.Match(backend, "APP_VERSION\\s*=\\s*\"([^\"]+)\"");
        Assert(launcherVersion.Success && backendVersion.Success,
            "could not resolve launcher/backend versions for Runtime Repair drift regression");
        Assert(launcherVersion.Groups[1].Value == backendVersion.Groups[1].Value,
            $"packaged launcher/backend versions drifted: launcher {launcherVersion.Groups[1].Value}, backend {backendVersion.Groups[1].Value}");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
