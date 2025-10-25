using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class PointLight2D : Light2D
{
    public float Radius { get; set; }

    public override Rectangle Bounds
    {
        get
        {
            var halfRadius = Radius * 0.5f;
            
            var rect = new Rectangle((int)Position.X - (int)halfRadius, (int)Position.Y - (int)halfRadius, (int)halfRadius, (int)halfRadius);
            return rect;
        }
    }
}