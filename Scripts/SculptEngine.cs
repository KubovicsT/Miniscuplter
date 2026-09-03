using Godot;
using System;
using System.Collections.Generic;

namespace Miniscuplter;

public enum SculptBrush { Draw, Smooth, Inflate, Grab, Crease, Flatten, Pinch, Scrape, Clay, SnakeHook }
public enum SculptFalloff { Smooth, Linear, Sharp, Dome }
public enum SculptAlpha { None, HardCenter, Pinpoint, Ring, Noise }

public static class SculptEngine
{
    public static ArrayMesh Apply(ArrayMesh mesh, Vector3 hitLocal, Vector3 dragLocal, float radius, float strength, SculptBrush brush)
        => ApplyAdvanced(mesh, new[] { hitLocal }, dragLocal, radius, strength, brush, SculptFalloff.Smooth, SculptAlpha.None, null);

    public static ArrayMesh ApplyAdvanced(ArrayMesh mesh, IReadOnlyList<Vector3> centers, Vector3 dragLocal, float radius, float strength,
        SculptBrush brush, SculptFalloff falloff, SculptAlpha alpha, float[]? mask)
    {
        if (mesh.GetSurfaceCount() == 0 || centers.Count == 0) return mesh;
        var mdt = new MeshDataTool();
        if (mdt.CreateFromSurface(mesh, 0) != Error.Ok) return mesh;
        int vertexCount = mdt.GetVertexCount();
        var original = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++) original[i] = mdt.GetVertex(i);

        Vector3 avgNormal = Vector3.Zero;
        float normalWeight = 0f;
        for (int i = 0; i < vertexCount; i++)
        {
            float d = DistanceToClosest(original[i], centers);
            if (d > radius) continue;
            float w = BrushWeight(d / Math.Max(radius, .0001f), falloff, alpha, original[i]);
            float protect = mask != null && i < mask.Length ? Math.Clamp(mask[i], 0f, 1f) : 0f;
            w *= 1f - protect;
            avgNormal += mdt.GetVertexNormal(i) * w;
            normalWeight += w;
        }
        if (normalWeight <= .000001f) return mesh;
        avgNormal = avgNormal.Normalized();

        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 p = original[i];
            Vector3 center = ClosestCenter(p, centers);
            float d = p.DistanceTo(center);
            if (d > radius) continue;
            float w = BrushWeight(d / Math.Max(radius, .0001f), falloff, alpha, p);
            float protect = mask != null && i < mask.Length ? Math.Clamp(mask[i], 0f, 1f) : 0f;
            w *= 1f - protect;
            if (w <= .000001f) continue;

            Vector3 n = mdt.GetVertexNormal(i).Normalized();
            Vector3 next = p;
            switch (brush)
            {
                case SculptBrush.Draw:
                    next = p + n * strength * w;
                    break;
                case SculptBrush.Inflate:
                    next = p + n * strength * 1.35f * w;
                    break;
                case SculptBrush.Grab:
                    next = p + dragLocal * w;
                    break;
                case SculptBrush.SnakeHook:
                    next = p + dragLocal * w * (1.25f + (1f - d / Math.Max(radius, .0001f)));
                    break;
                case SculptBrush.Flatten:
                {
                    float planeDistance = avgNormal.Dot(p - center);
                    next = p - avgNormal * planeDistance * Math.Clamp(strength * w, 0f, 1f);
                    break;
                }
                case SculptBrush.Scrape:
                {
                    float planeDistance = avgNormal.Dot(p - center);
                    if (planeDistance > 0f) next = p - avgNormal * planeDistance * Math.Clamp(strength * 1.4f * w, 0f, 1f);
                    break;
                }
                case SculptBrush.Clay:
                {
                    float planeDistance = avgNormal.Dot(p - center);
                    float slab = strength * .65f;
                    float target = slab - planeDistance;
                    next = p + avgNormal * target * Math.Clamp(w, 0f, 1f);
                    break;
                }
                case SculptBrush.Crease:
                {
                    Vector3 toward = (center - p).Normalized();
                    next = p + toward * strength * .35f * w - n * strength * .45f * w;
                    break;
                }
                case SculptBrush.Pinch:
                {
                    Vector3 toCenter = center - p;
                    Vector3 tangent = toCenter - n * toCenter.Dot(n);
                    next = p + tangent * Math.Clamp(strength * .18f * w, 0f, .9f);
                    break;
                }
                case SculptBrush.Smooth:
                {
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
            }
            mdt.SetVertex(i, next);
        }

        var output = new ArrayMesh();
        mdt.CommitToSurface(output);
        output.SurfaceSetMaterial(0, mesh.SurfaceGetMaterial(0));
        return output;
    }

    static float DistanceToClosest(Vector3 p, IReadOnlyList<Vector3> centers)
    {
        float best = float.PositiveInfinity;
        for (int i = 0; i < centers.Count; i++) best = Math.Min(best, p.DistanceTo(centers[i]));
        return best;
    }

    static Vector3 ClosestCenter(Vector3 p, IReadOnlyList<Vector3> centers)
    {
        int best = 0; float d = float.PositiveInfinity;
        for (int i = 0; i < centers.Count; i++) { float x = p.DistanceSquaredTo(centers[i]); if (x < d) { d = x; best = i; } }
        return centers[best];
    }

    public static float BrushWeight(float t, SculptFalloff falloff, SculptAlpha alpha, Vector3 position)
    {
        t = Math.Clamp(t, 0f, 1f);
        float baseWeight = falloff switch
        {
            SculptFalloff.Linear => 1f - t,
            SculptFalloff.Sharp => (1f - t) * (1f - t) * (1f - t),
            SculptFalloff.Dome => Mathf.Cos(t * Mathf.Pi * .5f),
            _ => SmoothFalloff(t)
        };
        float alphaWeight = alpha switch
        {
            SculptAlpha.HardCenter => t < .72f ? 1f : Math.Clamp((1f - t) / .28f, 0f, 1f),
            SculptAlpha.Pinpoint => MathF.Exp(-t * t * 7f),
            SculptAlpha.Ring => Math.Clamp(1f - Math.Abs(t - .58f) * 5.5f, 0f, 1f),
            SculptAlpha.Noise => .45f + .55f * HashNoise(position),
            _ => 1f
        };
        return baseWeight * alphaWeight;
    }

    static float HashNoise(Vector3 p)
    {
        double v = Math.Sin(p.X * 12.9898 + p.Y * 78.233 + p.Z * 37.719) * 43758.5453;
        return (float)(v - Math.Floor(v));
    }

    static float SmoothFalloff(float t)
    {
        float x = 1f - Math.Clamp(t, 0f, 1f);
        return x * x * (3f - 2f * x);
    }
}
