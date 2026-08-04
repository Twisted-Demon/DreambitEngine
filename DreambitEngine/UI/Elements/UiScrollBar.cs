using System;
using System.Xml;

namespace Dreambit.UI;

/// <summary>A slider whose thumb represents a viewport within a larger extent.</summary>
public sealed class UiScrollBar : UiSlider
{
    /// <summary>Gets or sets the visible extent represented by the thumb.</summary>
    public float ViewportSize { get; set; }

    /// <summary>Gets or sets the increment used for wheel and page movement.</summary>
    public float LargeChange { get; set; } = 10f;

    /// <summary>Gets or sets the minimum thumb length.</summary>
    public int MinimumThumbSize { get; set; } = 10;

    /// <inheritdoc />
    protected override int GetThumbLength()
    {
        var mainLength = Orientation == StackOrientation.Horizontal
            ? Bounds.Width
            : Bounds.Height;
        var extent = Maximum - Minimum + Math.Max(0f, ViewportSize);
        if (extent <= 0f || ViewportSize <= 0f)
            return base.GetThumbLength();

        return Math.Clamp(
            (int)MathF.Round(mainLength * ViewportSize / extent),
            Math.Min(MinimumThumbSize, mainLength),
            mainLength);
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(UiPointerEventArgs args)
    {
        Value -= Math.Sign(args.WheelDelta) * LargeChange;
        args.Handled = true;
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        ViewportSize = Math.Max(
            0f,
            UiXmlParser.ParseFloat(node, "viewport-size"));
        LargeChange = Math.Max(
            0f,
            UiXmlParser.ParseFloat(node, "large-change", 10f));
        MinimumThumbSize = Math.Max(
            1,
            UiXmlParser.ParseInt(node, "minimum-thumb-size", 10));
    }
}