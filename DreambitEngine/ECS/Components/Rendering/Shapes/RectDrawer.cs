using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType($"{nameof(RectDrawer)}")]
public class RectDrawer : DrawableComponent
{
    [DreambitSerialize] public int Height = 32;

    [DreambitSerialize] public int Width = 32;
    public override RectangleF Bounds => GetBounds();

    [DreambitSerialize]
    public PivotType PivotType { get; set; } = PivotType.Center;

    [DreambitSerialize]
    public Vector2 Pivot { get; set; }

    [DreambitSerialize]
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

    protected override void OnDraw()
    {
        Core.SpriteBatch.DrawHollowRectangle(
            Bounds,
            Color,
            System.MathF.Max(1f, Scene.MainCamera?.WorldUnitsPerScreenPixel ?? 1f)
        );
    }
}
