using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    string _v095FineTuneObject = "";

    public void InstallV095AttachmentGuards()
    {
        ReplaceV095Button("Snap Selected Object", SnapSelectedV095Object);
        ReplaceV095Button("Refresh Attachments", RefreshV095Attachments);
        if (_v07FollowTimer != null)
        {
            _v07FollowTimer.Stop();
            _v07FollowTimer.QueueFree();
            _v07FollowTimer = null;
        }
        _v07FollowTimer = new Timer { WaitTime = .2, OneShot = false, Autostart = true };
        _v07FollowTimer.Timeout += () =>
        {
            if (_v07Attachments.Count > 0 || _v07Sockets.Count > 0) RefreshV095Attachments(false);
            SyncV095AttachmentControls();
        };
        AddChild(_v07FollowTimer);
    }

    static Basis V095MountAlignment(V07PartDefinition? def)
    {
        Vector3 normal = def == null || def.MountNormal == null || def.MountNormal.Length < 3
            ? Vector3.Up
            : new Vector3(def.MountNormal[0], def.MountNormal[1], def.MountNormal[2]).Normalized();
        Basis align = new(new Quaternion(normal, Vector3.Up));
        float roll = def?.MountRollDeg ?? 0f;
        if (Math.Abs(roll) > .001f) align = new Basis(new Quaternion(Vector3.Up, Mathf.DegToRad(-roll))) * align;
        return align;
    }

    void SnapSelectedV095Object()
    {
        if (_selected == null) { SetStatus("Select the part object to snap first."); return; }
        var socket = _v07Sockets.FirstOrDefault(s => s.Id == _v07SelectedSocketId);
        if (socket == null) { SetStatus("Choose a socket first."); return; }
        if (_selected.Name.ToString() == socket.OwnerObject) { SetStatus("The socket owner cannot be snapped to its own socket."); return; }
        if (!TryGetV07SocketWorld(socket, out var p, out var n)) { SetStatus("Socket owner no longer exists."); return; }

        string library = _v07SelectedPartId;
        var def = _v07Parts.FirstOrDefault(x => x.Id == library);
        Basis basis = V07SocketBasis(n, socket.RollDeg) * V095MountAlignment(def);
        Vector3 mountPoint = def == null || def.MountPoint == null || def.MountPoint.Length < 3
            ? Vector3.Zero
            : new Vector3(def.MountPoint[0], def.MountPoint[1], def.MountPoint[2]);

        float retainedScale = Math.Max(.01f, (_selected.Scale.X + _selected.Scale.Y + _selected.Scale.Z) / 3f);
        var gt = _selected.GlobalTransform;
        gt.Basis = basis.Scaled(Vector3.One * retainedScale);
        gt.Origin = p - gt.Basis * mountPoint;
        _selected.GlobalTransform = gt;

        _v07Attachments.RemoveAll(a => a.PartObjectName == _selected.Name.ToString());
        _v07Attachments.Add(new V07AttachmentDto
        {
            PartObjectName = _selected.Name.ToString(),
            SocketId = socket.Id,
            LibraryId = library,
            UniformScale = retainedScale
        });
        ImportV06Role(_selected.Name.ToString(), "attachment");
        _v095FineTuneObject = "";
        SyncV095AttachmentControls();
        SetStatus($"Snapped {_selected.Name} to {socket.Type} using stored mount point, mount roll and current part scale.");
    }

    void RefreshV095Attachments() => RefreshV095Attachments(true);

    void RefreshV095Attachments(bool rebuildVisuals)
    {
        foreach (var a in _v07Attachments.ToList())
        {
            var part = _objects.FirstOrDefault(o => GodotObject.IsInstanceValid(o) && o.Name.ToString() == a.PartObjectName);
            var socket = _v07Sockets.FirstOrDefault(s => s.Id == a.SocketId);
            if (part == null || socket == null) continue;
            if (!TryGetV07SocketWorld(socket, out var p, out var n)) continue;

            var def = _v07Parts.FirstOrDefault(x => x.Id == a.LibraryId);
            Vector3 mountPoint = def == null || def.MountPoint == null || def.MountPoint.Length < 3
                ? Vector3.Zero
                : new Vector3(def.MountPoint[0], def.MountPoint[1], def.MountPoint[2]);
            Basis basis = V07SocketBasis(n, socket.RollDeg) * V095MountAlignment(def);

            if (a.LocalRotationDeg == null || a.LocalRotationDeg.Length < 3) a.LocalRotationDeg = new float[3];
            if (a.LocalOffset == null || a.LocalOffset.Length < 3) a.LocalOffset = new float[3];
            Vector3 rot = new(Mathf.DegToRad(a.LocalRotationDeg[0]), Mathf.DegToRad(a.LocalRotationDeg[1]), Mathf.DegToRad(a.LocalRotationDeg[2]));
            basis *= new Basis(Quaternion.FromEuler(rot));

            float uniform = Math.Max(.01f, a.UniformScale);
            var gt = part.GlobalTransform;
            gt.Basis = basis.Scaled(Vector3.One * uniform);
            Vector3 localOffset = new(a.LocalOffset[0], a.LocalOffset[1], a.LocalOffset[2]);
            gt.Origin = p + basis * localOffset - gt.Basis * mountPoint;
            part.GlobalTransform = gt;
        }
        if (rebuildVisuals) RebuildV07SocketVisuals();
    }

    void SyncV095AttachmentControls()
    {
        if (_selected == null) { _v095FineTuneObject = ""; return; }
        string name = _selected.Name.ToString();
        if (name == _v095FineTuneObject) return;
        var a = _v07Attachments.FirstOrDefault(x => x.PartObjectName == name);
        if (a == null) { _v095FineTuneObject = ""; return; }
        if (a.LocalOffset == null || a.LocalOffset.Length < 3) a.LocalOffset = new float[3];
        if (a.LocalRotationDeg == null || a.LocalRotationDeg.Length < 3) a.LocalRotationDeg = new float[3];
        if (_v07AttachOffsetX != null) _v07AttachOffsetX.Value = a.LocalOffset[0];
        if (_v07AttachOffsetY != null) _v07AttachOffsetY.Value = a.LocalOffset[1];
        if (_v07AttachOffsetZ != null) _v07AttachOffsetZ.Value = a.LocalOffset[2];
        if (_v07AttachRotX != null) _v07AttachRotX.Value = a.LocalRotationDeg[0];
        if (_v07AttachRotY != null) _v07AttachRotY.Value = a.LocalRotationDeg[1];
        if (_v07AttachRotZ != null) _v07AttachRotZ.Value = a.LocalRotationDeg[2];
        if (_v07AttachScale != null) _v07AttachScale.Value = Math.Max(.01f, a.UniformScale);
        _v095FineTuneObject = name;
    }
}
