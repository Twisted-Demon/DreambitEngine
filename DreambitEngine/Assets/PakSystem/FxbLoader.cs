using System;
using System.Buffers.Binary;
using System.IO;

namespace Dreambit;

internal static class FxbLoader
{
    public static byte[] ReadEffectCode(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        stream.ReadExactly(header);
        if (!header[..4].SequenceEqual("FXB0"u8))
            throw new InvalidDataException("Not an FXB document.");
        var version = BinaryPrimitives.ReadUInt16LittleEndian(header[4..6]);
        if (version != 1)
            throw new NotSupportedException($"FXB v{version}");
        var length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(header[8..12]));
        if (length <= 0 || (stream.CanSeek && length > stream.Length - stream.Position))
            throw new InvalidDataException("FXB bytecode length is invalid.");
        var code = GC.AllocateUninitializedArray<byte>(length);
        stream.ReadExactly(code);
        return code;
    }
}
