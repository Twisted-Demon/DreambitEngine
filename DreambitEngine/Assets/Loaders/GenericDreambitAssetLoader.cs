using System;
using System.IO;

namespace Dreambit;

/// <summary>
/// Engine-owned fallback for concrete game-defined DreambitAsset types that do not provide an
/// explicit loader. Instances are created by Resources and scoped to their target runtime type.
/// </summary>
internal sealed class GenericDreambitAssetLoader : AssetLoaderBase
{
    public GenericDreambitAssetLoader(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        if (!DreambitAssetTypeRegistry.CanUseGenericJsonLoader(targetType))
            throw new ArgumentException(
                $"'{targetType.FullName}' is not eligible for generic Dreambit JSON loading.",
                nameof(targetType));

        TargetType = targetType;
    }

    public override string Extension => ".jsonb";
    public override bool AddToDisposableList => true;
    public override Type TargetType { get; }

    public override object Load(
        string assetName,
        string pakName,
        bool usePak,
        string contentDirectory)
    {
        using var stream = GetDocumentStream(
            assetName,
            string.Empty,
            pakName,
            usePak,
            contentDirectory,
            out var resolvedAssetName);
        var asset = JsnbLoader.Deserialize(stream, TargetType);
        if (asset is not DreambitAsset dreambitAsset || !TargetType.IsInstanceOfType(asset))
        {
            throw new InvalidDataException(
                $"The JSONB document for '{assetName}' did not produce '{TargetType.FullName}'.");
        }

        dreambitAsset.AssetName = resolvedAssetName;
        return dreambitAsset;
    }
}
