using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public static class GameWindow
{
    public static int Width => Core.GraphicsDeviceManager.PreferredBackBufferWidth;
    public static int Height => Core.GraphicsDeviceManager.PreferredBackBufferHeight;

    public static void SetSize(int width, int height)
    {
        Core.GraphicsDeviceManager.PreferredBackBufferWidth = width;
        Core.GraphicsDeviceManager.PreferredBackBufferHeight = height;
        Core.GraphicsDeviceManager.ApplyChanges();
    }

    public static void SetTitle(string title)
    {
        Core.Instance.Window.Title = title;
    }

    public static void SetFullscreen(bool value)
    {
        Core.GraphicsDeviceManager.IsFullScreen = value;
        Core.GraphicsDeviceManager.ApplyChanges();
    }

    public static void SetBorderless(bool value)
    {
        Core.Instance.Window.IsBorderless = value;
    }

    public static void SetBorderlessFullscreen(bool value)
    {
        if (value == true)
        {
            var w = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
            var h = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
            
            SetSize(w,h);
            SetBorderless(true);
        }
    }
}