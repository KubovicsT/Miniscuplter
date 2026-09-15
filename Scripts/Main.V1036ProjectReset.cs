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
        if (toolbar == null) return;

        var newButton = toolbar.GetChildren().OfType<Button>().FirstOrDefault(b => b.Text == "New");
        if (newButton != null)
        {
            newButton.Pressed -= V1036ClearProjectPresentation;
            newButton.Pressed += V1036ClearProjectPresentation;
        }

        // The legacy project actions were appended after a crowded modeling toolbar, which can
        // clip Open/Load entirely on ordinary window widths. Keep the existing handler but make
        // the project-open action a first-class neighbor of New.
        var openButton = toolbar.GetChildren().OfType<Button>().FirstOrDefault(b => b.Text is "Load Project" or "Open Project");
        if (openButton != null)
        {
            openButton.Text = "Open Project";
            toolbar.MoveChild(openButton, Math.Min(1, toolbar.GetChildCount() - 1));
        }
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
