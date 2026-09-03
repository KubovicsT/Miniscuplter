using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    Mesh? _v095HeatmapMesh;
    double _v095WatchdogElapsed;

    public void InstallV095Stability()
    {
        ReplaceV095Button("Save Project", SafeV095SaveProjectDialog);
        ReplaceV095Button("Load Project", SafeV095LoadProjectDialog);
        ReplaceV095Button("Save Selected as Part", SafeV095SaveSelectedAsPart);
        ReplaceV095Button("Bake Entire Scene", async () => await SafeV095BakeVisibleSceneAsync());
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _v095WatchdogElapsed += delta;
        if (_v095WatchdogElapsed < .5) return;
        _v095WatchdogElapsed = 0;

        _objects.RemoveAll(o => !GodotObject.IsInstanceValid(o));

        if (_v09ThicknessOverlay != null && _v09ThicknessSource != null && GodotObject.IsInstanceValid(_v09ThicknessSource))
        {
            if (_v095HeatmapMesh == null) _v095HeatmapMesh = _v09ThicknessSource.Mesh;
            if (!ReferenceEquals(_v095HeatmapMesh, _v09ThicknessSource.Mesh))
            {
                ClearV09ThicknessHeatmap();
                _v095HeatmapMesh = null;
                SetStatus("Thickness heatmap cleared because the analyzed mesh geometry changed.");
            }
            else if (GodotObject.IsInstanceValid(_v09ThicknessOverlay))
            {
                _v09ThicknessOverlay.GlobalTransform = _v09ThicknessSource.GlobalTransform;
            }
        }
        else
        {
            _v095HeatmapMesh = null;
        }
    }

    void ReplaceV095Button(string text, Action action)
    {
        var buttons = FindChildren("*", "Button", true, false).OfType<Button>().Where(b => b.Text == text && b.Visible).ToList();
        foreach (var old in buttons)
        {
            if (old.GetParent() is not Container parent) continue;
            int index = old.GetIndex();
            old.Visible = false;
            old.Disabled = true;
            var replacement = new Button { Text = text, SizeFlagsHorizontal = old.SizeFlagsHorizontal };
            replacement.Pressed += action;
            parent.AddChild(replacement);
            parent.MoveChild(replacement, index);
        }
    }

    static bool V095Finite(float v) => float.IsFinite(v);
    static bool V095Vec3(float[]? v) => v != null && v.Length >= 3 && V095Finite(v[0]) && V095Finite(v[1]) && V095Finite(v[2]);

    void SafeV095SaveProjectDialog()
    {
        var d = new FileDialog { FileMode = FileDialog.FileModeEnum.SaveFile, Access = FileDialog.AccessEnum.Filesystem, Filters = new[] { "*.msculpt ; Miniscuplter projects" }, CurrentFile = "miniature.msculpt", UseNativeDialog = true };
        AddChild(d);
        d.FileSelected += p => { SafeV095SaveProject(p); d.QueueFree(); };
        d.Canceled += d.QueueFree;
        d.PopupCenteredRatio(.75f);
    }

    void SafeV095SaveProject(string projectPath)
    {
        string? generationDir = null;
        try
        {
            string full = Path.GetFullPath(projectPath);
            string parent = Path.GetDirectoryName(full) ?? throw new InvalidOperationException("Project destination has no parent directory.");
            Directory.CreateDirectory(parent);
            string assetsRoot = Path.Combine(parent, Path.GetFileNameWithoutExtension(full) + "_assets");
            Directory.CreateDirectory(assetsRoot);
            string generation = "gen_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N")[..8];
            generationDir = Path.Combine(assetsRoot, generation);
            Directory.CreateDirectory(generationDir);

            var dto = new ProjectDto
            {
                Version = 6,
                AiLayers = ExportV055AiLayers(),
                Rigs = ExportV06Rigs(),
                Sockets = ExportV07Sockets(),
                Attachments = ExportV07Attachments(),
                SculptMasks = ExportV08Masks()
            };

            int meshIndex = 0;
            foreach (var obj in _objects.Where(o => GodotObject.IsInstanceValid(o) && o.Mesh != null))
            {
                if (!V095Finite(obj.Position.X) || !V095Finite(obj.Position.Y) || !V095Finite(obj.Position.Z) ||
                    !V095Finite(obj.Rotation.X) || !V095Finite(obj.Rotation.Y) || !V095Finite(obj.Rotation.Z) ||
                    !V095Finite(obj.Scale.X) || !V095Finite(obj.Scale.Y) || !V095Finite(obj.Scale.Z))
                    throw new InvalidDataException($"Object '{obj.Name}' has a non-finite transform and cannot be saved safely.");

                string meshFile = $"mesh_{meshIndex++:000}.stl";
                string meshPath = Path.Combine(generationDir, meshFile);
                MeshIO.SaveBinaryStl(obj.Mesh!, meshPath);
                if (!File.Exists(meshPath) || new FileInfo(meshPath).Length == 0) throw new IOException($"Mesh asset was not written: {meshPath}");

                string name = obj.Name.ToString();
                string role = V06RoleFor(name);
                if (role == "mesh" && name.StartsWith("AI Edit Layer")) role = "ai_edit_layer";
                dto.Objects.Add(new ObjectDto
                {
                    Name = name,
                    Mesh = Path.Combine(generation, meshFile).Replace('\\', '/'),
                    Role = role,
                    Position = new[] { obj.Position.X, obj.Position.Y, obj.Position.Z },
                    Rotation = new[] { obj.Rotation.X, obj.Rotation.Y, obj.Rotation.Z },
                    Scale = new[] { obj.Scale.X, obj.Scale.Y, obj.Scale.Z }
                });
            }

            string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            _ = JsonSerializer.Deserialize<ProjectDto>(json) ?? throw new InvalidDataException("Generated project manifest failed validation.");

            string tmp = full + ".tmp";
            File.WriteAllText(tmp, json);
            using (var fs = new FileStream(tmp, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read)) fs.Flush(true);
            if (File.Exists(full))
            {
                string backup = full + ".bak";
                File.Copy(full, backup, true);
            }
            File.Move(tmp, full, true);

            foreach (string old in Directory.EnumerateDirectories(assetsRoot, "gen_*"))
            {
                if (Path.GetFullPath(old).Equals(Path.GetFullPath(generationDir), StringComparison.OrdinalIgnoreCase)) continue;
                try { Directory.Delete(old, true); } catch { }
            }
            SetStatus($"Saved transactional v{dto.Version} project with {dto.Objects.Count} object(s): {full}");
        }
        catch (Exception ex)
        {
            if (generationDir != null) { try { if (Directory.Exists(generationDir)) Directory.Delete(generationDir, true); } catch { } }
            SetStatus("Project save failed safely; the previous project file was not replaced: " + ex.Message);
        }
    }

    void SafeV095LoadProjectDialog()
    {
        var d = new FileDialog { FileMode = FileDialog.FileModeEnum.OpenFile, Access = FileDialog.AccessEnum.Filesystem, Filters = new[] { "*.msculpt ; Miniscuplter projects" }, UseNativeDialog = true };
        AddChild(d);
        d.FileSelected += p => { SafeV095LoadProject(p); d.QueueFree(); };
        d.Canceled += d.QueueFree;
        d.PopupCenteredRatio(.75f);
    }

    void SafeV095LoadProject(string projectPath)
    {
        try
        {
            string full = Path.GetFullPath(projectPath);
            if (!File.Exists(full)) throw new FileNotFoundException("Project file does not exist.", full);
            var dto = JsonSerializer.Deserialize<ProjectDto>(File.ReadAllText(full), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Invalid project JSON.");
            if (dto.Version < 1 || dto.Version > 6) throw new InvalidDataException($"Unsupported project version {dto.Version}. This build supports versions 1–6.");
            dto.Objects ??= new(); dto.AiLayers ??= new(); dto.Rigs ??= new(); dto.Sockets ??= new(); dto.Attachments ??= new(); dto.SculptMasks ??= new();

            string assetsRoot = Path.Combine(Path.GetDirectoryName(full)!, Path.GetFileNameWithoutExtension(full) + "_assets");
            var staged = new List<(ObjectDto dto, ArrayMesh mesh)>();
            foreach (var item in dto.Objects)
            {
                if (string.IsNullOrWhiteSpace(item.Mesh)) throw new InvalidDataException($"Object '{item.Name}' has no mesh asset reference.");
                if (!V095Vec3(item.Position) || !V095Vec3(item.Rotation) || !V095Vec3(item.Scale)) throw new InvalidDataException($"Object '{item.Name}' has an invalid transform record.");
                string meshPath = Path.GetFullPath(Path.Combine(assetsRoot, item.Mesh.Replace('/', Path.DirectorySeparatorChar)));
                if (!meshPath.StartsWith(Path.GetFullPath(assetsRoot), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Project contains an asset path outside its asset directory.");
                if (!File.Exists(meshPath)) throw new FileNotFoundException($"Project mesh asset for '{item.Name}' is missing.", meshPath);
                var mesh = MeshIO.LoadStl(meshPath);
                if (mesh.GetSurfaceCount() == 0) throw new InvalidDataException($"Project mesh asset for '{item.Name}' contains no surfaces.");
                staged.Add((item, mesh));
            }

            ClearV09ThicknessHeatmap();
            foreach (var o in _objects.ToList()) if (GodotObject.IsInstanceValid(o)) o.QueueFree();
            _objects.Clear(); _selected = null; _undo.Clear(); _redo.Clear();

            foreach (var entry in staged)
            {
                var item = entry.dto;
                AddMeshObject(entry.mesh, string.IsNullOrWhiteSpace(item.Name) ? "Object" : item.Name, new Vector3(item.Position[0], item.Position[1], item.Position[2]));
                if (_selected != null)
                {
                    _selected.Rotation = new Vector3(item.Rotation[0], item.Rotation[1], item.Rotation[2]);
                    _selected.Scale = new Vector3(item.Scale[0], item.Scale[1], item.Scale[2]);
                    ImportV06Role(_selected.Name.ToString(), item.Role ?? "mesh");
                }
            }

            ImportV055AiLayers(dto.AiLayers);
            ImportV06Rigs(dto.Rigs);
            ImportV08Masks(dto.SculptMasks);
            ImportV07State(dto.Sockets, dto.Attachments);
            RebuildSceneList();
            var rigObject = _objects.FirstOrDefault(o => dto.Rigs.Any(r => r.ObjectName == o.Name.ToString()));
            if (rigObject != null) { Select(rigObject); RestoreV06RigForObject(rigObject); }
            else if (_objects.Count > 0) Select(_objects[0]);
            FrameSelected();
            SetStatus($"Loaded validated project v{dto.Version} with {_objects.Count} object(s). Preflight completed before the previous scene was replaced.");
        }
        catch (Exception ex)
        {
            SetStatus("Project load rejected before scene replacement: " + ex.Message);
        }
    }

    void SafeV095SaveSelectedAsPart()
    {
        if (_selected?.Mesh == null) { SetStatus("Select a scene mesh first."); return; }
        try
        {
            string id = Guid.NewGuid().ToString("N");
            string dest = Path.Combine(V07LibraryRoot(), "meshes", id + ".stl");
            MeshIO.SaveBinaryStl(_selected.Mesh, dest);
            if (!File.Exists(dest) || new FileInfo(dest).Length == 0) throw new IOException("Reusable part STL was not written.");
            var def = new V07PartDefinition
            {
                Id = id,
                Name = _selected.Name.ToString(),
                Category = CurrentV07Category() == "All" ? "Generic" : CurrentV07Category(),
                SocketType = CurrentV07SocketType(),
                MeshPath = dest,
                DefaultScale = 1f
            };
            _v07Parts.Add(def);
            SaveV095LibraryIndexAtomic();
            _v07SelectedPartId = id;
            RebuildV07LibraryList();
            SetStatus($"Saved {_selected.Name} as a reusable local-frame part. Scene translation/rotation/scale were not baked into the library mesh.");
        }
        catch (Exception ex) { SetStatus("Reusable part save failed: " + ex.Message); }
    }

    void SaveV095LibraryIndexAtomic()
    {
        string path = V07LibraryIndex();
        string tmp = path + ".tmp";
        var persistent = _v07Parts.Where(p => string.IsNullOrEmpty(p.Builtin)).ToList();
        File.WriteAllText(tmp, JsonSerializer.Serialize(persistent, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, path, true);
    }

    async Task SafeV095BakeVisibleSceneAsync()
    {
        var meshes = _objects.Where(o => GodotObject.IsInstanceValid(o) && o.Visible && o.Mesh != null).ToList();
        if (meshes.Count == 0) { SetStatus("No visible scene meshes are available to finalize."); return; }
        try
        {
            foreach (var obj in meshes)
            {
                if (!obj.GlobalTransform.IsFinite()) throw new InvalidDataException($"'{obj.Name}' has an invalid transform.");
            }
            var inputs = new List<string>();
            foreach (var obj in meshes) inputs.Add(ExportSelectedV09Input(obj, "final_part"));
            string output = Path.Combine(V09PrepDir(), $"final_model_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
            double pitch = _v09RepairVoxel?.Value ?? .30;
            SetStatus($"Finalizing {inputs.Count} visible mesh(es) at {pitch:0.00} mm voxel pitch…");
            string path = await _ai.VoxelRemeshAsync(inputs, output, pitch);
            var result = MeshIO.LoadStl(path);
            if (result.GetSurfaceCount() == 0) throw new InvalidDataException("Finalization backend returned an empty mesh.");
            AddMeshObject(result, "Final Model v0.9.5");
            await AnalyzeSelectedV09Async();
            SetStatus($"Final model created from visible scene meshes only. Originals remain unchanged. STL: {path}");
        }
        catch (Exception ex) { SetStatus("Final model creation failed without modifying source objects: " + ex.Message); }
    }
}
