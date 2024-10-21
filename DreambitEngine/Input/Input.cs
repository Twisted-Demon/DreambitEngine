using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit;

public static class Input
{
    private static KeyboardState _previousKeyboardState;
    private static KeyboardState _currentKeyboardState;

    public static void Init()
    {
        _previousKeyboardState = Keyboard.GetState();
        _currentKeyboardState = Keyboard.GetState();
    }

    public static void PreUpdate()
    {
        _currentKeyboardState = Keyboard.GetState();
    }

    public static void PostUpdate()
    {
        _previousKeyboardState = _currentKeyboardState;
    }

    public static bool IsKeyPressed(Keys key)
    {
        var wasKeyDownInPreviousState = _previousKeyboardState.IsKeyDown(key);
        var isKeyDownInCurrentState = _currentKeyboardState.IsKeyDown(key);

        return !wasKeyDownInPreviousState && isKeyDownInCurrentState;
    }

    public static bool IsKeyHeld(Keys key)
    {
        return _currentKeyboardState.IsKeyDown(key);
    }

    public static bool IsKeyReleased(Keys key)
    {
        var wasKeyDownInPreviousState = _previousKeyboardState.IsKeyDown(key);
        var isKeyDownInCurrentState = _currentKeyboardState.IsKeyDown(key);

        return wasKeyDownInPreviousState && !isKeyDownInCurrentState;
    }

    public static Vector2 GetMousePosition()
    {
        var ms = Mouse.GetState();
        return ms.Position.ToVector2();
    }

    public static bool IsMouseInWindow()
    {
        var mousePosition = Mouse.GetState().Position.ToVector2();
        return Core.Instance.Window.ClientBounds.Contains(mousePosition);
    }
}