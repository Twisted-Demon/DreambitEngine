using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType($"{nameof(CircleDrawer)}")]
public class CircleDrawer : DrawableComponent
{
    public override RectangleF Bounds => GetBounds();

    [DreambitSerialize] public Color Color { get; set; } = Color.White;
    [DreambitSerialize] public float Radius { get; set; } = 128f;
    [DreambitSerialize] public int Segments { get; set; } = 64;
    [DreambitSerialize] public float LineThickness { get; set; } = 1.0f;

    private RectangleF GetBounds()
    {
        var pivotToUse = Transform.WorldPosition2D;

        var pivotOffset = PivotHelper.GetRelativePivot(PivotType.Center);
        pivotToUse -= new Vector2(pivotOffset.X * Radius, pivotOffset.Y * Radius);

        var bounds = new RectangleF(
            pivotToUse.X,
            pivotToUse.Y,
            Radius,
            Radius);

        return bounds;
    }

    protected override void OnDraw()
    {
        var center = Transform.WorldPosition2D;
        Core.SpriteBatch.DrawCircle(center, Radius, Color, Segments,
            LineThickness * Scene.MainCamera.WorldUnitsPerScreenPixel);
    }
}
