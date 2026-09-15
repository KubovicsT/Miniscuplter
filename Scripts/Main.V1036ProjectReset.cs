using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV1036ProjectResetIsolation()
    {
        var root = GetChildren().OfType<VBoxContainer>().FirstOrDefault();
        var toolbar = root?.GetChildren().OfType<HBoxContainer>().FirstOrDefault();
        var newButton = toolbar?.GetChildren().OfType<Button>().FirstOrDefault(b => b.Text == "New");
        if (newButton == null) return;

        newButton.Pressed -= V1036ClearProjectPresentation;
        newButton.Pressed += V1036ClearProjectPresentation;
    }

    void V1036ClearProjectPresentation()
    {
        // NewScene owns scene-object reset. This companion runs after that handler and retires
        // project-scoped 2D/prompt-selection presentation that historically survived New.
        _lastCapture = "";
        _lastEditedImage = "";
        _lastMask = "";
        _v03StartingImage = "";
        _v1011BaselineImage = "";

        if (_aiPreview != null) _aiPreview.Texture = null;
        SyncV1015CanvasSource("");
        _v1015ImageCanvas?.ClearSelection();
        if (_v1011BaselineStatus != null) _v1011BaselineStatus.Text = "Baseline: not accepted yet";
        if (_v1015EditStatus != null) _v1015EditStatus.Text = "2D source: generate or load an image first.";
        if (_v109Generate3D != null)
        {
            _v109Generate3D.Disabled = true;
            _v109Generate3D.Text = "Accept a 2D Baseline First";
        }
    }
}
