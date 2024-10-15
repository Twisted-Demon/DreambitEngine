using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.Sandbox;

public partial class Smoke : LDtkEntity<Smoke>
{
    protected override void SetUp(LDtkLevel level)
    {
        var e = CreateEntity(this, tags: ["smoke"]);
        var rect = e.AttachComponent<RectDrawer>();
        rect.DrawLayer = 5;

        rect.Width = (int)Size.X;
        rect.Height = (int)Size.Y;

        var pivotX = Pivot.X * rect.Width;
        var pivotY = Pivot.Y * rect.Height;

        var pivot = new Vector2(pivotX, pivotY);

        rect.Pivot = pivot;
        rect.PivotType = PivotType.Custom;
        rect.Color = SmartColor;
    }
}