using System.Text.Json;
using Miniscuplter.Core;

internal static class Program
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

    static void WriteBinaryStl(string path, MeshData mesh)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[80]);
        writer.Write((uint)mesh.TriangleCount);
        for (int t = 0; t < mesh.Indices.Length; t += 3)
        {
            writer.Write(0f); writer.Write(0f); writer.Write(0f);
            for (int j = 0; j < 3; j++)
            {
                int index = mesh.Indices[t + j] * 3;
                writer.Write(mesh.Positions[index]);
                writer.Write(mesh.Positions[index + 1]);
                writer.Write(mesh.Positions[index + 2]);
            }
            writer.Write((ushort)0);
        }
        writer.Flush();
        stream.Flush(true);
    }

    static async Task Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "MiniscuplterCoreTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new ProjectStore();
            string projectPath = Path.Combine(root, "foundation.msculpt2");
            var objectId = ObjectId.New();
            var first = await store.CreateMeshRevisionAsync(projectPath, objectId, Tetra(), "unit-test:first");
            var state = ProjectState.Create("Foundation")
                .WithMeshRevision(first)
                .WithObject(new ProjectObject(objectId, "Body", first.Id, TransformState.Identity));

            var session = new ProjectSession(state);
            session.Execute("Move body", current => current.WithObject(current.Objects[objectId] with
            {
                Transform = new TransformState(new Vec3(4, 5, 6), Vec3.Zero, Vec3.One)
            }), objectId);
            Assert(session.Current.Objects[objectId].Transform.Position == new Vec3(4, 5, 6), "transform command did not apply");
            Assert(session.IsDirty, "a new transaction should mark the project dirty");
            session.MarkSaved();
            Assert(!session.IsDirty, "MarkSaved should establish a durable save point");
            session.Undo();
            Assert(session.Current.Objects[objectId].Transform.Position == Vec3.Zero, "undo did not restore full before state");
            session.Redo();
            Assert(session.Current.Objects[objectId].Transform.Position == new Vec3(4, 5, 6), "redo did not restore full after state");

            var branchingSession = new ProjectSession(state);
            branchingSession.Execute("Saved branch", current => current.WithObject(current.Objects[objectId] with
            {
                Transform = new TransformState(new Vec3(1, 2, 3), Vec3.Zero, Vec3.One)
            }), objectId);
            branchingSession.MarkSaved();
            long savedBranchRevision = branchingSession.SavedRevisionNumber;
            branchingSession.Undo();
            Assert(branchingSession.IsDirty, "undoing a saved transaction should make the project dirty");
            branchingSession.Execute("Divergent branch", current => current.WithObject(current.Objects[objectId] with
            {
                Transform = new TransformState(new Vec3(7, 8, 9), Vec3.Zero, Vec3.One)
            }), objectId);
            Assert(branchingSession.Current.RevisionNumber > savedBranchRevision,
                "a new edit after undo reused an already observed revision number");
            Assert(branchingSession.IsDirty,
                "a divergent edit after undo incorrectly compared equal to the saved revision");
            Assert(!branchingSession.CanRedo,
                "a divergent edit after undo should clear the abandoned redo branch");

            var second = await store.CreateMeshRevisionAsync(projectPath, objectId, Tetra(1.1f), "unit-test:second", first.Id);
            var third = await store.CreateMeshRevisionAsync(projectPath, objectId, Tetra(1.2f), "unit-test:third", second.Id);
            session.Execute("Add generated revisions", current => current.WithMeshRevision(second).WithMeshRevision(third), objectId);
            var candidate = new CandidateRecord(CandidateId.New(), objectId, first.Id, third.Id, "ai-shape", CandidateStatus.Ready, "unit-test", DateTimeOffset.UtcNow);
            session.Execute("Add candidate", current => current.WithCandidate(candidate), objectId);
            session.Execute("Advance object independently", current => current.WithObject(current.Objects[objectId] with { ActiveMeshRevisionId = second.Id }), objectId);
            var result = session.ApplyCandidate(candidate.Id);
            Assert(!result.Applied && result.Conflict, "stale candidate should become a conflict");
            Assert(session.Current.Objects[objectId].ActiveMeshRevisionId == second.Id, "stale candidate overwrote newer mesh revision");
            Assert(session.Current.Candidates[candidate.Id].Status == CandidateStatus.Conflict, "stale candidate was not preserved as conflict");

            await store.SaveAsync(session.Current, projectPath);
            Assert(File.Exists(projectPath), "project manifest was not saved");
            var loaded = await store.LoadAsync(projectPath);
            Assert(loaded.ProjectId == session.Current.ProjectId, "project identity changed across save/load");
            Assert(loaded.Objects.Count == 1 && loaded.MeshRevisions.Count == 3, "project graph did not round-trip");
            Assert(loaded.Candidates[candidate.Id].Status == CandidateStatus.Conflict, "candidate lineage did not round-trip");
            session.MarkSaved();
            Assert(!session.IsDirty, "saving the canonical state should clear the dirty flag");

            await File.WriteAllTextAsync(projectPath, "{ this is intentionally corrupt");
            var recovered = await store.LoadWithRecoveryAsync(projectPath);
            Assert(recovered.Recovered, "recovery load should identify that it used a checkpoint");
            Assert(recovered.State.ProjectId == session.Current.ProjectId, "recovery load changed project identity");
            Assert(recovered.Warnings.Count > 0, "recovery load should explain the fallback");

            bool invalidAssetRejected = false;
            try
            {
                _ = state.WithMeshRevision(new MeshRevision(
                    RevisionId.New(), objectId, null, "../escape.msh", new string('0', 64),
                    4, 4, "unit-test", DateTimeOffset.UtcNow));
            }
            catch (InvalidDataException) { invalidAssetRejected = true; }
            Assert(invalidAssetRejected, "project state accepted an escaping asset reference");

            var layout = ProjectLayout.FromManifest(projectPath);
            Assert(Directory.EnumerateFiles(layout.RecoveryDirectory, "manifest_*.json").Any(), "bounded recovery checkpoint was not created");
            MeshData decoded = MeshBinaryCodec.Read(ProjectStore.ResolveAsset(layout, first.AssetPath));
            Assert(decoded.VertexCount == 4 && decoded.TriangleCount == 4, "internal indexed mesh codec did not round-trip");

            string legacy = Path.Combine(root, "legacy.msculpt");
            string legacyAssets = Path.Combine(root, "legacy_assets");
            string legacyMesh = Path.Combine(legacyAssets, "mesh_000.stl");
            WriteBinaryStl(legacyMesh, Tetra());
            await File.WriteAllTextAsync(legacy, JsonSerializer.Serialize(new
            {
                Version = 6,
                Objects = new[]
                {
                    new { Name = "Legacy Body", Mesh = "mesh_000.stl", Role = "mesh", Position = new[]{1f,2f,3f}, Rotation = new[]{0f,0f,0f}, Scale = new[]{1f,1f,1f} }
                },
                AiLayers = Array.Empty<object>(), Rigs = Array.Empty<object>(), Sockets = Array.Empty<object>(), Attachments = Array.Empty<object>(), SculptMasks = Array.Empty<object>()
            }));
            string migratedPath = Path.Combine(root, "legacy_migrated.msculpt2");
            var migration = await new LegacyProjectImporter(store).ImportAsync(legacy, migratedPath);
            Assert(migration.SourceSchema == 6 && migration.ObjectCount == 1, "legacy migration did not report source schema/object count");
            Assert(File.Exists(legacy), "legacy migration modified or removed the original project");
            Assert(File.Exists(migration.MigrationLogPath), "legacy migration log was not written");
            var migrated = await store.LoadAsync(migratedPath);
            Assert(migrated.Objects.Values.Single().DisplayName == "Legacy Body", "legacy object label was not preserved");
            Assert(migrated.Objects.Values.Single().Transform.Position == new Vec3(1,2,3), "legacy object transform was not preserved");

            string malicious = Path.Combine(root, "malicious.msculpt");
            await File.WriteAllTextAsync(malicious, "{\"Version\":6,\"Objects\":[{\"Name\":\"Escape\",\"Mesh\":\"../outside.stl\"}]}");
            bool rejected = false;
            try { await new LegacyProjectImporter(store).ImportAsync(malicious, Path.Combine(root, "malicious_out.msculpt2")); }
            catch (InvalidDataException) { rejected = true; }
            Assert(rejected, "legacy path traversal was not rejected");

            await StageCGenerationTests.RunAsync(root);
            await StageCEditingTests.RunAsync(root);

            Console.WriteLine("v1.0.20 canonical project-state, Stage-C generation and editing tests passed; core foundation tests passed");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }
}
