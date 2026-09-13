using Miniscuplter.Core;

internal static class GeneratedObjectPlacementTests
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }

    public static void Run()
    {
        var mesh = new MeshData(
            new float[]
            {
                -2f, -3f, -4f,
                 8f, -3f, -4f,
                -2f,  7f, -4f,
                -2f, -3f, 16f
            },
            new int[] { 0, 2, 1, 0, 1, 3, 0, 3, 2, 1, 2, 3 });

        TransformState transform = GeneratedObjectPlacement.FitAndGround(mesh);
        Assert(MathF.Abs(transform.Scale.X - 5f) < 0.0001f &&
               transform.Scale.X == transform.Scale.Y && transform.Scale.Y == transform.Scale.Z,
            "generated placement did not use one uniform canonical scale");
        Assert(MathF.Abs(transform.Position.Y - 15f) < 0.0001f,
            "generated placement did not move the mesh minimum Y to the grid plane");
        Assert(MathF.Abs(transform.Position.X + 15f) < 0.0001f && MathF.Abs(transform.Position.Z + 30f) < 0.0001f,
            "generated placement did not center X/Z bounds around the workspace origin");

        float groundedMinY = -3f * transform.Scale.Y + transform.Position.Y;
        float largestDimension = 20f * transform.Scale.X;
        Assert(MathF.Abs(groundedMinY) < 0.0001f, "generated mesh does not rest on Y=0 after canonical placement");
        Assert(MathF.Abs(largestDimension - GeneratedObjectPlacement.DefaultTargetLargestDimension) < 0.0001f,
            "generated mesh largest dimension does not match the canonical target size");

        bool degenerateRejected = false;
        try
        {
            _ = GeneratedObjectPlacement.FitAndGround(new MeshData(
                new float[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                new int[] { 0, 1, 2 }));
        }
        catch (InvalidDataException) { degenerateRejected = true; }
        Assert(degenerateRejected, "degenerate generated mesh bounds were not rejected");
    }
}
