using DreambitEngine.AssetBaker.Pipeline;

namespace DreambitEngine.AssetBaker.Abstractions;

public interface IAssetBaker
{
    string AssetTypeName { get; }
    string[] SupportedInputs { get;}
    string OutputExtension { get; }

    void Bake(BakeContext ctx);
    AssetBlob BakeToBytes(BakeContext ctx);
}

public sealed class BakeContext
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    
    public bool GenerateMips { get; init; }
    public bool PremultiplyAlpha { get; init; }
    public int? MaxDimension { get; init; }
    public bool MarkSRgb { get; init; }
    
    public string? LogicalRoot { get; init; }
}