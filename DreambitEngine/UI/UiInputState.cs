using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
/// Contains the pointer and primary-button state supplied to a UI layout for
/// one update.
/// </summary>
/// <param name="PointerPosition">The pointer position in UI coordinates.</param>
/// <param name="PointerInWindow">Whether the pointer is inside the game window.</param>
/// <param name="PrimaryPressed">Whether the primary button was pressed this update.</param>
/// <param name="PrimaryHeld">Whether the primary button is currently held.</param>
/// <param name="PrimaryReleased">Whether the primary button was released this update.</param>
public readonly record struct UiInputState(
    Vector2 PointerPosition,
    bool PointerInWindow,
    bool PrimaryPressed,
    bool PrimaryHeld,
    bool PrimaryReleased);
