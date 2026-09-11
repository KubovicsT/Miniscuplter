using System.Runtime.CompilerServices;

internal static class StageCAuthorityRetirementTests
{
    [ModuleInitializer]
    internal static void ValidateStageCAuthority()
    {
        string root = Directory.GetCurrentDirectory();
        string legacyPath = Path.Combine(root, "Scripts", "Main.V109Experience.cs");
        string stageCPath = Path.Combine(root, "Scripts", "Main.V1020StageCBridge.cs");
        string editingPath = Path.Combine(root, "Scripts", "Main.V1020StageCEditing.cs");
        if (!File.Exists(legacyPath) || !File.Exists(stageCPath) || !File.Exists(editingPath))
            throw new InvalidOperationException("TEST FAILED: Stage-C authority source files are missing");

        string legacy = File.ReadAllText(legacyPath);
        string stageC = File.ReadAllText(stageCPath);
        string editing = File.ReadAllText(editingPath);

        Assert(
            legacy.Contains("void V109Generate3DAsync() => V1020Generate3DAsync();", StringComparison.Ordinal),
            "historical v1.0.9 generation entry point must delegate to Stage-C authority");
        Assert(
            !legacy.Contains("Generate3DRoutedAsync(", StringComparison.Ordinal),
            "historical v1.0.9 layer must not own a direct 3D provider execution path");
        Assert(
            stageC.Contains("StageCGeneration.BeginImageToMesh", StringComparison.Ordinal),
            "Stage-C generation must remain bound to durable project identity");
        Assert(
            stageC.Contains("Generate3DStageCAsync", StringComparison.Ordinal),
            "Stage-C must remain the backend transport authority");
        Assert(
            stageC.Contains("StageCGeneration.RegisterResult", StringComparison.Ordinal) &&
            stageC.Contains("StageCGeneration.ApplyCandidate", StringComparison.Ordinal),
            "Stage-C must retain candidate registration and explicit Apply ownership");

        Assert(
            editing.Contains("V1020CommitMoveCommandAsync", StringComparison.Ordinal) &&
            editing.Contains("Vec3 position = current.Transform.Position;", StringComparison.Ordinal),
            "mapped move nudges must derive from Core durable transform state");
        Assert(
            editing.Contains("new Vec3(position.X + delta.X, position.Y + delta.Y, position.Z + delta.Z)", StringComparison.Ordinal),
            "mapped move nudges must apply their command delta to durable Core state");
        Assert(
            !editing.Contains("HookV1020TransformButton(\"Move +X 1 mm\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Move -X 1 mm\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Move +Y 1 mm\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Move -Y 1 mm\"", StringComparison.Ordinal),
            "mapped nudge commands must not regress to scene-observed transform persistence");

        Assert(
            editing.Contains("V1020CommitRotateCommandAsync", StringComparison.Ordinal) &&
            editing.Contains("Vec3 rotation = current.Transform.RotationEuler;", StringComparison.Ordinal),
            "mapped rotate nudges must derive from Core durable transform state");
        Assert(
            editing.Contains("new Vec3(rotation.X, rotation.Y + deltaYRadians, rotation.Z)", StringComparison.Ordinal),
            "mapped rotate nudges must apply their command delta to durable Core state");
        Assert(
            !editing.Contains("HookV1020TransformButton(\"Rotate Y +5°\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Rotate Y -5°\"", StringComparison.Ordinal),
            "mapped rotate commands must not regress to scene-observed transform persistence");

        Assert(
            editing.Contains("V1020CommitScaleCommandAsync", StringComparison.Ordinal) &&
            editing.Contains("Vec3 scale = current.Transform.Scale;", StringComparison.Ordinal),
            "mapped scale nudges must derive from Core durable transform state");
        Assert(
            editing.Contains("new Vec3(scale.X * factor, scale.Y * factor, scale.Z * factor)", StringComparison.Ordinal),
            "mapped scale nudges must apply their command factor to durable Core state");
        Assert(
            !editing.Contains("HookV1020TransformButton(\"Scale +5%\"", StringComparison.Ordinal) &&
            !editing.Contains("HookV1020TransformButton(\"Scale -5%\"", StringComparison.Ordinal),
            "mapped scale commands must not regress to scene-observed transform persistence");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
