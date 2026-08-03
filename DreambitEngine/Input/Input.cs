using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Dreambit.UI;

namespace Dreambit;

/// <summary>
///     Centralized input helper for keyboard and mouse state tracking.
///     Call <see cref="Init" />, then <see cref="PreUpdate" /> at the start of Update
///     and <see cref="PostUpdate" /> at the end of Update each frame.
/// </summary>
public static class Input
{
    #region Properties

    /// <summary>
    ///     Window-local client bounds for hit-testing the mouse position.
    ///     Mouse coordinates are relative to the game window, so this rectangle
    ///     must always start at zero rather than using the desktop window position.
    /// </summary>
    public static Rectangle WindowClientBounds =>
        new(0, 0, Window.ClientWidth, Window.ClientHeight);

    /// <summary>Gets the device channels consumed by UI during the current frame.</summary>
    public static UiInputCapture UiCapture { get; private set; }

    /// <summary>Gets whether any UI layout consumed input during this frame.</summary>
    public static bool IsCapturedByUi => UiCapture != UiInputCapture.None;

    /// <summary>Gets whether UI consumed pointer input during this frame.</summary>
    public static bool IsPointerCapturedByUi =>
        UiCapture.HasFlag(UiInputCapture.Pointer);

    /// <summary>Gets whether UI consumed keyboard input during this frame.</summary>
    public static bool IsKeyboardCapturedByUi =>
        UiCapture.HasFlag(UiInputCapture.Keyboard);

    /// <summary>Gets whether UI consumed game-pad input during this frame.</summary>
    public static bool IsGamePadCapturedByUi =>
        UiCapture.HasFlag(UiInputCapture.GamePad);

    #endregion

    #region Fields

    // --- Keyboard ---
    private static KeyboardState _prevKb;
    private static KeyboardState _currKb;

    // --- Mouse ---
    private static MouseState _prevMs;
    private static MouseState _currMs;

    // --- Game pad ---
    private static GamePadState _prevGp;
    private static GamePadState _currGp;
    private static readonly System.Collections.Generic.List<char> _pendingTextInput = [];
    private static char[] _textInput = [];
    private static bool _textInputSubscribed;

    #endregion

    #region Initialization & Frame Hooks

    /// <summary>
    ///     Initialize cached input states. Call once at startup.
    /// </summary>
    public static void Init()
    {
        _prevKb = _currKb = Keyboard.GetState();
        _prevMs = _currMs = Mouse.GetState();
        _prevGp = _currGp = GamePad.GetState(PlayerIndex.One);

        if (!_textInputSubscribed && Core.Instance?.Window is not null)
        {
            Core.Instance.Window.TextInput += OnTextInput;
            _textInputSubscribed = true;
        }
    }

    /// <summary>
    ///     Call at the START of your Update loop to sample current states.
    /// </summary>
    public static void PreUpdate()
    {
        UiCapture = UiInputCapture.None;
        _currKb = Keyboard.GetState();
        _currMs = Mouse.GetState();
        _currGp = GamePad.GetState(PlayerIndex.One);
        _textInput = _pendingTextInput.ToArray();
        _pendingTextInput.Clear();
    }

    /// <summary>
    ///     Call at the END of your Update loop to advance previous states.
    /// </summary>
    public static void PostUpdate()
    {
        _prevKb = _currKb;
        _prevMs = _currMs;
        _prevGp = _currGp;
    }

    internal static void CaptureForUi(UiInputCapture capture)
    {
        UiCapture |= capture;
    }

    #endregion

    #region Keyboard Helpers

    /// <summary>True on the frame a key transitions from Up to Down.</summary>
    public static bool IsKeyPressed(Keys key)
    {
        return !IsKeyboardCapturedByUi && IsRawKeyPressed(key);
    }

    /// <summary>True while the key is held down.</summary>
    public static bool IsKeyHeld(Keys key)
    {
        return !IsKeyboardCapturedByUi && IsRawKeyHeld(key);
    }

    /// <summary>True on the frame a key transitions from Down to Up.</summary>
    public static bool IsKeyReleased(Keys key)
    {
        return !IsKeyboardCapturedByUi && IsRawKeyReleased(key);
    }

    /// <summary>True while either Shift key is down.</summary>
    public static bool IsShiftDown()
    {
        return !IsKeyboardCapturedByUi && IsRawShiftDown();
    }

    /// <summary>True while either Ctrl key is down.</summary>
    public static bool IsCtrlDown()
    {
        return !IsKeyboardCapturedByUi &&
               (_currKb.IsKeyDown(Keys.LeftControl) ||
                _currKb.IsKeyDown(Keys.RightControl));
    }

    /// <summary>True while either Alt key is down.</summary>
    public static bool IsAltDown()
    {
        return !IsKeyboardCapturedByUi &&
               (_currKb.IsKeyDown(Keys.LeftAlt) ||
                _currKb.IsKeyDown(Keys.RightAlt));
    }

    internal static bool IsRawKeyPressed(Keys key)
    {
        return !_prevKb.IsKeyDown(key) && _currKb.IsKeyDown(key);
    }

    internal static bool IsRawKeyHeld(Keys key)
    {
        return _currKb.IsKeyDown(key);
    }

    internal static bool IsRawKeyReleased(Keys key)
    {
        return _prevKb.IsKeyDown(key) && !_currKb.IsKeyDown(key);
    }

    internal static bool IsRawShiftDown()
    {
        return _currKb.IsKeyDown(Keys.LeftShift) ||
               _currKb.IsKeyDown(Keys.RightShift);
    }

    internal static bool IsRawControlDown()
    {
        return _currKb.IsKeyDown(Keys.LeftControl) ||
               _currKb.IsKeyDown(Keys.RightControl);
    }

    internal static char[] GetRawTextInput()
    {
        return _textInput;
    }

    private static void OnTextInput(object sender, TextInputEventArgs args)
    {
        _pendingTextInput.Add(args.Character);
    }

    internal static Keys[] GetRawPressedKeys()
    {
        var heldKeys = _currKb.GetPressedKeys();
        var pressedKeys = new System.Collections.Generic.List<Keys>(heldKeys.Length);
        foreach (var key in heldKeys)
        {
            if (!_prevKb.IsKeyDown(key))
                pressedKeys.Add(key);
        }

        return pressedKeys.ToArray();
    }

    internal static Keys[] GetRawReleasedKeys()
    {
        var previousKeys = _prevKb.GetPressedKeys();
        var releasedKeys = new System.Collections.Generic.List<Keys>(previousKeys.Length);
        foreach (var key in previousKeys)
        {
            if (!_currKb.IsKeyDown(key))
                releasedKeys.Add(key);
        }

        return releasedKeys.ToArray();
    }

    #endregion

    #region Mouse Helpers

    /// <summary>Returns the current mouse position in window coordinates.</summary>
    public static Vector2 GetMousePosition()
    {
        return _currMs.Position.ToVector2();
    }

    /// <summary>Returns per-frame mouse movement delta (pixels).</summary>
    public static Vector2 GetMouseDelta()
    {
        if (IsPointerCapturedByUi)
            return Vector2.Zero;

        return GetRawMouseDelta();
    }

    internal static Vector2 GetRawMouseDelta()
    {
        var dx = _currMs.X - _prevMs.X;
        var dy = _currMs.Y - _prevMs.Y;
        return new Vector2(dx, dy);
    }

    /// <summary>
    ///     Returns scroll delta this frame (Positive = up, Negative = down).
    /// </summary>
    public static int GetScrollDelta()
    {
        return IsPointerCapturedByUi
            ? 0
            : GetRawScrollDelta();
    }

    internal static int GetRawScrollDelta()
    {
        return _currMs.ScrollWheelValue - _prevMs.ScrollWheelValue;
    }

    /// <summary>True if the current mouse position is within the client bounds.</summary>
    public static bool IsMouseInWindow()
    {
        var p = new Point(_currMs.X, _currMs.Y);
        return WindowClientBounds.Contains(p);
    }

    /// <summary>True on the frame the specified mouse button is pressed.</summary>
    public static bool IsMousePressed(MouseButton button)
    {
        return !IsPointerCapturedByUi && IsRawMousePressed(button);
    }

    /// <summary>True on the frame the specified mouse button is released.</summary>
    public static bool IsMouseReleased(MouseButton button)
    {
        return !IsPointerCapturedByUi && IsRawMouseReleased(button);
    }

    /// <summary>True while the specified mouse button is held.</summary>
    public static bool IsMouseHeld(MouseButton button)
    {
        return !IsPointerCapturedByUi && IsRawMouseHeld(button);
    }

    internal static bool IsRawMousePressed(MouseButton button)
    {
        return !WasDown(_prevMs, button) && IsDown(_currMs, button);
    }

    internal static bool IsRawMouseReleased(MouseButton button)
    {
        return WasDown(_prevMs, button) && !IsDown(_currMs, button);
    }

    internal static bool IsRawMouseHeld(MouseButton button)
    {
        return IsDown(_currMs, button);
    }

    #region Convenience Buttons

    /// <summary>Left button pressed this frame.</summary>
    public static bool LeftPressed()
    {
        return IsMousePressed(MouseButton.Left);
    }

    /// <summary>Left button released this frame.</summary>
    public static bool LeftReleased()
    {
        return IsMouseReleased(MouseButton.Left);
    }

    /// <summary>Left button held.</summary>
    public static bool LeftHeld()
    {
        return IsMouseHeld(MouseButton.Left);
    }

    /// <summary>Right button pressed this frame.</summary>
    public static bool RightPressed()
    {
        return IsMousePressed(MouseButton.Right);
    }

    /// <summary>Right button released this frame.</summary>
    public static bool RightReleased()
    {
        return IsMouseReleased(MouseButton.Right);
    }

    /// <summary>Right button held.</summary>
    public static bool RightHeld()
    {
        return IsMouseHeld(MouseButton.Right);
    }

    /// <summary>Middle button pressed this frame.</summary>
    public static bool MiddlePressed()
    {
        return IsMousePressed(MouseButton.Middle);
    }

    /// <summary>Middle button released this frame.</summary>
    public static bool MiddleReleased()
    {
        return IsMouseReleased(MouseButton.Middle);
    }

    /// <summary>Middle button held.</summary>
    public static bool MiddleHeld()
    {
        return IsMouseHeld(MouseButton.Middle);
    }

    #endregion

    /// <summary>
    ///     True if the specified button is down and the mouse moved since last frame.
    /// </summary>
    public static bool IsDragging(MouseButton button)
    {
        return IsMouseHeld(button) && (_currMs.X != _prevMs.X || _currMs.Y != _prevMs.Y);
    }

    #endregion

    #region Game Pad Helpers

    /// <summary>Gets whether the primary game pad is connected.</summary>
    public static bool IsGamePadConnected()
    {
        return _currGp.IsConnected;
    }

    /// <summary>Gets whether a primary game-pad button was pressed this frame.</summary>
    public static bool IsGamePadButtonPressed(Buttons button)
    {
        return !IsGamePadCapturedByUi && IsRawGamePadButtonPressed(button);
    }

    /// <summary>Gets whether a primary game-pad button is held.</summary>
    public static bool IsGamePadButtonHeld(Buttons button)
    {
        return !IsGamePadCapturedByUi && IsRawGamePadButtonHeld(button);
    }

    /// <summary>Gets whether a primary game-pad button was released this frame.</summary>
    public static bool IsGamePadButtonReleased(Buttons button)
    {
        return !IsGamePadCapturedByUi && IsRawGamePadButtonReleased(button);
    }

    /// <summary>Gets the primary game pad's left stick, or zero when UI consumed it.</summary>
    public static Vector2 GetGamePadLeftStick()
    {
        return IsGamePadCapturedByUi
            ? Vector2.Zero
            : _currGp.ThumbSticks.Left;
    }

    internal static bool IsRawGamePadButtonPressed(Buttons button)
    {
        return !_prevGp.IsButtonDown(button) && _currGp.IsButtonDown(button);
    }

    internal static bool IsRawGamePadButtonHeld(Buttons button)
    {
        return _currGp.IsButtonDown(button);
    }

    internal static bool IsRawGamePadButtonReleased(Buttons button)
    {
        return _prevGp.IsButtonDown(button) && !_currGp.IsButtonDown(button);
    }

    internal static Vector2 GetRawGamePadLeftStick()
    {
        return _currGp.ThumbSticks.Left;
    }

    internal static bool IsRawLeftStickDirectionPressed(
        UiNavigationDirection direction,
        float threshold = 0.5f)
    {
        var previous = _prevGp.ThumbSticks.Left;
        var current = _currGp.ThumbSticks.Left;

        return direction switch
        {
            UiNavigationDirection.Left =>
                current.X <= -threshold && previous.X > -threshold,
            UiNavigationDirection.Right =>
                current.X >= threshold && previous.X < threshold,
            UiNavigationDirection.Up =>
                current.Y >= threshold && previous.Y < threshold,
            UiNavigationDirection.Down =>
                current.Y <= -threshold && previous.Y > -threshold,
            _ => false
        };
    }

    #endregion

    #region Internals

    /// <summary>Returns true if the button is down for the given <see cref="MouseState" />.</summary>
    private static bool IsDown(in MouseState ms, MouseButton button)
    {
        return GetButtonState(ms, button) == ButtonState.Pressed;
    }

    /// <summary>Returns true if the button is down for the given <see cref="MouseState" /> (alias for clarity).</summary>
    private static bool WasDown(in MouseState ms, MouseButton button)
    {
        return GetButtonState(ms, button) == ButtonState.Pressed;
    }

    /// <summary>Maps <see cref="MouseButton" /> to the corresponding state in <see cref="MouseState" />.</summary>
    private static ButtonState GetButtonState(in MouseState ms, MouseButton button)
    {
        switch (button)
        {
            case MouseButton.Left: return ms.LeftButton;
            case MouseButton.Right: return ms.RightButton;
            case MouseButton.Middle: return ms.MiddleButton;
            case MouseButton.Button1: return ms.XButton1;
            case MouseButton.Button2: return ms.XButton2;
            default: return ButtonState.Released;
        }
    }

    #endregion
}
