using DreambitEngine.AssetBaker.Abstractions;
using SixLabors.ImageSharp.Memory;

namespace DreambitEngine.AssetBaker.Core;

public sealed class AssetBakerRegistry
{
    private readonly Dictionary<AssetType, IAssetBaker> _byType = new();
    private readonly Dictionary<string, IAssetBaker> _byExtension = new();

    public AssetBakerRegistry Register(AssetType type, IAssetBaker baker)
    {
        _byType[type] = baker;
        
        foreach(var ext in baker.SupportedInputs)
            _byExtension[ext] = baker;
        
        return this;
    }
    
    public IAssetBaker Get(AssetType type)
        => _byType.TryGetValue(type, out var b) ? b : 
            throw new InvalidOperationException($"no baker registered for {type}.");

    public IAssetBaker? GetByExt(string ext)
    {
        _byExtension.TryGetValue(ext, out var b);

        return b;
    }
}