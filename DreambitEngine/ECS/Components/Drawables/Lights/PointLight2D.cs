using System;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class PointLight2D : Light2D
{
    public float Radius { get; set; }

    public override Rectangle Bounds
    {
        get
        {
            float r = MathF.Max(0f, Radius); // safety
            int left  = (int)MathF.Floor(Position.X - r);
            int top   = (int)MathF.Floor(Position.Y - r);
            int size  = (int)MathF.Ceiling(r * 2f);
            return new Rectangle(left, top, size, size);
        }
    }

    public override void OnDebugDraw()
    {
        Core.SpriteBatch.DrawHollowRectangle(Bounds, Color.White);
    }
}