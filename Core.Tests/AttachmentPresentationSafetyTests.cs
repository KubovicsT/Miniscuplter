using System.Runtime.CompilerServices;

internal static class AttachmentPresentationSafetyTests
{
    [ModuleInitializer]
    internal static void ValidateMappedAttachmentControlsFailClosed()
    {
        string root = Directory.GetCurrentDirectory();
        string presentationPath = Path.Combine(root, "Scripts", "Main.V095Attachments.cs");
        string authorityPath = Path.Combine(root, "Scripts", "Main.V1033AttachmentAuthority.cs");
        if (!File.Exists(presentationPath) || !File.Exists(authorityPath))
            throw new InvalidOperationException("TEST FAILED: attachment presentation bridge is missing");

        string source = File.ReadAllText(presentationPath);
        string authority = File.ReadAllText(authorityPath);
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

        Assert(authority.Contains("if (V1033HasStableCoreIdentity(_selected))", StringComparison.Ordinal) &&
               authority.Contains("legacy attachment state will not take authority", StringComparison.Ordinal),
            "mapped snap can fall back to legacy attachment authority when the Core socket owner cannot be resolved");
        Assert(authority.Contains("V1033RetireLegacyAttachmentProjection(_selected.Name.ToString());", StringComparison.Ordinal) &&
               authority.Contains("has no durable attachment; stale legacy attachment presentation was cleared", StringComparison.Ordinal),
            "mapped detach can resurrect legacy authority when no durable Core attachment exists");
        Assert(authority.Contains("bool V1033HasStableCoreIdentity(MeshInstance3D part)", StringComparison.Ordinal) &&
               authority.Contains("_v1020StageCSession.Current.Objects.ContainsKey(objectId)", StringComparison.Ordinal),
            "mapped attachment fallback guard does not prove stable live Core object identity");
        Assert(authority.Contains("bool V1033TryResolveAttachmentSocketOwner(AttachmentRecord attachment", StringComparison.Ordinal) &&
               authority.Contains("parentId == attachment.ParentObjectId", StringComparison.Ordinal) &&
               authority.Contains("if (!V1033TryResolveAttachmentSocketOwner(attachment, out _)) return false;", StringComparison.Ordinal),
            "mapped attachment presentation can remain authoritative when its socket is absent or owned by a different Core parent");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
