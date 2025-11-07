using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components.Galaga;

public class BulletDrawer : DrawableComponent
{
    public Color Tint = Color.White;
    public int Size = 4;

    public override Rectangle Bounds
    {
        get
        {
            var pos = Transform.Position;
            return new Rectangle((int)pos.X, (int)pos.X, 1, Size);
        }
    }

    public override void OnDraw()
    {
        var pos = Transform.WorldPosToVec2;
        var end = new Vector2(pos.X, pos.Y - Size);

        Core.SpriteBatch.DrawLine(pos, end, Tint, 1.0f);
    }
}