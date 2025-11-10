using System;
using System.IO;
using System.Text.Json;
using LDtk;

namespace Dreambit;

public class LDtkLevelLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(LDtkLevel);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        
        
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var json = JsnbLoader.GetJsonString(s);

        var file = JsonSerializer.Deserialize(json, Constants.JsonSourceGenerator.LDtkLevel);

        if (file is not null)
            file.FilePath = Path.GetFullPath(assetName);

        return file;
    }
}