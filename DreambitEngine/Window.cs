using System;
using Microsoft.Xna.Framework;

namespace Dreambit;

public static class Window
{
    public static int Width => Core.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth;
    public static int Height => Core.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight;

    public static Point ScreenSize => new(Width, Height);

    public static float AspectRatio => Width / (float)Height;

    public static event EventHandler<WindowEventArgs> WindowResized;

    public static void Init()
    {
        Core.Instance.Window.ClientSizeChanged += (sender, args) =>
        {
            WindowResized?.Invoke(sender, new WindowEventArgs
            {
                Width = Width,
                Height = Height
            });
        };
    }

    public static void SetSize(int width, int height)
    {
        Core.GraphicsDeviceManager.PreferredBackBufferWidth = width;
        Core.GraphicsDeviceManager.PreferredBackBufferHeight = height;
        Core.GraphicsDeviceManager.ApplyChanges();
    }

    public static void SetAllowUserResizing(bool value)
    {
        Core.Instance.Window.AllowUserResizing = value;
    }

    public static void SetTitle(string title)
    {
        Core.Instance.Window.Title = title;
    }

    public static void SetFullscreen(bool value)
    {
        var w = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
        var h = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
        Core.GraphicsDeviceManager.IsFullScreen = value;

        Core.GraphicsDeviceManager.PreferredBackBufferWidth = w;
        Core.GraphicsDeviceManager.PreferredBackBufferHeight = h;

        Core.GraphicsDeviceManager.ApplyChanges();
    }

    public static void SetBorderless(bool value)
    {
        Core.Instance.Window.IsBorderless = value;
    }

    public static void SetBorderlessFullscreen(bool value)
    {
        if (value)
        {
            var w = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
            var h = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode.Height;

            Core.GraphicsDeviceManager.PreferredBackBufferWidth = w;
            Core.GraphicsDeviceManager.PreferredBackBufferHeight = h;

            Core.GraphicsDeviceManager.IsFullScreen = false;
            GraphicsUtil.DeviceManager.ApplyChanges();
            Core.Instance.Window.IsBorderless = true;
            Core.Instance.Window.Position = new Point(0, 0);
        }
    }

    public static void SetVsync(bool value)
    {
        Core.GraphicsDeviceManager.SynchronizeWithVerticalRetrace = value;
        Core.GraphicsDeviceManager.ApplyChanges();
    }
}

public class WindowEventArgs : EventArgs
{
    public int Width { get; set; }
    public int Height { get; set; }
}