using Godot;
using System;

namespace Miniscuplter;

public partial class Main
{
    const float WorkspaceBottomDockNormalHeight = 188f;
    const float WorkspaceBottomDockCompactHeight = 104f;
    const float WorkspaceBodyReservedHeight = 180f;

    ScrollContainer? _workspaceBottomDockScroll;
    HBoxContainer? _workspaceBottomDock;
    VBoxContainer? _workspaceTelemetryDock;
    VBoxContainer? _workspaceCommandDock;

    bool EnsureWorkspaceBottomDock()
    {
        if (_workspaceBottomDockScroll != null &&
            _workspaceBottomDock != null &&
            _workspaceTelemetryDock != null &&
            _workspaceCommandDock != null)
            return true;

        if (FindChild("VBoxContainer", false, false) is not VBoxContainer root)
            return false;

        // The content keeps its normal 188 px presentation and rail alignment, while this
        // smaller viewport permits both axes to scroll at the supported minimum client size.
        var dockScroll = new ScrollContainer
        {
            Name = "WorkspaceBottomDockScroll",
            CustomMinimumSize = new Vector2(0, WorkspaceBottomDockNormalHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };

        var dock = new HBoxContainer
        {
            Name = "WorkspaceBottomDock",
            CustomMinimumSize = new Vector2(0, 188),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        dock.AddThemeConstantOverride("separation", 8);

        var telemetry = new VBoxContainer
        {
            Name = "WorkspaceTelemetryDock",
            CustomMinimumSize = new Vector2(300, 0),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };

        var command = new VBoxContainer
        {
            Name = "WorkspaceCommandDock",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };

        // Keep the bottom-center command surface aligned with the viewport rather
        // than extending beneath the fixed right workspace rail.
        var rightRailGutter = new Control
        {
            Name = "WorkspaceRightRailGutter",
            CustomMinimumSize = new Vector2(330, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        dock.AddChild(telemetry);
        dock.AddChild(new VSeparator());
        dock.AddChild(command);
        dock.AddChild(rightRailGutter);
        dockScroll.AddChild(dock);
        root.AddChild(dockScroll);
        if (_status != null)
            root.MoveChild(dockScroll, _status.GetIndex());

        _workspaceBottomDockScroll = dockScroll;
        _workspaceBottomDock = dock;
        _workspaceTelemetryDock = telemetry;
        _workspaceCommandDock = command;
        SyncWorkspaceBottomDockToClientHeight(GetViewport().GetVisibleRect().Size.Y);
        return true;
    }

    void SyncWorkspaceBottomDockToClientHeight(float clientHeight)
    {
        if (_workspaceBottomDockScroll == null) return;

        const float fixedChromeHeight = 72f; // 46 px toolbar + 26 px status line.
        float heightForDock = clientHeight - fixedChromeHeight - WorkspaceBodyReservedHeight;
        float dockViewportHeight = Math.Clamp(
            heightForDock,
            WorkspaceBottomDockCompactHeight,
            WorkspaceBottomDockNormalHeight);
        _workspaceBottomDockScroll.CustomMinimumSize = new Vector2(0, dockViewportHeight);
    }

    public void ReconcileWorkspaceBottomDock()
    {
        if (!EnsureWorkspaceBottomDock() || _workspaceTelemetryDock == null)
            return;

        if (FindChild("Resource Telemetry", true, false) is Control telemetry)
        {
            if (telemetry.GetParent() != _workspaceTelemetryDock)
                telemetry.Reparent(_workspaceTelemetryDock, false);

            // Resource telemetry historically owned viewport-overlay anchors and offsets.
            // Once it is docked, clear that obsolete absolute layout state so the container
            // is the sole geometry authority and cannot keep covering the viewport.
            telemetry.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            telemetry.OffsetLeft = 0;
            telemetry.OffsetTop = 0;
            telemetry.OffsetRight = 0;
            telemetry.OffsetBottom = 0;
            telemetry.ZIndex = 0;
            telemetry.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            telemetry.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        }
    }
}
