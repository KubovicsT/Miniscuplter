using Miniscuplter.Core;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

internal static class StageCAssetStoreSourceBoundaryTests
{
    [ModuleInitializer]
    internal static void ValidateSourceBoundaryAndCopy()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    static async Task RunAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "miniscuplter-ld-1005-" + Guid.NewGuid().ToString("N"));
        string projectPath = Path.Combine(root, "project.msculpt2");
        try
        {
            foreach (string? invalid in new string?[] { null, "", "   " })
            {
                try
                {
                    await StageCAssetStore.CreateImageRevisionAsync(projectPath, invalid!, "baseline", "ld-1005");
                    Fail("null/blank sourcePath was accepted");
                }
                catch (ArgumentException ex)
                {
                    Require(ex.ParamName == "sourcePath", "source guard did not name sourcePath");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("LD_FAIL_20260920_1005: wrong source exception", ex);
                }
                Require(!Directory.Exists(root), "rejected source mutated the project directory");
            }

            Directory.CreateDirectory(root);
            string source = Path.Combine(root, "source.png");
            byte[] payload = System.Text.Encoding.UTF8.GetBytes("ld-1005-valid-image-" + Guid.NewGuid().ToString("N"));
            await File.WriteAllBytesAsync(source, payload);
            ImageRevision revision = await StageCAssetStore.CreateImageRevisionAsync(
                projectPath, source, " accepted-purpose ", " test-provenance ");
            string durable = StageCAssetStore.ResolveImagePath(projectPath, revision);
            Require(File.Exists(durable), "valid image was not copied");
            Require((await File.ReadAllBytesAsync(durable)).SequenceEqual(payload), "copied payload changed");
            Require(revision.Purpose == "accepted-purpose", "purpose metadata was not normalized");
            Require(revision.Provenance == "test-provenance", "provenance metadata was not normalized");
            string digest = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
            Require(revision.Sha256 == digest, "revision digest did not match copied bytes");
            Console.WriteLine("LD_PASS_20260920_1005");
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
        throw new InvalidOperationException("LD_FAIL_20260920_1005: " + message);
    }
}
