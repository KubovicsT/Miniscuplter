using System;
using System.IO;
using System.Runtime.CompilerServices;

internal static class ProjectStoreSaveExtensionContractTests
{
    [ModuleInitializer]
    internal static void ValidateSaveExtensionGuard()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Core", "ProjectStore.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("LD_FAIL_20260919_1002: ProjectStore source is missing");

        string source = File.ReadAllText(path);
        int method = source.IndexOf("public async Task SaveAsync(ProjectState state, string projectPath", StringComparison.Ordinal);
        int gate = method >= 0
            ? source.IndexOf("await _saveGate.WaitAsync(cancellationToken);", method, StringComparison.Ordinal)
            : -1;
        Require(method >= 0 && gate > method, "SaveAsync anchor or save gate is missing");

        string body = source[method..gate];
        Require(body.Contains("string.IsNullOrWhiteSpace(projectPath)", StringComparison.Ordinal) &&
                body.Contains("Path.GetExtension(projectPath)", StringComparison.Ordinal) &&
                body.Contains("ProjectExtension", StringComparison.Ordinal) &&
                body.Contains("StringComparison.OrdinalIgnoreCase", StringComparison.Ordinal) &&
                body.Contains("ArgumentException", StringComparison.Ordinal) &&
                body.Contains("nameof(projectPath)", StringComparison.Ordinal),
            "save destination extension guard is absent or does not name projectPath");
        Require(body.IndexOf("ArgumentException", StringComparison.Ordinal) <
                body.IndexOf("await _saveGate.WaitAsync(cancellationToken);", StringComparison.Ordinal),
            "save extension rejection occurs after the save gate");
        Console.WriteLine("LD_PASS_20260919_1002");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("LD_FAIL_20260919_1002: " + message);
    }
}