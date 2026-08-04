using System;
using System.Xml;

namespace Dreambit.UI;

/// <summary>
///     A single-content control that tracks pointer interaction, raises click
///     events, and changes its background tint for hover and pressed states.
/// </summary>
public class UiButton : UiControl
{
    private bool _pressStartedInside;

    /// <summary>Creates a focusable button that participates in pointer hit testing.</summary>
    public UiButton()
    {
        IsFocusable = true;
    }

    /// <summary>Gets whether the pointer is currently inside the button bounds.</summary>
    public bool IsHovered { get; private set; }

    /// <summary>Gets whether an inside press is currently held.</summary>
    public bool IsPressed { get; private set; }

    /// <inheritdoc />
    protected override bool IsPressedForVisualState => IsPressed;

    /// <summary>Raised after a press begins inside and is released inside the button.</summary>
    public event Action<UiButton> Clicked;

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
            OnClick();

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
        OnClick();
        args.Handled = true;
    }

    /// <inheritdoc />
    protected internal override void OnPointerCaptureLost()
    {
        _pressStartedInside = false;
        IsPressed = false;
    }

    /// <summary>Raises the click event and provides an override point for derived controls.</summary>
    protected virtual void OnClick()
    {
        Clicked?.Invoke(this);
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
    }
}