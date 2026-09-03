using Godot;
using System;

namespace Miniscuplter;

public enum SculptBrush { Draw, Smooth, Inflate, Grab, Crease, Flatten }

public static class SculptEngine
{
    public static ArrayMesh Apply(ArrayMesh mesh, Vector3 hitLocal, Vector3 dragLocal, float radius, float strength, SculptBrush brush)
    {
        if (mesh.GetSurfaceCount() == 0) return mesh;
        var mdt = new MeshDataTool();
        if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return mesh;
        var original = new Vector3[mdt.GetVertexCount()];
        for (int i = 0; i < original.Length; i++) original[i] = mdt.GetVertex(i);

        Vector3 avgNormal = Vector3.Zero;
        int affected = 0;
        for (int i = 0; i < original.Length; i++)
        {
            float d = original[i].DistanceTo(hitLocal);
            if (d > radius) continue;
            float w = SmoothFalloff(d / Math.Max(radius, 0.0001f));
            avgNormal += mdt.GetVertexNormal(i) * w;
            affected++;
        }
        if (affected == 0) return mesh;
        avgNormal = avgNormal.Normalized();

        for (int i = 0; i < original.Length; i++)
        {
            var p = original[i];
            float d = p.DistanceTo(hitLocal);
            if (d > radius) continue;
            float w = SmoothFalloff(d / Math.Max(radius, 0.0001f));
            var n = mdt.GetVertexNormal(i).Normalized();
            Vector3 next = p;
            switch (brush)
            {
                case SculptBrush.Draw: next = p + n * strength * w; break;
                case SculptBrush.Inflate: next = p + n * strength * 1.35f * w; break;
                case SculptBrush.Grab: next = p + dragLocal * w; break;
                case SculptBrush.Flatten:
                    float planeDistance = avgNormal.Dot(p - hitLocal);
                    next = p - avgNormal * planeDistance * Math.Clamp(strength * w, 0f, 1f);
                    break;
                case SculptBrush.Crease:
                    var toward = (hitLocal - p).Normalized();
                    next = p + toward * strength * 0.35f * w - n * strength * 0.45f * w;
                    break;
                case SculptBrush.Smooth:
                    var edges = mdt.GetVertexEdges(i);
                    if (edges.Length > 0)
                    {
                        Vector3 avg = Vector3.Zero; int count = 0;
                        foreach (int edgeId in edges)
                        {
                            int a = mdt.GetEdgeVertex(edgeId, 0), b = mdt.GetEdgeVertex(edgeId, 1);
                            int other = a == i ? b : a;
                            avg += original[other]; count++;
                        }
                        if (count > 0) next = p.Lerp(avg / count, Math.Clamp(strength * w, 0f, 1f));
                    }
                    break;
            }
            mdt.SetVertex(i, next);
        }

        var output = new ArrayMesh();
        mdt.CommitToSurface(output);
        output.SurfaceSetMaterial(0, mesh.SurfaceGetMaterial(0));
        return output;
    }

    static float SmoothFalloff(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float x = 1f - t;
        return x * x * (3f - 2f * x);
    }
}
