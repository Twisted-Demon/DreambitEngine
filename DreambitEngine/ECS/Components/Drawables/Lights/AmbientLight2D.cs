using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class AmbientLight2D : Light2D
{
    public override Rectangle Bounds
    {
        get
        {
            var screenBounds = Core.GraphicsDeviceManager.GraphicsDevice.Viewport.Bounds;
            var rect = new Rectangle((int)Position.X, (int)Position.Y, screenBounds.Width, screenBounds.Height);

            return rect;
        }
    }
}