using Godot;
using Miniscuplter.Core;
using System.Collections.Generic;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    readonly HashSet<ulong> _v1036ObservedMappedPresentations = new();
    Timer? _v1036InitialTransformTimer;

    public void InstallV1036InitialTransformProjection()
    {
        foreach (ulong instanceId in _v1013ObjectIds.Keys)
            _v1036ObservedMappedPresentations.Add(instanceId);

        if (_v1036InitialTransformTimer != null) return;
        _v1036InitialTransformTimer = new Timer
        {
            Name = "v1.0.36 Initial Transform Projection",
            WaitTime = .05,
            OneShot = false,
            Autostart = true
        };
        _v1036InitialTransformTimer.Timeout += V1036ProjectNewMappedPresentation;
        AddChild(_v1036InitialTransformTimer);
    }

    void V1036ProjectNewMappedPresentation()
    {
        if (_v1093DBusy || _v1020StageCSession == null) return;

        foreach (MeshInstance3D presentation in _objects.ToArray())
        {
            if (!GodotObject.IsInstanceValid(presentation)) continue;
            ulong instanceId = presentation.GetInstanceId();
            if (_v1036ObservedMappedPresentations.Contains(instanceId)) continue;
            if (!_v1013ObjectIds.TryGetValue(instanceId, out ObjectId objectId)) continue;
            if (!_v1020StageCSession.Current.Objects.TryGetValue(objectId, out ProjectObject? projectObject)) continue;

            // Stage-C candidate reconstruction currently carries positions + triangle indices into
            // Godot, but no normal array. Generate normals once at the presentation boundary so
            // standard back-face culling/lighting does not make the generated surface look like an
            // inside-out shell.
            V1036EnsureGeneratedPresentationNormals(presentation);

            // Generation creates the Godot mesh before its durable placement is projected. Do the
            // first projection immediately after the generation busy boundary instead of leaving a
            // tiny/default-transform presentation visible until a later reconciliation happens.
            V1020ProjectObjectStateToScene(presentation, projectObject, reloadMesh: false);
            _v1036ObservedMappedPresentations.Add(instanceId);
            if (ReferenceEquals(_selected, presentation))
                FrameSelected();
        }
    }

    static void V1036EnsureGeneratedPresentationNormals(MeshInstance3D presentation)
    {
        if (presentation.Mesh is not ArrayMesh mesh || mesh.GetSurfaceCount() == 0) return;
        var arrays = mesh.SurfaceGetArrays(0);
        if (arrays.Count > (int)Mesh.ArrayType.Normal && arrays[(int)Mesh.ArrayType.Normal].VariantType != Variant.Type.Nil)
            return;

        var surface = new SurfaceTool();
        surface.CreateFrom(mesh, 0);
        surface.GenerateNormals();
        ArrayMesh rebuilt = surface.Commit();
        presentation.Mesh = rebuilt;
    }
}
