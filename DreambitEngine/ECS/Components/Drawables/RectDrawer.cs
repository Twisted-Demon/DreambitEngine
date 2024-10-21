using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class RectDrawer : DrawableComponent
{
    public int Height = 32;

    public int Width = 32;
    public override Rectangle Bounds => GetBounds();

    public PivotType PivotType { get; set; } = PivotType.TopRight;

    public Vector2 Pivot { get; set; }

    public Color Color { get; set; } = Color.White;

    private Rectangle GetBounds()
    {
        var pivotToUse = Transform.WorldPosToVec2;

        switch (PivotType)
        {
            case PivotType.Custom:
                pivotToUse -= Pivot;
                break;
            default:
                var pivotOffset = PivotHelper.GetRelativePivot(PivotType);
                pivotToUse -= new Vector2(pivotOffset.X * Width, pivotOffset.Y * Height);
                break;
        }

        var bounds = new Rectangle(
            (int)pivotToUse.X,
            (int)pivotToUse.Y,
            Width,
            Height);

        return bounds;
    }

    public override void OnDraw()
    {
        Core.SpriteBatch.DrawFilledRectangle(
            Bounds, Color
        );
    }
}