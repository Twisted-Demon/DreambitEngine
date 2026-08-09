using System;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType($"{nameof(PointLight2D)}")]
public class PointLight2D : Light2D
{
    [DreambitSerialize]
    public float Radius { get; set; }

    public override RectangleF Bounds
    {
        get
        {
            var r = MathF.Max(0f, Radius); // safety
            var left = MathF.Floor(Position.X - r);
            var top = MathF.Floor(Position.Y - r);
            var size = MathF.Ceiling(r * 2f);
            return new RectangleF(left, top, size, size);
        }
    }

    public override void OnDebugDraw()
    {
        Core.SpriteBatch.DrawHollowRectangle(Bounds, Color.White);
    }
}
