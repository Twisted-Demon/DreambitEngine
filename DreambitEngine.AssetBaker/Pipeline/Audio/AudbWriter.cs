using System.Buffers.Binary;

namespace DreambitEngine.AssetBaker.pipeline.Audio;

public static class AudbWriter
{
    public static void Write(Stream s, ReadOnlySpan<byte> data, ushort subType, ushort channels, uint sampleRate,
        uint flags = 0, ushort version = 1)
    {
        Span<byte> b = stackalloc byte[16];
        s.Write("AUDB"u8);
        BinaryPrimitives.WriteUInt16LittleEndian(b[..2], version);  s.Write(b[..2]);
        BinaryPrimitives.WriteUInt16LittleEndian(b[..2], subType);  s.Write(b[..2]);
        BinaryPrimitives.WriteUInt16LittleEndian(b[..2], channels); s.Write(b[..2]);
        BinaryPrimitives.WriteUInt32LittleEndian(b[..4], sampleRate); s.Write(b[..4]);
        BinaryPrimitives.WriteUInt32LittleEndian(b[..4], flags);    s.Write(b[..4]);
        BinaryPrimitives.WriteUInt32LittleEndian(b[..4], (uint)data.Length); s.Write(b[..4]);
        s.Write(data);
    }

    public static void Write(string path, ReadOnlySpan<byte> data, ushort subType,
        ushort channels, uint sampleRate, uint flags = 0, ushort version = 1)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var fs = File.Create(path);
        Write(fs, data, subType, channels, sampleRate, flags, version);
    }
}