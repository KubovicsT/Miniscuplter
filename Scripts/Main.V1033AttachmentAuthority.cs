using Godot;
using Miniscuplter.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    async void SnapSelectedV1033Object()
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

        var projection = new V07AttachmentDto
        {
            PartObjectName = _selected.Name.ToString(),
            SocketId = socket.Id,
            LibraryId = library,
            UniformScale = retainedScale
        };

        if (!V1033TryResolveMappedAttachment(_selected, socket, out ProjectSession? session, out ObjectId parentId, out ObjectId childId))
        {
            if (V1033HasStableCoreIdentity(_selected))
            {
                SetStatus("Snap failed safely; the selected Core object cannot resolve a stable Core socket owner, so legacy attachment state will not take authority.");
                return;
            }
            SnapSelectedV095Object();
            return;
        }

        await _v1020StageCGate.WaitAsync();
        try
        {
            session = _v1020StageCSession ?? throw new InvalidOperationException("Core project session is unavailable.");
            AttachmentRecord? existing = session.Current.Attachments.Values.FirstOrDefault(x => x.ChildObjectId == childId);
            TransformState local = V1033ToCoreLocalTransform(projection);
            if (existing != null)
            {
                if (existing.ParentObjectId == parentId)
                {
                    if (!StageDAttachments.IsAuthoritative(session.Current, existing))
                        existing = StageDAttachments.RebindToCurrent(session, existing);
                    StageDAttachments.UpdateIfCurrent(session, existing, socket.Id, local);
                }
                else
                {
                    StageDAttachments.RemoveIfCurrent(session, existing);
                    StageDAttachments.Create(session, AttachmentId.New(), parentId, childId, socket.Id, local);
                }
            }
            else
            {
                StageDAttachments.Create(session, AttachmentId.New(), parentId, childId, socket.Id, local);
            }
            await V1020SaveSessionAsync();
        }
        catch (Exception ex)
        {
            SetStatus("Attachment commit failed safely; Core state remains authoritative: " + ex.Message);
            return;
        }
        finally
        {
            _v1020StageCGate.Release();
        }

        var gt = _selected.GlobalTransform;
        gt.Basis = basis.Scaled(Vector3.One * retainedScale);
        gt.Origin = p - gt.Basis * mountPoint;
        _selected.GlobalTransform = gt;
        V1033ReplaceLegacyProjection(projection);
        ImportV06Role(_selected.Name.ToString(), "attachment");
        _v095FineTuneObject = "";
        SyncV095AttachmentControls();
        SetStatus($"Snapped {_selected.Name} to {socket.Type}; Core now owns the durable attachment identity and revision binding.");
    }

    async void DetachSelectedV1033Object()
    {
        if (_selected == null) return;
        if (_v1020StageCSession == null ||
            !_v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out ObjectId childId))
        {
            DetachSelectedV07Object();
            return;
        }

        AttachmentRecord? existing = _v1020StageCSession.Current.Attachments.Values.FirstOrDefault(x => x.ChildObjectId == childId);
        if (existing == null)
        {
            V1033RetireLegacyAttachmentProjection(_selected.Name.ToString());
            SetStatus("Selected Core object has no durable attachment; stale legacy attachment presentation was cleared.");
            return;
        }

        await _v1020StageCGate.WaitAsync();
        try
        {
            ProjectSession session = _v1020StageCSession ?? throw new InvalidOperationException("Core project session is unavailable.");
            AttachmentRecord current = session.Current.Attachments.TryGetValue(existing.Id, out AttachmentRecord? persisted)
                ? persisted
                : throw new InvalidOperationException("Attachment changed before detach could commit.");
            StageDAttachments.RemoveIfCurrent(session, current);
            await V1020SaveSessionAsync();
            V1033RetireLegacyAttachmentProjection(_selected.Name.ToString());
            SetStatus($"Detached {_selected.Name}; durable Core attachment state was removed transactionally.");
        }
        catch (Exception ex)
        {
            SetStatus("Detach failed safely; Core state remains authoritative: " + ex.Message);
        }
        finally
        {
            _v1020StageCGate.Release();
        }
    }

    async void ApplyV1033AttachmentFineTune()
    {
        if (_selected == null) return;
        var projection = _v07Attachments.FirstOrDefault(x => x.PartObjectName == _selected.Name.ToString());
        if (projection == null) { SetStatus("Selected object is not attached to a socket."); return; }
        if (_v1020StageCSession == null || !_v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out ObjectId childId))
        {
            ApplyV07AttachmentFineTune();
            return;
        }

        var next = new V07AttachmentDto
        {
            PartObjectName = projection.PartObjectName,
            SocketId = projection.SocketId,
            LibraryId = projection.LibraryId,
            LocalOffset = new[] { (float)(_v07AttachOffsetX?.Value ?? 0), (float)(_v07AttachOffsetY?.Value ?? 0), (float)(_v07AttachOffsetZ?.Value ?? 0) },
            LocalRotationDeg = new[] { (float)(_v07AttachRotX?.Value ?? 0), (float)(_v07AttachRotY?.Value ?? 0), (float)(_v07AttachRotZ?.Value ?? 0) },
            UniformScale = Math.Max(.01f, (float)(_v07AttachScale?.Value ?? 1))
        };

        if (!await V1033UpdateCoreAttachmentAsync(childId, next)) return;
        V1033ReplaceLegacyProjection(next);
        RefreshV095Attachments();
        SetStatus("Attachment fine tune committed through Core durable state.");
    }

    async void ResetV1033AttachmentFineTune()
    {
        if (_selected == null) return;
        var projection = _v07Attachments.FirstOrDefault(x => x.PartObjectName == _selected.Name.ToString());
        if (projection == null) { SetStatus("Selected object is not attached to a socket."); return; }
        if (_v1020StageCSession == null || !_v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out ObjectId childId))
        {
            ResetV07AttachmentFineTune();
            return;
        }

        var next = new V07AttachmentDto
        {
            PartObjectName = projection.PartObjectName,
            SocketId = projection.SocketId,
            LibraryId = projection.LibraryId,
            UniformScale = 1f
        };
        if (!await V1033UpdateCoreAttachmentAsync(childId, next)) return;
        V1033ReplaceLegacyProjection(next);
        _v095FineTuneObject = "";
        SyncV095AttachmentControls();
        RefreshV095Attachments();
        SetStatus("Attachment fine tune reset through Core durable state.");
    }

    async Task<bool> V1033UpdateCoreAttachmentAsync(ObjectId childId, V07AttachmentDto projection)
    {
        await _v1020StageCGate.WaitAsync();
        try
        {
            ProjectSession session = _v1020StageCSession ?? throw new InvalidOperationException("Core project session is unavailable.");
            AttachmentRecord existing = session.Current.Attachments.Values.FirstOrDefault(x => x.ChildObjectId == childId)
                ?? throw new InvalidOperationException("No durable Core attachment exists for the selected object.");
            if (!StageDAttachments.IsAuthoritative(session.Current, existing))
                throw new InvalidOperationException("The attachment is stale because a bound mesh revision changed; snap again or detach instead of silently carrying the old placement forward.");
            StageDAttachments.UpdateIfCurrent(session, existing, existing.Socket, V1033ToCoreLocalTransform(projection));
            await V1020SaveSessionAsync();
            return true;
        }
        catch (Exception ex)
        {
            SetStatus("Attachment update failed safely; Core state remains authoritative: " + ex.Message);
            return false;
        }
        finally
        {
            _v1020StageCGate.Release();
        }
    }

    bool V1033HasStableCoreIdentity(MeshInstance3D part)
    {
        return _v1020StageCSession != null &&
               _v1013ObjectIds.TryGetValue(part.GetInstanceId(), out ObjectId objectId) &&
               _v1020StageCSession.Current.Objects.ContainsKey(objectId);
    }

    bool V1033TryResolveMappedAttachment(
        MeshInstance3D child,
        V07SocketDto socket,
        out ProjectSession? session,
        out ObjectId parentId,
        out ObjectId childId)
    {
        session = _v1020StageCSession;
        parentId = default;
        childId = default;
        if (session == null || !_v1013ObjectIds.TryGetValue(child.GetInstanceId(), out childId)) return false;
        MeshInstance3D? parent = _objects.FirstOrDefault(o => GodotObject.IsInstanceValid(o) && o.Name.ToString() == socket.OwnerObject);
        if (parent == null || !_v1013ObjectIds.TryGetValue(parent.GetInstanceId(), out parentId)) return false;
        return session.Current.Objects.ContainsKey(parentId) && session.Current.Objects.ContainsKey(childId);
    }

    static TransformState V1033ToCoreLocalTransform(V07AttachmentDto projection)
    {
        float[] offset = projection.LocalOffset?.Length >= 3 ? projection.LocalOffset : new float[3];
        float[] rotation = projection.LocalRotationDeg?.Length >= 3 ? projection.LocalRotationDeg : new float[3];
        float scale = Math.Max(.01f, projection.UniformScale);
        return new TransformState(
            new Vec3(offset[0], offset[1], offset[2]),
            new Vec3(Mathf.DegToRad(rotation[0]), Mathf.DegToRad(rotation[1]), Mathf.DegToRad(rotation[2])),
            new Vec3(scale, scale, scale));
    }

    static void V1033ProjectCoreLocalTransform(AttachmentRecord attachment, V07AttachmentDto projection)
    {
        projection.SocketId = attachment.Socket;
        projection.LocalOffset = new[] { attachment.LocalTransform.Position.X, attachment.LocalTransform.Position.Y, attachment.LocalTransform.Position.Z };
        projection.LocalRotationDeg = new[]
        {
            Mathf.RadToDeg(attachment.LocalTransform.RotationEuler.X),
            Mathf.RadToDeg(attachment.LocalTransform.RotationEuler.Y),
            Mathf.RadToDeg(attachment.LocalTransform.RotationEuler.Z)
        };
        projection.UniformScale = Math.Max(.01f, attachment.LocalTransform.Scale.X);
    }

    bool V1033TryResolveAttachmentSocketOwner(AttachmentRecord attachment, out V07SocketDto? socket)
    {
        string socketId = attachment.Socket;
        socket = _v07Sockets.FirstOrDefault(s => s.Id == socketId);
        if (socket == null) return false;
        string ownerObject = socket.OwnerObject;
        MeshInstance3D? parent = _objects.FirstOrDefault(o => GodotObject.IsInstanceValid(o) && o.Name.ToString() == ownerObject);
        if (parent == null || !_v1013ObjectIds.TryGetValue(parent.GetInstanceId(), out ObjectId parentId)) return false;
        return parentId == attachment.ParentObjectId &&
               _v1020StageCSession != null &&
               _v1020StageCSession.Current.Objects.ContainsKey(parentId);
    }

    bool V1033TryProjectAuthoritativeAttachment(V07AttachmentDto projection, MeshInstance3D part)
    {
        if (_v1020StageCSession == null || !_v1013ObjectIds.TryGetValue(part.GetInstanceId(), out ObjectId childId))
            return true;
        AttachmentRecord? attachment = _v1020StageCSession.Current.Attachments.Values.FirstOrDefault(x => x.ChildObjectId == childId);
        if (attachment == null) return false;
        if (!StageDAttachments.IsAuthoritative(_v1020StageCSession.Current, attachment)) return false;
        if (!V1033TryResolveAttachmentSocketOwner(attachment, out _)) return false;
        V1033ProjectCoreLocalTransform(attachment, projection);
        return true;
    }

    void V1033RetireLegacyAttachmentProjection(string partObjectName)
    {
        _v07Attachments.RemoveAll(a => a.PartObjectName == partObjectName);
        _v095FineTuneObject = "";
        SyncV095AttachmentControls();
    }

    void V1033ReplaceLegacyProjection(V07AttachmentDto projection)
    {
        _v07Attachments.RemoveAll(a => a.PartObjectName == projection.PartObjectName);
        _v07Attachments.Add(projection);
    }
}
