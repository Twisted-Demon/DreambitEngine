using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit;

/// <summary>
///     Receives high-frequency Windows raw mouse input on a background thread.
///     Legacy mouse messages are disabled for the process so MonoGame's game
///     thread does not have to drain thousands of motion events each second.
/// </summary>
internal static class Win32RawMouseInput
{
    private const ushort GenericDesktopUsagePage = 0x01;
    private const ushort MouseUsage = 0x02;

    private const uint InputSink = 0x00000100;
    private const uint NoLegacy = 0x00000030;
    private const uint RemoveDevice = 0x00000001;

    private const uint RawInputMessage = 0x00FF;
    private const uint RawInputCommand = 0x10000003;
    private const uint RawInputMouse = 0;
    private const uint StopMessage = 0x8001;
    private const uint TimerMessage = 0x0113;
    private const uint RawMouseWheel = 0x0400;
    private const uint RawMouseHorizontalWheel = 0x0800;
    private const uint WindowBoundaryCheckMilliseconds = 10;

    private static readonly UIntPtr WindowBoundaryTimerId =
        new(1u);

    private const int VirtualLeftButton = 0x01;
    private const int VirtualRightButton = 0x02;
    private const int VirtualMiddleButton = 0x04;
    private const int VirtualXButton1 = 0x05;
    private const int VirtualXButton2 = 0x06;

    private static readonly object Sync = new();
    private static readonly ManualResetEventSlim ThreadReady = new(false);

    private static Thread _messageThread;
    private static IntPtr _messageWindow;
    private static IntPtr _gameWindow;
    private static uint _messageThreadId;
    private static int _active;
    private static bool _startAttempted;
    private static int _verticalScroll;
    private static int _horizontalScroll;
    private static bool _legacyMessagesEnabled;
    private static bool _nonClientInteractionActive;
    private static string _diagnostic = "Not initialized";

    public static string Diagnostic => _diagnostic;

    public static bool TryStart(
        GameWindow gameWindow,
        in MouseState initialState)
    {
        if (!OperatingSystem.IsWindows())
        {
            _diagnostic = "Not required on this operating system";
            return false;
        }

        if (Volatile.Read(ref _active) != 0)
            return true;

        lock (Sync)
        {
            if (Volatile.Read(ref _active) != 0)
                return true;

            if (_startAttempted)
                return false;

            var gameWindowHandle = ResolveWindowHandle(gameWindow);

            if (gameWindowHandle == IntPtr.Zero)
            {
                _diagnostic = "Waiting for the game window";
                return false;
            }

            _gameWindow = gameWindowHandle;
            _verticalScroll = initialState.ScrollWheelValue;
            _horizontalScroll = initialState.HorizontalScrollWheelValue;
            _startAttempted = true;
            ThreadReady.Reset();

            _messageThread = new Thread(MessageLoop)
            {
                IsBackground = true,
                Name = "Dreambit raw mouse input"
            };

            _messageThread.Start();
        }

        ThreadReady.Wait(TimeSpan.FromSeconds(1));
        return Volatile.Read(ref _active) != 0;
    }

    public static bool TryGetState(out MouseState state)
    {
        state = default;

        if (Volatile.Read(ref _active) == 0 ||
            _gameWindow == IntPtr.Zero ||
            !GetCursorPos(out var cursorPosition) ||
            !ScreenToClient(_gameWindow, ref cursorPosition))
        {
            return false;
        }

        var acceptsInput =
            GetForegroundWindow() == _gameWindow &&
            IsClientPoint(cursorPosition);

        state = new MouseState(
            cursorPosition.X,
            cursorPosition.Y,
            Volatile.Read(ref _verticalScroll),
            ReadButton(VirtualLeftButton, acceptsInput),
            ReadButton(VirtualMiddleButton, acceptsInput),
            ReadButton(VirtualRightButton, acceptsInput),
            ReadButton(VirtualXButton1, acceptsInput),
            ReadButton(VirtualXButton2, acceptsInput),
            Volatile.Read(ref _horizontalScroll));

        return true;
    }

    public static void Stop()
    {
        Thread messageThread;
        IntPtr messageWindow;
        uint messageThreadId;

        lock (Sync)
        {
            messageThread = _messageThread;
            messageWindow = _messageWindow;
            messageThreadId = _messageThreadId;
        }

        if (messageThread != null && messageThread.IsAlive)
        {
            if (messageWindow != IntPtr.Zero)
            {
                PostMessage(
                    messageWindow,
                    StopMessage,
                    UIntPtr.Zero,
                    IntPtr.Zero);
            }
            else if (messageThreadId != 0)
            {
                PostThreadMessage(
                    messageThreadId,
                    StopMessage,
                    UIntPtr.Zero,
                    IntPtr.Zero);
            }

            if (Thread.CurrentThread != messageThread)
                messageThread.Join(TimeSpan.FromSeconds(1));
        }

        lock (Sync)
        {
            _messageThread = null;
            _messageWindow = IntPtr.Zero;
            _gameWindow = IntPtr.Zero;
            _messageThreadId = 0;
            _startAttempted = false;
            _legacyMessagesEnabled = false;
            _nonClientInteractionActive = false;
            Volatile.Write(ref _active, 0);
            _diagnostic = "Stopped";
        }
    }

    private static void MessageLoop()
    {
        _messageThreadId = GetCurrentThreadId();
        var deviceRegistered = false;
        var timerStarted = false;

        try
        {
            _messageWindow = CreateWindowEx(
                0,
                "STATIC",
                "Dreambit raw mouse input",
                0,
                0,
                0,
                0,
                0,
                new IntPtr(-3),
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (_messageWindow == IntPtr.Zero)
            {
                _diagnostic =
                    $"Could not create the raw-input window ({Marshal.GetLastPInvokeError()})";
                return;
            }

            _legacyMessagesEnabled =
                !IsCursorInClientArea();
            _nonClientInteractionActive = false;

            if (!RegisterMouseDevice(
                    _legacyMessagesEnabled))
            {
                _diagnostic =
                    $"Could not register raw mouse input ({Marshal.GetLastPInvokeError()})";
                return;
            }

            deviceRegistered = true;
            timerStarted = SetTimer(
                               _messageWindow,
                               WindowBoundaryTimerId,
                               WindowBoundaryCheckMilliseconds,
                               IntPtr.Zero) != UIntPtr.Zero;

            Volatile.Write(ref _active, 1);
            _diagnostic =
                $"Active for HWND 0x{_gameWindow.ToInt64():X}";
            ThreadReady.Set();

            while (GetMessage(
                       out var message,
                       IntPtr.Zero,
                       0,
                       0) > 0)
            {
                if (message.Message == StopMessage)
                    break;

                if (message.Message == RawInputMessage)
                    ProcessRawInput(message.LParam);
                else if (message.Message == TimerMessage)
                    UpdateLegacyMouseMessageMode();

                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
        finally
        {
            Volatile.Write(ref _active, 0);
            ThreadReady.Set();

            if (timerStarted)
            {
                KillTimer(
                    _messageWindow,
                    WindowBoundaryTimerId);
            }

            if (deviceRegistered)
            {
                var device = new RawInputDevice
                {
                    UsagePage = GenericDesktopUsagePage,
                    Usage = MouseUsage,
                    Flags = RemoveDevice,
                    Target = IntPtr.Zero
                };

                RegisterRawInputDevices(
                    new[] { device },
                    1,
                    (uint)Marshal.SizeOf<RawInputDevice>());
            }

            if (_messageWindow != IntPtr.Zero)
            {
                DestroyWindow(_messageWindow);
                _messageWindow = IntPtr.Zero;
            }
        }
    }

    private static unsafe void ProcessRawInput(
        IntPtr rawInputHandle)
    {
        const int maximumInputSize = 256;
        var data = stackalloc byte[maximumInputSize];
        var dataSize = (uint)maximumInputSize;
        var headerSize = (uint)sizeof(RawInputHeader);

        var bytesRead = GetRawInputData(
            rawInputHandle,
            RawInputCommand,
            (IntPtr)data,
            ref dataSize,
            headerSize);

        if (bytesRead == uint.MaxValue ||
            bytesRead < headerSize + sizeof(RawMouse) ||
            ((RawInputHeader*)data)->Type != RawInputMouse)
        {
            return;
        }

        var mouse = (RawMouse*)(data + headerSize);
        UpdateLegacyMouseMessageMode();

        if (GetForegroundWindow() != _gameWindow)
            return;

        var wheelDelta = unchecked((short)mouse->ButtonData);

        if ((mouse->ButtonFlags & RawMouseWheel) != 0)
            Interlocked.Add(ref _verticalScroll, wheelDelta);

        if ((mouse->ButtonFlags & RawMouseHorizontalWheel) != 0)
            Interlocked.Add(ref _horizontalScroll, wheelDelta);
    }

    private static void UpdateLegacyMouseMessageMode()
    {
        var cursorInClientArea =
            IsCursorInClientArea();

        var anyButtonDown =
            IsAnyMouseButtonDown();

        if (!_legacyMessagesEnabled)
        {
            if (!cursorInClientArea &&
                SetLegacyMouseMessagesEnabled(true))
            {
                _nonClientInteractionActive =
                    anyButtonDown;
            }

            return;
        }

        if (!cursorInClientArea)
        {
            if (anyButtonDown)
                _nonClientInteractionActive = true;

            return;
        }

        if (_nonClientInteractionActive)
        {
            if (anyButtonDown)
                return;

            _nonClientInteractionActive = false;
        }

        if (!anyButtonDown)
            SetLegacyMouseMessagesEnabled(false);
    }

    private static bool SetLegacyMouseMessagesEnabled(
        bool enabled)
    {
        if (_legacyMessagesEnabled == enabled)
            return true;

        if (!RegisterMouseDevice(enabled))
        {
            _diagnostic =
                $"Could not switch the raw mouse mode ({Marshal.GetLastPInvokeError()})";
            return false;
        }

        _legacyMessagesEnabled = enabled;
        return true;
    }

    private static bool RegisterMouseDevice(
        bool legacyMessagesEnabled)
    {
        var device = new RawInputDevice
        {
            UsagePage = GenericDesktopUsagePage,
            Usage = MouseUsage,
            Flags = InputSink |
                    (legacyMessagesEnabled
                        ? 0
                        : NoLegacy),
            Target = _messageWindow
        };

        return RegisterRawInputDevices(
            new[] { device },
            1,
            (uint)Marshal.SizeOf<RawInputDevice>());
    }

    private static bool IsCursorInClientArea()
    {
        if (_gameWindow == IntPtr.Zero ||
            !GetCursorPos(out var cursorPosition) ||
            !ScreenToClient(_gameWindow, ref cursorPosition) ||
            !GetClientRect(_gameWindow, out var clientBounds))
        {
            return false;
        }

        return IsClientPoint(
            cursorPosition,
            clientBounds);
    }

    private static bool IsClientPoint(
        in NativePoint point)
    {
        return GetClientRect(
                   _gameWindow,
                   out var clientBounds) &&
               IsClientPoint(point, clientBounds);
    }

    private static bool IsClientPoint(
        in NativePoint point,
        in NativeRectangle clientBounds)
    {
        return point.X >= clientBounds.Left &&
               point.Y >= clientBounds.Top &&
               point.X < clientBounds.Right &&
               point.Y < clientBounds.Bottom;
    }

    private static bool IsAnyMouseButtonDown()
    {
        return IsButtonDown(VirtualLeftButton) ||
               IsButtonDown(VirtualMiddleButton) ||
               IsButtonDown(VirtualRightButton) ||
               IsButtonDown(VirtualXButton1) ||
               IsButtonDown(VirtualXButton2);
    }

    private static bool IsButtonDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private static ButtonState ReadButton(
        int virtualKey,
        bool acceptsInput)
    {
        return acceptsInput &&
               IsButtonDown(virtualKey)
            ? ButtonState.Pressed
            : ButtonState.Released;
    }

    private static IntPtr ResolveWindowHandle(
        GameWindow gameWindow)
    {
        if (gameWindow.Handle != IntPtr.Zero &&
            IsWindow(gameWindow.Handle))
        {
            return gameWindow.Handle;
        }

        var processId =
            (uint)Environment.ProcessId;

        var expectedTitle =
            gameWindow.Title ?? string.Empty;

        var exactMatch = IntPtr.Zero;
        var bestMatch = IntPtr.Zero;
        long bestArea = 0;

        EnumWindows((windowHandle, _) =>
        {
            GetWindowThreadProcessId(
                windowHandle,
                out var ownerProcessId);

            if (ownerProcessId != processId ||
                !IsWindowVisible(windowHandle) ||
                !GetClientRect(windowHandle, out var bounds))
            {
                return true;
            }

            var className = new StringBuilder(256);
            GetClassName(
                windowHandle,
                className,
                className.Capacity);

            if (className.ToString() == "ConsoleWindowClass")
                return true;

            var width = bounds.Right - bounds.Left;
            var height = bounds.Bottom - bounds.Top;

            if (width <= 0 || height <= 0)
                return true;

            var title = new StringBuilder(512);
            GetWindowText(
                windowHandle,
                title,
                title.Capacity);

            if (!string.IsNullOrEmpty(expectedTitle) &&
                title.ToString() == expectedTitle)
            {
                exactMatch = windowHandle;
                return false;
            }

            var area = (long)width * height;

            if (area > bestArea)
            {
                bestArea = area;
                bestMatch = windowHandle;
            }

            return true;
        }, IntPtr.Zero);

        return exactMatch != IntPtr.Zero
            ? exactMatch
            : bestMatch;
    }

    private delegate bool EnumWindowsProcedure(
        IntPtr windowHandle,
        IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public UIntPtr WParam;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct RawMouse
    {
        [FieldOffset(0)] public ushort Flags;
        [FieldOffset(4)] public uint Buttons;
        [FieldOffset(4)] public ushort ButtonFlags;
        [FieldOffset(6)] public ushort ButtonData;
        [FieldOffset(8)] public uint RawButtons;
        [FieldOffset(12)] public int LastX;
        [FieldOffset(16)] public int LastY;
        [FieldOffset(20)] public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr Window;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public NativePoint Point;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices(
        RawInputDevice[] devices,
        uint deviceCount,
        uint deviceSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(
        IntPtr rawInput,
        uint command,
        IntPtr data,
        ref uint dataSize,
        uint headerSize);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(
        IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern int GetMessage(
        out NativeMessage message,
        IntPtr windowHandle,
        uint minimumMessage,
        uint maximumMessage);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(
        ref NativeMessage message);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(
        ref NativeMessage message);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern UIntPtr SetTimer(
        IntPtr windowHandle,
        UIntPtr timerId,
        uint intervalMilliseconds,
        IntPtr timerProcedure);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool KillTimer(
        IntPtr windowHandle,
        UIntPtr timerId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(
        IntPtr windowHandle,
        uint message,
        UIntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(
        uint threadId,
        uint message,
        UIntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(
        int virtualKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(
        out NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(
        IntPtr windowHandle,
        ref NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(
        EnumWindowsProcedure procedure,
        IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr windowHandle,
        out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(
        IntPtr windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(
        IntPtr windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(
        IntPtr windowHandle,
        out NativeRectangle bounds);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(
        IntPtr windowHandle,
        StringBuilder className,
        int maximumCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(
        IntPtr windowHandle,
        StringBuilder title,
        int maximumCount);
}
