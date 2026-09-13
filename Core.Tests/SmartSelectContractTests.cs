using System.Runtime.CompilerServices;

internal static class SmartSelectContractTests
{
    [ModuleInitializer]
    internal static void ValidateSmartSelectRouteContract()
    {
        string root = Directory.GetCurrentDirectory();
        string clientPath = Path.Combine(root, "Scripts", "AIClient.cs");
        string backendPath = Path.Combine(root, "ai_backend", "app.py");
        if (!File.Exists(clientPath) || !File.Exists(backendPath))
            throw new InvalidOperationException("TEST FAILED: Smart Select contract source files are missing");

        string client = File.ReadAllText(clientPath);
        string backend = File.ReadAllText(backendPath);

        Assert(client.Contains("PostJsonTextAsync(\"/semantic-select\", new { input_path = inputPath, query }, true)", StringComparison.Ordinal),
            "editor Smart Select client no longer uses the semantic-select mesh contract");
        Assert(backend.Contains("class SemanticSelectRequest(BaseModel):", StringComparison.Ordinal),
            "backend semantic-select request model is missing");
        Assert(backend.Contains("@app.post(\"/semantic-select\")", StringComparison.Ordinal),
            "backend no longer exposes the route used by the editor Smart Select client");
        Assert(backend.Contains("return semantic_select(req.input_path, req.query)", StringComparison.Ordinal),
            "backend semantic-select route no longer forwards the mesh path and query contract");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
