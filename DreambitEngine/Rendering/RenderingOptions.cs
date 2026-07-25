using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class RenderingOptions
{
    /// <summary>
    /// Alpha Blend if textures are premultiplied
    /// NonPremultiplied if textures are NOT premultiplied
    /// </summary>
    public BlendState BlendState { get; set; } = BlendState.AlphaBlend;
    public SamplerState SamplerState { get; set; } = SamplerState.PointClamp;
    public SamplerState UISamplerState { get; set; } = SamplerState.AnisotropicClamp;
}