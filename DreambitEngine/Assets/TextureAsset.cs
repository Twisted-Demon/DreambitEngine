using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Dreambit;

/// <summary>
/// Dreambit asset-system handle for a MonoGame texture.
/// Use this type in serialized assets and components, then access <see cref="Texture"/>
/// when passing the underlying resource to MonoGame drawing APIs.
/// </summary>
[DreambitAssetType("dreambit.texture", FileExtension = ".png")]
public sealed class TextureAsset : DreambitAsset
{
    private readonly bool _ownsTexture;

    internal TextureAsset(Texture2D texture, string assetName, bool ownsTexture)
    {
        Texture = texture;
        AssetName = assetName;
        _ownsTexture = ownsTexture;
    }

    [JsonIgnore]
    public Texture2D Texture { get; private set; }

    [JsonIgnore]
    public int Width => Texture?.Width ?? 0;

    [JsonIgnore]
    public int Height => Texture?.Height ?? 0;

    public static implicit operator Texture2D(TextureAsset asset) => asset?.Texture;

    public static TextureAsset FromTexture(Texture2D texture, string assetName = null) =>
        texture is null ? null : new TextureAsset(texture, assetName ?? texture.Name, false);

    internal static TextureAsset Own(Texture2D texture, string assetName) =>
        new(texture, assetName, true);

    protected override void CleanUp()
    {
        if (_ownsTexture)
            Texture?.Dispose();
        Texture = null;
    }
}
