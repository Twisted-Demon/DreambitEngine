using System.Buffers.Binary;
using DreambitEngine.AssetBaker.Abstractions;

namespace DreambitEngine.AssetBaker.Pipeline.Textures;

public sealed class PakWriter
{
    private sealed record Entry(string Path, AssetType Type, byte[] Data, uint Crc);

    private readonly Dictionary<string, Entry> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public int Count => _entries.Count;

    public void Add(AssetBlob blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        var path = Normalize(blob.LogicalPath);
        if (path.Length == 0)
            throw new InvalidOperationException("A PAK entry path cannot be empty.");
        if (!_entries.TryAdd(path, new Entry(path, blob.Type, blob.Data, Crc32(blob.Data))))
            throw new InvalidOperationException(
                $"Two source assets produce the same case-insensitive PAK path '{path}'.");
    }

    public void Save(string outputPak)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPak);
        if (_entries.Count > ushort.MaxValue)
            throw new InvalidOperationException(
                $"PAK contains {_entries.Count} entries; the format supports at most {ushort.MaxValue}.");

        var outputPath = Path.GetFullPath(outputPak);
        var directory = Path.GetDirectoryName(outputPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                WritePak(stream);
                stream.Flush(true);
            }

            File.Move(temporaryPath, outputPath, true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private void WritePak(Stream stream)
    {
        var entries = _entries.Values
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToArray();

        stream.Write("PAK0"u8);
        Span<byte> buffer = stackalloc byte[16];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], 1);
        stream.Write(buffer[..2]);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], (ushort)entries.Length);
        stream.Write(buffer[..2]);
        var tocOffsetPosition = stream.Position;
        stream.Position += 8;
        var dataOffsetPosition = stream.Position;
        stream.Position += 8;
        var tocOffset = stream.Position;

        long tocSize = 0;
        foreach (var entry in entries)
        {
            var pathLength = System.Text.Encoding.UTF8.GetByteCount(entry.Path);
            if (pathLength > ushort.MaxValue)
                throw new InvalidOperationException($"PAK path '{entry.Path}' is too long.");
            tocSize += 2 + pathLength + 2 + 8 + 8 + 4;
        }

        var dataOffset = tocOffset + tocSize;
        var currentDataOffset = dataOffset;
        foreach (var entry in entries)
        {
            var pathBytes = System.Text.Encoding.UTF8.GetBytes(entry.Path);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], (ushort)pathBytes.Length);
            stream.Write(buffer[..2]);
            stream.Write(pathBytes);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], (ushort)entry.Type);
            stream.Write(buffer[..2]);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer[..8], (ulong)currentDataOffset);
            stream.Write(buffer[..8]);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer[..8], (ulong)entry.Data.LongLength);
            stream.Write(buffer[..8]);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[..4], entry.Crc);
            stream.Write(buffer[..4]);
            currentDataOffset += entry.Data.LongLength;
        }

        stream.Position = tocOffsetPosition;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[..8], (ulong)tocOffset);
        stream.Write(buffer[..8]);
        stream.Position = dataOffsetPosition;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[..8], (ulong)dataOffset);
        stream.Write(buffer[..8]);
        stream.Position = dataOffset;
        foreach (var entry in entries)
            stream.Write(entry.Data);
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').Trim().TrimStart('.', '/').ToLowerInvariant();

    internal static uint Crc32(ReadOnlySpan<byte> data)
    {
        const uint polynomial = 0xEDB88320u;
        Span<uint> table = stackalloc uint[256];
        for (uint index = 0; index < 256; index++)
        {
            var value = index;
            for (var bit = 0; bit < 8; bit++)
                value = (value & 1) != 0 ? polynomial ^ (value >> 1) : value >> 1;
            table[(int)index] = value;
        }

        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
            crc = table[(int)((crc ^ value) & 0xFF)] ^ (crc >> 8);
        return ~crc;
    }
}
