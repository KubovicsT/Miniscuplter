using System.Runtime.CompilerServices;

internal static class StageCStorageContainmentTests
{
    [ModuleInitializer]
    internal static void ValidateStageCStorageContainmentWiring()
    {
        string root = Directory.GetCurrentDirectory();
        string bridgePath = Path.Combine(root, "Scripts", "Main.V1020StageCBridge.cs");
        string clientPath = Path.Combine(root, "Scripts", "AIClient.cs");
        string appDataPath = Path.Combine(root, "Scripts", "AppDataRoot.cs");
        string hunyuanPath = Path.Combine(root, "ai_backend", "hunyuan_shape.py");
        if (!File.Exists(bridgePath) || !File.Exists(clientPath) || !File.Exists(appDataPath) || !File.Exists(hunyuanPath))
            throw new InvalidOperationException("TEST FAILED: Stage-C containment sources are missing");

        string bridge = File.ReadAllText(bridgePath);
        string client = File.ReadAllText(clientPath);
        string appData = File.ReadAllText(appDataPath);
        string hunyuan = File.ReadAllText(hunyuanPath);

        Assert(appData.Contains("Path escapes the Miniscuplter data root", StringComparison.Ordinal),
            "AppDataRoot must reject paths outside the configured data root");
        Assert(appData.Contains("environment[\"TEMP\"] = temp;", StringComparison.Ordinal)
            && appData.Contains("environment[\"TMP\"] = temp;", StringComparison.Ordinal),
            "backend temporary files must inherit the contained Miniscuplter Temp directory");
        Assert(bridge.Contains("Path.Combine(\"Temp\", \"StageC\"", StringComparison.Ordinal),
            "Stage-C generation output must be written under the configured temporary root");
        Assert(!bridge.Contains("AppDataRoot.Resolve($\"ai_part_", StringComparison.Ordinal),
            "Stage-C generation must not leave transient mesh output at the data-root top level");
        Assert(bridge.Contains("if (File.Exists(output)) File.Delete(output)", StringComparison.Ordinal),
            "Stage-C transient generation output must be cleaned after completion or failure");
        Assert(client.Contains("returnedPath.Equals(expectedPath, pathComparison)", StringComparison.Ordinal),
            "Stage-C must reject backend output redirected away from the exact requested path");
        Assert(hunyuan.Contains("component_path(\"hunyuan21-shape\")", StringComparison.Ordinal)
            && hunyuan.Contains("from_pretrained(str(weights)", StringComparison.Ordinal),
            "Hunyuan Stage-C inference must load already-installed local model assets");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
