using System;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>Identifies the simultaneous interaction states of a UI control.</summary>
[Flags]
public enum UiControlState
{
    /// <summary>The control has no special interaction state.</summary>
    Normal = 0,
    /// <summary>The pointer is over the control.</summary>
    Hovered = 1 << 0,
    /// <summary>The control is actively pressed or dragged.</summary>
    Pressed = 1 << 1,
    /// <summary>The control cannot receive input.</summary>
    Disabled = 1 << 2,
    /// <summary>The control owns keyboard/controller focus.</summary>
    Focused = 1 << 3,
    /// <summary>The control is toggled on.</summary>
    Checked = 1 << 4,
    /// <summary>The control is selected by a selector.</summary>
    Selected = 1 << 5,
    /// <summary>The control currently has expanded popup content.</summary>
    Open = 1 << 6
}

/// <summary>
/// Stores state-specific background tints independently from control behavior.
/// Unset colors fall back through the control's normal tint.
/// </summary>
public sealed class UiControlStyle
{
    /// <summary>Gets or sets the normal control tint.</summary>
    public Color NormalTint { get; set; } = Color.White;
    /// <summary>Gets or sets the pointer-hover tint.</summary>
    public Color? HoveredTint { get; set; }
    /// <summary>Gets or sets the pressed tint.</summary>
    public Color? PressedTint { get; set; }
    /// <summary>Gets or sets the disabled tint.</summary>
    public Color? DisabledTint { get; set; }
    /// <summary>Gets or sets the focused tint.</summary>
    public Color? FocusedTint { get; set; }
    /// <summary>Gets or sets the checked tint.</summary>
    public Color? CheckedTint { get; set; }
    /// <summary>Gets or sets the selected tint.</summary>
    public Color? SelectedTint { get; set; }
    /// <summary>Gets or sets the expanded/open tint.</summary>
    public Color? OpenTint { get; set; }

    /// <summary>Resolves the most specific configured tint for a state set.</summary>
    public Color Resolve(UiControlState state)
    {
        if (state.HasFlag(UiControlState.Disabled) && DisabledTint.HasValue)
            return DisabledTint.Value;
        if (state.HasFlag(UiControlState.Pressed) && PressedTint.HasValue)
            return PressedTint.Value;
        if (state.HasFlag(UiControlState.Open) && OpenTint.HasValue)
            return OpenTint.Value;
        if (state.HasFlag(UiControlState.Checked) && CheckedTint.HasValue)
            return CheckedTint.Value;
        if (state.HasFlag(UiControlState.Selected) && SelectedTint.HasValue)
            return SelectedTint.Value;
        if (state.HasFlag(UiControlState.Hovered) && HoveredTint.HasValue)
            return HoveredTint.Value;
        if (state.HasFlag(UiControlState.Focused) && FocusedTint.HasValue)
            return FocusedTint.Value;
        return NormalTint;
    }
}
