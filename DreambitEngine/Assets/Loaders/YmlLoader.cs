using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Dreambit;

public static class YmlLoader
{

    public static string GetYamlString(Stream s)
    {
        Span<byte> h = stackalloc byte[8];
        
        s.ReadExactly(h[..4]);
        if (h[0] != (byte)'Y' || h[1] != (byte)'M' || h[2] != (byte)'L' || h[3] != (byte)'B')
            throw new InvalidDataException("Not YMLB");
        
        s.ReadExactly(h[..2]); var ver = BinaryPrimitives.ReadUInt16LittleEndian(h[..2]);
        if (ver != 1) throw new NotSupportedException($"YMLB v{ver}");
        
        s.ReadExactly(h[..4]); /* flags = */ _ = BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);
        s.ReadExactly(h[..4]); var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);

        var bytes = GC.AllocateUninitializedArray<byte>(size);
        s.ReadExactly(bytes);

        return Encoding.UTF8.GetString(bytes);
    }
}