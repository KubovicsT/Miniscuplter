using Godot;
using System;
using System.Collections.Generic;

namespace Miniscuplter;

public partial class Main
{
    PanelContainer? _viewCubePanel;
    Control? _viewCubeSurface;
    Timer? _viewCubeSyncTimer;
    readonly Dictionary<string, (Button Button, Vector3 Normal)> _viewCubeFaces = new();

    public void InstallViewCube()
    {
        if (_viewCubePanel != null && IsInstanceValid(_viewCubePanel)) return;
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host) return;

        _viewCubePanel = new PanelContainer
        {
            Name = "ViewportViewCube",
            MouseFilter = Control.MouseFilterEnum.Pass,
            ZIndex = 30,
            TooltipText = "View cube — click a visible face to snap the camera. Orbit pivots around the selected object."
        };
        _viewCubePanel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _viewCubePanel.OffsetLeft = -126;
        _viewCubePanel.OffsetRight = -12;
        _viewCubePanel.OffsetTop = 12;
        _viewCubePanel.OffsetBottom = 126;
        host.AddChild(_viewCubePanel);

        _viewCubeSurface = new Control
        {
            Name = "ViewCubeSurface",
            CustomMinimumSize = new Vector2(112, 112),
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        _viewCubePanel.AddChild(_viewCubeSurface);

        AddViewCubeFace("Front", "F", Vector3.Back, Mathf.Pi, 0f);
        AddViewCubeFace("Back", "B", Vector3.Forward, 0f, 0f);
        AddViewCubeFace("Left", "L", Vector3.Left, Mathf.Pi / 2f, 0f);
        AddViewCubeFace("Right", "R", Vector3.Right, -Mathf.Pi / 2f, 0f);
        AddViewCubeFace("Top", "T", Vector3.Up, _yaw, -1.5f);
        AddViewCubeFace("Bottom", "D", Vector3.Down, _yaw, 1.5f);

        // Observe the existing viewport input path only to retarget the orbit pivot before the first
        // motion event. V1018 remains the owner of orbit state and mouse handling.
        host.GuiInput += ViewCubeObserveViewportInput;

        _viewCubeSyncTimer = new Timer
        {
            Name = "ViewCubeSync",
            WaitTime = 0.1,
            OneShot = false,
            Autostart = true
        };
        _viewCubeSyncTimer.Timeout += RefreshViewCube;
        AddChild(_viewCubeSyncTimer);
        RefreshViewCube();
    }

    void AddViewCubeFace(string name, string label, Vector3 normal, float yaw, float pitch)
    {
        if (_viewCubeSurface == null) return;

        var button = new Button
        {
            Name = $"ViewCube{name}",
            Text = label,
            TooltipText = $"Snap to {name} view",
            CustomMinimumSize = new Vector2(28, 24),
            Size = new Vector2(28, 24),
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        button.Pressed += () => SnapViewCube(yaw, pitch, name);
        _viewCubeSurface.AddChild(button);
        _viewCubeFaces[name] = (button, normal);
    }

    void ViewCubeObserveViewportInput(InputEvent input)
    {
        if (input is InputEventMouseButton button &&
            button.ButtonIndex == MouseButton.Right && button.Pressed)
            PrepareOrbitFocusForSelection();
    }

    void RefreshViewCube()
    {
        if (_viewCubeSurface == null || !IsInstanceValid(_viewCubeSurface) ||
            _camera == null || !IsInstanceValid(_camera))
            return;

        Vector2 center = _viewCubeSurface.Size / 2f;
        if (center.X <= 1f || center.Y <= 1f)
            center = new Vector2(56, 56);

        Vector3 right = _camera.GlobalTransform.Basis.X.Normalized();
        Vector3 up = _camera.GlobalTransform.Basis.Y.Normalized();
        Vector3 viewVector = (_camera.GlobalPosition - _focus).Normalized();

        foreach (var pair in _viewCubeFaces)
        {
            Button button = pair.Value.Button;
            Vector3 normal = pair.Value.Normal;
            if (!IsInstanceValid(button)) continue;

            float facing = normal.Dot(viewVector);
            button.Visible = facing >= -0.12f;
            if (!button.Visible) continue;

            float screenX = normal.Dot(right);
            float screenY = -normal.Dot(up);
            Vector2 position = center + new Vector2(screenX, screenY) * 35f - button.Size / 2f;
            button.Position = position;
            button.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(0.72f + Math.Max(0f, facing) * 0.28f, 0.72f, 1f));
        }
    }

    void SnapViewCube(float yaw, float pitch, string name)
    {
        FocusCameraOnSelectionPreservingPosition(false);
        _yaw = yaw;
        _pitch = pitch;
        UpdateCamera();
        RefreshViewCube();
        SetStatus($"View: {name}");
    }

    // Presentation/input helper only. Main's existing _focus/_yaw/_pitch/_distance fields remain
    // the single camera authority. Recomputing the spherical camera state from the current camera
    // position prevents an RMB orbit from jumping when its pivot moves to the selected object.
    void PrepareOrbitFocusForSelection()
    {
        FocusCameraOnSelectionPreservingPosition(true);
    }

    void FocusCameraOnSelectionPreservingPosition(bool preserveCameraPosition)
    {
        if (_selected == null || !IsInstanceValid(_selected)) return;

        Aabb bounds = _selected.GetAabb();
        Vector3 selectedFocus = _selected.GlobalTransform * (bounds.Position + bounds.Size / 2f);
        if (_camera == null || !IsInstanceValid(_camera))
        {
            _focus = selectedFocus;
            return;
        }

        if (!preserveCameraPosition)
        {
            _focus = selectedFocus;
            return;
        }

        Vector3 cameraPosition = _camera.GlobalPosition;
        Vector3 toFocus = selectedFocus - cameraPosition;
        float distance = toFocus.Length();
        if (distance < 0.001f)
        {
            _focus = selectedFocus;
            return;
        }

        Vector3 direction = toFocus / distance;
        _focus = selectedFocus;
        _distance = Math.Clamp(distance, 5f, 2000f);
        _pitch = Math.Clamp(Mathf.Asin(Math.Clamp(direction.Y, -1f, 1f)), -1.5f, 1.5f);
        _yaw = Mathf.Atan2(direction.X, direction.Z);
        UpdateCamera();
    }
}
