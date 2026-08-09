using System;

namespace Dreambit.LDtk.Loaders;

public sealed class LDtkFileLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = false;
    public override Type TargetType { get; } = typeof(LDtkFile);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var project = LdtkJson.DeserializeProject(JsnbLoader.GetJsonString(stream));
        project.Attach(
            assetName,
            resolvedPath =>
            {
                var levelAssetName = LdtkPath.RemoveExtension(resolvedPath) + ".jsonb";
                using var levelStream = GetStream(levelAssetName, pakName, usePak, contentDirectory);
                return LdtkJson.DeserializeLevel(JsnbLoader.GetJsonString(levelStream));
            },
            usesLogicalAssetPaths: true);
        return project;
    }
}
