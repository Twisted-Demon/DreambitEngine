using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class RenderingOptions
{
    public BlendState BlendState { get; set; } = BlendState.AlphaBlend;
    public SamplerState SamplerState { get; set; } = SamplerState.AnisotropicClamp;
    public SamplerState UISamplerState { get; set; } = SamplerState.AnisotropicClamp;
}