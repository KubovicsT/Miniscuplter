using Godot;
using System;
using System.Collections.Generic;

namespace Miniscuplter;

public partial class Main
{
    sealed partial class ViewAxisGizmo : Control
    {
        readonly List<(Button Button, Vector3 Axis, string Label)> _points = new();
        Basis _viewBasis = Basis.Identity;

        public event Action<Vector3, string>? AxisClicked;

        public ViewAxisGizmo()
        {
            Name = "ViewportAxisGizmo";
            CustomMinimumSize = new Vector2(104, 104);
            MouseFilter = MouseFilterEnum.Stop;
            Resized += LayoutPoints;

            AddPoint(Vector3.Right, "X", "Right view (+X)");
            AddPoint(Vector3.Left, "•", "Left view (-X)");
            AddPoint(Vector3.Up, "Y", "Top view (+Y)");
            AddPoint(Vector3.Down, "•", "Bottom view (-Y)");
            AddPoint(Vector3.Back, "Z", "Front view (+Z)");
            AddPoint(Vector3.Forward, "•", "Back view (-Z)");
        }

        void AddPoint(Vector3 axis, string label, string tooltip)
        {
            var button = new Button
            {
                Text = label,
                TooltipText = tooltip,
                Flat = true,
                FocusMode = FocusModeEnum.None,
                CustomMinimumSize = new Vector2(28, 28),
                MouseFilter = MouseFilterEnum.Stop
            };
            button.Pressed += () => AxisClicked?.Invoke(axis, AxisName(axis));
            AddChild(button);
            _points.Add((button, axis, label));
        }

        public void SetViewBasis(Basis basis)
        {
            _viewBasis = basis.Orthonormalized();
            LayoutPoints();
            QueueRedraw();
        }

        Vector2 ProjectAxis(Vector3 axis)
        {
            Vector2 center = Size * .5f;
            float radius = Math.Max(18f, Math.Min(Size.X, Size.Y) * .34f);
            float x = axis.Dot(_viewBasis.X);
            float y = -axis.Dot(_viewBasis.Y);
            var projected = new Vector2(x, y);
            float length = projected.Length();
            if (length < .08f)
                return center;
            return center + projected / length * radius * Math.Clamp(length, .25f, 1f);
        }

        void LayoutPoints()
        {
            if (Size.X <= 1 || Size.Y <= 1) return;
            foreach (var point in _points)
            {
                Vector2 p = ProjectAxis(point.Axis);
                point.Button.Position = p - point.Button.CustomMinimumSize * .5f;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 center = Size * .5f;
            float ring = Math.Max(20f, Math.Min(Size.X, Size.Y) * .42f);
            DrawArc(center, ring, 0, Mathf.Tau, 64, new Color(.62f, .66f, .72f, .8f), 1.5f, true);

            DrawAxisLine(center, Vector3.Right, new Color(.92f, .30f, .28f));
            DrawAxisLine(center, Vector3.Up, new Color(.36f, .82f, .42f));
            DrawAxisLine(center, Vector3.Back, new Color(.34f, .56f, .96f));
            DrawCircle(center, 3.2f, new Color(.82f, .84f, .88f));
        }

        void DrawAxisLine(Vector2 center, Vector3 axis, Color color)
        {
            DrawLine(center, ProjectAxis(axis), color, 2f, true);
        }

        static string AxisName(Vector3 axis)
        {
            if (axis.X > .5f) return "Right";
            if (axis.X < -.5f) return "Left";
            if (axis.Y > .5f) return "Top";
            if (axis.Y < -.5f) return "Bottom";
            if (axis.Z > .5f) return "Front";
            return "Back";
        }
    }

    Control? _viewAxisOverlay;
    ViewAxisGizmo? _viewAxisGizmo;
    Timer? _viewCubeSyncTimer;

    public void InstallViewCube()
    {
        if (_viewAxisOverlay != null && IsInstanceValid(_viewAxisOverlay)) return;
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host) return;

        _viewAxisOverlay = new Control
        {
            Name = "ViewportAxisOrientation",
            MouseFilter = Control.MouseFilterEnum.Pass,
            ZIndex = 30,
            TooltipText = "Circular XYZ orientation gizmo — click an axis endpoint to snap the authoritative viewport camera."
        };
        _viewAxisOverlay.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _viewAxisOverlay.OffsetLeft = -118;
        _viewAxisOverlay.OffsetRight = -10;
        _viewAxisOverlay.OffsetTop = 10;
        _viewAxisOverlay.OffsetBottom = 118;
        host.AddChild(_viewAxisOverlay);

        _viewAxisGizmo = new ViewAxisGizmo();
        _viewAxisGizmo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _viewAxisGizmo.AxisClicked += SnapViewAxis;
        _viewAxisOverlay.AddChild(_viewAxisGizmo);

        host.GuiInput += ViewCubeObserveViewportInput;

        _viewCubeSyncTimer = new Timer
        {
            Name = "ViewAxisSync",
            WaitTime = 0.1,
            OneShot = false,
            Autostart = true
        };
        _viewCubeSyncTimer.Timeout += RefreshViewCube;
        AddChild(_viewCubeSyncTimer);
        RefreshViewCube();
    }

    void ViewCubeObserveViewportInput(InputEvent input)
    {
        if (input is InputEventMouseButton button &&
            button.ButtonIndex == MouseButton.Right && button.Pressed)
            PrepareOrbitFocusForSelection();
    }

    void RefreshViewCube()
    {
        if (_viewAxisGizmo == null || !IsInstanceValid(_viewAxisGizmo) ||
            _camera == null || !IsInstanceValid(_camera))
            return;

        _viewAxisGizmo.SetViewBasis(_camera.GlobalTransform.Basis);
    }

    void SnapViewAxis(Vector3 cameraFrom, string name)
    {
        FocusCameraOnSelectionPreservingPosition(false);
        Vector3 dir = -cameraFrom.Normalized();
        _yaw = Mathf.Atan2(dir.X, dir.Z);
        _pitch = Math.Clamp(Mathf.Asin(Math.Clamp(dir.Y, -1f, 1f)), -1.5f, 1.5f);
        UpdateCamera();
        RefreshViewCube();
        SetStatus($"View: {name}");
    }

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
