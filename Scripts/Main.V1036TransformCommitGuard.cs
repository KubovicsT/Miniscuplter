using Godot;
using Miniscuplter.Core;

namespace Miniscuplter;

public partial class Main
{
    bool _v1036TransformCommitGuardInstalled;

    public void InstallV1036TransformCommitGuard()
    {
        if (_v1036TransformCommitGuardInstalled) return;
        Control? host = FindChild("Viewport Input", true, false) as Control;
        if (host == null) return;
        _v1036TransformCommitGuardInstalled = true;
        host.GuiInput += V1036GuardTransformCommitOverlap;
    }

    void V1036GuardTransformCommitOverlap(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouse ||
            mouse.ButtonIndex != MouseButton.Left || !mouse.Pressed)
            return;
        if (_v1018Tool is not (V1018ViewportTool.Move or V1018ViewportTool.Rotate or V1018ViewportTool.Scale))
            return;
        if (_v1020StageCGate.CurrentCount != 0 || !V1020SelectedIsMappedStageC(out ObjectId objectId, out _))
            return;

        // The authoritative viewport handler and Stage-C observer run before this guard. If the
        // preceding transform is still being persisted, retire the just-started overlapping
        // gesture rather than letting it capture stale durableStart and later fail/restore.
        _v1018Dragging = false;
        _v1020TransformGestureActive = false;
        V1032ResetDirectManipulation();
        V1020RestoreMappedObjectFromCurrentState(_selected, objectId, reloadMesh: false);
        V1017UpdateGizmo();
        SetStatus("Finishing the previous transform; drag again when the project state is ready.");
    }
}
