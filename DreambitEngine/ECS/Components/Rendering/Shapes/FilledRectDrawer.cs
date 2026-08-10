using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType(nameof(FilledRectDrawer))]
public sealed class FilledRectDrawer : DrawableComponent<FilledRectDrawer>
{
    [DreambitSerialize] public float Width { get; set; } = 32f;
    [DreambitSerialize] public float Height { get; set; } = 32f;
    [DreambitSerialize] public Color Color { get; set; } = Color.White;

    public override RectangleF Bounds
    {
        get
        {
            var position = Transform.WorldPosition2D;
            return new RectangleF(position.X, position.Y, Width, Height);
        }
    }

    protected override void OnDraw()
    {
        Core.SpriteBatch.DrawFilledRectangle(Bounds, Color);
    }
}
