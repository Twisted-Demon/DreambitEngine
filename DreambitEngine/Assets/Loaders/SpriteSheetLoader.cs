using System;

namespace Dreambit;

public class SpriteSheetLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(SpriteSheet);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var sheet = JsnbLoader.Deserialize<SpriteSheet>(s);
        sheet.AssetName = assetName;
        
        sheet.LoadSpriteSheet();
        
        return sheet;
    }
}