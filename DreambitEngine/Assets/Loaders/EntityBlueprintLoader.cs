namespace Dreambit;

public class EntityBlueprintLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);

        var entity = JsnbLoader.Deserialize<EntityBlueprint>(s);
        entity.AssetName = assetName;

        return entity;
    }
}