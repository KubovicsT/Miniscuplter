using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    bool _v1032DirectMoveDragging;
    Vector3 _v1032DirectMovePlaneNormal;
    Vector3 _v1032DirectMoveStartWorld;
    Vector3 _v1032DirectMoveObjectStart;

    void V1032SyncDirectManipulation()
    {
        if (_v1017Gizmo == null || !IsInstanceValid(_v1017Gizmo)) return;

        void EnsureRing(string name, Vector3 rotationDegrees, Color color)
        {
            MeshInstance3D? ring = _v1017Gizmo.GetChildren().OfType<MeshInstance3D>()
                .FirstOrDefault(child => child.Name.ToString().Equals(name, StringComparison.Ordinal));
            if (ring == null)
            {
                ring = new MeshInstance3D
                {
                    Name = name,
                    Mesh = new TorusMesh
                    {
                        InnerRadius = .78f,
                        OuterRadius = .86f,
                        Rings = 48,
                        RingSegments = 8
                    },
                    RotationDegrees = rotationDegrees,
                    MaterialOverride = new StandardMaterial3D
                    {
                        AlbedoColor = color,
                        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                        CullMode = BaseMaterial3D.CullModeEnum.Disabled
                    }
                };
                _v1017Gizmo.AddChild(ring);
            }
            ring.Visible = _v1018Tool == V1018ViewportTool.Rotate;
        }

        EnsureRing("X rotation ring", new Vector3(0, 0, 90), new Color(.95f, .22f, .18f));
        EnsureRing("Y rotation ring", Vector3.Zero, new Color(.24f, .90f, .35f));
        EnsureRing("Z rotation ring", new Vector3(90, 0, 0), new Color(.20f, .48f, 1f));
    }

    bool V1032TryBeginDirectManipulation(Vector2 screenPosition)
    {
        if (_v1018Tool == V1018ViewportTool.Rotate && V1032TryHitRotationRing(screenPosition, out Vector3 ringAxis))
        {
            BeginV1018Transform(ringAxis);
            return true;
        }

        if (_v1018Tool != V1018ViewportTool.Move ||
            !V1018RaycastScene(screenPosition, out MeshInstance3D target))
            return false;

        V1027SelectStableViewportHit(target);
        RebuildSceneList();
        V1017UpdateGizmo();
        return V1032BeginDirectMove(screenPosition);
    }

    bool V1032TryHitRotationRing(Vector2 screenPosition, out Vector3 axis)
    {
        axis = Vector3.Zero;
        if (_camera == null || _v1017Gizmo == null || !IsInstanceValid(_v1017Gizmo) || !_v1017Gizmo.Visible)
            return false;

        Vector3 origin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 direction = _camera.ProjectRayNormal(screenPosition);
        float best = float.PositiveInfinity;
        foreach (MeshInstance3D ring in _v1017Gizmo.GetChildren().OfType<MeshInstance3D>())
        {
            string name = ring.Name.ToString();
            if (!ring.Visible || !name.Contains("rotation ring", StringComparison.OrdinalIgnoreCase)) continue;
            if (!V1018RayMeshSafe(origin, direction, ring, out Vector3 hit)) continue;
            float distance = origin.DistanceSquaredTo(hit);
            if (distance >= best) continue;
            best = distance;
            axis = name.StartsWith("X", StringComparison.OrdinalIgnoreCase)
                ? Vector3.Right
                : name.StartsWith("Y", StringComparison.OrdinalIgnoreCase)
                    ? Vector3.Up
                    : Vector3.Back;
        }

        return axis != Vector3.Zero;
    }

    bool V1032BeginDirectMove(Vector2 screenPosition)
    {
        if (_selected == null || !IsInstanceValid(_selected) || _camera == null) return false;
        Vector3 normal = -_camera.GlobalTransform.Basis.Z.Normalized();
        if (!V1032TryRayPlanePoint(screenPosition, _selected.GlobalPosition, normal, out Vector3 start))
            return false;

        _v1018Dragging = true;
        _v1032DirectMoveDragging = true;
        _v1018HandleAxis = Vector3.Zero;
        _v1018DragAmount = 0;
        _v1018DragStartPosition = _selected.Position;
        _v1018DragStartRotation = _selected.Rotation;
        _v1018DragStartScale = _selected.Scale;
        _v1032DirectMovePlaneNormal = normal;
        _v1032DirectMoveStartWorld = start;
        _v1032DirectMoveObjectStart = _selected.GlobalPosition;
        SetStatus($"Move {_selected.Name} freely in the view plane; use colored axes for constrained movement.");
        return true;
    }

    void V1032ApplyDirectMove(Vector2 screenPosition)
    {
        if (!_v1032DirectMoveDragging || _selected == null || !IsInstanceValid(_selected)) return;
        if (!V1032TryRayPlanePoint(screenPosition, _v1032DirectMoveObjectStart, _v1032DirectMovePlaneNormal, out Vector3 current))
            return;

        Vector3 worldDelta = current - _v1032DirectMoveStartWorld;
        Vector3 localDelta = worldDelta;
        if (_selected.GetParent() is Node3D parent)
            localDelta = parent.GlobalTransform.Basis.Inverse() * worldDelta;
        _selected.Position = _v1018DragStartPosition + localDelta;
        V1017UpdateGizmo();
    }

    bool V1032TryRayPlanePoint(Vector2 screenPosition, Vector3 point, Vector3 normal, out Vector3 hit)
    {
        hit = default;
        if (_camera == null) return false;
        Vector3 origin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 direction = _camera.ProjectRayNormal(screenPosition);
        float denominator = normal.Dot(direction);
        if (Math.Abs(denominator) < 1e-5f) return false;
        float distance = normal.Dot(point - origin) / denominator;
        if (!float.IsFinite(distance) || distance < 0) return false;
        hit = origin + direction * distance;
        return true;
    }

    void V1032ResetDirectManipulation()
    {
        _v1032DirectMoveDragging = false;
    }
}
