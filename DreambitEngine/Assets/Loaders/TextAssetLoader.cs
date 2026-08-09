using System;

namespace Dreambit;

public sealed class TextAssetLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".txtb";
    public override bool AddToDisposableList { get; } = false;
    public override Type TargetType { get; } = typeof(string);

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

        return TxtbLoader.GetText(stream);
    }
}
