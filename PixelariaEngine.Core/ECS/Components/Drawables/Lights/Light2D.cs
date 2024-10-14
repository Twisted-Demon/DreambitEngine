using Microsoft.Xna.Framework;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public abstract class Light2D : DrawableComponent
{
    public Vector2 Position => Transform.WorldPosToVec2;
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; }
    
    public override void OnCreated()
    {
        DrawLayer = RenderLayers.LightLayer;
    }


    public static readonly int SizeInBytes = (2 + 3 + 1) * sizeof(float);
}