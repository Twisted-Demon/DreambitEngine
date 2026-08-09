using System;

namespace Dreambit;

public class SceneBlueprintLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(SceneBlueprint);
    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);

        var sceneBlueprint = JsnbLoader.Deserialize<SceneBlueprint>(s);
        sceneBlueprint.AssetName = assetName;
        
        return sceneBlueprint;
    }
}