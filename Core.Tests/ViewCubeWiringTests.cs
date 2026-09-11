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
        Assert(cube.Contains("new SubViewport", StringComparison.Ordinal) &&
               cube.Contains("new BoxMesh", StringComparison.Ordinal) &&
               cube.Contains("OrientationCubePresentationCamera", StringComparison.Ordinal),
            "orientation control is not a real rendered 3D cube");
        Assert(cube.Contains("basis = _camera.GlobalTransform.Basis.Orthonormalized()", StringComparison.Ordinal),
            "orientation cube does not mirror the authoritative viewport camera");
        Assert(cube.Contains("ViewCubeGuiInput", StringComparison.Ordinal) &&
               cube.Contains("TryHitOrientationCube", StringComparison.Ordinal) &&
               cube.Contains("axes >= 3 ? \"Corner\" : axes == 2 ? \"Edge\"", StringComparison.Ordinal),
            "orientation cube does not support face/edge/corner interaction");
        Assert(cube.Contains("host.GuiInput += ViewCubeObserveViewportInput", StringComparison.Ordinal),
            "selected-object orbit pivot observer missing");
        Assert(cube.Contains("FocusCameraOnSelectionPreservingPosition", StringComparison.Ordinal),
            "selected-object focus helper missing");
        Assert(cube.Contains("_yaw = Mathf.Atan2", StringComparison.Ordinal) &&
               cube.Contains("_pitch = Math.Clamp", StringComparison.Ordinal) &&
               cube.Contains("UpdateCamera();", StringComparison.Ordinal),
            "view snapping does not reuse authoritative camera state");
        Assert(!cube.Contains("ProjectStore", StringComparison.Ordinal),
            "view cube must remain presentation-only");
        Assert(!cube.Contains("_orbiting =", StringComparison.Ordinal),
            "view cube must not own orbit interaction state");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
