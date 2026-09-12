using Miniscuplter.Core;

internal static class StageCEditingTests
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }

    static MeshData Tetra(float scale = 1f) => new(
        new float[]
        {
            0,0,0,
            scale,0,0,
            0,scale,0,
            0,0,scale
        },
        new int[]
        {
            0,2,1,
            0,1,3,
            0,3,2,
            1,2,3
        });

    public static async Task RunAsync(string root)
    {
        var store = new ProjectStore();
        string projectPath = Path.Combine(root, "stagec_editing.msculpt2");
        ObjectId objectId = ObjectId.New();
        MeshRevision generated = await store.CreateMeshRevisionAsync(
            projectPath, objectId, Tetra(), "test:generated");
        ProjectState initial = ProjectState.Create("Stage-C editing")
            .WithMeshRevision(generated)
            .WithObject(new ProjectObject(objectId, "Generated body", generated.Id, TransformState.Identity));
        var session = new ProjectSession(initial);

        TransformState transformed = new(
            new Vec3(12.5f, -3f, 8f),
            new Vec3(0.1f, 0.5f, -0.2f),
            new Vec3(1.2f, 0.8f, 1.1f));
        bool changed = StageCEditing.SetTransform(session, objectId, transformed, "gizmo transform");
        Assert(changed, "transform command reported no change");
        Assert(session.Current.Objects[objectId].Transform == transformed, "transform did not become authoritative project state");
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == generated.Id, "transform changed active mesh revision");
        Assert(StageCEditing.IsEditingTransaction(session.UndoTransactions.First()), "transform transaction was not marked as Stage-C editing history");

        await store.SaveAsync(session.Current, projectPath);
        session.MarkSaved();
        ProjectState transformedReload = await store.LoadAsync(projectPath);
        Assert(transformedReload.Objects[objectId].Transform == transformed, "transform did not survive save/reload");
        Assert(StageCCleanup.ResolveExportRevision(transformedReload, objectId, generated.Id).Id == generated.Id,
            "transform changed exact export revision scope");

        session.Undo();
        Assert(session.Current.Objects[objectId].Transform == TransformState.Identity, "transform undo did not restore complete prior state");
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == generated.Id, "transform undo changed mesh identity");
        session.Redo();
        Assert(session.Current.Objects[objectId].Transform == transformed, "transform redo did not restore complete state");

        TransformState grounded = transformed with
        {
            Position = new Vec3(transformed.Position.X, 0f, transformed.Position.Z)
        };
        bool groundedChanged = StageCEditing.SetTransformIfCurrent(
            session,
            objectId,
            generated.Id,
            transformed,
            grounded,
            "ground");
        Assert(groundedChanged, "conditional transform command reported no change");
        Assert(session.Current.Objects[objectId].Transform == grounded,
            "conditional transform did not become authoritative project state");
        session.Undo();
        Assert(session.Current.Objects[objectId].Transform == transformed,
            "conditional transform undo did not restore the expected durable transform");

        bool staleTransformRejected = false;
        try
        {
            StageCEditing.SetTransformIfCurrent(
                session,
                objectId,
                generated.Id,
                TransformState.Identity,
                grounded,
                "stale ground");
        }
        catch (InvalidOperationException) { staleTransformRejected = true; }
        Assert(staleTransformRejected, "conditional transform accepted a stale expected transform");
        Assert(session.Current.Objects[objectId].Transform == transformed,
            "stale conditional transform rejection changed durable state");

        ObjectSelectionRef objectSelection = StageCSelection.BindObject(session.Current, objectId);
        Assert(objectSelection.ObjectId == objectId && objectSelection.MeshRevisionId == generated.Id,
            "whole-object selection did not bind stable object/revision identity");
        Assert(StageCSelection.IsCurrent(session.Current, objectSelection),
            "fresh whole-object selection was incorrectly considered stale");

        SelectionId selectionId = SelectionId.New();
        ProjectLayout layout = ProjectLayout.FromManifest(projectPath);
        layout.EnsureDirectories();
        string selectionAsset = $"data/selection_{selectionId}.json";
        await File.WriteAllTextAsync(ProjectStore.ResolveAsset(layout, selectionAsset), "{\"weights\":[1,0,1,0]}");
        SelectionBinding durableSelection = StageCSelection.BindRevisionSelection(
            session,
            selectionId,
            objectId,
            generated.Id,
            "smart-select-vertex-weights",
            selectionAsset);
        Assert(StageCSelection.IsCurrent(session.Current, durableSelection),
            "fresh durable revision-dependent selection was incorrectly considered stale");
        await store.SaveAsync(session.Current, projectPath);
        ProjectState selectionReload = await store.LoadAsync(projectPath);
        Assert(selectionReload.Selections.TryGetValue(selectionId, out SelectionBinding? persistedSelection) && persistedSelection == durableSelection,
            "durable revision-dependent selection did not survive save/reload");
        Assert(StageCSelection.IsCurrent(selectionReload, durableSelection),
            "reloaded durable selection was incorrectly considered stale");

        MeshRevision sculpt = await store.CreateMeshRevisionAsync(
            projectPath, objectId, Tetra(1.15f), "stagec-edit:sculpt-stroke", generated.Id);
        StageCEditing.CommitMeshRevision(session, objectId, generated.Id, sculpt, "sculpt stroke");
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == sculpt.Id, "sculpt commit did not advance active mesh revision");
        Assert(session.Current.MeshRevisions[sculpt.Id].ParentRevisionId == generated.Id, "sculpt revision lost exact parent lineage");
        Assert(session.Current.Objects[objectId].Transform == transformed, "sculpt commit changed object transform");
        Assert(StageCCleanup.ResolveExportRevision(session.Current, objectId, sculpt.Id).Id == sculpt.Id,
            "export scope did not advance to committed sculpt revision");
        Assert(!StageCSelection.IsCurrent(session.Current, objectSelection),
            "revision-bound whole-object selection silently remained current after topology revision advanced");
        Assert(!StageCSelection.IsCurrent(session.Current, durableSelection),
            "revision-dependent vertex selection silently survived a mesh revision change");
        ObjectSelectionRef reboundSelection = StageCSelection.RebindWholeObject(session.Current, objectSelection);
        Assert(reboundSelection.ObjectId == objectId && reboundSelection.MeshRevisionId == sculpt.Id,
            "whole-object selection did not explicitly transfer to the current mesh revision");
        Assert(StageCSelection.IsCurrent(session.Current, reboundSelection),
            "explicitly rebound whole-object selection is not current");

        bool staleRevisionRejected = false;
        try
        {
            StageCEditing.SetTransformIfCurrent(
                session,
                objectId,
                generated.Id,
                transformed,
                grounded,
                "stale revision ground");
        }
        catch (InvalidOperationException) { staleRevisionRejected = true; }
        Assert(staleRevisionRejected, "conditional transform accepted a stale mesh revision");
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == sculpt.Id,
            "stale revision rejection changed active mesh identity");
        Assert(session.Current.Objects[objectId].Transform == transformed,
            "stale revision rejection changed durable transform");

        session.Undo();
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == generated.Id, "sculpt undo did not restore generated revision");
        Assert(session.Current.Objects[objectId].Transform == transformed, "sculpt undo changed transform");
        Assert(StageCSelection.IsCurrent(session.Current, durableSelection),
            "sculpt undo did not restore revision-dependent selection validity");
        session.Redo();
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == sculpt.Id, "sculpt redo did not restore edited revision");
        Assert(!StageCSelection.IsCurrent(session.Current, durableSelection),
            "sculpt redo incorrectly left stale vertex selection valid");

        MeshRevision stale = await store.CreateMeshRevisionAsync(
            projectPath, objectId, Tetra(1.25f), "stagec-edit:stale", generated.Id);
        bool staleRejected = false;
        try { StageCEditing.CommitMeshRevision(session, objectId, generated.Id, stale, "stale sculpt"); }
        catch (InvalidOperationException) { staleRejected = true; }
        Assert(staleRejected, "stale sculpt output was allowed to overwrite a newer active revision");
        Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == sculpt.Id, "stale sculpt rejection changed active revision");

        await store.SaveAsync(session.Current, projectPath);
        session.MarkSaved();
        ProjectState editedReload = await store.LoadAsync(projectPath);
        Assert(editedReload.Objects[objectId].ActiveMeshRevisionId == sculpt.Id, "committed sculpt revision did not survive save/reload");
        Assert(editedReload.Objects[objectId].Transform == transformed, "edited object transform did not survive save/reload");
        Assert(editedReload.Selections.TryGetValue(selectionId, out SelectionBinding? stalePersisted) && stalePersisted == durableSelection,
            "historical durable selection binding was lost across edited save/reload");
        Assert(!StageCSelection.IsCurrent(editedReload, durableSelection),
            "stale revision-dependent selection became current after save/reload");
        Assert(StageCCleanup.ResolveExportRevision(editedReload, objectId, sculpt.Id).Id == sculpt.Id,
            "reloaded export scope is not the exact edited revision");
    }
}
