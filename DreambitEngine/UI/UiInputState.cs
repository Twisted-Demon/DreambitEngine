using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.UI;

/// <summary>
/// Contains raw pointer, keyboard, and navigation state supplied to a UI
/// layout for one update.
/// </summary>
/// <param name="PointerPosition">The pointer position in UI coordinates.</param>
/// <param name="PointerInWindow">Whether the pointer is inside the game window.</param>
/// <param name="PrimaryPressed">Whether the primary button was pressed this update.</param>
/// <param name="PrimaryHeld">Whether the primary button is currently held.</param>
/// <param name="PrimaryReleased">Whether the primary button was released this update.</param>
/// <param name="ScrollDelta">The pointer-wheel movement for this update.</param>
/// <param name="PressedKeys">Keys pressed during this update.</param>
/// <param name="ReleasedKeys">Keys released during this update.</param>
/// <param name="NavigationDirection">The requested spatial focus direction.</param>
/// <param name="NavigationDevice">The device that requested spatial navigation.</param>
/// <param name="FocusNext">Whether sequential focus should move forward.</param>
/// <param name="FocusPrevious">Whether sequential focus should move backward.</param>
/// <param name="ActivateKeyboard">Whether the keyboard requested activation.</param>
/// <param name="ActivateGamePad">Whether the game pad requested activation.</param>
/// <param name="CancelKeyboard">Whether the keyboard requested cancellation.</param>
/// <param name="CancelGamePad">Whether the game pad requested cancellation.</param>
/// <param name="KeyboardNavigationHeld">Whether a keyboard navigation or command key is held.</param>
/// <param name="GamePadNavigationHeld">Whether a game-pad navigation or command control is held.</param>
public readonly record struct UiInputState(
    Vector2 PointerPosition,
    bool PointerInWindow,
    bool PrimaryPressed,
    bool PrimaryHeld,
    bool PrimaryReleased,
    int ScrollDelta,
    Keys[] PressedKeys,
    Keys[] ReleasedKeys,
    UiNavigationDirection? NavigationDirection,
    UiInputDevice NavigationDevice,
    bool FocusNext,
    bool FocusPrevious,
    bool ActivateKeyboard,
    bool ActivateGamePad,
    bool CancelKeyboard,
    bool CancelGamePad,
    bool KeyboardNavigationHeld,
    bool GamePadNavigationHeld);
