using System.Runtime.CompilerServices;

internal static class WorkspaceAcceptanceTests
{
    [ModuleInitializer]
    internal static void ValidateCompactWorkspaceTranche()
    {
        string root = Directory.GetCurrentDirectory();
        string consolePath = Path.Combine(root, "Scripts", "Main.AiCommandConsole.cs");
        string cubePath = Path.Combine(root, "Scripts", "Main.ViewCube.cs");
        string toolsPath = Path.Combine(root, "Scripts", "Main.V1023ViewportToolStrip.cs");
        string densityPath = Path.Combine(root, "Scripts", "Main.WorkspaceDensity.cs");
        string bottomDockPath = Path.Combine(root, "Scripts", "Main.WorkspaceBottomDock.cs");
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        foreach (string path in new[] { consolePath, cubePath, toolsPath, densityPath, bottomDockPath, installerPath })
            if (!File.Exists(path)) throw new InvalidOperationException("TEST FAILED: workspace acceptance source missing: " + path);

        string console = File.ReadAllText(consolePath);
        string cube = File.ReadAllText(cubePath);
        string tools = File.ReadAllText(toolsPath);
        string density = File.ReadAllText(densityPath);
        string bottomDock = File.ReadAllText(bottomDockPath);
        string installer = File.ReadAllText(installerPath);

        Assert(console.Contains("TextEdit? _aiCommandInput", StringComparison.Ordinal) &&
               console.Contains("CustomMinimumSize = new Vector2(0, 72)", StringComparison.Ordinal) &&
               console.Contains("AI Command History", StringComparison.Ordinal) &&
               console.Contains("ScrollContainer", StringComparison.Ordinal) &&
               console.Contains("Text = \"Run\"", StringComparison.Ordinal),
            "AI command surface is not multi-line, scrollable, and explicitly runnable");
        Assert(console.Contains("CtrlPressed", StringComparison.Ordinal) &&
               console.Contains("NavigateAiCommandHistory", StringComparison.Ordinal),
            "AI command keyboard history navigation missing");
        Assert(console.Contains("_workspaceCommandDock.AddChild(panel)", StringComparison.Ordinal) &&
               console.Contains("var actionRow = new HBoxContainer", StringComparison.Ordinal) &&
               !console.Contains("Control.LayoutPreset.CenterBottom", StringComparison.Ordinal),
            "AI command console is not a dedicated non-overlay bottom-center workspace surface");
        Assert(console.Contains("DispatchAiConsoleActionAsync", StringComparison.Ordinal) &&
               console.Contains("V1020Generate3DAsync", StringComparison.Ordinal) &&
               !console.Contains("new AIClient", StringComparison.Ordinal),
            "AI console bypasses or duplicates authoritative action dispatch");

        Assert(cube.Contains("sealed partial class ViewAxisGizmo", StringComparison.Ordinal) &&
               cube.Contains("DrawArc(center, ring", StringComparison.Ordinal) &&
               cube.Contains("Text = \"X\"", StringComparison.Ordinal) &&
               cube.Contains("Text = \"Y\"", StringComparison.Ordinal) &&
               cube.Contains("Text = \"Z\"", StringComparison.Ordinal) &&
               !cube.Contains("new BoxMesh", StringComparison.Ordinal),
            "orientation selector must remain a circular XYZ gizmo without a cube");
        Assert(cube.Contains("_viewAxisGizmo.SetViewBasis(_camera.GlobalTransform.Basis)", StringComparison.Ordinal) &&
               cube.Contains("UpdateCamera();", StringComparison.Ordinal),
            "orientation XYZ gizmo is not synchronized from the authoritative viewport camera");

        Assert(tools.Contains("⌖", StringComparison.Ordinal) &&
               tools.Contains("↔", StringComparison.Ordinal) &&
               tools.Contains("⟳", StringComparison.Ordinal) &&
               tools.Contains("⤢", StringComparison.Ordinal) &&
               tools.Contains("✎", StringComparison.Ordinal),
            "compact icon viewport tool strip missing");
        Assert(tools.Contains("text.StartsWith(\"RMB orbit\"", StringComparison.Ordinal) &&
               tools.Contains("label.Visible = false", StringComparison.Ordinal),
            "permanent viewport instruction paragraph remains visible");

        Assert(density.Contains("V1023ReplaceWorkflowExplanationsWithInfo", StringComparison.Ordinal) &&
               density.Contains("Text = \"ⓘ\"", StringComparison.Ordinal) &&
               density.Contains("TooltipText = text", StringComparison.Ordinal),
            "workflow explanatory prose is not moved behind compact info hover help");

        string telemetry = File.ReadAllText(Path.Combine(root, "Scripts", "Main.ResourceTelemetry.cs"));
        Assert(telemetry.Contains("Columns = 2", StringComparison.Ordinal),
            "resource telemetry compact 2x2 presentation missing");
        Assert(bottomDock.Contains("WorkspaceTelemetryDock", StringComparison.Ordinal) &&
               bottomDock.Contains("WorkspaceCommandDock", StringComparison.Ordinal) &&
               bottomDock.Contains("WorkspaceRightRailGutter", StringComparison.Ordinal) &&
               bottomDock.Contains("telemetry.Reparent(_workspaceTelemetryDock, false)", StringComparison.Ordinal) &&
               installer.Contains("ReconcileWorkspaceBottomDock();", StringComparison.Ordinal),
            "telemetry/AI workspace surfaces are not composed outside the viewport with fixed right-rail alignment");

        Assert(installer.IndexOf("InstallViewCube();", StringComparison.Ordinal) <
               installer.IndexOf("InstallWorkspaceDensity();", StringComparison.Ordinal),
            "workspace acceptance layers compose out of order");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
