using System.Runtime.CompilerServices;

internal static class SettingsQualityBridgeTests
{
    [ModuleInitializer]
    internal static void ValidateSettingsQualityBridge()
    {
        string root = Directory.GetCurrentDirectory();
        string bridgePath = Path.Combine(root, "Scripts", "Main.V1036SettingsQualityBridge.cs");
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        if (!File.Exists(bridgePath) || !File.Exists(installerPath))
            throw new InvalidOperationException("TEST FAILED: settings-quality bridge source is missing");

        string bridge = File.ReadAllText(bridgePath);
        string installer = File.ReadAllText(installerPath);
        Assert(bridge.Contains("ACTIVE PRESET PARAMETERS", StringComparison.Ordinal) &&
               bridge.Contains("2D inference steps", StringComparison.Ordinal) &&
               bridge.Contains("3D inference steps", StringComparison.Ordinal) &&
               bridge.Contains("Voxel budget", StringComparison.Ordinal),
            "Settings > Quality does not expose concrete active-preset parameters");
        Assert(bridge.Contains("Edit Parameters / Create Custom Preset", StringComparison.Ordinal) &&
               bridge.Contains("Quality v0.9.7", StringComparison.OrdinalIgnoreCase),
            "Settings > Quality has no visible route to parameter editing/custom-preset creation");
        Assert(installer.Contains("main.InstallV1036SettingsQualityBridge();", StringComparison.Ordinal),
            "settings-quality bridge is not composed by the installer");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
