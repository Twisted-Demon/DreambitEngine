using System.Buffers.Binary;

namespace DreambitEngine.AssetBaker.Pipeline.Textures;

public static class TexbWriter
{
    public static void Write(string path, List<(int w,int h,byte[] data)> mips, ushort version, uint flags)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var fs = File.Create(path);
        Write(fs, mips, version, flags);
    }

    public static void Write(Stream fs, List<(int w,int h,byte[] data)> mips, ushort version, uint flags)
    {
        Span<byte> buf = stackalloc byte[8];

        fs.Write("TEXB"u8);
        BinaryPrimitives.WriteUInt16LittleEndian(buf[..2], version); fs.Write(buf[..2]);
        BinaryPrimitives.WriteUInt16LittleEndian(buf[..2], (ushort)mips[0].w); fs.Write(buf[..2]);
        BinaryPrimitives.WriteUInt16LittleEndian(buf[..2], (ushort)mips[0].h); fs.Write(buf[..2]);
        BinaryPrimitives.WriteUInt16LittleEndian(buf[..2], (ushort)mips.Count); fs.Write(buf[..2]);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[..4], flags); fs.Write(buf[..4]);

        foreach (var (_, _, data) in mips)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buf[..4], (uint)data.Length);
            fs.Write(buf[..4]);
            fs.Write(data, 0, data.Length);
        }
    }
}