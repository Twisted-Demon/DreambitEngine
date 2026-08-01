using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType($"{nameof(CircleDrawer)}")]
public class CircleDrawer : DrawableComponent
{
    public override RectangleF Bounds => GetBounds();

    public Color Color { get; set; } = Color.White;
    public float Radius { get; set; } = 128f;
    public int Segments { get; set; } = 64;
    public float LineThickness { get; set; } = 1.0f;

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

    public override void OnDraw()
    {
        var center = Transform.WorldPosition2D;
        Core.SpriteBatch.DrawCircle(center, Radius, Color, Segments, LineThickness * Scene.MainCamera.WorldUnitsPerTexturePixel);
    }
}