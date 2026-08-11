using System;

namespace Dreambit;

public sealed class ParticleFxConfigLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(ParticleFxConfig);

    public override object Load(
        string assetName,
        string pakName,
        bool usePak,
        string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var config = JsnbLoader.Deserialize<ParticleFxConfig>(stream);
        config.AssetName = assetName;
        config.Validate();
        return config;
    }
}
