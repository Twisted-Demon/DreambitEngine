using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Dreambit;

/// <summary>Dreambit asset-system handle for compiled MonoGame Effect bytecode.</summary>
[DreambitAssetType("dreambit.effect")]
public sealed class EffectAsset : DreambitAsset
{
    private readonly bool _ownsEffect;

    internal EffectAsset(Effect effect, string assetName, bool ownsEffect)
    {
        Effect = effect;
        AssetName = assetName;
        _ownsEffect = ownsEffect;
    }

    [JsonIgnore]
    public Effect Effect { get; private set; }

    public static implicit operator Effect(EffectAsset asset) => asset?.Effect;

    public static EffectAsset FromEffect(Effect effect, string assetName = null) =>
        effect is null ? null : new EffectAsset(effect, assetName ?? effect.Name, false);

    internal static EffectAsset Own(Effect effect, string assetName) => new(effect, assetName, true);

    protected override void CleanUp()
    {
        if (_ownsEffect)
            Effect?.Dispose();
        Effect = null;
    }
}
