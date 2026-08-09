using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Dreambit;

public static class XmlbLoader
{
    public static T Deserialize<T>(Stream stream)
    {
        var xml = GetXmlString(stream);
        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(
            stringReader,
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

        var serializer = new XmlSerializer(typeof(T));

        return (T)(serializer.Deserialize(xmlReader)
            ?? throw new InvalidDataException(
                $"Could not deserialize XMLB payload as '{typeof(T).FullName}'."));
    }

    public static string GetXmlString(Stream stream)
    {
        Span<byte> header = stackalloc byte[8];

        stream.ReadExactly(header[..4]);
        if (header[0] != (byte)'X' ||
            header[1] != (byte)'M' ||
            header[2] != (byte)'L' ||
            header[3] != (byte)'B')
        {
            throw new InvalidDataException("Not XMLB");
        }

        stream.ReadExactly(header[..2]);
        var version = BinaryPrimitives.ReadUInt16LittleEndian(header[..2]);
        if (version != 1)
            throw new NotSupportedException($"XMLB v{version}");

        stream.ReadExactly(header[..4]);
        _ = BinaryPrimitives.ReadUInt32LittleEndian(header[..4]);

        stream.ReadExactly(header[..4]);
        var payloadSize = BinaryPrimitives.ReadUInt32LittleEndian(header[..4]);
        if (payloadSize > int.MaxValue)
            throw new InvalidDataException("XMLB payload is too large.");

        var bytes = GC.AllocateUninitializedArray<byte>((int)payloadSize);
        stream.ReadExactly(bytes);

        return Encoding.UTF8.GetString(bytes);
    }
}
