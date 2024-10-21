using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class PointLight2D : Light2D
{
    public float Radius { get; set; }
    public override Rectangle Bounds { get; }
}