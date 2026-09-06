namespace Miniscuplter.Core;

public readonly record struct Vec3(float X, float Y, float Z)
{
    public static Vec3 Zero => new(0, 0, 0);
    public static Vec3 One => new(1, 1, 1);
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);
}

public readonly record struct TransformState(Vec3 Position, Vec3 RotationEuler, Vec3 Scale)
{
    public static TransformState Identity => new(Vec3.Zero, Vec3.Zero, Vec3.One);

    public void Validate()
    {
        if (!Position.IsFinite || !RotationEuler.IsFinite || !Scale.IsFinite)
            throw new InvalidDataException("Transform contains non-finite values.");
        if (Scale.X == 0 || Scale.Y == 0 || Scale.Z == 0)
            throw new InvalidDataException("Transform scale cannot contain zero.");
    }
}

public sealed class MeshData
{
    public float[] Positions { get; }
    public int[] Indices { get; }

    public int VertexCount => Positions.Length / 3;
    public int TriangleCount => Indices.Length / 3;

    public MeshData(IEnumerable<float> positions, IEnumerable<int> indices)
    {
        Positions = positions.ToArray();
        Indices = indices.ToArray();
        Validate();
    }

    public void Validate()
    {
        if (Positions.Length == 0 || Positions.Length % 3 != 0)
            throw new InvalidDataException("Mesh position buffer must contain XYZ triples.");
        if (Indices.Length == 0 || Indices.Length % 3 != 0)
            throw new InvalidDataException("Mesh index buffer must contain triangle triples.");
        for (int i = 0; i < Positions.Length; i++)
            if (!float.IsFinite(Positions[i])) throw new InvalidDataException($"Mesh position {i} is not finite.");
        int vertexCount = VertexCount;
        for (int i = 0; i < Indices.Length; i++)
            if (Indices[i] < 0 || Indices[i] >= vertexCount)
                throw new InvalidDataException($"Mesh index {i} references vertex {Indices[i]}, outside 0..{vertexCount - 1}.");
    }
}
