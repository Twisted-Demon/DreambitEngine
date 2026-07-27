using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class AmbientLight2D : Light2D
{
    public override RectangleF Bounds
    {
        get
        {
            var screenBounds = Core.GraphicsDeviceManager.GraphicsDevice.Viewport.Bounds;
            var rect = new RectangleF(Position.X, Position.Y, screenBounds.Width, screenBounds.Height);

            return rect;
        }
    }
}