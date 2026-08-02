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

    /// <summary>Creates a focusable button that participates in pointer hit testing.</summary>
    public UiButton()
    {
        IsFocusable = true;
    }

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
    /// <summary>Gets or sets the background tint used while keyboard/controller focused.</summary>
    public Color FocusedTint { get; set; } = Color.LightGray;

    /// <inheritdoc />
    protected override void OnPointerPressed(UiPointerEventArgs args)
    {
        _pressStartedInside = args.IsInside;
        IsPressed = _pressStartedInside;

        if (_pressStartedInside)
            args.CapturePointer();

        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(UiPointerEventArgs args)
    {
        if (_pressStartedInside && IsPointerOver)
            Clicked?.Invoke(this);

        _pressStartedInside = false;
        IsPressed = false;
        args.ReleasePointerCapture();
        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerOverChanged(bool isPointerOver)
    {
        IsHovered = isPointerOver;
    }

    /// <inheritdoc />
    protected override void OnActivated(UiCommandEventArgs args)
    {
        Clicked?.Invoke(this);
        args.Handled = true;
    }

    /// <inheritdoc />
    protected internal override void OnPointerCaptureLost()
    {
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
                : IsFocused
                    ? FocusedTint
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

        if (node.Attributes?["focused-tint"] is not null)
            FocusedTint = UiXmlParser.ParseColor(node, "focused-tint");
    }
    
}
