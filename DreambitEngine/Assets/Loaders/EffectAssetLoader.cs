using System;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public sealed class EffectAssetLoader : AssetLoaderBase
{
    public override string Extension => ".fxb";
    public override bool AddToDisposableList => true;
    public override Type TargetType => typeof(EffectAsset);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var effect = new Effect(Graphics.Device, FxbLoader.ReadEffectCode(stream)) { Name = assetName };
        return EffectAsset.Own(effect, assetName);
    }
}

public sealed class EffectLoader : AssetLoaderBase
{
    public override string Extension => ".fxb";
    public override bool AddToDisposableList => true;
    public override Type TargetType => typeof(Effect);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        return new Effect(Graphics.Device, FxbLoader.ReadEffectCode(stream)) { Name = assetName };
    }
}
