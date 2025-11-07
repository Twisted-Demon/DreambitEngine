using System.IO;

namespace Dreambit;

public sealed class Texture2dLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".texb";
    public override bool AddToDisposableList { get; } = true;
    
    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak,  contentDirectory);
        var asset = TexbLoader.LoadTexture(s);
        
        return asset;
    }
}