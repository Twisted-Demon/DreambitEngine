using System;
using System.Buffers.Binary;
using System.IO;

namespace Dreambit;

internal static class TtfbLoader
{
    public static byte[] ReadFontData(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        stream.ReadExactly(header);
        if (!header[..4].SequenceEqual("TTFB"u8))
            throw new InvalidDataException("Not a TTFB document.");
        var version = BinaryPrimitives.ReadUInt16LittleEndian(header[4..6]);
        if (version != 1)
            throw new NotSupportedException($"TTFB v{version}");
        var length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(header[8..12]));
        if (length <= 0 || (stream.CanSeek && length > stream.Length - stream.Position))
            throw new InvalidDataException("TTFB font length is invalid.");
        var data = GC.AllocateUninitializedArray<byte>(length);
        stream.ReadExactly(data);
        return data;
    }
}
