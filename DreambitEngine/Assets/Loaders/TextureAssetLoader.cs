using System;

namespace Dreambit;

public sealed class TextureAssetLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".texb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(TextureAsset);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var texture = TexbLoader.LoadTexture(stream);
        texture.Name = assetName;
        return TextureAsset.Own(texture, assetName);
    }
}
