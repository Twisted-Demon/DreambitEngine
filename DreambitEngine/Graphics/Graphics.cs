using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public static class GraphicsUtil
{
    public static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    public static GraphicsDeviceManager DeviceManager => Core.GraphicsDeviceManager;
}