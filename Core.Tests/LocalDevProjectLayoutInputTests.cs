using System.Runtime.CompilerServices;
using Miniscuplter.Core;

internal static class LocalDevProjectLayoutInputTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        foreach (string? input in new string?[] { null, "", " ", "\t\r\n", "\u2003" })
        {
            try { ProjectLayout.FromManifest(input!); }
            catch (ArgumentException error) when (error.ParamName == "projectPath") { continue; }
            catch (Exception error) { throw new Exception("LD_FAIL_20260919_1001: wrong exception or parameter", error); }
            throw new Exception("LD_FAIL_20260919_1001: blank path accepted");
        }
        string relative = Path.Combine("localdev-layout-" + Guid.NewGuid().ToString("N"), "my model.msculpt2");
        foreach (string input in new[] { relative, Path.GetFullPath(relative) })
        {
            string manifest = Path.GetFullPath(input);
            string assets = Path.Combine(Path.GetDirectoryName(manifest)!, "my model_assets_v7");
            var expected = new ProjectLayout(manifest, assets, Path.Combine(assets, "meshes"),
                Path.Combine(assets, "images"), Path.Combine(assets, "data"), Path.Combine(assets, "recovery"));
            if (ProjectLayout.FromManifest(input) != expected)
                throw new Exception("LD_FAIL_20260919_1001: valid layout changed");
            if (Directory.Exists(Path.GetDirectoryName(manifest)))
                throw new Exception("LD_FAIL_20260919_1001: layout created directories");
        }
        Console.WriteLine("LD_PASS_20260919_1001");
    }
}
