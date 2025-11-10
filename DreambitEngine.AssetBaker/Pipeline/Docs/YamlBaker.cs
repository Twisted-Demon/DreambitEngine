using System.Text;
using DreambitEngine.AssetBaker.Abstractions;
using YamlDotNet.RepresentationModel;

namespace DreambitEngine.AssetBaker.Pipeline.Docs;

public class YamlBaker : AssetBakerBase
{
    private const uint FlagMinified = 1u << 0;
    private const uint FlagNormalizedNewlines = 1u << 1;
    private const uint FlagUtf8NoBom = 1u << 2;
    
    public override string AssetTypeName => "Yaml";
    public override string[] SupportedInputs => [".yaml"];
    public override string OutputExtension => ".yamlb";
    public override void Bake(BakeContext ctx)
    {
        var blob = BakeToBytes(ctx);
    }

    public override AssetBlob BakeToBytes(BakeContext ctx)
    {
        var ext = Path.GetExtension(ctx.InputPath).ToLowerInvariant();
        var text = File.ReadAllText(ctx.InputPath);
        
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        byte[] payload;
        var flags = FlagNormalizedNewlines | FlagUtf8NoBom;

        var yaml = new YamlStream();
        using var sr = new StringReader(text);
        yaml.Load(sr);

        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        yaml.Save(writer);

        var normalizedYaml = sb.ToString().Replace("\r\n", "\n");
        payload = Encoding.UTF8.GetBytes(normalizedYaml);
        
        using var ms = new MemoryStream(payload.Length + 32);
        YmlbWriter.Write(ms, payload, flags);
        var data = ms.ToArray();

        var logical = GetLogicalPath(ctx, OutputExtension);

        return new AssetBlob(logical, AssetType.Yaml, OutputExtension, data);
    }
}