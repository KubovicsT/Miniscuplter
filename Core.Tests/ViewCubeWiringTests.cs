using System.Runtime.CompilerServices;

internal static class ViewCubeWiringTests
{
    [ModuleInitializer]
    internal static void ValidateViewCubeComposition()
    {
        string root = Directory.GetCurrentDirectory();
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        string cubePath = Path.Combine(root, "Scripts", "Main.ViewCube.cs");
        if (!File.Exists(installerPath) || !File.Exists(cubePath))
            throw new InvalidOperationException("TEST FAILED: view-cube source/composition files are missing");

        string installer = File.ReadAllText(installerPath);
        string cube = File.ReadAllText(cubePath);

        Assert(installer.Contains("InstallViewCube();", StringComparison.Ordinal), "view cube is not composed");
        Assert(installer.IndexOf("InstallSceneHierarchy();", StringComparison.Ordinal) < installer.IndexOf("InstallViewCube();", StringComparison.Ordinal), "view cube must compose after the earlier MS-027 presentation slices");
        Assert(cube.Contains("AddViewCubeFace(\"Front\"", StringComparison.Ordinal) && cube.Contains("AddViewCubeFace(\"Back\"", StringComparison.Ordinal), "front/back face snaps missing");
        Assert(cube.Contains("AddViewCubeFace(\"Left\"", StringComparison.Ordinal) && cube.Contains("AddViewCubeFace(\"Right\"", StringComparison.Ordinal), "left/right face snaps missing");
        Assert(cube.Contains("AddViewCubeFace(\"Top\"", StringComparison.Ordinal) && cube.Contains("AddViewCubeFace(\"Bottom\"", StringComparison.Ordinal), "top/bottom face snaps missing");
        Assert(cube.Contains("host.GuiInput += ViewCubeObserveViewportInput", StringComparison.Ordinal), "selected-object orbit pivot observer missing");
        Assert(cube.Contains("FocusCameraOnSelectionPreservingPosition", StringComparison.Ordinal), "selected-object focus helper missing");
        Assert(cube.Contains("_yaw = Mathf.Atan2", StringComparison.Ordinal) && cube.Contains("_pitch = Math.Clamp", StringComparison.Ordinal), "orbit retarget does not reuse existing camera state");
        Assert(!cube.Contains("new Camera3D", StringComparison.Ordinal), "view cube must not create a second camera owner");
        Assert(!cube.Contains("_orbiting =", StringComparison.Ordinal), "view cube must not own orbit interaction state");
        Assert(!cube.Contains("ProjectStore", StringComparison.Ordinal), "view cube must remain presentation-only");
        Assert(!cube.Contains("SubViewport.Size =", StringComparison.Ordinal), "view cube must not reintroduce viewport sizing ownership");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
