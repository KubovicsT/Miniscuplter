using System.Runtime.CompilerServices;

internal static class RotateDirectManipulationTests
{
    [ModuleInitializer]
    internal static void ValidateRotateDirectManipulation()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "Main.V1032DirectManipulation.cs");
        if (!File.Exists(path)) throw new InvalidOperationException("TEST FAILED: direct-manipulation source is missing");
        string source = File.ReadAllText(path);

        Assert(source.Contains("_v1018Tool == V1018ViewportTool.Rotate &&", StringComparison.Ordinal) &&
               source.Contains("V1018RaycastScene(screenPosition, out MeshInstance3D rotateTarget)", StringComparison.Ordinal) &&
               source.Contains("BeginV1018Transform(Vector3.Up);", StringComparison.Ordinal),
            "Rotate tool does not start a transform when the user directly drags the selected/model surface");
        Assert(source.Contains("V1032TryHitRotationRing", StringComparison.Ordinal),
            "axis-constrained rotation rings must remain available alongside direct object rotation");
        Assert(source.Contains("_v1017Gizmo.GlobalPosition = _selected.GlobalPosition;", StringComparison.Ordinal),
            "visible rotate rings do not share the MeshInstance object-origin pivot used by the actual transform");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
