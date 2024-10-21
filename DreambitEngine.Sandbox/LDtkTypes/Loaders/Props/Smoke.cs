using Dreambit.Sandbox.Drawable;
using LDtk;
using Microsoft.Xna.Framework;
using Dreambit.ECS;
using Dreambit.Graphics;

namespace Dreambit.Sandbox;

public partial class Smoke : LDtkEntity<Smoke>
{
    protected override void SetUp(LDtkLevel level)
    {
        var e = CreateEntity(this, tags: ["smoke"]);
        var fog = e.AttachComponent<ScreenFog>();
        fog.DrawLayer = 5;

        fog.Width = (int)Size.X;
        fog.Height = (int)Size.Y;

        var pivotX = Pivot.X * fog.Width;
        var pivotY = Pivot.Y * fog.Height;

        var pivot = new Vector2(pivotX, pivotY);

        fog.Pivot = pivot;
        fog.PivotType = PivotType.Custom;
    }
}