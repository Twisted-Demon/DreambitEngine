using System;

namespace Dreambit;

public class SpriteLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(Sprite);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        
        var sprite = JsnbLoader.Deserialize<Sprite>(s);
        sprite.AssetName = assetName;

        return sprite;
        
    }
}