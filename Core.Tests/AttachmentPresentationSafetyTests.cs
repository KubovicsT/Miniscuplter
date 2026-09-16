using System.Runtime.CompilerServices;

internal static class AttachmentPresentationSafetyTests
{
    [ModuleInitializer]
    internal static void ValidateMappedAttachmentControlsFailClosed()
    {
        string root = Directory.GetCurrentDirectory();
        string presentationPath = Path.Combine(root, "Scripts", "Main.V095Attachments.cs");
        string authorityPath = Path.Combine(root, "Scripts", "Main.V1033AttachmentAuthority.cs");
        string editingPath = Path.Combine(root, "Scripts", "Main.V1020StageCEditing.cs");
        string bridgePath = Path.Combine(root, "Scripts", "Main.V1020StageCBridge.cs");
        if (!File.Exists(presentationPath) || !File.Exists(authorityPath) ||
            !File.Exists(editingPath) || !File.Exists(bridgePath))
            throw new InvalidOperationException("TEST FAILED: attachment presentation bridge is missing");

        string source = File.ReadAllText(presentationPath);
        string authority = File.ReadAllText(authorityPath);
        string editing = File.ReadAllText(editingPath);
        string bridge = File.ReadAllText(bridgePath);
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
        Assert(source.Contains("if (V1033HasStableCoreIdentity(part))", StringComparison.Ordinal) &&
               source.Contains("V1033RetireLegacyAttachmentProjection(a.PartObjectName);", StringComparison.Ordinal),
            "mapped stale attachment projection remains exportable after Core authority rejects it");

        Assert(authority.Contains("if (V1033HasStableCoreIdentity(_selected))", StringComparison.Ordinal) &&
               authority.Contains("legacy attachment state will not take authority", StringComparison.Ordinal),
            "mapped snap can fall back to legacy attachment authority when the Core socket owner cannot be resolved");
        int snapStart = authority.IndexOf("async void SnapSelectedV1033Object()", StringComparison.Ordinal);
        int detachStart = authority.IndexOf("async void DetachSelectedV1033Object()", StringComparison.Ordinal);
        string snap = authority[snapStart..detachStart];
        Assert(snap.Contains("await V1020SaveSessionAsync();", StringComparison.Ordinal) &&
               snap.Contains("V1033RebuildMappedAttachmentProjectionsFromCore();", StringComparison.Ordinal) &&
               !snap.Contains("V1033ReplaceLegacyProjection(projection);", StringComparison.Ordinal),
            "mapped snap can publish caller-built legacy projection values instead of rebuilding from committed Core state");
        Assert(!authority.Contains("void V1033ReplaceLegacyProjection", StringComparison.Ordinal),
            "unused legacy attachment write helper remains available after mapped snap migrated to Core reconstruction");
        Assert(authority.Contains("V1033RetireLegacyAttachmentProjection(_selected.Name.ToString());", StringComparison.Ordinal) &&
               authority.Contains("has no durable attachment; stale legacy attachment presentation was cleared", StringComparison.Ordinal),
            "mapped detach can resurrect legacy authority when no durable Core attachment exists");
        int fineTuneStart = authority.IndexOf("async void ApplyV1033AttachmentFineTune()", StringComparison.Ordinal);
        string detach = authority[detachStart..fineTuneStart];
        Assert(detach.Contains("ProjectId expectedProjectId = _v1020StageCSession.Current.ProjectId;", StringComparison.Ordinal) &&
               detach.Contains("session.Current.ProjectId != expectedProjectId", StringComparison.Ordinal) &&
               detach.Contains("current.ChildObjectId != childId", StringComparison.Ordinal),
            "mapped detach does not revalidate project and child identity at its serialized commit boundary");
        Assert(detach.Contains("bool stale = !StageDAttachments.IsAuthoritative(session.Current, current);", StringComparison.Ordinal) &&
               detach.Contains("StageDAttachments.RemoveIfCurrent(session, current);", StringComparison.Ordinal) &&
               detach.Contains("V1033RebuildMappedAttachmentProjectionsFromCore();", StringComparison.Ordinal) &&
               detach.Contains("no legacy attachment authority was restored", StringComparison.Ordinal),
            "mapped detach does not remove the exact stale/current Core record and rebuild presentation without legacy fallback");
        Assert(authority.Contains("bool V1033HasStableCoreIdentity(MeshInstance3D part)", StringComparison.Ordinal) &&
               authority.Contains("_v1020StageCSession.Current.Objects.ContainsKey(objectId)", StringComparison.Ordinal),
            "mapped attachment fallback guard does not prove stable live Core object identity");
        Assert(authority.Contains("bool V1033TryResolveAttachmentSocketOwner(AttachmentRecord attachment", StringComparison.Ordinal) &&
               authority.Contains("parentId == attachment.ParentObjectId", StringComparison.Ordinal) &&
               authority.Contains("if (!V1033TryResolveAttachmentSocketOwner(attachment, out _)) return false;", StringComparison.Ordinal),
            "mapped attachment presentation can remain authoritative when its socket is absent or owned by a different Core parent");
        Assert(authority.Contains("local, library);", StringComparison.Ordinal) &&
               authority.Contains("projection.LibraryId = attachment.PartLibraryId ?? \"\";", StringComparison.Ordinal) &&
               authority.Contains("string.IsNullOrWhiteSpace(attachment.PartLibraryId)", StringComparison.Ordinal),
            "mapped snap and read-side reconstruction do not carry or fail closed durable part-library identity through Core");
        Assert(authority.Contains("void V1033RebuildMappedAttachmentProjectionsFromCore()", StringComparison.Ordinal) &&
               authority.Contains("objectId == attachment.ChildObjectId", StringComparison.Ordinal) &&
               authority.Contains("StageDAttachments.IsAuthoritative(session.Current, attachment)", StringComparison.Ordinal),
            "attachment presentation reconstruction does not resolve authoritative Core records by stable child identity");
        int applyStart = fineTuneStart;
        int resetStart = authority.IndexOf("async void ResetV1033AttachmentFineTune()", StringComparison.Ordinal);
        int updateStart = authority.IndexOf("async Task<bool> V1033UpdateCoreAttachmentAsync", StringComparison.Ordinal);
        string applyFineTune = authority[applyStart..resetStart];
        string resetFineTune = authority[resetStart..updateStart];
        Assert(applyFineTune.IndexOf("_v1020StageCSession == null", StringComparison.Ordinal) <
               applyFineTune.IndexOf("_v07Attachments.FirstOrDefault", StringComparison.Ordinal) &&
               resetFineTune.IndexOf("_v1020StageCSession == null", StringComparison.Ordinal) <
               resetFineTune.IndexOf("_v07Attachments.FirstOrDefault", StringComparison.Ordinal),
            "mapped Core attachment fine tune still requires a legacy projection before resolving durable child identity");
        Assert(applyFineTune.Contains("V1033RebuildMappedAttachmentProjectionsFromCore();", StringComparison.Ordinal) &&
               resetFineTune.Contains("V1033RebuildMappedAttachmentProjectionsFromCore();", StringComparison.Ordinal),
            "mapped attachment fine tune does not rebuild its disposable projection from committed Core state");
        Assert(editing.Contains("StageDAttachments.IsAttachmentTransaction(current)", StringComparison.Ordinal) &&
               editing.Contains("V1033UndoRedoAttachmentAsync(undo: true)", StringComparison.Ordinal) &&
               editing.Contains("V1033UndoRedoAttachmentAsync(undo: false)", StringComparison.Ordinal),
            "mapped attachment transactions are not routed through Core undo and redo history");
        Assert(bridge.Contains("V1031RestoreAppliedStageCObjects(session);", StringComparison.Ordinal) &&
               bridge.Contains("V1033RebuildMappedAttachmentProjectionsFromCore();", StringComparison.Ordinal),
            "project load does not rebuild mapped attachment presentation from durable Core state");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
