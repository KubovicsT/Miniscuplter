using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    bool _v1022AcceptanceInstalled;
    bool _v1022ClientFillQueued;

    public void InstallV1022Acceptance()
    {
        if (_v1022AcceptanceInstalled) return;
        _v1022AcceptanceInstalled = true;

        V1022OwnStageCGenerationButton();
        V1022RemoveLegacyStarterSphere();
        V1022InstallEmptySceneGuard();
        V1022InstallViewportPresentationGuard();
        V1022InstallClientFillGuard();

        CallDeferred(nameof(V1022ReassertClientFill));
        CallDeferred(nameof(V1022ReassertNativeViewportPresentation));
    }

    void V1022OwnStageCGenerationButton()
    {
        if (_v109Generate3D == null) return;

        // MS-023: v1.0.17 replaced the v1.0.9 handler, then v1.0.20 removed only the
        // v1.0.9 handler before adding Stage-C. That left both v1.0.17 and v1.0.20
        // subscribed to one button. The legacy handler could claim _v1093DBusy first,
        // import a transient mesh, and make Stage-C exit without registering a candidate.
        // Remove every historical production owner we know about, remove Stage-C itself
        // defensively, then add exactly one Stage-C owner.
        _v109Generate3D.Pressed -= V109Generate3DAsync;
        _v109Generate3D.Pressed -= V1017Generate3DAsync;
        _v109Generate3D.Pressed -= V1020Generate3DAsync;
        _v109Generate3D.Pressed += V1020Generate3DAsync;
        _v109Generate3D.TooltipText =
            "Generate one revision-bound 3D candidate from the accepted baseline. Apply explicitly to make it a persistent editable object.";
    }

    void V1022InstallEmptySceneGuard()
    {
        var root = GetChildren().OfType<VBoxContainer>().FirstOrDefault();
        var toolbar = root?.GetChildren().OfType<HBoxContainer>().FirstOrDefault();
        var newButton = toolbar?.GetChildren().OfType<Button>()
            .FirstOrDefault(button => button.Text.Equals("New", StringComparison.OrdinalIgnoreCase));
        if (newButton != null)
            newButton.Pressed += () => CallDeferred(nameof(V1022RemoveLegacyStarterSphere));
    }

    void V1022RemoveLegacyStarterSphere()
    {
        var starters = _objects
            .Where(obj => GodotObject.IsInstanceValid(obj) &&
                          obj.Name.ToString().Equals("Starter sphere", StringComparison.Ordinal))
            .ToArray();
        if (starters.Length == 0) return;

        bool selectedStarter = _selected != null && starters.Contains(_selected);
        foreach (var starter in starters)
        {
            _objects.Remove(starter);
            starter.QueueFree();
        }

        if (selectedStarter)
            _selected = _objects.LastOrDefault(obj => GodotObject.IsInstanceValid(obj));
        if (_selected != null)
            Select(_selected);
        RebuildSceneList();
        V1017UpdateGizmo();
    }

    void V1022InstallViewportPresentationGuard()
    {
        if (!_v1019ViewportPipelineInstalled) return;
        if (FindChild("ViewportHost", true, false) is SubViewportContainer host)
            host.Resized += V1022ReassertNativeViewportPresentation;

        // A modeling grid should provide spatial reference, not hide geometry behind an
        // opaque floor slab. v1.0.19's bars/axes remain; only the old filled ground is hidden.
        V1022HideOpaqueGridGround();
    }

    void V1022ReassertNativeViewportPresentation()
    {
        if (!_v1019ViewportPipelineInstalled) return;
        V1019ConfigureStudioLighting();
        V1022HideOpaqueGridGround();
        V1017UpdateGizmo();
        V1019ArmRenderProbe();
    }

    void V1022HideOpaqueGridGround()
    {
        if (_v109GridRoot == null || !GodotObject.IsInstanceValid(_v109GridRoot)) return;
        foreach (var ground in _v109GridRoot.GetChildren().OfType<MeshInstance3D>()
                     .Where(mesh => mesh.Name.ToString().StartsWith("Grid ground", StringComparison.OrdinalIgnoreCase)))
            ground.Visible = false;
    }

    void V1022InstallClientFillGuard()
    {
        GetViewport().SizeChanged += V1022QueueClientFill;
    }

    void V1022QueueClientFill()
    {
        if (_v1022ClientFillQueued) return;
        _v1022ClientFillQueued = true;
        CallDeferred(nameof(V1022ReassertClientFill));
    }

    void V1022ReassertClientFill()
    {
        _v1022ClientFillQueued = false;
        var root = GetChildren().OfType<VBoxContainer>().FirstOrDefault();
        if (root == null) return;

        // MS-024: keep the outer UI explicitly pinned to the full client rect after native
        // window resizes. This changes only Control layout ownership; it never writes
        // SubViewport.Size and therefore does not reintroduce the old render-target race.
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.OffsetLeft = 0;
        root.OffsetTop = 0;
        root.OffsetRight = 0;
        root.OffsetBottom = 0;
        root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        root.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var body = root.GetChildren().OfType<HSplitContainer>().FirstOrDefault();
        if (body != null)
        {
            body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        }

        V1022ReassertNativeViewportPresentation();
    }
}
