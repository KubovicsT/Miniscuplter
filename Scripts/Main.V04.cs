using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    HSlider? _v04VoxelSize;
    CheckButton? _v04PreserveOriginals;

    public void InstallV04Extras()
    {
        var print = FindChild("Print", true, false) as VBoxContainer;
        if (print != null)
        {
            print.AddChild(new HSeparator());
            print.AddChild(new Label { Text = "GEOMETRY — v0.4", ThemeTypeVariation = "HeaderSmall" });
            print.AddChild(new Label
            {
                Text = "Voxel remesh rebuilds topology from a solid occupancy field. Smaller voxels preserve more detail but use more RAM/CPU.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            print.AddChild(new Label { Text = "Voxel size (mm)" });
            _v04VoxelSize = new HSlider { MinValue = 0.15, MaxValue = 1.5, Step = 0.05, Value = 0.35 };
            print.AddChild(_v04VoxelSize);
            _v04PreserveOriginals = new CheckButton { Text = "Preserve original objects", ButtonPressed = true };
            print.AddChild(_v04PreserveOriginals);

            var selected = new Button { Text = "Voxel Remesh Selected" };
            selected.Pressed += async () => await VoxelRemeshSelected();
            print.AddChild(selected);

            var union = new Button { Text = "Voxel Union All Objects" };
            union.Pressed += async () => await VoxelUnionAll();
            print.AddChild(union);
        }

        var ai = FindChild("AI", true, false) as VBoxContainer;
        if (ai != null)
        {
            ai.AddChild(new HSeparator());
            ai.AddChild(new Label { Text = "GEOMETRY-AWARE AI — v0.4", ThemeTypeVariation = "HeaderSmall" });
            var capture = new Button { Text = "Capture Geometry Context" };
            capture.Pressed += CaptureGeometryContext;
            ai.AddChild(capture);
            ai.AddChild(new Label
            {
                Text = "Saves the viewport plus camera FOV/transform and object transforms. Depth/normal buffers will plug into this same context bundle in a later refinement.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
        }
    }

    async Task VoxelRemeshSelected()
    {
        if (_selected?.Mesh == null)
        {
            SetStatus("Select a mesh first.");
            return;
        }
        await RunVoxelOperation(new List<MeshInstance3D> { _selected }, "Voxel remesh");
    }

    async Task VoxelUnionAll()
    {
        if (_objects.Count == 0)
        {
            SetStatus("There are no mesh objects to union.");
            return;
        }
        await RunVoxelOperation(new List<MeshInstance3D>(_objects), "Voxel union");
    }

    async Task RunVoxelOperation(List<MeshInstance3D> source, string resultName)
    {
        double voxel = _v04VoxelSize?.Value ?? 0.35;
        string job = ProjectSettings.GlobalizePath($"user://geometry/job_{DateTime.Now:yyyyMMdd_HHmmss_fff}");
        Directory.CreateDirectory(job);
        var inputs = new List<string>();

        try
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].Mesh == null) continue;
                string p = Path.Combine(job, $"input_{i:000}.stl");
                MeshIO.SaveBinaryStl(BakeToWorldMesh(source[i]), p);
                inputs.Add(p);
            }
            if (inputs.Count == 0) throw new InvalidOperationException("No valid mesh surfaces were available.");

            string output = Path.Combine(job, "result.stl");
            await RunAi(async () =>
            {
                SetStatus($"{resultName}: voxelizing {inputs.Count} object(s) at {voxel:0.00} mm...");
                string result = await _ai.VoxelRemeshAsync(inputs, output, voxel);
                var mesh = MeshIO.LoadStl(result);
                AddMeshObject(mesh, resultName);

                if (!(_v04PreserveOriginals?.ButtonPressed ?? true))
                {
                    foreach (var obj in source)
                    {
                        if (_objects.Remove(obj)) obj.QueueFree();
                    }
                    RebuildSceneList();
                }
                SetStatus($"{resultName} complete at {voxel:0.00} mm. New mesh added as a separate object.");
            });
        }
        catch (Exception ex)
        {
            SetStatus(resultName + " failed: " + ex.Message);
        }
    }

    void CaptureGeometryContext()
    {
        if (_camera == null)
        {
            SetStatus("Camera is not ready.");
            return;
        }

        CaptureView();
        string folder = ProjectSettings.GlobalizePath("user://geometry_context");
        Directory.CreateDirectory(folder);
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string metadata = Path.Combine(folder, $"context_{stamp}.json");

        var objects = new List<object>();
        foreach (var obj in _objects)
        {
            var t = obj.GlobalTransform;
            objects.Add(new
            {
                name = obj.Name.ToString(),
                selected = obj == _selected,
                position = Vec(t.Origin),
                basis_x = Vec(t.Basis.X),
                basis_y = Vec(t.Basis.Y),
                basis_z = Vec(t.Basis.Z),
                aabb_position = Vec(obj.GetAabb().Position),
                aabb_size = Vec(obj.GetAabb().Size)
            });
        }

        var cam = _camera.GlobalTransform;
        var sub = GetNode<SubViewport>("VBoxContainer/HSplitContainer/HSplitContainer/ViewportHost/Viewport");
        var dto = new
        {
            version = "0.4",
            rgb_path = _lastCapture,
            viewport_width = (int)sub.GetVisibleRect().Size.X,
            viewport_height = (int)sub.GetVisibleRect().Size.Y,
            camera = new
            {
                fov_degrees = _camera.Fov,
                near = _camera.Near,
                far = _camera.Far,
                position = Vec(cam.Origin),
                basis_x = Vec(cam.Basis.X),
                basis_y = Vec(cam.Basis.Y),
                basis_z = Vec(cam.Basis.Z)
            },
            objects
        };
        File.WriteAllText(metadata, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
        SetStatus("Geometry context captured: " + metadata);
    }

    static float[] Vec(Vector3 v) => new[] { v.X, v.Y, v.Z };
}
