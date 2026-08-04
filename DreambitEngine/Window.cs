using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class Window
{
    private static readonly TimeSpan ResizeDebounce =
        TimeSpan.FromMilliseconds(50);

    private static Point _previousWindowedSize;
    private static Point _previousWindowedPosition;
    private static Point _pendingClientSize;
    private static Point _lastNotifiedBackBufferSize;
    private static Point _clientSize;

    private static long _lastResizeTimestampMilliseconds;

    private static bool _hasPreviousWindowedState;
    private static bool _pendingResize;
    private static bool _applyingGraphicsChanges;
    private static bool _initialized;

    public static ILogger Logger = new Logger<Window>();

    public static int MonitorCount =>
        GraphicsAdapter.Adapters.Count;

    public static GraphicsAdapter Adapter =>
        Core.Instance?.GraphicsDevice?.Adapter ??
        GraphicsAdapter.DefaultAdapter;

    public static int BackBufferWidth
    {
        get
        {
            var graphicsDevice =
                Core.Instance?.GraphicsDevice;

            if (graphicsDevice != null)
                return Math.Max(
                    1,
                    graphicsDevice
                        .PresentationParameters
                        .BackBufferWidth);

            return Math.Max(
                1,
                Core.GraphicsDeviceManager?
                    .PreferredBackBufferWidth ?? 1);
        }
    }

    public static int BackBufferHeight
    {
        get
        {
            var graphicsDevice =
                Core.Instance?.GraphicsDevice;

            if (graphicsDevice != null)
                return Math.Max(
                    1,
                    graphicsDevice
                        .PresentationParameters
                        .BackBufferHeight);

            return Math.Max(
                1,
                Core.GraphicsDeviceManager?
                    .PreferredBackBufferHeight ?? 1);
        }
    }

    public static Point BackBufferSize =>
        new(BackBufferWidth, BackBufferHeight);

    public static int ClientWidth
    {
        get
        {
            if (_clientSize.X > 0)
                return _clientSize.X;

            return ReadClientSize().X;
        }
    }

    public static int ClientHeight
    {
        get
        {
            if (_clientSize.Y > 0)
                return _clientSize.Y;

            return ReadClientSize().Y;
        }
    }

    public static Point ClientSize
    {
        get
        {
            if (_clientSize.X > 0 && _clientSize.Y > 0)
                return _clientSize;

            return ReadClientSize();
        }
    }

    // Preserve the original API: Width/Height represent renderable pixels.
    public static int Width => BackBufferWidth;
    public static int Height => BackBufferHeight;
    public static Point ScreenSize => BackBufferSize;

    public static float AspectRatio =>
        BackBufferHeight > 0
            ? BackBufferWidth / (float)BackBufferHeight
            : 1f;

    /// <summary>
    ///     Raised after the backbuffer has been synchronized with a settled
    ///     window-client resize.
    /// </summary>
    public static event EventHandler<WindowResizedEventArgs> WindowResized;

    public static void Init()
    {
        if (_initialized)
            return;

        var instance =
            Core.Instance ??
            throw new InvalidOperationException(
                "Core.Instance must exist before Window.Init().");

        if (instance.GraphicsDevice == null)
            throw new InvalidOperationException(
                "Window.Init() requires an initialized GraphicsDevice. " +
                "Call it from Core.Initialize(), not the Core constructor.");

        _previousWindowedSize =
            BackBufferSize;

        _previousWindowedPosition =
            instance.Window.Position;

        _hasPreviousWindowedState = true;
        _lastNotifiedBackBufferSize = BackBufferSize;
        _clientSize = ReadClientSize();

        instance.Window.ClientSizeChanged +=
            OnClientSizeChanged;

        // Set this only after initialization succeeds. Otherwise a failed
        // first call would permanently block later initialization.
        _initialized = true;
    }

    public static void Shutdown()
    {
        if (!_initialized)
            return;

        if (Core.Instance?.Window != null)
            Core.Instance.Window.ClientSizeChanged -=
                OnClientSizeChanged;

        _initialized = false;
        _pendingResize = false;
        _clientSize = Point.Zero;
    }

    /// <summary>
    ///     Call once per game update, before scene/component updates.
    /// </summary>
    public static void Tick(GameTime gameTime)
    {
        _ = gameTime;

        if (!_pendingResize)
            return;

        var elapsedMilliseconds =
            Environment.TickCount64 -
            _lastResizeTimestampMilliseconds;

        if (elapsedMilliseconds <
            ResizeDebounce.TotalMilliseconds)
            return;

        _pendingResize = false;

        SynchronizeBackBufferToClient(
            _pendingClientSize);
    }

    private static void OnClientSizeChanged(
        object sender,
        EventArgs args)
    {
        _ = sender;
        _ = args;

        if (_applyingGraphicsChanges)
            return;

        var bounds =
            Core.Instance.Window.ClientBounds;

        // Minimized windows can temporarily report zero-sized bounds.
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        _clientSize = new Point(
            bounds.Width,
            bounds.Height);

        _pendingClientSize = _clientSize;

        _lastResizeTimestampMilliseconds =
            Environment.TickCount64;

        _pendingResize = true;
    }

    private static void SynchronizeBackBufferToClient(
        Point clientSize)
    {
        if (clientSize.X <= 0 || clientSize.Y <= 0)
            return;

        var graphics =
            Core.GraphicsDeviceManager;

        if (BackBufferSize != clientSize)
        {
            graphics.PreferredBackBufferWidth =
                clientSize.X;

            graphics.PreferredBackBufferHeight =
                clientSize.Y;

            ApplyGraphicsChanges();
        }

        RaiseResizeIfBackBufferChanged();
    }

    private static void ApplyGraphicsChanges()
    {
        var graphics =
            Core.GraphicsDeviceManager;

        if (graphics == null ||
            Core.Instance?.GraphicsDevice == null)
            return;

        _applyingGraphicsChanges = true;

        try
        {
            graphics.ApplyChanges();
        }
        finally
        {
            _applyingGraphicsChanges = false;
            _clientSize = ReadClientSize();
        }
    }

    private static Point ReadClientSize()
    {
        var instance = Core.Instance;

        if (instance?.Window == null)
            return BackBufferSize;

        var bounds = instance.Window.ClientBounds;

        return new Point(
            Math.Max(1, bounds.Width),
            Math.Max(1, bounds.Height));
    }

    private static void RaiseResizeIfBackBufferChanged()
    {
        var size =
            BackBufferSize;

        if (size.X <= 0 || size.Y <= 0)
            return;

        if (size == _lastNotifiedBackBufferSize)
            return;

        _lastNotifiedBackBufferSize = size;

        WindowResized?.Invoke(
            null,
            new WindowResizedEventArgs
            {
                Width = size.X,
                Height = size.Y,
                ClientWidth = ClientWidth,
                ClientHeight = ClientHeight
            });

        Logger.Debug($"Window resized: {ScreenSize}");
    }

    public static Vector2 ClientToBackBuffer(
        Vector2 clientPosition)
    {
        var clientSize =
            ClientSize;

        var backBufferSize =
            BackBufferSize;

        return new Vector2(
            clientPosition.X *
            backBufferSize.X / clientSize.X,
            clientPosition.Y *
            backBufferSize.Y / clientSize.Y);
    }

    public static Vector2 BackBufferToClient(
        Vector2 backBufferPosition)
    {
        var backBufferSize =
            BackBufferSize;

        var clientSize =
            ClientSize;

        return new Vector2(
            backBufferPosition.X *
            clientSize.X / Math.Max(1, backBufferSize.X),
            backBufferPosition.Y *
            clientSize.Y / Math.Max(1, backBufferSize.Y));
    }

    public static void SetSize(
        int width,
        int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _pendingResize = false;

        var graphics =
            Core.GraphicsDeviceManager ??
            throw new InvalidOperationException(
                "GraphicsDeviceManager has not been created.");

        graphics.PreferredBackBufferWidth = width;
        graphics.PreferredBackBufferHeight = height;

        // Before Core.Initialize(), these preferred values are enough.
        // MonoGame will use them while creating GraphicsDevice.
        if (Core.Instance?.GraphicsDevice == null)
            return;

        ApplyGraphicsChanges();
        RaiseResizeIfBackBufferChanged();

        RememberWindowedStateIfBordered();
    }

    public static void SetPosition(
        int x,
        int y)
    {
        Core.Instance.Window.Position =
            new Point(x, y);

        RememberWindowedStateIfBordered();
    }

    public static Point GetPosition()
    {
        return Core.Instance.Window.Position;
    }

    public static Point GetCenter()
    {
        var position =
            GetPosition();

        return new Point(
            position.X + ClientWidth / 2,
            position.Y + ClientHeight / 2);
    }

    public static void CenterOnPrimaryDisplay()
    {
        var displayMode =
            Adapter.CurrentDisplayMode;

        var clientSize =
            ClientSize;

        Core.Instance.Window.Position =
            new Point(
                Math.Max(
                    0,
                    (displayMode.Width - clientSize.X) / 2),
                Math.Max(
                    0,
                    (displayMode.Height - clientSize.Y) / 2));

        RememberWindowedStateIfBordered();
    }

    public static void SetAllowUserResizing(
        bool value)
    {
        Core.Instance.Window.AllowUserResizing =
            value;
    }

    public static void SetTitle(
        string title)
    {
        Core.Instance.Window.Title =
            title ?? string.Empty;
    }

    public static void SetBorderless(
        bool value)
    {
        Core.Instance.Window.IsBorderless =
            value;

        if (!value)
            RememberWindowedStateIfBordered();
    }

    public static void SetFullscreen(
        bool enabled)
    {
        var graphics =
            Core.GraphicsDeviceManager;

        _pendingResize = false;

        if (enabled)
        {
            RememberWindowedStateIfBordered();

            var displayMode =
                Adapter.CurrentDisplayMode;

            graphics.PreferredBackBufferWidth =
                displayMode.Width;

            graphics.PreferredBackBufferHeight =
                displayMode.Height;

            graphics.IsFullScreen = true;

            ApplyGraphicsChanges();
            RaiseResizeIfBackBufferChanged();
            return;
        }

        graphics.IsFullScreen = false;

        if (_hasPreviousWindowedState &&
            _previousWindowedSize.X > 0 &&
            _previousWindowedSize.Y > 0)
        {
            graphics.PreferredBackBufferWidth =
                _previousWindowedSize.X;

            graphics.PreferredBackBufferHeight =
                _previousWindowedSize.Y;
        }

        ApplyGraphicsChanges();

        Core.Instance.Window.IsBorderless = false;

        if (_hasPreviousWindowedState)
            Core.Instance.Window.Position =
                _previousWindowedPosition;

        RaiseResizeIfBackBufferChanged();
    }

    public static void SetBorderlessFullscreen(
        bool enabled)
    {
        var graphics =
            Core.GraphicsDeviceManager;

        _pendingResize = false;

        if (enabled)
        {
            RememberWindowedStateIfBordered();

            var displayMode =
                Adapter.CurrentDisplayMode;

            graphics.IsFullScreen = false;

            graphics.PreferredBackBufferWidth =
                displayMode.Width;

            graphics.PreferredBackBufferHeight =
                displayMode.Height;

            Core.Instance.Window.IsBorderless = true;

            ApplyGraphicsChanges();

            Core.Instance.Window.Position =
                Point.Zero;

            RaiseResizeIfBackBufferChanged();
            return;
        }

        Core.Instance.Window.IsBorderless = false;

        if (_hasPreviousWindowedState &&
            _previousWindowedSize.X > 0 &&
            _previousWindowedSize.Y > 0)
        {
            graphics.PreferredBackBufferWidth =
                _previousWindowedSize.X;

            graphics.PreferredBackBufferHeight =
                _previousWindowedSize.Y;
        }

        ApplyGraphicsChanges();

        if (_hasPreviousWindowedState)
            Core.Instance.Window.Position =
                _previousWindowedPosition;

        RaiseResizeIfBackBufferChanged();
    }

    public static void ToggleBorderlessFullscreen()
    {
        var displayMode =
            Adapter.CurrentDisplayMode;

        var isBorderlessFullscreen =
            Core.Instance.Window.IsBorderless &&
            BackBufferWidth == displayMode.Width &&
            BackBufferHeight == displayMode.Height;

        SetBorderlessFullscreen(
            !isBorderlessFullscreen);
    }

    public static void SetVsync(
        bool enabled)
    {
        Core.GraphicsDeviceManager
            .SynchronizeWithVerticalRetrace = enabled;

        ApplyGraphicsChanges();
        RaiseResizeIfBackBufferChanged();
    }

    public static void SetFixedTimeStep(
        bool enabled,
        double targetFps = 60.0)
    {
        Core.Instance.IsFixedTimeStep =
            enabled;

        if (enabled && targetFps > 0.0)
            Core.Instance.TargetElapsedTime =
                TimeSpan.FromSeconds(
                    1.0 / targetFps);
    }

    private static void RememberWindowedStateIfBordered()
    {
        if (Core.Instance.Window.IsBorderless ||
            Core.GraphicsDeviceManager.IsFullScreen)
            return;

        _previousWindowedSize =
            BackBufferSize;

        _previousWindowedPosition =
            Core.Instance.Window.Position;

        _hasPreviousWindowedState = true;
    }
}

public sealed class WindowResizedEventArgs : EventArgs
{
    /// <summary>
    ///     Actual backbuffer width after GraphicsDeviceManager.ApplyChanges().
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     Actual backbuffer height after GraphicsDeviceManager.ApplyChanges().
    /// </summary>
    public int Height { get; init; }

    public int ClientWidth { get; init; }
    public int ClientHeight { get; init; }

    public Point BackBufferSize =>
        new(Width, Height);

    public Point ClientSize =>
        new(ClientWidth, ClientHeight);
}