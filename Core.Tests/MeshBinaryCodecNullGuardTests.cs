using Miniscuplter.Core;
using System.Runtime.CompilerServices;

internal static class MeshBinaryCodecNullGuardTests
{
    [ModuleInitializer]
    internal static void ValidateNullGuardAndRoundTrip()
    {
        string root = Path.Combine(Path.GetTempPath(), "miniscuplter-ld-1004-" + Guid.NewGuid().ToString("N"));
        string invalidPath = Path.Combine(root, "should-not-exist", "null.mesh");
        try
        {
            try
            {
                MeshBinaryCodec.WriteAtomicAsync(invalidPath, null!).GetAwaiter().GetResult();
                Fail("null mesh was accepted");
            }
            catch (ArgumentNullException ex)
            {
                Require(ex.ParamName == "mesh", "null guard did not name mesh");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("LD_FAIL_20260919_1004: wrong null exception", ex);
            }

            Require(!Directory.Exists(root), "null rejection mutated the filesystem");

            Directory.CreateDirectory(root);
            string validPath = Path.Combine(root, "roundtrip.mesh");
            var expected = new MeshData(
                new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 },
                new int[] { 0, 1, 2 });
            string digest = MeshBinaryCodec.WriteAtomicAsync(validPath, expected).GetAwaiter().GetResult();
            MeshData actual = MeshBinaryCodec.Read(validPath);
            Require(digest.Length == 64, "write did not return a SHA-256 digest");
            Require(actual.Positions.SequenceEqual(expected.Positions), "round-trip positions changed");
            Require(actual.Indices.SequenceEqual(expected.Indices), "round-trip indices changed");
            Console.WriteLine("LD_PASS_20260919_1004");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) Fail(message);
    }

    static void Fail(string message)
    {
        throw new InvalidOperationException("LD_FAIL_20260919_1004: " + message);
    }
}
