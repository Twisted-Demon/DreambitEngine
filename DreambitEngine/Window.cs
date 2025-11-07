using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public static class Window
{
    // Cashed "windowed mode" state so we can restore after fullscreen
    private static Point _prevWindowedSize;
    private static Point _prevWindowedPos;
    private static bool _hasPrevWindowed;

    // Debounce for resize spam (Windows/SDL can fire many per drag)
    private static readonly TimeSpan ResizeDebounce = TimeSpan.FromMilliseconds(50);
    private static TimeSpan _lastResizeAt = TimeSpan.Zero;
    private static bool _pendingResize;
    private static int _pendingW, _pendingH;
    
    public static int MonitorCount => GraphicsAdapter.Adapters.Count;

    public static int Width => Core.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth;
    public static int Height => Core.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight;

    public static Point ScreenSize => new(Width, Height);
    
    public static GraphicsAdapter Adapter => Core.Instance.GraphicsDevice.Adapter;

    public static float AspectRatio
    {
        get
        {
            var h = Height;
            return h > 0 ? Width / (float)h : 1f; // avoid div-by-zero during device transitions
        }
    }

    /// <summary>
    ///     Raised when the backbuffer size has *settled* (debounced).
    /// </summary>
    public static event EventHandler<WindowResizedEventArgs> WindowResized;

    public static void Init()
    {
        // Capture initial windowed state
        if (Core.Instance != null)
        {
            _prevWindowedSize = new Point(
                Core.GraphicsDeviceManager.PreferredBackBufferWidth,
                Core.GraphicsDeviceManager.PreferredBackBufferHeight);
            _prevWindowedPos = Core.Instance.Window.Position;
            _hasPrevWindowed = true;
        }

        // Debounced resize handler
        Core.Instance!.Window.ClientSizeChanged += OnClientSizeChanged;
    }

    public static void Tick(GameTime time)
    {
        // Drive debounce delivery from the game loop
        if (_pendingResize && time.TotalGameTime - _lastResizeAt >= ResizeDebounce)
        {
            _pendingResize = false;
            WindowResized?.Invoke(null, new WindowResizedEventArgs { Width = _pendingW, Height = _pendingH });
        }
    }

    private static void OnClientSizeChanged(object sender, EventArgs args)
    {
        // Track latest size; fire later (debounced)
        _pendingW = Width;
        _pendingH = Height;
        _lastResizeAt = Time.GameTime.TotalGameTime; // Core.GameTime optional; otherwise raise next Tick
        _pendingResize = true;
    }

    public static void SetSize(int width, int height)
    {
        if (width <= 0 || height <= 0) return;

        var gdm = Core.GraphicsDeviceManager;
        gdm.PreferredBackBufferWidth = width;
        gdm.PreferredBackBufferHeight = height;
        gdm.ApplyChanges();

        RememberWindowedStateIfBordered();
    }

    public static void SetPosition(int x, int y)
    {
        var p = new Point(x, y);
        Core.Instance.Window.Position = p;
        RememberWindowedStateIfBordered();
    }

    public static Point GetPosition()
    {
        return Core.Instance.Window.Position;
    }

    public static void CenterOnPrimaryDisplay()
    {
        var dm = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode;
        var winSize = new Point(Width, Height);
        var pos = new Point(Math.Max(0, (dm.Width - winSize.X) / 2),
            Math.Max(0, (dm.Height - winSize.Y) / 2));
        Core.Instance.Window.Position = pos;
        RememberWindowedStateIfBordered();
    }
    
    // ---- Window chrome & behavior -----------------------------------------

    public static void SetAllowUserResizing(bool value)
    {
        Core.Instance.Window.AllowUserResizing = value;
    }

    public static void SetTitle(string title)
    {
        Core.Instance.Window.Title = title ?? string.Empty;
    }

    public static void SetBorderless(bool value)
    {
        Core.Instance.Window.IsBorderless = value;
        // Borderless flips window chrome only; backbuffer stays as-is
        if (!value) RememberWindowedStateIfBordered();
    }

    // ---- Fullscreen modes --------------------------------------------------

    /// <summary>
    ///     Exclusive fullscreen using GraphicsDeviceManager.IsFullScreen.
    ///     Note: on DesktopGL this may change video mode & vsync behavior.
    /// </summary>
    public static void SetFullscreen(bool enabled)
    {
        var gdm = Core.GraphicsDeviceManager;

        if (enabled)
        {
            RememberWindowedStateIfBordered(); // store windowed state for restore
            var dm = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode;
            gdm.PreferredBackBufferWidth = dm.Width;
            gdm.PreferredBackBufferHeight = dm.Height;
            gdm.IsFullScreen = true;
            gdm.ApplyChanges();
        }
        else
        {
            gdm.IsFullScreen = false;

            if (_hasPrevWindowed && _prevWindowedSize is { X: > 0, Y: > 0 })
            {
                gdm.PreferredBackBufferWidth = _prevWindowedSize.X;
                gdm.PreferredBackBufferHeight = _prevWindowedSize.Y;
            }

            gdm.ApplyChanges();

            if (_hasPrevWindowed)
                Core.Instance.Window.Position = _prevWindowedPos;

            // Return to bordered window unless caller wants borderless explicitly
            Core.Instance.Window.IsBorderless = false;
        }
    }

    /// <summary>
    ///     Borderless fullscreen (aka “fullscreen window”). Safer than exclusive.
    ///     Toggles cleanly and restores previous windowed size/pos.
    /// </summary>
    public static void SetBorderlessFullscreen(bool enabled)
    {
        var gdm = Core.GraphicsDeviceManager;
        if (enabled)
        {
            RememberWindowedStateIfBordered();

            var dm = Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode;
            gdm.IsFullScreen = false; // ensure windowed mode behind the scenes
            gdm.PreferredBackBufferWidth = dm.Width;
            gdm.PreferredBackBufferHeight = dm.Height;
            gdm.ApplyChanges();

            Core.Instance.Window.IsBorderless = true;
            Core.Instance.Window.Position = new Point(0, 0);
        }
        else
        {
            Core.Instance.Window.IsBorderless = false;

            if (_hasPrevWindowed && _prevWindowedSize.X > 0 && _prevWindowedSize.Y > 0)
            {
                gdm.PreferredBackBufferWidth = _prevWindowedSize.X;
                gdm.PreferredBackBufferHeight = _prevWindowedSize.Y;
                gdm.ApplyChanges();
                Core.Instance.Window.Position = _prevWindowedPos;
            }
        }
    }

    public static void ToggleBorderlessFullscreen()
    {
        SetBorderlessFullscreen(!Core.Instance.Window.IsBorderless ||
                                Width != Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode.Width ||
                                Height != Core.Instance.GraphicsDevice.Adapter.CurrentDisplayMode.Height);
    }

    // ---- Timing / VSync ----------------------------------------------------

    /// <summary>Enable/disable VSync (swap interval).</summary>
    public static void SetVsync(bool enabled)
    {
        Core.GraphicsDeviceManager.SynchronizeWithVerticalRetrace = enabled;
        Core.GraphicsDeviceManager.ApplyChanges();
    }

    /// <summary>
    ///     Fixed timestep proxy (if your Core exposes the Game instance).
    ///     Useful to hard-cap logic rate while allowing unlocked rendering.
    /// </summary>
    public static void SetFixedTimeStep(bool enabled, double targetFps = 60.0)
    {
        try
        {
            Core.Instance.IsFixedTimeStep = enabled;
            if (enabled && targetFps > 0)
                Core.Instance.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / targetFps);
        }
        catch
        {
            // Some platforms or Core wrappers may not expose these—ignore safely.
        }
    }

    // ---- Helpers -----------------------------------------------------------

    private static void RememberWindowedStateIfBordered()
    {
        // Only remember if NOT already borderless/fullscreen (true windowed)
        if (!Core.Instance.Window.IsBorderless && !Core.GraphicsDeviceManager.IsFullScreen)
        {
            _prevWindowedSize = new Point(
                Core.GraphicsDeviceManager.PreferredBackBufferWidth,
                Core.GraphicsDeviceManager.PreferredBackBufferHeight);
            _prevWindowedPos = Core.Instance.Window.Position;
            _hasPrevWindowed = true;
        }
    }
}

public class WindowResizedEventArgs : EventArgs
{
    public int Width { get; set; }
    public int Height { get; set; }
}