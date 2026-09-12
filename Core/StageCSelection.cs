namespace Miniscuplter.Core;

/// <summary>
/// Stable whole-object selection identity for the bounded Stage-D viewport selection seam.
/// Godot may discover a hit, but durable project object/revision identity determines what is selected.
/// Whole-object selection can explicitly transfer to a newer active revision because it contains
/// no topology indices; future component selections must instead define their own transfer rules.
/// </summary>
public readonly record struct ObjectSelectionRef(ObjectId ObjectId, RevisionId MeshRevisionId);

public static class StageCSelection
{
    public static ObjectSelectionRef BindObject(ProjectState state, ObjectId objectId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Objects.TryGetValue(objectId, out ProjectObject? obj))
            throw new InvalidOperationException($"Cannot select missing object {objectId}.");
        return new ObjectSelectionRef(objectId, obj.ActiveMeshRevisionId);
    }

    public static bool IsCurrent(ProjectState state, ObjectSelectionRef selection)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Objects.TryGetValue(selection.ObjectId, out ProjectObject? obj) &&
               obj.ActiveMeshRevisionId == selection.MeshRevisionId;
    }

    public static ObjectSelectionRef RebindWholeObject(ProjectState state, ObjectSelectionRef selection)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Objects.TryGetValue(selection.ObjectId, out ProjectObject? obj))
            throw new InvalidOperationException($"Selected object {selection.ObjectId} no longer exists.");
        return new ObjectSelectionRef(selection.ObjectId, obj.ActiveMeshRevisionId);
    }
}
