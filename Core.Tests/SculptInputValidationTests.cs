using System.Runtime.CompilerServices;

internal static class SculptInputValidationTests
{
    [ModuleInitializer]
    internal static void ValidateSculptInputBoundaries()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "SculptEngine.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: SculptEngine source is missing");

        string source = File.ReadAllText(path);
        int apply = source.IndexOf("public static ArrayMesh Apply(ArrayMesh mesh", StringComparison.Ordinal);
        int advanced = source.IndexOf("public static ArrayMesh ApplyAdvanced(ArrayMesh mesh", StringComparison.Ordinal);
        int helpers = source.IndexOf("static float DistanceToClosest", StringComparison.Ordinal);
        Require(apply >= 0 && advanced > apply && helpers > advanced, "sculpt entry points are missing");

        string applyBody = source[apply..advanced];
        Require(Before(applyBody, "ArgumentNullException.ThrowIfNull(mesh);", "ApplyOverride(mesh"),
            "Apply does not reject null mesh before invoking ApplyOverride");
        Require(applyBody.Contains("return ApplyAdvanced(mesh, new[] { hitLocal }", StringComparison.Ordinal),
            "Apply no longer delegates ordinary work to ApplyAdvanced");

        string advancedBody = source[advanced..helpers];
        Require(Before(advancedBody, "ArgumentNullException.ThrowIfNull(mesh);", "mesh.GetSurfaceCount()"),
            "ApplyAdvanced mesh guard is missing or late");
        Require(Before(advancedBody, "ArgumentNullException.ThrowIfNull(centers);", "centers.Count"),
            "ApplyAdvanced centers guard is missing or late");
        Require(Before(advancedBody, "!float.IsFinite(radius)", "mesh.GetSurfaceCount()") &&
                advancedBody.Contains("nameof(radius)", StringComparison.Ordinal),
            "radius finite guard is missing or does not name radius");
        Require(Before(advancedBody, "!float.IsFinite(strength)", "mesh.GetSurfaceCount()") &&
                advancedBody.Contains("nameof(strength)", StringComparison.Ordinal),
            "strength finite guard is missing or does not name strength");
        Require(Before(advancedBody, "if (radius <= 0f || strength == 0f) return mesh;", "new MeshDataTool()"),
            "no-op boundary does not return the original mesh before allocation");
    }

    static bool Before(string source, string first, string second)
    {
        int a = source.IndexOf(first, StringComparison.Ordinal);
        int b = source.IndexOf(second, StringComparison.Ordinal);
        return a >= 0 && b > a;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
