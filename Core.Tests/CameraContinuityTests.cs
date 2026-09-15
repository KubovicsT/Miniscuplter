using System.Runtime.CompilerServices;

internal static class CameraContinuityTests
{
    [ModuleInitializer]
    internal static void ValidateWorkflowTabCameraContinuity()
    {
        string root = Directory.GetCurrentDirectory();
        string continuityPath = Path.Combine(root, "Scripts", "Main.V1036CameraContinuity.cs");
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        if (!File.Exists(continuityPath) || !File.Exists(installerPath))
            throw new InvalidOperationException("TEST FAILED: camera-continuity source is missing");

        string continuity = File.ReadAllText(continuityPath);
        string installer = File.ReadAllText(installerPath);
        Assert(continuity.Contains("tabs.TabChanged -= V1019WorkflowTabChanged", StringComparison.Ordinal),
            "v1.0.19 tab-change camera-reset handler is not explicitly retired");
        Assert(!continuity.Contains("FrameSelected()", StringComparison.Ordinal) &&
               !continuity.Contains("UpdateCamera()", StringComparison.Ordinal),
            "workflow-tab continuity repair must not rewrite camera framing/orbit state");
        Assert(installer.Contains("main.InstallV1036CameraContinuity();", StringComparison.Ordinal),
            "camera-continuity repair is not composed by the installer");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
