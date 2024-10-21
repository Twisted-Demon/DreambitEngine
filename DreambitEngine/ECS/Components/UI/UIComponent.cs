using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class UIComponent : DrawableComponent
{
    public override Rectangle Bounds { get; }

    public virtual void OnDrawUI()
    {
    }
}