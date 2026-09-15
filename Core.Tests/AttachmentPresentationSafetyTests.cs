using System.Runtime.CompilerServices;

internal static class AttachmentPresentationSafetyTests
{
    [ModuleInitializer]
    internal static void ValidateMappedAttachmentControlsFailClosed()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Scripts", "Main.V095Attachments.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: attachment presentation bridge is missing");

        string source = File.ReadAllText(path);
        Assert(source.Contains("if (!V1033TryProjectAuthoritativeAttachment(a, _selected))", StringComparison.Ordinal),
            "mapped attachment fine-tune controls do not verify Core attachment authority before projecting values");
        Assert(source.Contains("if (_selected == null)", StringComparison.Ordinal) &&
               source.Contains("V095ClearAttachmentFineTuneControls();", StringComparison.Ordinal),
            "clearing selection can leave the previous attachment fine-tune values visually authoritative");
        Assert(source.Contains("if (_v1020StageCSession != null && _v1013ObjectIds.ContainsKey(_selected.GetInstanceId()))", StringComparison.Ordinal),
            "mapped objects without a matching legacy attachment projection can inherit stale fine-tune presentation values");
        Assert(source.Contains("V095ClearAttachmentFineTuneControls();", StringComparison.Ordinal),
            "stale mapped attachment fine-tune values are not cleared from presentation state");
        Assert(source.Contains("if (_v07AttachScale != null) _v07AttachScale.Value = 1;", StringComparison.Ordinal),
            "stale attachment presentation does not reset scale to a neutral value");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
