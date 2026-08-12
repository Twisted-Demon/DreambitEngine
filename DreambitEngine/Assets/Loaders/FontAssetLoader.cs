using System;

namespace Dreambit;

public sealed class FontAssetLoader : AssetLoaderBase
{
    public override string Extension => ".ttfb";
    public override bool AddToDisposableList => true;
    public override Type TargetType => typeof(FontAsset);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        return new FontAsset(TtfbLoader.ReadFontData(stream), assetName);
    }
}
