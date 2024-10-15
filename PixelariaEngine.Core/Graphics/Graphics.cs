using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public static class GraphicsUtil
{
    public static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    public static GraphicsDeviceManager DeviceManager => Core.GraphicsDeviceManager;
}