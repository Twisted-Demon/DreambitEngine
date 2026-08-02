using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public readonly record struct UiInputState(
    Vector2 PointerPosition,
    bool PointerInWindow,
    bool PrimaryPressed,
    bool PrimaryHeld,
    bool PrimaryReleased);
