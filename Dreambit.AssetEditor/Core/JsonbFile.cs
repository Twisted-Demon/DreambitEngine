using System.Buffers.Binary;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit.AssetEditor.Core;

internal static class JsonbFile
{
    private const uint FlagMinified = 1u << 0;
    private const uint FlagNormalizedNewlines = 1u << 1;
    private const uint FlagUtf8NoBom = 1u << 2;

    public static JObject Load(string path)
    {
        var json = Path.GetExtension(path).Equals(".jsonb", StringComparison.OrdinalIgnoreCase)
            ? LoadJsonb(path)
            : File.ReadAllText(path);

        return JObject.Parse(json);
    }

    public static void Save(string path, JObject root)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        if (!Path.GetExtension(path).Equals(".jsonb", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllText(path, root.ToString(Formatting.Indented) + Environment.NewLine, new UTF8Encoding(false));
            return;
        }

        var normalized = root.ToString(Formatting.None)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");
        var payload = Encoding.UTF8.GetBytes(normalized);

        using var stream = File.Create(path);
        stream.Write("JSNB"u8);

        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], 1);
        stream.Write(buffer[..2]);

        var flags = FlagMinified | FlagNormalizedNewlines | FlagUtf8NoBom;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, flags);
        stream.Write(buffer);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, checked((uint)payload.Length));
        stream.Write(buffer);
        stream.Write(payload);
    }

    private static string LoadJsonb(string path)
    {
        using var stream = File.OpenRead(path);
        return Dreambit.JsnbLoader.GetJsonString(stream);
    }
}
