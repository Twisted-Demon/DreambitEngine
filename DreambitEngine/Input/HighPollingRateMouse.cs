using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit;

internal static class HighPollingRateMouse
{
    private static bool _sdlAttempted;
    private static bool _sdlEventsSuppressed;
    public static bool TryEnable(GameWindow gameWindow)
    {
        if (OperatingSystem.IsWindows())
        {
            var initialState = Mouse.GetState();

            return Win32RawMouseInput.TryStart(
                gameWindow,
                initialState);
        }

        if (!_sdlAttempted)
        {
            _sdlEventsSuppressed =
                SdlMouseMotionEvents.TrySuppress();

            _sdlAttempted = true;
        }

        return _sdlEventsSuppressed;
    }

    public static bool TryGetState(out MouseState state)
    {
        return Win32RawMouseInput.TryGetState(out state);
    }

    public static string Diagnostic => OperatingSystem.IsWindows()
        ? Win32RawMouseInput.Diagnostic
        : _sdlEventsSuppressed
            ? "SDL mouse-motion events suppressed"
            : "Using MonoGame mouse input";

    public static void Restore()
    {
        Win32RawMouseInput.Stop();
        SdlMouseMotionEvents.Restore();

        _sdlAttempted = false;
        _sdlEventsSuppressed = false;
    }
}
