using Microsoft.Xna.Framework;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public abstract class UIElement : UIComponent
{
    public HorizontalAlignment HTextAlignment { get; set; } = HorizontalAlignment.Center;
    public VerticalAlignment VTextAlignment { get; set; } = VerticalAlignment.Center;
    public Canvas Canvas { get; internal set; }

    public Vector2 GetScreenPos()
    {
        return Canvas.ConvertToScreenCoord(this);
    }

}