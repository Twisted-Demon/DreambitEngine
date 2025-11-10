using System;
using LDtk;

namespace Dreambit;

public class LDtkFileLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(LDtkFile);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var json = JsnbLoader.GetJsonString(s);

        var file = LdtkLoader.DreambitFomJson(json, assetName);
        return file;
    }
}