using System.Runtime.CompilerServices;

internal static class CameraContinuityTests
{
    [ModuleInitializer]
    internal static void ValidateWorkflowTabCameraContinuity()
    {
        string root = Directory.GetCurrentDirectory();
        string continuityPath = Path.Combine(root, "Scripts", "Main.V1036CameraContinuity.cs");
        string resetPath = Path.Combine(root, "Scripts", "Main.V1036ProjectReset.cs");
        string hierarchyPath = Path.Combine(root, "Scripts", "Main.SceneHierarchy.cs");
        string installerPath = Path.Combine(root, "Scripts", "ExtrasInstaller.cs");
        if (!File.Exists(continuityPath) || !File.Exists(resetPath) ||
            !File.Exists(hierarchyPath) || !File.Exists(installerPath))
            throw new InvalidOperationException("TEST FAILED: camera-continuity source is missing");

        string continuity = File.ReadAllText(continuityPath);
        string reset = File.ReadAllText(resetPath);
        string hierarchy = File.ReadAllText(hierarchyPath);
        string installer = File.ReadAllText(installerPath);
        Assert(continuity.Contains("tabs.TabChanged -= V1017WorkflowTabChanged", StringComparison.Ordinal) &&
               continuity.Contains("tabs.TabChanged -= V1019WorkflowTabChanged", StringComparison.Ordinal) &&
               continuity.Contains("tabs.TabChanged -= V1036WorkflowTabChangedPreserveCamera", StringComparison.Ordinal) &&
               continuity.Contains("tabs.TabChanged += V1036WorkflowTabChangedPreserveCamera", StringComparison.Ordinal),
            "workflow tabs do not have exactly one final camera-preserving handler");
        Assert(!continuity.Contains("FrameSelected()", StringComparison.Ordinal) &&
               !continuity.Contains("UpdateCamera()", StringComparison.Ordinal),
            "workflow-tab continuity repair must not rewrite camera framing/orbit state");
        string handler = reset[(reset.IndexOf("void V1036WorkflowTabChangedPreserveCamera", StringComparison.Ordinal))..
            reset.IndexOf("async void V1036ClearProjectPresentation", StringComparison.Ordinal)];
        foreach (string token in new[]
                 {
                     "preservedCameraTransform", "preservedOrbitFocus", "preservedDistance",
                     "preservedYaw", "preservedPitch", "V1013ObjectId(_selected)",
                     "_selected = V1020FindSceneObject(stableId)"
                 })
            Assert(handler.Contains(token, StringComparison.Ordinal),
                "workflow-tab round trip does not preserve camera/pivot/selection state token: " + token);
        Assert(!handler.Contains("FrameSelected", StringComparison.Ordinal) &&
               !handler.Contains("UpdateCamera", StringComparison.Ordinal),
            "camera-preserving tab handler still reframes the scene");
        Assert(reset.Contains("stableSelection = V1020FindSceneObject(selectedObjectId)", StringComparison.Ordinal) &&
               reset.Contains("_selected = null", StringComparison.Ordinal),
            "deleted stable selection does not fail closed before orbit-pivot use");
        Assert(hierarchy.Contains("Dictionary<ObjectId, TreeItem>", StringComparison.Ordinal) &&
               hierarchy.Contains("item.SetMetadata(0, objectId.ToString())", StringComparison.Ordinal) &&
               hierarchy.Contains("V1020FindSceneObject(objectId)", StringComparison.Ordinal) &&
               hierarchy.Contains("SceneHierarchyStableSignature", StringComparison.Ordinal),
            "scene hierarchy and viewport selection are not synchronized by stable ObjectId");
        Assert(!hierarchy.Contains("candidate.Name ==", StringComparison.Ordinal),
            "scene hierarchy still resolves duplicate display names by text");
        Assert(installer.Contains("main.InstallV1036CameraContinuity();", StringComparison.Ordinal),
            "camera-continuity repair is not composed by the installer");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
