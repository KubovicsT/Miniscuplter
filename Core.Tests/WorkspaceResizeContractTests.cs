using System.Runtime.CompilerServices;

internal static class WorkspaceResizeContractTests
{
    [ModuleInitializer]
    internal static void ValidateResponsiveWorkspaceContract()
    {
        string root = Directory.GetCurrentDirectory();
        string responsivePath = Path.Combine(root, "Scripts", "Main.V109Responsive.cs");
        string viewportPath = Path.Combine(root, "Scripts", "Main.V1019ViewportPipeline.cs");
        if (!File.Exists(responsivePath) || !File.Exists(viewportPath))
            throw new InvalidOperationException("TEST FAILED: responsive workspace source files are missing");

        string responsive = File.ReadAllText(responsivePath);
        string viewport = File.ReadAllText(viewportPath);

        Assert(responsive.Contains("GetViewport().SizeChanged += SyncV109RootToViewport", StringComparison.Ordinal),
            "workspace root is not synchronized to OS/client viewport resize");
        Assert(responsive.Contains("_v109ResponsiveRoot.Size = size", StringComparison.Ordinal),
            "workspace root no longer consumes the full visible client area");
        Assert(responsive.Contains("const float sidebar = 330f", StringComparison.Ordinal),
            "right workspace rail no longer has a fixed intended width");
        Assert(!responsive.Contains("width * 0.27f", StringComparison.Ordinal),
            "responsive layout reintroduced proportional sidebar growth instead of viewport-first resizing");
        Assert(viewport.Contains("host.Stretch = true", StringComparison.Ordinal),
            "native SubViewportContainer stretch ownership is required for resize correctness");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
