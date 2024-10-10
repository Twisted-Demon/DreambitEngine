using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public class UIElement : UIComponent
{
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;
    public Canvas Canvas { get; internal set; }

}