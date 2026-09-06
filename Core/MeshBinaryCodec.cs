using System.Security.Cryptography;
using System.Text;

namespace Miniscuplter.Core;

public static class MeshBinaryCodec
{
    static readonly byte[] Magic = Encoding.ASCII.GetBytes("MSHM");
    public const int FormatVersion = 1;

    public static async Task<string> WriteAtomicAsync(string path, MeshData mesh, CancellationToken cancellationToken = default)
    {
        mesh.Validate();
        string full = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(full) ?? throw new InvalidOperationException("Mesh asset path has no parent directory.");
        Directory.CreateDirectory(directory);
        string temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1024 * 128, useAsync: true))
            {
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
                writer.Write(Magic);
                writer.Write(FormatVersion);
                writer.Write(mesh.VertexCount);
                writer.Write(mesh.Indices.Length);
                foreach (float value in mesh.Positions) writer.Write(value);
                foreach (int index in mesh.Indices) writer.Write(index);
                writer.Flush();
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            _ = Read(temp);
            string digest = await ComputeSha256Async(temp, cancellationToken);
            File.Move(temp, full, overwrite: true);
            return digest;
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            throw;
        }
    }

    public static MeshData Read(string path)
    {
        string full = Path.GetFullPath(path);
        using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        byte[] magic = reader.ReadBytes(Magic.Length);
        if (!magic.SequenceEqual(Magic)) throw new InvalidDataException("Mesh asset has an invalid Miniscuplter mesh header.");
        int version = reader.ReadInt32();
        if (version != FormatVersion) throw new InvalidDataException($"Unsupported internal mesh format {version}.");
        int vertexCount = reader.ReadInt32();
        int indexCount = reader.ReadInt32();
        if (vertexCount <= 0 || vertexCount > 100_000_000) throw new InvalidDataException("Mesh asset vertex count is invalid.");
        if (indexCount <= 0 || indexCount % 3 != 0 || indexCount > 300_000_000) throw new InvalidDataException("Mesh asset index count is invalid.");
        long expected = 16L + vertexCount * 3L * sizeof(float) + indexCount * (long)sizeof(int);
        if (stream.Length != expected) throw new InvalidDataException($"Mesh asset byte length {stream.Length:N0} does not match declared buffers ({expected:N0}).");
        var positions = new float[checked(vertexCount * 3)];
        var indices = new int[indexCount];
        for (int i = 0; i < positions.Length; i++) positions[i] = reader.ReadSingle();
        for (int i = 0; i < indices.Length; i++) indices[i] = reader.ReadInt32();
        return new MeshData(positions, indices);
    }

    public static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken = default)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, useAsync: true);
        byte[] buffer = new byte[1024 * 1024];
        while (true)
        {
            int count = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (count <= 0) break;
            hash.AppendData(buffer, 0, count);
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
}
