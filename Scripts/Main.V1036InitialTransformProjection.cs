using Godot;
using Miniscuplter.Core;
using System.Collections.Generic;

namespace Miniscuplter;

public partial class Main
{
    readonly HashSet<ulong> _v1036ObservedMappedPresentations = new();

    public void InstallV1036InitialTransformProjection()
    {
        // Existing mapped presentations are restored through the Stage-C load path, which already
        // projects durable state directly. New presentations are projected synchronously at the
        // insertion/mapping boundary instead of being discovered by a polling timer.
        foreach (ulong instanceId in _v1013ObjectIds.Keys)
            _v1036ObservedMappedPresentations.Add(instanceId);
    }

    void V1036ProjectMappedPresentation(MeshInstance3D presentation, ObjectId objectId)
    {
        if (!GodotObject.IsInstanceValid(presentation) || _v1020StageCSession == null) return;
        ulong instanceId = presentation.GetInstanceId();
        if (_v1036ObservedMappedPresentations.Contains(instanceId)) return;
        if (!_v1013ObjectIds.TryGetValue(instanceId, out ObjectId mappedObjectId) || mappedObjectId != objectId) return;
        if (!_v1020StageCSession.Current.Objects.TryGetValue(objectId, out ProjectObject? projectObject)) return;

        // Stage-C candidate reconstruction currently carries positions + triangle indices into
        // Godot, but no normal array. Generate normals once at the exact presentation boundary so
        // standard back-face culling/lighting does not make the generated surface look like an
        // inside-out shell.
        V1036EnsureGeneratedPresentationNormals(presentation);

        // The Stage-C generation path remains the sole owner of its busy lifetime. Once it has
        // durably applied the candidate and mapped the new presentation, project that exact
        // object's transform immediately; no timer scans unrelated scene objects or clears busy.
        V1020ProjectObjectStateToScene(presentation, projectObject, reloadMesh: false);
        _v1036ObservedMappedPresentations.Add(instanceId);
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
