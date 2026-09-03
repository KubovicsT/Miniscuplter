using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV095LoadGuards()
    {
        ReplaceV095Button("Load Project", OpenV095StrictLoadDialog);
    }

    void OpenV095StrictLoadDialog()
    {
        var d = new FileDialog { FileMode = FileDialog.FileModeEnum.OpenFile, Access = FileDialog.AccessEnum.Filesystem, Filters = new[] { "*.msculpt ; Miniscuplter projects" }, UseNativeDialog = true };
        AddChild(d);
        d.FileSelected += p => { StrictV095LoadProject(p); d.QueueFree(); };
        d.Canceled += d.QueueFree;
        d.PopupCenteredRatio(.75f);
    }

    void StrictV095LoadProject(string path)
    {
        string full = Path.GetFullPath(path);
        try
        {
            if (!File.Exists(full)) throw new FileNotFoundException("Project file does not exist.", full);
            var dto = ReadAndValidateV095ProjectManifest(full);
            SafeV095LoadProject(full);
        }
        catch (Exception primaryError)
        {
            string backup = full + ".bak";
            try
            {
                if (!File.Exists(backup)) throw new InvalidDataException("No saved project backup is available.");
                _ = ReadAndValidateV095ProjectManifest(backup);
                string corruptCopy = full + ".corrupt_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                if (File.Exists(full)) File.Copy(full, corruptCopy, true);
                File.Copy(backup, full, true);
                _ = ReadAndValidateV095ProjectManifest(full);
                SetStatus("Primary project manifest was invalid; restored the last validated .bak manifest and preserved the broken file as a .corrupt copy.");
                SafeV095LoadProject(full);
            }
            catch (Exception backupError)
            {
                SetStatus("Project load rejected; current scene is unchanged. Primary error: " + primaryError.Message + " Backup recovery: " + backupError.Message);
            }
        }
    }

    static ProjectDto ReadAndValidateV095ProjectManifest(string path)
    {
        var dto = JsonSerializer.Deserialize<ProjectDto>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Invalid project JSON.");
        ValidateV095ProjectMetadata(dto);
        return dto;
    }

    static bool V095FiniteArray(float[]? values, int minimum)
        => values != null && values.Length >= minimum && values.All(float.IsFinite);

    static void ValidateV095ProjectMetadata(ProjectDto dto)
    {
        if (dto.Version < 1 || dto.Version > 6) throw new InvalidDataException($"Unsupported project version {dto.Version}.");
        dto.Objects ??= new(); dto.Rigs ??= new(); dto.Sockets ??= new(); dto.Attachments ??= new(); dto.SculptMasks ??= new(); dto.AiLayers ??= new();
        if (dto.Objects.Count > 10000) throw new InvalidDataException("Project contains an unreasonable number of scene objects.");

        var objectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in dto.Objects)
        {
            if (string.IsNullOrWhiteSpace(o.Name)) throw new InvalidDataException("Project contains an unnamed object.");
            if (!objectNames.Add(o.Name)) throw new InvalidDataException($"Duplicate object identity '{o.Name}'.");
            if (!V095FiniteArray(o.Position, 3) || !V095FiniteArray(o.Rotation, 3) || !V095FiniteArray(o.Scale, 3)) throw new InvalidDataException($"Object '{o.Name}' has invalid transform data.");
            if (Math.Abs(o.Scale[0]) < 1e-8f || Math.Abs(o.Scale[1]) < 1e-8f || Math.Abs(o.Scale[2]) < 1e-8f) throw new InvalidDataException($"Object '{o.Name}' has a zero scale component.");
        }

        foreach (var rig in dto.Rigs)
        {
            if (!objectNames.Contains(rig.ObjectName)) throw new InvalidDataException($"Rig references missing object '{rig.ObjectName}'.");
            if (rig.Joints == null || rig.Joints.Count == 0 || rig.Joints.Count > 512) throw new InvalidDataException($"Rig for '{rig.ObjectName}' has an invalid joint count.");
            for (int i = 0; i < rig.Joints.Count; i++)
            {
                var j = rig.Joints[i];
                if (!V095FiniteArray(j.Position, 3) || !V095FiniteArray(j.RotationDeg, 3)) throw new InvalidDataException($"Rig joint {i} on '{rig.ObjectName}' has invalid coordinates.");
                if (j.Parent < -1 || j.Parent >= rig.Joints.Count || j.Parent == i) throw new InvalidDataException($"Rig joint {i} on '{rig.ObjectName}' has invalid parent {j.Parent}.");
            }
            for (int i = 0; i < rig.Joints.Count; i++)
            {
                var seen = new HashSet<int>(); int p = i;
                while (p >= 0)
                {
                    if (!seen.Add(p)) throw new InvalidDataException($"Rig for '{rig.ObjectName}' contains a parent cycle.");
                    p = rig.Joints[p].Parent;
                }
            }
        }

        var socketIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in dto.Sockets)
        {
            if (string.IsNullOrWhiteSpace(s.Id) || !socketIds.Add(s.Id)) throw new InvalidDataException("Project contains a missing or duplicate socket ID.");
            if (!objectNames.Contains(s.OwnerObject)) throw new InvalidDataException($"Socket '{s.Name}' references missing owner '{s.OwnerObject}'.");
            if (!V095FiniteArray(s.LocalPosition, 3) || !V095FiniteArray(s.LocalNormal, 3) || !V095FiniteArray(s.JointOffset, 3) || !float.IsFinite(s.SurfaceOffset) || !float.IsFinite(s.RollDeg)) throw new InvalidDataException($"Socket '{s.Name}' contains invalid numeric data.");
            if (new Vector3(s.LocalNormal[0], s.LocalNormal[1], s.LocalNormal[2]).LengthSquared() < 1e-10f) throw new InvalidDataException($"Socket '{s.Name}' has a zero normal.");
        }

        var attachedObjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in dto.Attachments)
        {
            if (!objectNames.Contains(a.PartObjectName)) throw new InvalidDataException($"Attachment references missing object '{a.PartObjectName}'.");
            if (!socketIds.Contains(a.SocketId)) throw new InvalidDataException($"Attachment '{a.PartObjectName}' references missing socket '{a.SocketId}'.");
            if (!attachedObjects.Add(a.PartObjectName)) throw new InvalidDataException($"Object '{a.PartObjectName}' has multiple attachment records.");
            if (!V095FiniteArray(a.LocalOffset, 3) || !V095FiniteArray(a.LocalRotationDeg, 3) || !float.IsFinite(a.UniformScale) || a.UniformScale <= 0f) throw new InvalidDataException($"Attachment '{a.PartObjectName}' contains invalid transform data.");
        }

        var maskObjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mask in dto.SculptMasks)
        {
            if (!objectNames.Contains(mask.ObjectName)) throw new InvalidDataException($"Sculpt mask references missing object '{mask.ObjectName}'.");
            if (!maskObjects.Add(mask.ObjectName)) throw new InvalidDataException($"Object '{mask.ObjectName}' has duplicate sculpt masks.");
            if (mask.Values == null || mask.Values.Length > 100000000) throw new InvalidDataException($"Sculpt mask for '{mask.ObjectName}' has invalid size.");
            if (mask.Values.Any(v => !float.IsFinite(v) || v < 0f || v > 1f)) throw new InvalidDataException($"Sculpt mask for '{mask.ObjectName}' contains invalid values.");
        }
    }
}
