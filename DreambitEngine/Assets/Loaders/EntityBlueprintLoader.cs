using System;

namespace Dreambit;

public class EntityBlueprintLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(EntityBlueprint);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetDocumentStream(
            assetName,
            ".blueprint",
            pakName,
            usePak,
            contentDirectory,
            out var resolvedAssetName);

        var entity = JsnbLoader.Deserialize<EntityBlueprint>(s);
        entity.AssetName = resolvedAssetName;

        return entity;
    }
}
