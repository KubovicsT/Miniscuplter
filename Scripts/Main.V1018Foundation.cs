using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    enum V1018ViewportTool
    {
        Sculpt,
        Select,
        Move,
        Rotate,
        Scale
    }

    bool _v1018ViewportInstalled;
    bool _v1018Dragging;
    Vector3 _v1018HandleAxis = Vector3.Zero;
    Vector3 _v1018DragStartPosition;
    Vector3 _v1018DragStartRotation;
    Vector3 _v1018DragStartScale;
    float _v1018DragAmount;
    V1018ViewportTool _v1018Tool = V1018ViewportTool.Sculpt;
    OptionButton? _v1018ToolSelect;
    Label? _v1018ToolStatus;
    PanelContainer? _v1018ToolOverlay;

    void V1018Process(double delta)
    {
        if (!_v1018ViewportInstalled)
        {
            if (FindChild("ViewportHost", true, false) is SubViewportContainer)
                InstallV1018ViewportFoundation();
        }

        if (!_v1018ViewportInstalled) return;

        RemoveLegacyViewportSurface();
        V1017SyncViewport();
        V1017UpdateGizmo();
        if (_v1018ToolStatus != null)
        {
            string selected = _selected == null ? "none" : _selected.Name.ToString();
            _v1018ToolStatus.Text = $"Tool: {_v1018Tool} · selected: {selected}";
        }
    }

    void InstallV1018ViewportFoundation()
    {
        if (_v1018ViewportInstalled) return;
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host ||
            FindChild("Viewport", true, false) is not SubViewport)
            return;

        _v1018ViewportInstalled = true;
        RemoveLegacyViewportSurface();
        host.MouseFilter = Control.MouseFilterEnum.Stop;
        host.GuiInput -= OnViewportInput;
        host.GuiInput += V1018OnViewportInput;

        _v1018ToolOverlay = new PanelContainer
        {
            Name = "Viewport Tool Overlay v1.0.18",
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 20,
            Position = new Vector2(10, 10),
            CustomMinimumSize = new Vector2(190, 0)
        };
        var tools = new VBoxContainer();
        _v1018ToolOverlay.AddChild(tools);
        tools.AddChild(new Label { Text = "VIEWPORT TOOL", ThemeTypeVariation = "HeaderSmall" });
        _v1018ToolSelect = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (string name in Enum.GetNames<V1018ViewportTool>())
            _v1018ToolSelect.AddItem(name);
        _v1018ToolSelect.Select((int)_v1018Tool);
        _v1018ToolSelect.ItemSelected += V1018ToolSelected;
        tools.AddChild(_v1018ToolSelect);
        _v1018ToolStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        tools.AddChild(_v1018ToolStatus);
        tools.AddChild(new Label
        {
            Text = "RMB orbit · MMB pan · wheel zoom\nMove/Rotate/Scale: drag a colored gizmo axis",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        host.AddChild(_v1018ToolOverlay);

        V1017RepairViewport();
        SetStatus("Viewport ready — native 3D surface and editor tools active.");
    }

    void RemoveLegacyViewportSurface()
    {
        if (_v1017ViewportTexture != null)
        {
            _v1017ViewportTexture.QueueFree();
            _v1017ViewportTexture = null;
        }

        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host) return;
        foreach (Node child in host.GetChildren().OfType<Node>().ToArray())
        {
            if (child is TextureRect || child.Name.ToString().Equals("3D Viewport Texture", StringComparison.Ordinal))
                child.QueueFree();
        }
    }

    void V1018ToolSelected(long index)
    {
        _v1018Tool = (V1018ViewportTool)Math.Clamp((int)index, 0, Enum.GetValues<V1018ViewportTool>().Length - 1);
        _orbiting = false;
        _panning = false;
        _sculpting = false;
        _v1018Dragging = false;
        SetStatus($"Viewport tool: {_v1018Tool}");
    }

    void V1018OnViewportInput(InputEvent ev)
    {
        if (_camera == null) return;

        if (ev is InputEventMouseButton button)
        {
            _lastMouse = button.Position;
            if (button.ButtonIndex == MouseButton.Right)
            {
                _orbiting = button.Pressed;
                return;
            }

            if (button.ButtonIndex == MouseButton.Middle)
            {
                _panning = button.Pressed;
                return;
            }

            if (button.Pressed && button.ButtonIndex == MouseButton.WheelUp)
            {
                _distance = Math.Max(5f, _distance * .9f);
                UpdateCamera();
                return;
            }

            if (button.Pressed && button.ButtonIndex == MouseButton.WheelDown)
            {
                _distance = Math.Min(2000f, _distance * 1.1f);
                UpdateCamera();
                return;
            }

            if (button.ButtonIndex == MouseButton.Left)
            {
                if (button.Pressed)
                {
                    if (_v1018Tool is V1018ViewportTool.Move or V1018ViewportTool.Rotate or V1018ViewportTool.Scale)
                    {
                        if (V1018TryHitGizmo(button.Position, out var axis))
                        {
                            BeginV1018Transform(axis);
                            return;
                        }

                        V1018SelectAt(button.Position);
                        return;
                    }

                    if (_v1018Tool == V1018ViewportTool.Select)
                    {
                        V1018SelectAt(button.Position);
                        return;
                    }

                    _sculpting = true;
                    SculptAt(button.Position, Vector2.Zero);
                }
                else
                {
                    _sculpting = false;
                    _v1018Dragging = false;
                    _v1018HandleAxis = Vector3.Zero;
                }

                return;
            }
        }

        if (ev is not InputEventMouseMotion motion) return;
        Vector2 delta = motion.Position - _lastMouse;
        _lastMouse = motion.Position;

        if (_orbiting)
        {
            _yaw -= delta.X * .008f;
            _pitch = Math.Clamp(_pitch - delta.Y * .008f, -1.5f, 1.5f);
            UpdateCamera();
            return;
        }

        if (_panning)
        {
            var right = _camera.GlobalTransform.Basis.X;
            var up = _camera.GlobalTransform.Basis.Y;
            _focus += (-right * delta.X + up * delta.Y) * (_distance * .0015f);
            UpdateCamera();
            return;
        }

        if (_v1018Dragging)
        {
            ApplyV1018Transform(delta);
            return;
        }

        if (_v1018Tool == V1018ViewportTool.Sculpt && _sculpting)
            SculptAt(motion.Position, delta);
    }

    void V1018SelectAt(Vector2 screenPosition)
    {
        if (!V1018RaycastScene(screenPosition, out var selected)) return;
        Select(selected);
        RebuildSceneList();
        V1017UpdateGizmo();
        SetStatus($"Selected: {selected.Name}");
    }

    bool V1018RaycastScene(Vector2 screenPosition, out MeshInstance3D selected)
    {
        selected = null!;
        if (_camera == null) return false;
        Vector3 origin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 direction = _camera.ProjectRayNormal(screenPosition);
        float best = float.PositiveInfinity;
        bool found = false;

        foreach (var obj in _objects.Where(o => IsInstanceValid(o) && o.Visible))
        {
            if (!V1018RayMeshSafe(origin, direction, obj, out Vector3 hit)) continue;
            float distance = origin.DistanceSquaredTo(hit);
            if (distance >= best) continue;
            best = distance;
            selected = obj;
            found = true;
        }

        return found;
    }

    bool V1018TryHitGizmo(Vector2 screenPosition, out Vector3 axis)
    {
        axis = Vector3.Zero;
        if (_camera == null || _v1017Gizmo == null || !IsInstanceValid(_v1017Gizmo) || !_v1017Gizmo.Visible)
            return false;

        Vector3 origin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 direction = _camera.ProjectRayNormal(screenPosition);
        float best = float.PositiveInfinity;
        foreach (var handle in _v1017Gizmo.GetChildren().OfType<MeshInstance3D>())
        {
            if (!handle.Name.ToString().Contains("handle", StringComparison.OrdinalIgnoreCase)) continue;
            if (!V1018RayMeshSafe(origin, direction, handle, out Vector3 hit)) continue;
            float distance = origin.DistanceSquaredTo(hit);
            if (distance >= best) continue;
            best = distance;
            axis = handle.Name.ToString().StartsWith("X", StringComparison.OrdinalIgnoreCase)
                ? Vector3.Right
                : handle.Name.ToString().StartsWith("Y", StringComparison.OrdinalIgnoreCase)
                    ? Vector3.Up
                    : Vector3.Back;
        }

        return axis != Vector3.Zero;
    }

    static bool V1018RayMeshSafe(Vector3 origin, Vector3 direction, MeshInstance3D obj, out Vector3 hit)
    {
        hit = default;
        if (obj.Mesh == null) return false;
        float best = float.PositiveInfinity;
        bool found = false;
        Transform3D transform = obj.GlobalTransform;

        for (int surface = 0; surface < obj.Mesh.GetSurfaceCount(); surface++)
        {
            Godot.Collections.Array arrays = obj.Mesh.SurfaceGetArrays(surface);
            Vector3[] vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            int[] indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            if (vertices.Length < 3) continue;
            int triangleCount = indices.Length > 0 ? indices.Length : vertices.Length;
            for (int i = 0; i + 2 < triangleCount; i += 3)
            {
                int i0 = indices.Length > 0 ? indices[i] : i;
                int i1 = indices.Length > 0 ? indices[i + 1] : i + 1;
                int i2 = indices.Length > 0 ? indices[i + 2] : i + 2;
                if (i0 < 0 || i1 < 0 || i2 < 0 ||
                    i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length)
                    continue;

                Variant result = Geometry3D.RayIntersectsTriangle(
                    origin,
                    direction,
                    transform * vertices[i0],
                    transform * vertices[i1],
                    transform * vertices[i2]);
                if (result.VariantType != Variant.Type.Vector3) continue;
                Vector3 candidate = result.AsVector3();
                float distance = origin.DistanceSquaredTo(candidate);
                if (distance >= best) continue;
                best = distance;
                hit = candidate;
                found = true;
            }
        }

        return found;
    }

    void BeginV1018Transform(Vector3 axis)
    {
        if (_selected == null || !IsInstanceValid(_selected)) return;
        _v1018Dragging = true;
        _v1018HandleAxis = axis;
        _v1018DragAmount = 0;
        _v1018DragStartPosition = _selected.Position;
        _v1018DragStartRotation = _selected.Rotation;
        _v1018DragStartScale = _selected.Scale;
        SetStatus($"{_v1018Tool} {_selected.Name} on {axis}");
    }

    void ApplyV1018Transform(Vector2 delta)
    {
        if (!_v1018Dragging || _selected == null || !IsInstanceValid(_selected)) return;
        _v1018DragAmount += (delta.X - delta.Y) * .01f * Math.Max(1f, _distance / 100f);

        switch (_v1018Tool)
        {
            case V1018ViewportTool.Move:
                _selected.Position = _v1018DragStartPosition + _v1018HandleAxis * _v1018DragAmount;
                break;
            case V1018ViewportTool.Rotate:
                _selected.Rotation = _v1018DragStartRotation + _v1018HandleAxis * (_v1018DragAmount * 2f);
                break;
            case V1018ViewportTool.Scale:
                float factor = Math.Clamp(1f + _v1018DragAmount, .05f, 20f);
                _selected.Scale = _v1018DragStartScale * factor;
                break;
        }

        V1017UpdateGizmo();
    }

    async void V1018CaptureViewAfterFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInstanceValid(this)) return;
        if (FindChild("Viewport", true, false) is not SubViewport sub) return;
        Image image = sub.GetTexture().GetImage();
        if (image == null || image.IsEmpty())
        {
            SetStatus("Viewport capture failed: the 3D render target is empty.");
            return;
        }

        string directory = AppDataRoot.DirectoryFor("Captures");
        string path = AppDataRoot.Resolve($"Captures/capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Error result = image.SavePng(path);
        if (result != Error.Ok)
        {
            SetStatus($"Viewport capture failed with Godot error {result}.");
            return;
        }

        _lastCapture = path;
        SetStatus("Captured viewport: " + path);
    }
}
