using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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
    ///     Cached window client bounds for hit-testing the mouse position.
    /// </summary>
    public static Rectangle WindowClientBounds => Core.Instance.Window.ClientBounds;

    #endregion

    #region Fields

    // --- Keyboard ---
    private static KeyboardState _prevKb;
    private static KeyboardState _currKb;

    // --- Mouse ---
    private static MouseState _prevMs;
    private static MouseState _currMs;

    #endregion

    #region Initialization & Frame Hooks

    /// <summary>
    ///     Initialize cached input states. Call once at startup.
    /// </summary>
    public static void Init()
    {
        _prevKb = _currKb = Keyboard.GetState();
        _prevMs = _currMs = Mouse.GetState();
    }

    /// <summary>
    ///     Call at the START of your Update loop to sample current states.
    /// </summary>
    public static void PreUpdate()
    {
        _currKb = Keyboard.GetState();
        _currMs = Mouse.GetState();
    }

    /// <summary>
    ///     Call at the END of your Update loop to advance previous states.
    /// </summary>
    public static void PostUpdate()
    {
        _prevKb = _currKb;
        _prevMs = _currMs;
    }

    #endregion

    #region Keyboard Helpers

    /// <summary>True on the frame a key transitions from Up to Down.</summary>
    public static bool IsKeyPressed(Keys key)
    {
        return !_prevKb.IsKeyDown(key) && _currKb.IsKeyDown(key);
    }

    /// <summary>True while the key is held down.</summary>
    public static bool IsKeyHeld(Keys key)
    {
        return _currKb.IsKeyDown(key);
    }

    /// <summary>True on the frame a key transitions from Down to Up.</summary>
    public static bool IsKeyReleased(Keys key)
    {
        return _prevKb.IsKeyDown(key) && !_currKb.IsKeyDown(key);
    }

    /// <summary>True while either Shift key is down.</summary>
    public static bool IsShiftDown()
    {
        return _currKb.IsKeyDown(Keys.LeftShift) || _currKb.IsKeyDown(Keys.RightShift);
    }

    /// <summary>True while either Ctrl key is down.</summary>
    public static bool IsCtrlDown()
    {
        return _currKb.IsKeyDown(Keys.LeftControl) || _currKb.IsKeyDown(Keys.RightControl);
    }

    /// <summary>True while either Alt key is down.</summary>
    public static bool IsAltDown()
    {
        return _currKb.IsKeyDown(Keys.LeftAlt) || _currKb.IsKeyDown(Keys.RightAlt);
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
        var dx = _currMs.X - _prevMs.X;
        var dy = _currMs.Y - _prevMs.Y;
        return new Vector2(dx, dy);
    }

    /// <summary>
    ///     Returns scroll delta this frame (Positive = up, Negative = down).
    /// </summary>
    public static int GetScrollDelta()
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
        return !WasDown(_prevMs, button) && IsDown(_currMs, button);
    }

    /// <summary>True on the frame the specified mouse button is released.</summary>
    public static bool IsMouseReleased(MouseButton button)
    {
        return WasDown(_prevMs, button) && !IsDown(_currMs, button);
    }

    /// <summary>True while the specified mouse button is held.</summary>
    public static bool IsMouseHeld(MouseButton button)
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