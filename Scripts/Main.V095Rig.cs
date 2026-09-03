using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV095RigGuards()
    {
        ReplaceV095Button("Quick Rig", async () => await SafeV095GenerateRigAsync("quick"));
        ReplaceV095Button("Universal AI Rig", async () => await SafeV095GenerateRigAsync("universal"));
    }

    async Task SafeV095GenerateRigAsync(string mode)
    {
        if (_selected?.Mesh is not ArrayMesh) { SetStatus("Select a mesh object to rig."); return; }
        if (V06RoleFor(_selected.Name.ToString()) is "attachment" or "exclude") { SetStatus("This object is marked as attachment/do-not-rig."); return; }
        MeshInstance3D source = _selected;
        string? dir = null;
        try
        {
            dir = ProjectSettings.GlobalizePath($"user://rig/job_{DateTime.Now:yyyyMMdd_HHmmss_fff}");
            Directory.CreateDirectory(dir);
            string input = Path.Combine(dir, "character.stl"), output = Path.Combine(dir, "skeleton.json");
            MeshIO.SaveBinaryStl(BakeToWorldMesh(source), input);
            if (!File.Exists(input) || new FileInfo(input).Length == 0) throw new IOException("Rig input STL could not be written.");

            var sw = Stopwatch.StartNew();
            ResetV055Cancellation();
            if (_v06RigStatus != null) _v06RigStatus.Text = $"{mode} rig running… {EstimateText("rig-" + mode, "standard")}";
            string path = await _ai.PredictRigAsync(input, output, mode);
            ValidateV095RigJson(path);
            if (!GodotObject.IsInstanceValid(source) || !_objects.Contains(source)) { SetStatus("Rig result discarded because the source object no longer exists."); return; }

            sw.Stop();
            RecordJob("rig-" + mode, "standard", sw.Elapsed.TotalSeconds);
            LoadV06RigJson(path, source);
            if (_v06RigStatus != null) _v06RigStatus.Text = $"{_v06CurrentRig?.Provider}: {_v06CurrentRig?.Joints.Count ?? 0} joints · {FormatSeconds(sw.Elapsed.TotalSeconds)}";
            SetStatus($"{mode} rig validated and attached to {source.Name}.");
        }
        catch (Exception ex)
        {
            if (_v06RigStatus != null) _v06RigStatus.Text = ex.Message;
            SetStatus($"{mode} rig rejected without changing the current rig: {ex.Message}");
        }
        finally
        {
            if (dir != null) { try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { } }
        }
    }

    static void ValidateV095RigJson(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0) throw new InvalidDataException("Rig provider produced no JSON output.");
        var parsed = JsonSerializer.Deserialize<RigJson>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Rig provider returned invalid JSON.");
        if (parsed.Joints == null || parsed.Joints.Count == 0) throw new InvalidDataException("Rig provider returned no joints.");
        if (parsed.Joints.Count > 512) throw new InvalidDataException($"Rig provider returned an unreasonable joint count ({parsed.Joints.Count}).");

        for (int i = 0; i < parsed.Joints.Count; i++)
        {
            var j = parsed.Joints[i];
            if (j.Position == null || j.Position.Length < 3) throw new InvalidDataException($"Joint {i} has no valid 3D position.");
            if (!float.IsFinite(j.Position[0]) || !float.IsFinite(j.Position[1]) || !float.IsFinite(j.Position[2])) throw new InvalidDataException($"Joint {i} contains non-finite coordinates.");
            if (j.Parent < -1 || j.Parent >= parsed.Joints.Count || j.Parent == i) throw new InvalidDataException($"Joint {i} has invalid parent index {j.Parent}.");
        }

        for (int i = 0; i < parsed.Joints.Count; i++)
        {
            var seen = new HashSet<int>();
            int p = i;
            while (p >= 0)
            {
                if (!seen.Add(p)) throw new InvalidDataException($"Rig contains a parent cycle involving joint {i}.");
                p = parsed.Joints[p].Parent;
            }
        }
    }
}
