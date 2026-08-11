using System;

namespace Dreambit;

public class SceneBlueprintLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(SceneBlueprint);
    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetDocumentStream(
            assetName,
            ".scene",
            pakName,
            usePak,
            contentDirectory,
            out var resolvedAssetName);

        var sceneBlueprint = JsnbLoader.Deserialize<SceneBlueprint>(s);
        sceneBlueprint.AssetName = resolvedAssetName;
        
        return sceneBlueprint;
    }
}
