using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class UIComponent : DrawableComponent
{
    public virtual void OnDrawUI()
    {
        
    }

    public override Rectangle Bounds { get; }
}