using Microsoft.Xna.Framework.Input;

namespace PixelariaEngine.Core.Input;

public static class InputManager
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
        return _currentKeyboardState.IsKeyDown(key);
    }

    public static bool IsKeyHeld(Keys key)
    {
        var wasKeyDownInPreviousState = _previousKeyboardState.IsKeyDown(key);
        var isKeyDownInCurrentState = _currentKeyboardState.IsKeyDown(key);
        
        return wasKeyDownInPreviousState && isKeyDownInCurrentState;
    }

    public static bool IsKeyReleased(Keys key)
    {
        var wasKeyDownInPreviousState = _previousKeyboardState.IsKeyDown(key);
        var isKeyDownInCurrentState = _currentKeyboardState.IsKeyDown(key);
        
        return wasKeyDownInPreviousState && !isKeyDownInCurrentState;
    }
}