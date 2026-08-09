using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public abstract class Light2D : DrawableComponent
{
    public Vector2 Position => Transform.WorldPosition2D;
    [DreambitSerialize]
    public Color Color { get; set; } = Color.White;
    [DreambitSerialize]
    public float Intensity { get; set; } = 1.0f;

    public override void OnCreated()
    {
        DrawLayer = DrawLayers.LightLayer;
    }
}
