using System.Buffers.Binary;
using DreambitEngine.AssetBaker.Abstractions;

namespace DreambitEngine.AssetBaker.Pipeline.Textures;

public sealed class PakWriter
{
    private sealed record Entry(string Path, AssetType Type, byte[] Data, uint Crc);

    private readonly List<Entry> _entries = [];

    public void Add(AssetBlob blob)
    {
        var path = Normalize(blob.LogicalPath);
        uint crc = Crc32(blob.Data);
        _entries.Add(new Entry(path, blob.Type, blob.Data, crc));
    }

    public void Save(string outputPak)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPak))!);
        using var fs = File.Create(outputPak);

        // Header placeholders
        fs.Write("PAK0"u8);                 // Magic
        Span<byte> s = stackalloc byte[16];
        BinaryPrimitives.WriteUInt16LittleEndian(s[..2], 1); fs.Write(s[..2]);   // Version
        BinaryPrimitives.WriteUInt16LittleEndian(s[..2], (ushort)_entries.Count); fs.Write(s[..2]); // EntryCount
        long tocOffsetPos = fs.Position; fs.Position += 8; // TocOffset
        long dataOffsetPos = fs.Position; fs.Position += 8; // DataOffset

        // Compute TOC in memory (need sizes to compute offsets)
        // Layout: we'll place TOC right after header; data after TOC.
        long tocOffset = fs.Position;

        // First, build TOC entries with offsets computed after knowing header+TOC sizes
        // We need to compute size of TOC to know data start.
        long tocSize = 0;
        foreach (var e in _entries)
        {
            int pathLen = System.Text.Encoding.UTF8.GetByteCount(e.Path);
            tocSize += 2 + pathLen + 2 + 8 + 8 + 4; // pathLen + path + type + offset + size + crc
        }
        long dataOffset = tocOffset + tocSize;

        // Write TOC with known offsets
        long currentDataOffset = dataOffset;
        foreach (var e in _entries)
        {
            var pathBytes = System.Text.Encoding.UTF8.GetBytes(e.Path);
            BinaryPrimitives.WriteUInt16LittleEndian(s[..2], (ushort)pathBytes.Length); fs.Write(s[..2]);
            fs.Write(pathBytes);
            BinaryPrimitives.WriteUInt16LittleEndian(s[..2], (ushort)e.Type); fs.Write(s[..2]);
            BinaryPrimitives.WriteUInt64LittleEndian(s[..8], (ulong)currentDataOffset); fs.Write(s[..8]);
            BinaryPrimitives.WriteUInt64LittleEndian(s[..8], (ulong)e.Data.LongLength); fs.Write(s[..8]);
            BinaryPrimitives.WriteUInt32LittleEndian(s[..4], e.Crc); fs.Write(s[..4]);

            currentDataOffset += e.Data.LongLength;
        }

        // Backpatch offsets
        fs.Position = tocOffsetPos; BinaryPrimitives.WriteUInt64LittleEndian(s[..8], (ulong)tocOffset); fs.Write(s[..8]);
        fs.Position = dataOffsetPos; BinaryPrimitives.WriteUInt64LittleEndian(s[..8], (ulong)dataOffset); fs.Write(s[..8]);

        // Write data region
        fs.Position = dataOffset;
        foreach (var e in _entries)
            fs.Write(e.Data);

        fs.Flush();
    }

    private static string Normalize(string p)
        => p.Replace('\\', '/').Trim().TrimStart('.', '/').ToLowerInvariant();

    // Tiny CRC32 (IEEE) for quick integrity
    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        // .NET doesn't ship CRC32; quick table-based implementation:
        const uint poly = 0xEDB88320u;
        Span<uint> table = stackalloc uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
                c = ((c & 1) != 0) ? (poly ^ (c >> 1)) : (c >> 1);
            table[(int)i] = c;
        }
        uint crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            int idx = (int)((crc ^ b) & 0xFF);      // <-- key fix
            crc = table[idx] ^ (crc >> 8);
        }
        return ~crc;
    }
}