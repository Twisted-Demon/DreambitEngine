namespace Dreambit;

/// <summary>
/// Resolves stable asset identities to the logical names consumed by runtime loaders.
/// The Editor owns the source registry; packaged games can install a baked implementation.
/// </summary>
public interface IAssetRegistry
{
    bool TryResolveAssetName(AssetId assetId, out string assetName);

    bool TryGetAssetId(string assetName, out AssetId assetId);
}
