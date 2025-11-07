using System.Text.Json;
using DreambitEngine.AssetBaker.Abstractions;

namespace DreambitEngine.AssetBaker.Pipeline.Docs;

public sealed class JsonbBaker : AssetBakerBase
{
    //Flags
    private const uint FlagMinified = 1u << 0;
    private const uint FlagNormalizedNewlines = 1u << 1;
    private const uint FlagUtf8NoBom = 1u << 2;


    public override string AssetTypeName => "Json";
    public override string[] SupportedInputs => [".json", ".ldtk", ".ldtkl"];
    public override string OutputExtension => ".jsonb";

    public override void Bake(BakeContext ctx)
    {
        var blob = BakeToBytes(ctx);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ctx.OutputPath))!);
        File.WriteAllBytes(ctx.OutputPath, blob.Data);
    }

    public override AssetBlob BakeToBytes(BakeContext ctx)
    {
        var ext = Path.GetExtension(ctx.InputPath).ToLowerInvariant();
        var text = File.ReadAllText(ctx.InputPath);

        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        byte[] payload;
        var flags = FlagNormalizedNewlines | FlagUtf8NoBom;

        using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true });
        payload = JsonSerializer.SerializeToUtf8Bytes(doc.RootElement, new JsonSerializerOptions
        {
            WriteIndented = false,
        });
        flags |= FlagMinified;

        using var ms = new MemoryStream(payload.Length + 32);
        JsnbWriter.Write(ms, payload, flags);
        var blobData = ms.ToArray();

        var logical = GetLogicalPath(ctx, ".jsonb");

        return new AssetBlob(logical, AssetType.Json, ".jsonb", blobData);
    }
    
}