using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Miniscuplter;

public static class MeshIO
{
    public static ArrayMesh LoadStl(string path)
    {
        using var fs = File.OpenRead(path);
        if (fs.Length < 84) throw new InvalidDataException("STL file is too small.");
        var header = new byte[80]; fs.ReadExactly(header);
        var countBytes = new byte[4]; fs.ReadExactly(countBytes);
        uint count = BitConverter.ToUInt32(countBytes, 0);
        long expected = 84L + count * 50L;
        fs.Position = 0;
        return expected == fs.Length ? LoadBinaryStl(fs) : LoadAsciiStl(path);
    }

    static ArrayMesh LoadBinaryStl(Stream stream)
    {
        using var br = new BinaryReader(stream, Encoding.ASCII, true);
        br.ReadBytes(80);
        uint triCount = br.ReadUInt32();
        var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
        for (uint i = 0; i < triCount; i++)
        {
            var n = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            for (int v = 0; v < 3; v++)
            {
                var p = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                st.SetNormal(n); st.AddVertex(p);
            }
            br.ReadUInt16();
        }
        st.Index();
        return st.Commit();
    }

    static ArrayMesh LoadAsciiStl(string path)
    {
        var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 normal = Vector3.Up;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.StartsWith("facet normal ")) normal = ParseVec(line[13..]);
            else if (line.StartsWith("vertex ")) { st.SetNormal(normal); st.AddVertex(ParseVec(line[7..])); }
        }
        st.Index();
        return st.Commit();
    }

    static Vector3 ParseVec(string s)
    {
        var p = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new Vector3(float.Parse(p[0], CultureInfo.InvariantCulture), float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture));
    }

    public static void SaveBinaryStl(Mesh mesh, string path)
    {
        using var bw = new BinaryWriter(File.Create(path));
        var header = new byte[80]; Encoding.ASCII.GetBytes("Miniscuplter STL").CopyTo(header, 0); bw.Write(header);
        var tris = new List<(Vector3 a, Vector3 b, Vector3 c)>();
        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
        {
            var arrays = mesh.SurfaceGetArrays(s);
            var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            var idx = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            if (idx.Length > 0) for (int i = 0; i + 2 < idx.Length; i += 3) tris.Add((verts[idx[i]], verts[idx[i+1]], verts[idx[i+2]]));
            else for (int i = 0; i + 2 < verts.Length; i += 3) tris.Add((verts[i], verts[i+1], verts[i+2]));
        }
        bw.Write((uint)tris.Count);
        foreach (var t in tris)
        {
            var n = (t.b - t.a).Cross(t.c - t.a).Normalized(); WriteVec(bw, n); WriteVec(bw, t.a); WriteVec(bw, t.b); WriteVec(bw, t.c); bw.Write((ushort)0);
        }
    }

    static void WriteVec(BinaryWriter bw, Vector3 v) { bw.Write(v.X); bw.Write(v.Y); bw.Write(v.Z); }
}
