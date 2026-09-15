using System.Runtime.CompilerServices;

internal static class ProtectedRegionPresentationSafetyTests
{
    [ModuleInitializer]
    internal static void ValidateProtectedRegionPresentationFailsClosed()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "Scripts", "Main.V1027ProtectedRegionAuthority.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: protected-region authority bridge is missing");

        string source = File.ReadAllText(path);
        string normalizedSource = source.Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert(source.IndexOf("_selected != null", StringComparison.Ordinal) < source.IndexOf("_v096SelectionObject != null", StringComparison.Ordinal),
            "current selected Core object does not take precedence over stale Smart Selection presentation identity");
        Assert(source.Contains("else if (liveObjectId != null && liveObjectId != existing.ObjectId)", StringComparison.Ordinal) &&
               source.Contains("ClearV096Selection(false);", StringComparison.Ordinal),
            "switching to a different stable Core object can leave the prior object's Smart Selection visually authoritative");
        Assert(source.Contains("liveObjectId == null || liveObjectId == existing.ObjectId", StringComparison.Ordinal),
            "stale protected-region weights are not cleared when stable live object identity cannot be proven");
        Assert(source.Contains("_v1027DurableSmartSelection = null;", StringComparison.Ordinal),
            "stale durable protected-region presentation binding is retained after invalidation");
        Assert(source.Contains("_v1027FailedSelectionRestore = null;", StringComparison.Ordinal),
            "stale restore-failure presentation state is retained after revision invalidation");
        Assert(source.Contains("_v096Selection.AsSpan().SequenceEqual(weights)", StringComparison.Ordinal),
            "Smart Selection persistence can acknowledge a stale snapshot as if it contained newer live weights");
        Assert(normalizedSource.Contains("if (_v096Selection != null) V1027ReconcileDurableSmartSelection();", StringComparison.Ordinal) ||
               normalizedSource.Contains("if (_v096Selection != null)\n                V1027ReconcileDurableSmartSelection();", StringComparison.Ordinal),
            "Smart Selection changes made during an in-flight persistence operation are not re-queued afterward");
        Assert(source.Contains("bool destinationStillReferenced = _v1020StageCSession != null", StringComparison.Ordinal) &&
               source.Contains("_v1020StageCSession.Current.Selections.Values.Any", StringComparison.Ordinal) &&
               source.Contains("!destinationStillReferenced && File.Exists(destination)", StringComparison.Ordinal),
            "failed Smart Selection persistence does not re-check recovered Core references before cleaning its snapshot asset");
        Assert(source.Contains("_v1027PendingSmartSelectionClearObjectId", StringComparison.Ordinal) && source.Contains("StageCSelection.RemoveRevisionSelections(", StringComparison.Ordinal),
            "explicit Smart Selection clear is not serialized through durable Core selection removal");
        Assert(source.Contains("binding.ObjectId != _v1027PendingSmartSelectionClearObjectId", StringComparison.Ordinal),
            "a Smart Selection pending durable clear can be immediately resurrected by restore reconciliation");
        Assert(source.Contains("string[] retiredAssetPaths = currentSession.Current.Selections.Values", StringComparison.Ordinal) && source.Contains("await V1020SaveSessionAsync();", StringComparison.Ordinal) && source.Contains("File.Delete(assetPath);", StringComparison.Ordinal),
            "durable Smart Selection clear does not clean retired snapshot assets after the project save succeeds");
        Assert(source.Contains("bool stillReferenced = currentSession.Current.Selections.Values.Any", StringComparison.Ordinal),
            "durable Smart Selection clear can delete a selection asset that is still referenced by another binding");
        Assert(source.Contains("catch (Exception ex)", StringComparison.Ordinal) && source.Contains("cleared = false;", StringComparison.Ordinal),
            "failed durable Smart Selection clear does not re-enable reconciliation of the restored durable binding");

        string smartSelectPath = Path.Combine(root, "Scripts", "Main.V096SmartSelect.cs");
        if (!File.Exists(smartSelectPath)) throw new InvalidOperationException("TEST FAILED: Smart Selection implementation is missing");
        string smartSelectSource = File.ReadAllText(smartSelectPath).Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert(smartSelectSource.Contains("ApplyV096SelectionToSculptMask();\n        V1027ReconcileDurableSmartSelection();\n        SetStatus(\"Smart Selection inverted and queued for revision-bound persistence.\");", StringComparison.Ordinal),
            "inverted Smart Selection weights are not queued through revision-bound durable persistence");
        Assert(smartSelectSource.Contains("void ClearV096Selection(bool status = true)\n    {\n        if (status) V1027BeginDurableSmartSelectionClear();", StringComparison.Ordinal),
            "explicit Smart Selection clear does not retire its durable binding before clearing presentation state");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
