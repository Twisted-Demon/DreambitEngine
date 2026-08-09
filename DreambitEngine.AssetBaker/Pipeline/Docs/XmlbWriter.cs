using System.Buffers.Binary;

namespace DreambitEngine.AssetBaker.Pipeline.Docs;

public static class XmlbWriter
{
    public static void Write(
        Stream stream,
        ReadOnlySpan<byte> utf8,
        uint flags,
        ushort version = 1)
    {
        Span<byte> buffer = stackalloc byte[8];

        stream.Write("XMLB"u8);

        BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], version);
        stream.Write(buffer[..2]);

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[..4], flags);
        stream.Write(buffer[..4]);

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[..4], (uint)utf8.Length);
        stream.Write(buffer[..4]);

        stream.Write(utf8);
    }

    public static void Write(
        string path,
        ReadOnlySpan<byte> utf8,
        uint flags,
        ushort version = 1)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var stream = File.Create(path);
        Write(stream, utf8, flags, version);
    }
}
