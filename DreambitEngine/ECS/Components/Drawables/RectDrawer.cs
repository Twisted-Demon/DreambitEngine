using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType($"Dreambit.{nameof(RectDrawer)}")]
public class RectDrawer : DrawableComponent
{
    public int Height = 32;

    public int Width = 32;
    public override RectangleF Bounds => GetBounds();

    public PivotType PivotType { get; set; } = PivotType.Center;

    public Vector2 Pivot { get; set; }

    public Color Color { get; set; } = Color.White;

    private RectangleF GetBounds()
    {
        var pivotToUse = Transform.WorldPosition2D;

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

        var bounds = new RectangleF(
            pivotToUse.X,
            pivotToUse.Y,
            Width,
            Height);

        return bounds;
    }

    public override void OnDraw()
    {
        Core.SpriteBatch.DrawHollowRectangle(
            Bounds, Color
        );
    }
}