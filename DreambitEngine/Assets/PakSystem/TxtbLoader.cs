using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Dreambit;

public static class TxtbLoader
{
    public static string GetText(Stream s)
    {
        Span<byte> h = stackalloc byte[8];

        s.ReadExactly(h[..4]);
        if (h[0] != (byte)'T' || h[1] != (byte)'X' || h[2] != (byte)'T' || h[3] != (byte)'B')
            throw new InvalidDataException("Not TXTB");

        s.ReadExactly(h[..2]);
        var ver = BinaryPrimitives.ReadUInt16LittleEndian(h[..2]);
        if(ver != 1) throw new NotSupportedException($"TXTB v{ver}");

        s.ReadExactly(h[..4]); /* flags = */
        _ = BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);
        s.ReadExactly(h[..4]);
        var payloadSize = BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);
        if (payloadSize > int.MaxValue)
            throw new InvalidDataException("TXTB payload is too large.");

        var bytes = GC.AllocateUninitializedArray<byte>((int)payloadSize);
        s.ReadExactly(bytes);

        return Encoding.UTF8.GetString(bytes);
    }
}
