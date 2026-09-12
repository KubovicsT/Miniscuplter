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
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        foreach (string path in new[] { consolePath, cubePath, toolsPath, densityPath, installerPath })
            if (!File.Exists(path)) throw new InvalidOperationException("TEST FAILED: workspace acceptance source missing: " + path);

        string console = File.ReadAllText(consolePath);
        string cube = File.ReadAllText(cubePath);
        string tools = File.ReadAllText(toolsPath);
        string density = File.ReadAllText(densityPath);
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
        Assert(console.Contains("Control.LayoutPreset.CenterBottom", StringComparison.Ordinal) &&
               console.Contains("var actionRow = controls;", StringComparison.Ordinal),
            "AI command console is not bottom-centered with vertical contextual actions");
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
        Assert(telemetry.Contains("Control.LayoutPreset.BottomLeft", StringComparison.Ordinal) &&
               telemetry.Contains("Columns = 2", StringComparison.Ordinal),
            "resource telemetry is not a compact 2x2 bottom-left viewport overlay");

        Assert(installer.IndexOf("InstallViewCube();", StringComparison.Ordinal) <
               installer.IndexOf("InstallWorkspaceDensity();", StringComparison.Ordinal),
            "workspace acceptance layers compose out of order");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
