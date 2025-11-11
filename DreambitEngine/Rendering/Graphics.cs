using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public static class Graphics
{
    public static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    public static GraphicsDeviceManager DeviceManager => Core.GraphicsDeviceManager;
    public static SpriteBatch SpriteBatch => Core.SpriteBatch;
}