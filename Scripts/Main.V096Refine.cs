using Godot;
using System;
using System.Collections.Generic;

namespace Miniscuplter;

public partial class Main
{
    void RefineV096Selection(string operation, int iterations = 1)
    {
        V096ValidateSelection();
        if (_v096SelectionObject?.Mesh is not ArrayMesh mesh || _v096Selection == null)
        {
            SetStatus("Create a Smart Selection first."); return;
        }
        iterations = Math.Clamp(iterations, 1, 10);
        var mdt = new MeshDataTool();
        if (mdt.CreateFromSurface(mesh, 0) != Error.Ok || mdt.GetVertexCount() != _v096Selection.Length)
        {
            SetStatus("Selection refinement could not read the current mesh topology."); return;
        }
        var neighbors = new HashSet<int>[mdt.GetVertexCount()];
        for (int i = 0; i < neighbors.Length; i++) neighbors[i] = new HashSet<int>();
        for (int f = 0; f < mdt.GetFaceCount(); f++)
        {
            int a = mdt.GetFaceVertex(f, 0), b = mdt.GetFaceVertex(f, 1), c = mdt.GetFaceVertex(f, 2);
            neighbors[a].Add(b); neighbors[a].Add(c);
            neighbors[b].Add(a); neighbors[b].Add(c);
            neighbors[c].Add(a); neighbors[c].Add(b);
        }

        float[] current = (float[])_v096Selection.Clone();
        for (int pass = 0; pass < iterations; pass++)
        {
            var next = new float[current.Length];
            for (int i = 0; i < current.Length; i++)
            {
                if (neighbors[i].Count == 0) { next[i] = current[i]; continue; }
                if (operation == "grow")
                {
                    float v = current[i]; foreach (int n in neighbors[i]) v = Math.Max(v, current[n]); next[i] = v;
                }
                else if (operation == "shrink")
                {
                    float v = current[i]; foreach (int n in neighbors[i]) v = Math.Min(v, current[n]); next[i] = v;
                }
                else
                {
                    float sum = 0f; foreach (int n in neighbors[i]) sum += current[n];
                    next[i] = Math.Clamp(current[i] * .5f + (sum / neighbors[i].Count) * .5f, 0f, 1f);
                }
            }
            current = next;
        }
        _v096Selection = current;
        RebuildV096SelectionOverlay(); ApplyV096SelectionToSculptMask();
        SetStatus($"Smart Selection {operation} applied ({iterations} pass{(iterations == 1 ? "" : "es")}).");
    }
}
