using System.Runtime.CompilerServices;

internal static class DetailContractTests
{
    [ModuleInitializer]
    internal static void ValidateDetailContract()
    {
        string root = Directory.GetCurrentDirectory();
        string pipelinePath = Path.Combine(root, "ai_backend", "detail_pipeline.py");
        string appPath = Path.Combine(root, "ai_backend", "app.py");
        string clientPath = Path.Combine(root, "Scripts", "AIClient.cs");
        if (!File.Exists(pipelinePath) || !File.Exists(appPath) || !File.Exists(clientPath))
            throw new InvalidOperationException("TEST FAILED: detail contract source files are missing");

        string pipeline = File.ReadAllText(pipelinePath);
        string app = File.ReadAllText(appPath);
        string client = File.ReadAllText(clientPath);

        Assert(pipeline.Contains("def detail_2d(image_path,mask_path,prompt,output_path,quality_or_provider=\"auto\",image_provider=None)", StringComparison.Ordinal),
            "2D detail pipeline no longer accepts the released six-argument endpoint call shape");
        Assert(app.Contains("detail_2d(req.image_path, req.mask_path, req.prompt, req.output_path, req.quality, req.provider)", StringComparison.Ordinal),
            "2D endpoint call shape changed without updating the compatibility guard");
        Assert(client.Contains("image_provider = ImageDetailProvider", StringComparison.Ordinal),
            "editor 2D detail request no longer exposes the intended image-provider field");
        Assert(client.Contains("source_mesh = sourceMesh", StringComparison.Ordinal) && app.Contains("class Detail3DRequest", StringComparison.Ordinal),
            "adjacent 3D detail contract evidence disappeared; complete typed migration is still required before claiming 3D detail fixed");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
