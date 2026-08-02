using System;
using System.Runtime.InteropServices;

namespace Dreambit;

/// <summary>
///     Prevents DesktopGL's SDL event queue from filling with mouse-motion events.
///     Dreambit polls the latest mouse state once per frame, so retaining every
///     intermediate motion event only adds work for high-polling-rate mice.
/// </summary>
internal static class SdlMouseMotionEvents
{
    private const uint MouseMotion = 0x400;
    private const int Query = -1;
    private const int Ignore = 0;

    private static byte _previousState;
    private static bool _stateCaptured;
    private static IntPtr _library;
    private static EventStateDelegate _eventState;

    public static bool TrySuppress()
    {
        if (_stateCaptured)
            return _eventState(MouseMotion, Query) == Ignore;

        try
        {
            if (!TryLoadEventState())
                return false;

            _previousState = _eventState(MouseMotion, Ignore);
            _stateCaptured = true;

            return _eventState(MouseMotion, Query) == Ignore;
        }
        catch (Exception exception) when (
            exception is DllNotFoundException or
            EntryPointNotFoundException or
            BadImageFormatException)
        {
            ReleaseLibrary();
            return false;
        }
    }

    public static void Restore()
    {
        if (!_stateCaptured)
            return;

        try
        {
            _eventState(MouseMotion, _previousState);
        }
        catch (Exception exception) when (
            exception is DllNotFoundException or
            EntryPointNotFoundException or
            BadImageFormatException)
        {
        }
        finally
        {
            _stateCaptured = false;
            ReleaseLibrary();
        }
    }

    private static bool TryLoadEventState()
    {
        var libraryName = OperatingSystem.IsWindows()
            ? "SDL2.dll"
            : OperatingSystem.IsMacOS()
                ? "libSDL2-2.0.0.dylib"
                : "libSDL2-2.0.so.0";

        if (!NativeLibrary.TryLoad(libraryName, out _library) ||
            !NativeLibrary.TryGetExport(_library, "SDL_EventState", out var address))
        {
            ReleaseLibrary();
            return false;
        }

        _eventState =
            Marshal.GetDelegateForFunctionPointer<EventStateDelegate>(address);

        return true;
    }

    private static void ReleaseLibrary()
    {
        _eventState = null;

        if (_library == IntPtr.Zero)
            return;

        NativeLibrary.Free(_library);
        _library = IntPtr.Zero;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte EventStateDelegate(uint type, int state);
}
