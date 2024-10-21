using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public abstract class UIElement : UIComponent
{
    public Canvas Canvas { get; internal set; }

    public float Alpha { get; set; } = 1.0f;

    public Color Color { get; set; } = Color.White;

    public PivotType PivotType { get; set; } = PivotType.Center;

    public Vector2 Pivot { get; set; }

    public Vector2 GetScreenPos()
    {
        return Canvas.ConvertToScreenCoord(Transform.WorldPosToVec2);
    }

    public Vector2 GetScreenPos(Vector2 position)
    {
        return Canvas.ConvertToScreenCoord(position);
    }

    public override void OnDestroyed()
    {
        Canvas = null;
    }
}