using Godot;
using Miniscuplter.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    readonly Dictionary<ulong, ObjectId> _v1013ObjectIds = new();
    Window? _v1013ProjectDataWindow;
    Label? _v1013ProjectDataStatus;

    public void InstallV1013FoundationBridge()
    {
        if (FindChild("HBoxContainer", true, false) is not HBoxContainer top) return;
        var button = new Button
        {
            Text = "Project Data",
            TooltipText = "Safe next-generation project copies and legacy migration tools. Existing .msculpt projects are never overwritten."
        };
        button.Pressed += ShowV1013ProjectData;
        top.AddChild(button);
    }

    void ShowV1013ProjectData()
    {
        if (_v1013ProjectDataWindow == null || !GodotObject.IsInstanceValid(_v1013ProjectDataWindow))
            BuildV1013ProjectDataWindow();
        _v1013ProjectDataWindow!.PopupCentered();
    }

    void BuildV1013ProjectDataWindow()
    {
        var window = new Window
        {
            Title = "Project Data",
            Size = new Vector2I(700, 420),
            MinSize = new Vector2I(560, 360),
            Transient = true
        };
        AddChild(window);
        _v1013ProjectDataWindow = window;
        window.CloseRequested += window.Hide;

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.OffsetLeft = 16; root.OffsetTop = 16; root.OffsetRight = -16; root.OffsetBottom = -16;
        window.AddChild(root);

        root.AddChild(new Label { Text = "NEXT-GENERATION PROJECT FOUNDATION", ThemeTypeVariation = "HeaderSmall" });
        root.AddChild(new Label
        {
            Text = "The current editor remains on the proven .msculpt save path while the replacement project model is introduced beside it. These tools only create new copies: stable object IDs, immutable indexed mesh revisions, SHA-verified assets and bounded recovery checkpoints. Your current project is never replaced.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var save = new Button { Text = "Create Next-Generation Copy of Current Scene" };
        save.Pressed += OpenV1013SnapshotDialog;
        root.AddChild(save);

        var migrate = new Button { Text = "Migrate Existing .msculpt Project to a Safe Copy" };
        migrate.Pressed += OpenV1013MigrationDialog;
        root.AddChild(migrate);

        root.AddChild(new Label
        {
            Text = "The new .msculpt2 format is a development bridge in v1.0.13. Do not delete the original .msculpt project. Rich legacy rig/mask/socket/AI metadata is retained for the semantic migration adapters that follow.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _v1013ProjectDataStatus = new Label { Text = "Project foundation ready.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_v1013ProjectDataStatus);
        var close = new Button { Text = "Close" }; close.Pressed += window.Hide; root.AddChild(close);
    }

    void OpenV1013SnapshotDialog()
    {
        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.msculpt2 ; Miniscuplter next-generation project" },
            CurrentDir = V099ProjectRoot(),
            CurrentFile = "miniature.msculpt2",
            UseNativeDialog = true
        };
        AddChild(dialog);
        dialog.FileSelected += async path =>
        {
            try { await SaveV1013SceneSnapshotAsync(path); }
            finally { dialog.QueueFree(); }
        };
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCenteredRatio(.75f);
    }

    async Task SaveV1013SceneSnapshotAsync(string destination)
    {
        try
        {
            string full = Path.GetFullPath(destination);
            if (!full.EndsWith(ProjectStore.ProjectExtension, StringComparison.OrdinalIgnoreCase)) full += ProjectStore.ProjectExtension;
            if (File.Exists(full)) throw new IOException("Next-generation project copy already exists. Choose a new file name so the previous copy remains untouched.");

            var live = _objects.Where(o => GodotObject.IsInstanceValid(o) && o.Mesh != null).ToList();
            if (live.Count == 0) throw new InvalidOperationException("The scene contains no mesh objects to snapshot.");

            var store = new ProjectStore();
            var state = ProjectState.Create(Path.GetFileNameWithoutExtension(full))
                .WithMetadata("created_by", "v1.0.13-editor-bridge")
                .WithMetadata("legacy_scene_object_count", live.Count.ToString());

            foreach (var obj in live)
            {
                ObjectId id = V1013ObjectId(obj);
                MeshData data = V1013MeshData(obj.Mesh!);
                var revision = await store.CreateMeshRevisionAsync(full, id, data, "editor-scene-snapshot");
                state = state.WithMeshRevision(revision).WithObject(new ProjectObject(
                    id,
                    obj.Name.ToString(),
                    revision.Id,
                    new TransformState(V1013Vec(obj.Position), V1013Vec(obj.Rotation), V1013Vec(obj.Scale)),
                    V06RoleFor(obj.Name.ToString()),
                    obj.Visible));
            }

            await store.SaveAsync(state, full);
            var verified = await store.LoadAsync(full);
            if (verified.Objects.Count != live.Count) throw new InvalidDataException("Verification reload did not contain every scene object.");
            V1013ProjectStatus($"Created verified next-generation project copy with {verified.Objects.Count} object(s): {full}");
        }
        catch (Exception ex)
        {
            V1013ProjectStatus("Next-generation project copy failed safely: " + ex.Message);
        }
    }

    void OpenV1013MigrationDialog()
    {
        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.msculpt ; Miniscuplter legacy projects" },
            CurrentDir = V099ProjectRoot(),
            UseNativeDialog = true
        };
        AddChild(dialog);
        dialog.FileSelected += async source =>
        {
            try { await MigrateV1013LegacyProjectAsync(source); }
            finally { dialog.QueueFree(); }
        };
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCenteredRatio(.75f);
    }

    async Task MigrateV1013LegacyProjectAsync(string source)
    {
        try
        {
            string fullSource = Path.GetFullPath(source);
            string parent = Path.GetDirectoryName(fullSource) ?? V099ProjectRoot();
            string stem = Path.GetFileNameWithoutExtension(fullSource) + "_migrated";
            string destination = Path.Combine(parent, stem + ProjectStore.ProjectExtension);
            int suffix = 2;
            while (File.Exists(destination) || Directory.Exists(ProjectLayout.FromManifest(destination).AssetsRoot))
                destination = Path.Combine(parent, $"{stem}_{suffix++}{ProjectStore.ProjectExtension}");

            V1013ProjectStatus("Migrating legacy project into a new copy…");
            var result = await new LegacyProjectImporter().ImportAsync(fullSource, destination);
            string warningText = result.Warnings.Count == 0 ? "" : $" {result.Warnings.Count} rich legacy section(s) were retained for the next semantic adapter pass.";
            V1013ProjectStatus($"Migrated schema {result.SourceSchema} project with {result.ObjectCount} object(s): {result.DestinationProject}.{warningText}");
        }
        catch (Exception ex)
        {
            V1013ProjectStatus("Legacy migration failed safely; the original project was not changed: " + ex.Message);
        }
    }

    ObjectId V1013ObjectId(MeshInstance3D obj)
    {
        ulong key = obj.GetInstanceId();
        if (_v1013ObjectIds.TryGetValue(key, out var existing)) return existing;
        var created = ObjectId.New();
        _v1013ObjectIds[key] = created;
        return created;
    }

    static MeshData V1013MeshData(Mesh mesh)
    {
        var positions = new List<float>();
        var indices = new List<int>();
        int baseVertex = 0;
        for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++)
        {
            if (mesh.SurfaceGetPrimitiveType(surface) != Mesh.PrimitiveType.Triangles)
                throw new InvalidDataException($"Surface {surface} is not triangle geometry and cannot enter the indexed project store.");
            var arrays = mesh.SurfaceGetArrays(surface);
            var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            if (vertices.Length == 0) continue;
            foreach (Vector3 v in vertices)
            {
                if (!v.IsFinite()) throw new InvalidDataException("Scene mesh contains non-finite coordinates.");
                positions.Add(v.X); positions.Add(v.Y); positions.Add(v.Z);
            }
            var sourceIndices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            if (sourceIndices.Length > 0)
            {
                if (sourceIndices.Length % 3 != 0) throw new InvalidDataException("Scene mesh index buffer is not triangular.");
                foreach (int index in sourceIndices)
                {
                    if (index < 0 || index >= vertices.Length) throw new InvalidDataException("Scene mesh contains an invalid vertex index.");
                    indices.Add(baseVertex + index);
                }
            }
            else
            {
                if (vertices.Length % 3 != 0) throw new InvalidDataException("Unindexed scene mesh does not contain complete triangles.");
                for (int i = 0; i < vertices.Length; i++) indices.Add(baseVertex + i);
            }
            baseVertex += vertices.Length;
        }
        return new MeshData(positions, indices);
    }

    static Vec3 V1013Vec(Vector3 value) => new(value.X, value.Y, value.Z);

    void V1013ProjectStatus(string text)
    {
        if (_v1013ProjectDataStatus != null) _v1013ProjectDataStatus.Text = text;
        SetStatus(text);
    }
}
