using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class UIComponent : DrawableComponent
{
    public override Rectangle Bounds { get; }

    public virtual void OnDrawUI()
    {
    }
}