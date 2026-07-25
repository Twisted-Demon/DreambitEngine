using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class CircleDrawer : DrawableComponent
{
    public override Rectangle Bounds => GetBounds();

    public Color Color { get; set; } = Color.White;
    public float Radius { get; set; } = 128f;
    public int Segments { get; set; } = 64;
    public float LineThickness { get; set; } = 1.0f;

    private Rectangle GetBounds()
    {
        var pivotToUse = Transform.WorldPosToVec2;

        var pivotOffset = PivotHelper.GetRelativePivot(PivotType.Center);
        pivotToUse -= new Vector2(pivotOffset.X * Radius, pivotOffset.Y * Radius);
        
        var bounds = new Rectangle(
            (int)pivotToUse.X,
            (int)pivotToUse.Y,
            (int)Radius,
            (int)Radius);

        return bounds;
    }

    public override void OnDraw()
    {
        var center = Transform.WorldPosToVec2;
        Core.SpriteBatch.DrawCircle(center, Radius, Color, Segments, LineThickness);
    }
}