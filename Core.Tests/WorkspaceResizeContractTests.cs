using System.Runtime.CompilerServices;

internal static class WorkspaceResizeContractTests
{
    [ModuleInitializer]
    internal static void ValidateResponsiveWorkspaceContract()
    {
        string root = Directory.GetCurrentDirectory();
        string responsivePath = Path.Combine(root, "Scripts", "Main.V109Responsive.cs");
        string preferencesPath = Path.Combine(root, "Scripts", "Main.V1023UiPreferences.cs");
        string viewportPath = Path.Combine(root, "Scripts", "Main.V1019ViewportPipeline.cs");
        string acceptancePath = Path.Combine(root, "Scripts", "Main.V1022Acceptance.cs");
        string bottomDockPath = Path.Combine(root, "Scripts", "Main.WorkspaceBottomDock.cs");
        if (!File.Exists(responsivePath) || !File.Exists(preferencesPath) ||
            !File.Exists(viewportPath) || !File.Exists(acceptancePath) || !File.Exists(bottomDockPath))
            throw new InvalidOperationException("TEST FAILED: responsive workspace source files are missing");

        string responsive = File.ReadAllText(responsivePath);
        string preferences = File.ReadAllText(preferencesPath);
        string viewport = File.ReadAllText(viewportPath);
        string acceptance = File.ReadAllText(acceptancePath);
        string bottomDock = File.ReadAllText(bottomDockPath);

        Assert(responsive.Contains("GetViewport().SizeChanged += SyncV109RootToViewport", StringComparison.Ordinal),
            "workspace root is not synchronized to OS/client viewport resize");
        Assert(responsive.Contains("_v109ResponsiveRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft)", StringComparison.Ordinal) &&
               responsive.Contains("_v109ResponsiveRoot.Position = Vector2.Zero", StringComparison.Ordinal) &&
               responsive.Contains("_v109ResponsiveRoot.Size = size", StringComparison.Ordinal),
            "top-level workspace root does not use one explicit client-rect layout authority");
        Assert(responsive.Contains("V1023ReflowWorkspaceSplit();", StringComparison.Ordinal) &&
               !responsive.Contains("_v109ResponsiveSplit.SplitOffset =", StringComparison.Ordinal),
            "legacy responsive layer still competes with persisted splitter preference authority");
        Assert(preferences.Contains("V1023ClampSplit(", StringComparison.Ordinal) &&
               preferences.Contains("int minimumPrimary = Math.Min", StringComparison.Ordinal) &&
               preferences.Contains("int maximumPrimary = Math.Max", StringComparison.Ordinal) &&
               preferences.Contains("return Math.Clamp(requested, minimumPrimary, maximumPrimary);", StringComparison.Ordinal),
            "split bounds are not reduced into a valid range for narrow clients");
        Assert(responsive.Contains("_v109ResponsiveTabs.CustomMinimumSize = Vector2.Zero", StringComparison.Ordinal) &&
               viewport.Contains("host.CustomMinimumSize = Vector2.Zero", StringComparison.Ordinal),
            "hard child minimums can still force workspace geometry beyond the client rect");
        Assert(acceptance.Contains("SyncV109RootToViewport();", StringComparison.Ordinal) &&
               !acceptance.Contains("root.SetAnchorsAndOffsetsPreset", StringComparison.Ordinal),
            "a later acceptance layer still owns a competing client-rect write path");
        Assert(responsive.Contains("SyncWorkspaceBottomDockToClientHeight(size.Y);", StringComparison.Ordinal) &&
               bottomDock.Contains("Math.Clamp(", StringComparison.Ordinal) &&
               bottomDock.Contains("WorkspaceBottomDockCompactHeight", StringComparison.Ordinal),
            "client-height owner does not preserve normal dock height with bounded compact reflow");
        Assert(viewport.Contains("host.Stretch = true", StringComparison.Ordinal),
            "native SubViewportContainer stretch ownership is required for resize correctness");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
