using DreambitEngine.AssetBaker.Pipeline;

namespace DreambitEngine.AssetBaker.Abstractions;

public abstract class AssetBakerBase : IAssetBaker
{
    public abstract string AssetTypeName { get; }
    public abstract string[] SupportedInputs { get; }
    public abstract string OutputExtension { get; }
    public virtual string GetOutputPath(string inputPath) =>
        Path.ChangeExtension(inputPath, OutputExtension);
    public abstract void Bake(BakeContext ctx);

    public abstract AssetBlob BakeToBytes(BakeContext ctx);
    
    protected static string GetLogicalPath(BakeContext ctx, string outputExt)
    {
        // compute logical path relative to root (normalized forward slashes, lowercase)
        var root = string.IsNullOrWhiteSpace(ctx.LogicalRoot) ? Path.GetDirectoryName(ctx.InputPath)! : ctx.LogicalRoot!;
        var rel  = Path.GetRelativePath(root, ctx.InputPath);
        var logical = Path.ChangeExtension(rel, outputExt)
            .Replace('\\','/')
            .ToLowerInvariant();
        return logical.TrimStart('.', '/');
    }
}
