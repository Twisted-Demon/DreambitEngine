using System.Buffers.Binary;

namespace DreambitEngine.AssetBaker.Pipeline.Docs;

public static class JsnbWriter
{
    public static void Write(Stream s, ReadOnlySpan<byte> utf8, uint flags, ushort version = 1)
    {
        Span<byte> b = stackalloc byte[8];
        
        s.Write("JSNB"u8);
        
        BinaryPrimitives.WriteUInt16LittleEndian(b[..2], version); s.Write(b[..2]);
        BinaryPrimitives.WriteUInt32LittleEndian(b[..4], flags); s.Write(b[..4]);
        BinaryPrimitives.WriteUInt32LittleEndian(b[..4], (uint)utf8.Length); s.Write(b[..4]);
        s.Write(utf8);
    }

    public static void Write(string path, ReadOnlySpan<byte> utf8, uint flags, ushort version = 1)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var s = File.Create(path);
        Write(s, utf8, flags, version);
    }
}