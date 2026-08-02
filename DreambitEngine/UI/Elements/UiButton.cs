using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
/// A single-content control that tracks pointer interaction, raises click
/// events, and changes its background tint for hover and pressed states.
/// </summary>
public class UiButton : UiContentControl
{
    private bool _pressStartedInside;

    /// <summary>Raised after a press begins inside and is released inside the button.</summary>
    public event Action<UiButton> Clicked;

    /// <summary>Gets whether the pointer is currently inside the button bounds.</summary>
    public bool IsHovered { get; private set; }
    /// <summary>Gets whether an inside press is currently held.</summary>
    public bool IsPressed { get; private set; }
    /// <summary>Gets or sets the background tint used while hovered.</summary>
    public Color HoverTint { get; set; } = Color.LightGray;
    /// <summary>Gets or sets the background tint used while pressed.</summary>
    public Color PressedTint { get; set; } = Color.Gray;

    /// <inheritdoc />
    protected override void OnUpdate(in UiInputState input)
    {
        base.OnUpdate(input);

        IsHovered = input.PointerInWindow &&
                    Bounds.Contains(input.PointerPosition.ToPoint());

        if (input.PrimaryPressed)
            _pressStartedInside = IsHovered;

        IsPressed = _pressStartedInside && input.PrimaryHeld;

        if (!input.PrimaryReleased)
            return;

        if (_pressStartedInside && IsHovered)
            Clicked?.Invoke(this);

        _pressStartedInside = false;
        IsPressed = false;
    }

    /// <inheritdoc />
    protected override Color GetBackgroundTint()
    {
        return IsPressed
            ? PressedTint
            : IsHovered
                ? HoverTint
                : BackgroundTint;
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        
        if (node.Attributes?["hover-tint"] is not null)
            HoverTint = UiXmlParser.ParseColor(node, "hover-tint");

        if (node.Attributes?["pressed-tint"] is not null)
            PressedTint = UiXmlParser.ParseColor(node, "pressed-tint");
    }
    
}
