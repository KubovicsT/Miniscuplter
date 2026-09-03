using Godot;

namespace Miniscuplter;

public static class PrimitiveMeshExtensions
{
    public static Mesh.PrimitiveType SurfaceGetPrimitiveType(this PrimitiveMesh mesh, int surface)
        => Mesh.PrimitiveType.Triangles;
}
