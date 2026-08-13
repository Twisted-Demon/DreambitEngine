using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Dreambit;

public static class JsnbLoader
{
    public static T Deserialize<T>(Stream s)
    {
        return (T)Deserialize(s, typeof(T));
    }

    /// <summary>Deserializes a JSNB document to a type known only at runtime.</summary>
    public static object Deserialize(Stream s, Type type)
    {
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(type);

        return DreambitJson.Deserialize(GetJsonString(s), type)
               ?? throw new JsonSerializationException(
                   $"The JSNB document could not be deserialized as '{type.FullName}'.");
    }

    public static string GetJsonString(Stream s)
    {
        Span<byte> h = stackalloc byte[8];

        s.ReadExactly(h[..4]);
        if (h[0] != (byte)'J' || h[1] != (byte)'S' || h[2] != (byte)'N' || h[3] != (byte)'B')
            throw new InvalidDataException("Not JSNB");

        s.ReadExactly(h[..2]);
        var ver = BinaryPrimitives.ReadUInt16LittleEndian(h[..2]);
        if (ver != 1) throw new NotSupportedException($"JSNB v{ver}");

        s.ReadExactly(h[..4]); /* flags = */
        _ = BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);
        s.ReadExactly(h[..4]);
        var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);

        var bytes = GC.AllocateUninitializedArray<byte>(size);
        s.ReadExactly(bytes);

        return Encoding.UTF8.GetString(bytes);
    }
}
