using System;

namespace Dreambit;

public class SpriteSheetAnimationLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(SpriteSheetAnimation);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var anim = JsnbLoader.Deserialize<SpriteSheetAnimation>(s);
        anim.AssetName = assetName;
        
        return anim;
    }
}