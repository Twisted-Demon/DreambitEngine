using System;
using Microsoft.Xna.Framework.Media;

namespace Dreambit;

public sealed class SongLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".audb";

    public override bool AddToDisposableList { get; } = true;

    public override Type TargetType { get; } =
        typeof(Song);

    public override object Load(
        string assetName,
        string pakName,
        bool usePak,
        string contentDirectory)
    {
        using var stream = GetStream(
            GetPath(assetName),
            pakName,
            usePak,
            contentDirectory);

        return AudbLoader.LoadSongAsset(
            stream,
            assetName);
    }
}