using Microsoft.Xna.Framework;

namespace Dreambit;

/// <summary>Authorable rendering settings shared by a scene and its runtime instance.</summary>
public sealed class SceneSettings
{
    public float AmbientLightIntensity { get; set; } = 1f;
    public Color AmbientLightColor { get; set; } = Color.White;
    public PostProcessSettings PostProcessing { get; set; } = new();

    public SceneSettings Clone() => new()
    {
        AmbientLightIntensity = AmbientLightIntensity,
        AmbientLightColor = AmbientLightColor,
        PostProcessing = PostProcessing?.Clone() ?? new PostProcessSettings()
    };
}
