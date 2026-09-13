using Godot;

namespace Miniscuplter;

public partial class Main
{
    HBoxContainer? _workspaceBottomDock;
    VBoxContainer? _workspaceTelemetryDock;
    VBoxContainer? _workspaceCommandDock;

    bool EnsureWorkspaceBottomDock()
    {
        if (_workspaceBottomDock != null &&
            _workspaceTelemetryDock != null &&
            _workspaceCommandDock != null)
            return true;

        if (FindChild("VBoxContainer", false, false) is not VBoxContainer root)
            return false;

        var dock = new HBoxContainer
        {
            Name = "WorkspaceBottomDock",
            CustomMinimumSize = new Vector2(0, 188),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
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
        root.AddChild(dock);
        if (_status != null)
            root.MoveChild(dock, _status.GetIndex());

        _workspaceBottomDock = dock;
        _workspaceTelemetryDock = telemetry;
        _workspaceCommandDock = command;
        return true;
    }

    public void ReconcileWorkspaceBottomDock()
    {
        if (!EnsureWorkspaceBottomDock() || _workspaceTelemetryDock == null)
            return;

        if (FindChild("Resource Telemetry", true, false) is Control telemetry &&
            telemetry.GetParent() != _workspaceTelemetryDock)
        {
            telemetry.Reparent(_workspaceTelemetryDock, false);
            telemetry.ZIndex = 0;
            telemetry.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            telemetry.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        }
    }
}
