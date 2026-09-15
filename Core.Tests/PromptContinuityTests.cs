using System.Runtime.CompilerServices;

internal static class PromptContinuityTests
{
    [ModuleInitializer]
    internal static void ValidateProjectPromptContinuity()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Scripts", "Main.V1036PromptContinuity.cs");
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        if (!File.Exists(path) || !File.Exists(installerPath))
            throw new InvalidOperationException("TEST FAILED: prompt-continuity source is missing");

        string source = File.ReadAllText(path);
        string installer = File.ReadAllText(installerPath);
        Assert(source.Contains("ui.last_prompt", StringComparison.Ordinal) &&
               source.Contains("current.Metadata.TryGetValue", StringComparison.Ordinal) &&
               source.Contains("state.WithMetadata", StringComparison.Ordinal),
            "prompt text is not restored from and persisted into project metadata");
        Assert(source.Contains("session.Execute(\"Update concept prompt\"", StringComparison.Ordinal) &&
               source.Contains("await V1020SaveSessionAsync();", StringComparison.Ordinal),
            "prompt persistence does not use transactional Core project state and durable save");
        Assert(installer.Contains("main.InstallV1036PromptContinuity();", StringComparison.Ordinal),
            "prompt continuity is not composed by the installer");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
