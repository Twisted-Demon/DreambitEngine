using Microsoft.Xna.Framework;

namespace Dreambit;

public class PostProcessSettings
{
    public ToneMappingType ToneMappingType { get; set; } = ToneMappingType.None;
    public float HueShift { get; set; } = 0.0f;
    public float Saturation { get; set; } = 1.0f;
    public Color TintColor { get; set; } = Color.White;

    public bool BloomEnabled { get; set; } = true;

    public float BloomThreshold { get; set; } = 0.25f;

    public float BloomSoftKnee { get; set; } = 0.5f;

    public float BloomIntensity { get; set; } = 2f;

    public PostProcessSettings Clone() => new()
    {
        HueShift = HueShift,
        Saturation = Saturation,
        TintColor = TintColor,
        
        BloomEnabled = BloomEnabled,
        BloomThreshold = BloomThreshold,
        BloomSoftKnee = BloomSoftKnee,
        BloomIntensity = BloomIntensity,
        ToneMappingType = ToneMappingType,
    };
}
