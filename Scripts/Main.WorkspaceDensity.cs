using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    bool _workspaceDensityInstalled;

    public void InstallWorkspaceDensity()
    {
        if (_workspaceDensityInstalled) return;
        _workspaceDensityInstalled = true;

        // Keep the empty 2D workspace useful without occupying the viewport with a paragraph.
        if (_v1015CanvasHint != null)
        {
            _v1015CanvasHint.Text = "2D WORKSPACE\nGenerate, load, or choose an image · drag to select a region";
            _v1015CanvasHint.TooltipText =
                "The center workspace shows the active 2D source. Drag directly over the image to select a region for AI editing or enhancement.";
        }

        V1023CompactInstructionSection(
            "2D Canvas Editing",
            "The center workspace is the actual 2D image.",
            "Drag directly over the center image to select a region. Use Prompt for requested edits; the mask comes from the 2D canvas, not the 3D viewport.");

        V1023CompactInstructionSection(
            "Context Aware Image Editing",
            "Edit uses your prompt.",
            "Edit uses Prompt. Enhance needs no prompt and reconstructs the selected malformed or incoherent area from surrounding image context.");

        V1023SetCompactTooltip("Select Edit Region", "Select a region by dragging directly over the center 2D image.");
        V1023SetCompactTooltip("Clear Selection", "Clear the current 2D edit-region selection.");
        V1023SetCompactTooltip("AI Edit Selected Region", "Apply Prompt to the selected 2D region only.");
        V1023SetCompactTooltip("AI Edit Whole Image", "Apply Prompt to the complete active 2D image.");
        V1023SetCompactTooltip("Enhance Selected Region", "Reconstruct the selected area from surrounding image context; no prompt is required.");
    }

    void V1023CompactInstructionSection(string sectionName, string instructionPrefix, string replacementTooltip)
    {
        if (FindChild(sectionName, true, false) is not VBoxContainer section) return;

        section.AddThemeConstantOverride("separation", 4);
        var instruction = section.GetChildren().OfType<Label>()
            .FirstOrDefault(label => label.Text.StartsWith(instructionPrefix, StringComparison.Ordinal));
        if (instruction != null)
        {
            instruction.Visible = false;
            instruction.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        if (string.IsNullOrWhiteSpace(section.TooltipText))
            section.TooltipText = replacementTooltip;
    }

    void V1023SetCompactTooltip(string buttonText, string tooltip)
    {
        foreach (Button button in FindChildren("*", "Button", true, false).OfType<Button>()
                     .Where(button => button.Text.Equals(buttonText, StringComparison.OrdinalIgnoreCase)))
        {
            button.TooltipText = tooltip;
        }
    }
}
