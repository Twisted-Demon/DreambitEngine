using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public sealed class Texture2DLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".texb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(Texture2D);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak,  contentDirectory);
        var asset = TexbLoader.LoadTexture(s);
        asset.Name = assetName;
        
        return asset;
    }
}