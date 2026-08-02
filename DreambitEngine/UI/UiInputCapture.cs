using System;

namespace Dreambit.UI;

/// <summary>Identifies device channels consumed by a UI layout for one frame.</summary>
[Flags]
public enum UiInputCapture
{
    /// <summary>No device input was consumed.</summary>
    None = 0,
    /// <summary>Mouse or pointer input was consumed.</summary>
    Pointer = 1 << 0,
    /// <summary>Keyboard input was consumed.</summary>
    Keyboard = 1 << 1,
    /// <summary>Game-pad input was consumed.</summary>
    GamePad = 1 << 2,
    /// <summary>All supported input channels were consumed.</summary>
    All = Pointer | Keyboard | GamePad
}

/// <summary>Identifies the device that produced a UI navigation command.</summary>
public enum UiInputDevice
{
    /// <summary>No device produced the command.</summary>
    None,
    /// <summary>The command came from the keyboard.</summary>
    Keyboard,
    /// <summary>The command came from a game pad.</summary>
    GamePad
}

/// <summary>Specifies a direction for spatial focus navigation.</summary>
public enum UiNavigationDirection
{
    /// <summary>Moves focus toward the left.</summary>
    Left,
    /// <summary>Moves focus toward the right.</summary>
    Right,
    /// <summary>Moves focus upward.</summary>
    Up,
    /// <summary>Moves focus downward.</summary>
    Down
}
