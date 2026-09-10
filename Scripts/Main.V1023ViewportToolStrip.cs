using Godot;
using System;
using System.Collections.Generic;

namespace Miniscuplter;

public partial class Main
{
    readonly Dictionary<V1018ViewportTool, Button> _v1023ToolButtons = new();

    public void InstallV1023ViewportToolStrip()
    {
        if (_v1018ToolOverlay == null || !IsInstanceValid(_v1018ToolOverlay)) return;
        if (_v1018ToolOverlay.FindChild("ViewportToolStrip", true, false) != null) return;

        // Presentation only: V1018ViewportTool remains the single viewport tool/input owner.
        if (_v1018ToolSelect != null && IsInstanceValid(_v1018ToolSelect))
            _v1018ToolSelect.Visible = false;

        if (_v1018ToolOverlay.GetChildCount() == 0 || _v1018ToolOverlay.GetChild(0) is not VBoxContainer tools)
            return;

        var strip = new HBoxContainer
        {
            Name = "ViewportToolStrip",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        tools.AddChild(strip);
        tools.MoveChild(strip, Math.Min(1, tools.GetChildCount() - 1));

        AddV1023ToolButton(strip, V1018ViewportTool.Select, "S", "Select objects in the viewport");
        AddV1023ToolButton(strip, V1018ViewportTool.Move, "M", "Move — drag a colored gizmo axis");
        AddV1023ToolButton(strip, V1018ViewportTool.Rotate, "R", "Rotate — drag a colored gizmo axis");
        AddV1023ToolButton(strip, V1018ViewportTool.Scale, "X", "Scale — drag a colored gizmo axis");
        AddV1023ToolButton(strip, V1018ViewportTool.Sculpt, "B", "Sculpt with the active brush");

        V1023RefreshToolButtons();
    }

    void AddV1023ToolButton(HBoxContainer strip, V1018ViewportTool tool, string iconText, string tooltip)
    {
        var button = new Button
        {
            Name = $"Tool{tool}",
            Text = iconText,
            TooltipText = tooltip,
            ToggleMode = true,
            CustomMinimumSize = new Vector2(34, 32),
            FocusMode = Control.FocusModeEnum.None,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        button.Pressed += () => V1023ChooseViewportTool(tool);
        strip.AddChild(button);
        _v1023ToolButtons[tool] = button;
    }

    void V1023ChooseViewportTool(V1018ViewportTool tool)
    {
        V1018ToolSelected((long)tool);
        V1023RefreshToolButtons();
    }

    void V1023RefreshToolButtons()
    {
        foreach (var pair in _v1023ToolButtons)
            pair.Value.ButtonPressed = pair.Key == _v1018Tool;
    }
}
