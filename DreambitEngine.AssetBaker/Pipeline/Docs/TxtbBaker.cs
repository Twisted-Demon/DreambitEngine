using System.Text;
using DreambitEngine.AssetBaker.Abstractions;


namespace DreambitEngine.AssetBaker.Pipeline.Docs;

public sealed class TxtbBaker : AssetBakerBase
{
    public override string AssetTypeName => "Text";
    public override string[] SupportedInputs => [".txt"];
    public override string OutputExtension => ".txtb";

    public override void Bake(BakeContext ctx)
    {
        var blob = BakeToBytes(ctx);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ctx.OutputPath))!);
        File.WriteAllBytes(ctx.OutputPath, blob.Data);
    }

    public override AssetBlob BakeToBytes(BakeContext ctx)
    {
        var text = File.ReadAllText(ctx.InputPath);

        var payload = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();

        TxtbWriter.Write(output, payload, 0);

        return new AssetBlob(
            GetLogicalPath(ctx, OutputExtension),
            AssetType.Text,
            OutputExtension,
            output.ToArray());
    }
}
