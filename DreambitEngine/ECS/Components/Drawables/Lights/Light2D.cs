using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public abstract class Light2D : DrawableComponent
{
    public Vector2 Position => Transform.WorldPosToVec2;
    public Color Color { get; set; } = Color.White;
    public float Intensity { get; set; }

    public override void OnCreated()
    {
        DrawLayer = DrawLayers.LightLayer;
    }
}