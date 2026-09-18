using System.Runtime.CompilerServices;

internal static class WorkspaceBottomDockTests
{
    [ModuleInitializer]
    internal static void ValidateWorkspaceBottomDockComposition()
    {
        string root = Directory.GetCurrentDirectory();
        string dockPath = Path.Combine(root, "Scripts", "Main.WorkspaceBottomDock.cs");
        string telemetryPath = Path.Combine(root, "Scripts", "Main.ResourceTelemetry.cs");
        string consolePath = Path.Combine(root, "Scripts", "Main.AiCommandConsole.cs");
        if (!File.Exists(dockPath) || !File.Exists(telemetryPath) || !File.Exists(consolePath))
            throw new InvalidOperationException("TEST FAILED: workspace composition sources are missing");

        string dock = File.ReadAllText(dockPath);
        string telemetry = File.ReadAllText(telemetryPath);
        string console = File.ReadAllText(consolePath);

        Assert(dock.Contains("WorkspaceTelemetryDock", StringComparison.Ordinal) &&
               dock.Contains("WorkspaceCommandDock", StringComparison.Ordinal),
            "workspace does not expose dedicated telemetry and command dock regions");
        Assert(dock.Contains("WorkspaceBottomDockScroll", StringComparison.Ordinal) &&
               dock.Contains("WorkspaceBottomDockNormalHeight = 188f", StringComparison.Ordinal) &&
               dock.Contains("WorkspaceBottomDockCompactHeight = 104f", StringComparison.Ordinal) &&
               dock.Contains("SyncWorkspaceBottomDockToClientHeight", StringComparison.Ordinal) &&
               dock.Contains("HorizontalScrollMode = ScrollContainer.ScrollMode.Auto", StringComparison.Ordinal) &&
               dock.Contains("VerticalScrollMode = ScrollContainer.ScrollMode.Auto", StringComparison.Ordinal) &&
               dock.Contains("dockScroll.AddChild(dock);", StringComparison.Ordinal),
            "bottom dock content is not reachable through a bounded two-axis scroll viewport");
        Assert(dock.Contains("telemetry.Reparent(_workspaceTelemetryDock, false);", StringComparison.Ordinal) &&
               dock.Contains("telemetry.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);", StringComparison.Ordinal) &&
               dock.Contains("telemetry.OffsetLeft = 0;", StringComparison.Ordinal) &&
               dock.Contains("telemetry.OffsetBottom = 0;", StringComparison.Ordinal),
            "docked telemetry retains legacy viewport-overlay layout authority");
        Assert(console.Contains("_workspaceCommandDock.AddChild(panel);", StringComparison.Ordinal),
            "AI command console is not composed into the dedicated command dock");
        Assert(telemetry.Contains("host.AddChild(panel);", StringComparison.Ordinal),
            "telemetry bootstrap contract changed unexpectedly; reconciliation must remain explicit");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
