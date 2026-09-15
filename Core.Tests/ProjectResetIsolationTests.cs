using System.Runtime.CompilerServices;

internal static class ProjectResetIsolationTests
{
    [ModuleInitializer]
    internal static void ValidateProjectResetIsolation()
    {
        string root = Directory.GetCurrentDirectory();
        string resetPath = Path.Combine(root, "Scripts", "Main.V1036ProjectReset.cs");
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        if (!File.Exists(resetPath) || !File.Exists(installerPath))
            throw new InvalidOperationException("TEST FAILED: project reset isolation source files are missing");

        string reset = File.ReadAllText(resetPath);
        string installer = File.ReadAllText(installerPath);
        Assert(installer.Contains("InstallV1036ProjectResetIsolation();", StringComparison.Ordinal),
            "project reset isolation is not installed after workspace composition");
        foreach (string token in new[] { "_lastCapture = \"\"", "_lastEditedImage = \"\"", "_lastMask = \"\"", "_v03StartingImage = \"\"", "_v1011BaselineImage = \"\"", "SyncV1015CanvasSource(\"\")" })
            Assert(reset.Contains(token, StringComparison.Ordinal), "New project does not retire project-scoped 2D presentation token: " + token);
        Assert(reset.Contains("_v109Generate3D.Disabled = true", StringComparison.Ordinal),
            "New project does not retire prior accepted-baseline generation eligibility");
        Assert(reset.Contains("openButton.Text = \"Open Project\"", StringComparison.Ordinal) &&
               reset.Contains("toolbar.MoveChild(openButton", StringComparison.Ordinal),
            "project Open action is not promoted beside New and may be clipped off the toolbar");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
