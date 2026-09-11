using System.Runtime.CompilerServices;

internal static class ViewportResizeOwnershipTests
{
    [ModuleInitializer]
    internal static void ValidateViewportResizeOwnership()
    {
        string root = Directory.GetCurrentDirectory();
        string legacyPath = Path.Combine(root, "Scripts", "Main.V109Experience.cs");
        string nativePath = Path.Combine(root, "Scripts", "Main.V1019ViewportPipeline.cs");
        if (!File.Exists(legacyPath) || !File.Exists(nativePath))
            throw new InvalidOperationException("TEST FAILED: viewport ownership source files are missing");

        string legacy = File.ReadAllText(legacyPath);
        string native = File.ReadAllText(nativePath);

        Assert(legacy.Contains("host.Resized += V109SyncLegacyViewportSize", StringComparison.Ordinal),
            "historical v1.0.9 resize writer is not a named retireable handler");
        Assert(legacy.Contains("if (_v1019ViewportPipelineInstalled) return;", StringComparison.Ordinal),
            "legacy viewport resize writer does not defer to the native Stretch pipeline");
        Assert(native.Contains("host.Resized -= V109SyncLegacyViewportSize", StringComparison.Ordinal),
            "native viewport pipeline does not explicitly retire the historical resize writer");
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
