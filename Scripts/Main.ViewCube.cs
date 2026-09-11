using Godot;
using System;

namespace Miniscuplter;

public partial class Main
{
    PanelContainer? _viewCubePanel;
    SubViewportContainer? _viewCubeHost;
    SubViewport? _viewCubeViewport;
    Camera3D? _viewCubeCamera;
    Timer? _viewCubeSyncTimer;

    public void InstallViewCube()
    {
        if (_viewCubePanel != null && IsInstanceValid(_viewCubePanel)) return;
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host) return;

        _viewCubePanel = new PanelContainer
        {
            Name = "ViewportViewCube",
            MouseFilter = Control.MouseFilterEnum.Pass,
            ZIndex = 30,
            TooltipText = "Orientation cube — click a face, edge, or corner to snap the authoritative viewport camera."
        };
        _viewCubePanel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _viewCubePanel.OffsetLeft = -118;
        _viewCubePanel.OffsetRight = -12;
        _viewCubePanel.OffsetTop = 12;
        _viewCubePanel.OffsetBottom = 118;
        host.AddChild(_viewCubePanel);

        _viewCubeHost = new SubViewportContainer
        {
            Name = "ViewCube3D",
            Stretch = true,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(104, 104),
            TooltipText = _viewCubePanel.TooltipText
        };
        _viewCubePanel.AddChild(_viewCubeHost);

        _viewCubeViewport = new SubViewport
        {
            Name = "ViewCubeViewport",
            Size = new Vector2I(104, 104),
            TransparentBg = true,
            OwnWorld3D = true,
            HandleInputLocally = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        _viewCubeHost.AddChild(_viewCubeViewport);

        var root = new Node3D { Name = "OrientationCubeScene" };
        _viewCubeViewport.AddChild(root);

        var cubeMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(.58f, .62f, .68f),
            Roughness = .78f,
            Metallic = 0f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        var cube = new MeshInstance3D
        {
            Name = "OrientationCubeMesh",
            Mesh = new BoxMesh { Size = new Vector3(1.55f, 1.55f, 1.55f) },
            MaterialOverride = cubeMaterial
        };
        root.AddChild(cube);

        var key = new DirectionalLight3D
        {
            Name = "OrientationCubeLight",
            RotationDegrees = new Vector3(-40f, -35f, 0f),
            LightEnergy = 1.25f,
            ShadowEnabled = false
        };
        root.AddChild(key);

        var environment = new WorldEnvironment
        {
            Name = "OrientationCubeEnvironment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0f, 0f, 0f, 0f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(.75f, .78f, .82f),
                AmbientLightEnergy = .7f
            }
        };
        root.AddChild(environment);

        _viewCubeCamera = new Camera3D
        {
            Name = "OrientationCubePresentationCamera",
            Current = true,
            Fov = 28f,
            Near = .05f,
            Far = 20f
        };
        root.AddChild(_viewCubeCamera);

        _viewCubeHost.GuiInput += ViewCubeGuiInput;
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

    void ViewCubeObserveViewportInput(InputEvent input)
    {
        if (input is InputEventMouseButton button &&
            button.ButtonIndex == MouseButton.Right && button.Pressed)
            PrepareOrbitFocusForSelection();
    }

    void RefreshViewCube()
    {
        if (_viewCubeCamera == null || !IsInstanceValid(_viewCubeCamera) ||
            _camera == null || !IsInstanceValid(_camera))
            return;

        Basis basis = _camera.GlobalTransform.Basis.Orthonormalized();
        _viewCubeCamera.GlobalTransform = new Transform3D(basis, basis.Z * 3.8f);
    }

    void ViewCubeGuiInput(InputEvent input)
    {
        if (input is not InputEventMouseButton button ||
            button.ButtonIndex != MouseButton.Left || !button.Pressed ||
            _viewCubeCamera == null || !IsInstanceValid(_viewCubeCamera))
            return;

        Vector3 origin = _viewCubeCamera.ProjectRayOrigin(button.Position);
        Vector3 direction = _viewCubeCamera.ProjectRayNormal(button.Position).Normalized();
        if (!TryHitOrientationCube(origin, direction, out Vector3 hit))
            return;

        const float half = .775f;
        const float edgeThreshold = half * .68f;
        bool x = Math.Abs(hit.X) >= edgeThreshold;
        bool y = Math.Abs(hit.Y) >= edgeThreshold;
        bool z = Math.Abs(hit.Z) >= edgeThreshold;

        if (!x && !y && !z)
        {
            float ax = Math.Abs(hit.X), ay = Math.Abs(hit.Y), az = Math.Abs(hit.Z);
            x = ax >= ay && ax >= az;
            y = ay > ax && ay >= az;
            z = az > ax && az > ay;
        }

        Vector3 from = new(
            x ? Math.Sign(hit.X) : 0f,
            y ? Math.Sign(hit.Y) : 0f,
            z ? Math.Sign(hit.Z) : 0f);
        if (from.LengthSquared() < .5f)
            return;
        from = from.Normalized();

        int axes = (x ? 1 : 0) + (y ? 1 : 0) + (z ? 1 : 0);
        string label = axes >= 3 ? "Corner" : axes == 2 ? "Edge" : OrientationFaceName(from);
        SnapViewCube(from, label);
        GetViewport().SetInputAsHandled();
    }

    static bool TryHitOrientationCube(Vector3 origin, Vector3 direction, out Vector3 hit)
    {
        const float half = .775f;
        float near = float.NegativeInfinity;
        float far = float.PositiveInfinity;

        for (int axis = 0; axis < 3; axis++)
        {
            float o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            float d = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            if (Math.Abs(d) < 0.00001f)
            {
                if (o < -half || o > half)
                {
                    hit = default;
                    return false;
                }
                continue;
            }

            float t1 = (-half - o) / d;
            float t2 = (half - o) / d;
            if (t1 > t2) (t1, t2) = (t2, t1);
            near = Math.Max(near, t1);
            far = Math.Min(far, t2);
            if (near > far)
            {
                hit = default;
                return false;
            }
        }

        float t = near >= 0f ? near : far;
        if (t < 0f || float.IsInfinity(t))
        {
            hit = default;
            return false;
        }
        hit = origin + direction * t;
        return true;
    }

    static string OrientationFaceName(Vector3 from)
    {
        float ax = Math.Abs(from.X), ay = Math.Abs(from.Y), az = Math.Abs(from.Z);
        if (ax >= ay && ax >= az) return from.X >= 0 ? "Right" : "Left";
        if (ay >= ax && ay >= az) return from.Y >= 0 ? "Top" : "Bottom";
        return from.Z >= 0 ? "Front" : "Back";
    }

    void SnapViewCube(Vector3 cameraFrom, string name)
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
