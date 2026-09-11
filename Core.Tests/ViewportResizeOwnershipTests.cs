using System.Runtime.CompilerServices;

internal static class ViewportResizeOwnershipTests
{
    [ModuleInitializer]
    internal static void ValidateViewportResizeOwnership()
    {
        string root = Directory.GetCurrentDirectory();
        string legacyPath = Path.Combine(root, "Scripts", "Main.V109Experience.cs");
        string workflowPath = Path.Combine(root, "Scripts", "Main.V1011Workflow.cs");
        string nativePath = Path.Combine(root, "Scripts", "Main.V1019ViewportPipeline.cs");
        if (!File.Exists(legacyPath) || !File.Exists(workflowPath) || !File.Exists(nativePath))
            throw new InvalidOperationException("TEST FAILED: viewport ownership source files are missing");

        string legacy = File.ReadAllText(legacyPath);
        string workflow = File.ReadAllText(workflowPath);
        string native = File.ReadAllText(nativePath);

        Assert(legacy.Contains("host.Resized += V109SyncLegacyViewportSize", StringComparison.Ordinal),
            "historical v1.0.9 resize writer is not a named retireable handler");
        Assert(legacy.Contains("if (_v1019ViewportPipelineInstalled) return;", StringComparison.Ordinal),
            "legacy viewport resize writer does not defer to the native Stretch pipeline");
        Assert(native.Contains("host.Resized -= V109SyncLegacyViewportSize", StringComparison.Ordinal),
            "native viewport pipeline does not explicitly retire the historical resize writer");

        Assert(workflow.Contains("host.Resized += QueueV1011ViewportRepair", StringComparison.Ordinal),
            "v1.0.11 resize-triggered viewport repair is not a named retireable handler");
        Assert(workflow.Contains("if (_v1019ViewportPipelineInstalled) return;", StringComparison.Ordinal),
            "v1.0.11 resize-triggered world repair does not defer to the native pipeline");
        Assert(native.Contains("host.Resized -= QueueV1011ViewportRepair", StringComparison.Ordinal),
            "native viewport pipeline does not retire the v1.0.11 resize-triggered world repair");

        Assert(native.Contains("host.Stretch = true", StringComparison.Ordinal),
            "native viewport pipeline no longer declares SubViewportContainer.Stretch ownership");
        Assert(!native.Contains("sub.Size =", StringComparison.Ordinal),
            "native viewport pipeline reintroduced direct SubViewport size writes");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
