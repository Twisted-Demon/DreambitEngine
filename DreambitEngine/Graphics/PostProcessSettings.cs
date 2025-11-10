using Microsoft.Xna.Framework;

namespace Dreambit;

public class PostProcessSettings
{
    public float HueShift { get; set; } = 0.0f;
    public float Saturation { get; set; } = 1.0f;
    public Color TintColor { get; set; } = Color.White;
}