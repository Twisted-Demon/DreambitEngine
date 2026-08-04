using System.Xml;

namespace Dreambit.UI;

/// <summary>
///     A full-surface content control used for dimmers and modal dialogs. When
///     blocking is enabled it prevents pointer and keyboard input reaching UI or
///     gameplay beneath it.
/// </summary>
public class UiOverlay : UiContentControl
{
    /// <summary>Creates an input-blocking overlay.</summary>
    public UiOverlay()
    {
        BlocksInput = true;
        IsFocusable = true;
        CapturesKeyboardInput = true;
    }

    /// <summary>Gets or sets whether this overlay blocks lower input.</summary>
    public bool BlocksInput
    {
        get => IsHitTestVisible;
        set
        {
            IsHitTestVisible = value;
            CapturesKeyboardInput = value;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(UiPointerEventArgs args)
    {
        if (BlocksInput)
            args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyPressed(UiKeyEventArgs args)
    {
        if (BlocksInput)
            args.Handled = true;
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        BlocksInput = UiXmlParser.ParseBool(node, "blocks-input", true);
    }
}