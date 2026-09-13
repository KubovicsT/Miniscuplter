namespace Miniscuplter.Core;

/// <summary>
/// Computes the canonical initial transform for generated meshes. The transform remains Core-owned:
/// presentation code consumes it but does not invent an additional viewport-only placement/scale.
/// </summary>
public static class GeneratedObjectPlacement
{
    public const float DefaultTargetLargestDimension = 100f;

    public static TransformState FitAndGround(MeshData mesh, float targetLargestDimension = DefaultTargetLargestDimension)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        mesh.Validate();
        if (!float.IsFinite(targetLargestDimension) || targetLargestDimension <= 0f)
            throw new ArgumentOutOfRangeException(nameof(targetLargestDimension));

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        float maxZ = float.NegativeInfinity;

        for (int i = 0; i < mesh.Positions.Length; i += 3)
        {
            float x = mesh.Positions[i];
            float y = mesh.Positions[i + 1];
            float z = mesh.Positions[i + 2];
            minX = MathF.Min(minX, x);
            minY = MathF.Min(minY, y);
            minZ = MathF.Min(minZ, z);
            maxX = MathF.Max(maxX, x);
            maxY = MathF.Max(maxY, y);
            maxZ = MathF.Max(maxZ, z);
        }

        float sizeX = maxX - minX;
        float sizeY = maxY - minY;
        float sizeZ = maxZ - minZ;
        float largest = MathF.Max(sizeX, MathF.Max(sizeY, sizeZ));
        if (!float.IsFinite(largest) || largest <= 1e-6f)
            throw new InvalidDataException("Generated mesh bounds are degenerate and cannot be placed reliably.");

        float uniformScale = targetLargestDimension / largest;
        float centerX = (minX + maxX) * .5f;
        float centerZ = (minZ + maxZ) * .5f;
        var transform = new TransformState(
            new Vec3(-centerX * uniformScale, -minY * uniformScale, -centerZ * uniformScale),
            Vec3.Zero,
            new Vec3(uniformScale, uniformScale, uniformScale));
        transform.Validate();
        return transform;
    }
}
