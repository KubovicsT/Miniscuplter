using System.Runtime.CompilerServices;

internal static class MeshIOValidationTests
{
    [ModuleInitializer]
    internal static void ValidateBlankStlInputGuard()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "MeshIO.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: MeshIO source is missing");

        string source = File.ReadAllText(path);
        int load = source.IndexOf("public static ArrayMesh LoadStl(string path)", StringComparison.Ordinal);
        int binary = load >= 0
            ? source.IndexOf("static ArrayMesh LoadBinaryStl(Stream stream)", load, StringComparison.Ordinal)
            : -1;
        Assert(load >= 0 && binary > load, "LoadStl source could not be isolated");

        string loadBody = source[load..binary];
        int guard = loadBody.IndexOf("ArgumentException.ThrowIfNullOrWhiteSpace(path);", StringComparison.Ordinal);
        int open = loadBody.IndexOf("File.OpenRead(path)", StringComparison.Ordinal);
        Assert(guard >= 0 && open > guard,
            "LoadStl does not reject a blank path before filesystem access");
        Assert(loadBody.Contains("LoadBinaryStl(fs)", StringComparison.Ordinal) &&
               loadBody.Contains("LoadAsciiStl(path)", StringComparison.Ordinal),
            "blank-path validation changed the existing binary/ASCII STL dispatch");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
