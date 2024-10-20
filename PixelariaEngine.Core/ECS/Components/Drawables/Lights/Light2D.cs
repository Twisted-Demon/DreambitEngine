using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public abstract class Light2D : DrawableComponent
{
    public static readonly int SizeInBytes = (2 + 3 + 1) * sizeof(float);
    public Vector2 Position => Transform.WorldPosToVec2;
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; }

    public override void OnCreated()
    {
        DrawLayer = RenderLayers.LightLayer;
    }
}