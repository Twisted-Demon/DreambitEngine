using System.Buffers.Binary;
using DreambitEngine.AssetBaker.Abstractions;
using DreambitEngine.AssetBaker.Pipeline;

namespace DreambitEngine.AssetBaker.Pipeline.Fonts;

public sealed class FontBaker : AssetBakerBase
{
    public override string AssetTypeName => "font";
    public override string[] SupportedInputs => [".ttf"];
    public override string OutputExtension => ".ttfb";

    public override void Bake(BakeContext ctx)
    {
        var blob = BakeToBytes(ctx);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ctx.OutputPath))!);
        File.WriteAllBytes(ctx.OutputPath, blob.Data);
    }

    public override AssetBlob BakeToBytes(BakeContext ctx)
    {
        var source = File.ReadAllBytes(ctx.InputPath);
        using var output = new MemoryStream(source.Length + 16);
        output.Write("TTFB"u8);
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], 1);
        output.Write(buffer[..2]);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], 0);
        output.Write(buffer[..2]);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[..4], checked((uint)source.Length));
        output.Write(buffer[..4]);
        output.Write(source);
        return new AssetBlob(GetLogicalPath(ctx, OutputExtension), AssetType.Font, OutputExtension, output.ToArray());
    }
}
