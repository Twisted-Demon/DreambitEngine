using System;

namespace Dreambit.LDtk.Loaders;

public sealed class LDtkLevelLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = false;
    public override Type TargetType { get; } = typeof(LDtkLevel);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        return LdtkJson.DeserializeLevel(JsnbLoader.GetJsonString(stream));
    }
}
