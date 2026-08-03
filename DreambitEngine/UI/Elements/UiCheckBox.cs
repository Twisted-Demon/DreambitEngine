using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>A toggle button with a brush-composed checked indicator.</summary>
public class UiCheckBox : UiToggleButton
{
    private IUiBrush _indicatorBrush = new SolidColorBrush();
    private IUiBrush _markBrush = new SolidColorBrush();

    /// <summary>Gets or sets the brush used for the indicator box.</summary>
    public IUiBrush IndicatorBrush
    {
        get => _indicatorBrush;
        set => _indicatorBrush = value;
    }

    /// <summary>Gets or sets the brush used for the checked mark.</summary>
    public IUiBrush MarkBrush
    {
        get => _markBrush;
        set => _markBrush = value;
    }

    /// <summary>Gets or sets the square indicator size.</summary>
    public int IndicatorSize { get; set; } = 18;
    /// <summary>Gets or sets the gap between the indicator and content.</summary>
    public int IndicatorSpacing { get; set; } = 6;
    /// <summary>Gets or sets the unchecked indicator tint.</summary>
    public Color IndicatorTint { get; set; } = Color.Gray;
    /// <summary>Gets or sets the checked mark tint.</summary>
    public Color MarkTint { get; set; } = Color.White;

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        var contentAvailable = new Point(
            Math.Max(0, availableSize.X - IndicatorSize - IndicatorSpacing),
            availableSize.Y);
        var contentSize = base.MeasureContent(contentAvailable);
        return new Point(
            contentSize.X + IndicatorSize + IndicatorSpacing,
            Math.Max(contentSize.Y, IndicatorSize));
    }

    /// <inheritdoc />
    public override void Arrange(Rectangle parentBounds)
    {
        if (!IsEffectivelyVisible)
        {
            Bounds = Rectangle.Empty;
            return;
        }

        ArrangeSelf(parentBounds, Width.IsAuto || Height.IsAuto);
        if (Content is null)
            return;

        var contentBounds = new Rectangle(
            Bounds.X + IndicatorSize + IndicatorSpacing + Padding.Left,
            Bounds.Y + Padding.Top,
            Math.Max(
                0,
                Bounds.Width - IndicatorSize - IndicatorSpacing -
                Padding.Horizontal),
            Math.Max(0, Bounds.Height - Padding.Vertical));
        Content.X = UiLength.Pixels(0);
        Content.Y = UiLength.Pixels(0);
        Content.Anchor = ContentAlignment;
        Content.Origin = ContentAlignment;
        Content.Arrange(contentBounds);
    }

    /// <inheritdoc />
    public override void ResolveDependencies()
    {
        base.ResolveDependencies();
        IndicatorBrush?.ResolveDependencies();
        MarkBrush?.ResolveDependencies();
    }

    /// <inheritdoc />
    public override void OnDraw()
    {
        base.OnDraw();
        DrawIndicator(GetIndicatorBounds());
    }

    /// <summary>Draws the indicator and checked mark.</summary>
    protected virtual void DrawIndicator(Rectangle indicatorBounds)
    {
        IndicatorBrush?.Draw(indicatorBounds, IndicatorTint);
        if (!IsChecked)
            return;

        var inset = Math.Max(2, IndicatorSize / 4);
        MarkBrush?.Draw(
            new Rectangle(
                indicatorBounds.X + inset,
                indicatorBounds.Y + inset,
                Math.Max(0, indicatorBounds.Width - inset * 2),
                Math.Max(0, indicatorBounds.Height - inset * 2)),
            MarkTint);
    }

    /// <summary>Gets the centered indicator rectangle.</summary>
    protected Rectangle GetIndicatorBounds()
    {
        return new Rectangle(
            Bounds.X,
            Bounds.Center.Y - IndicatorSize / 2,
            IndicatorSize,
            IndicatorSize);
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        IndicatorSize = Math.Max(
            1,
            UiXmlParser.ParseInt(node, "indicator-size", 18));
        IndicatorSpacing = Math.Max(
            0,
            UiXmlParser.ParseInt(node, "indicator-spacing", 6));
        if (node.Attributes?["indicator-tint"] is not null)
            IndicatorTint = UiXmlParser.ParseColor(node, "indicator-tint");
        if (node.Attributes?["mark-tint"] is not null)
            MarkTint = UiXmlParser.ParseColor(node, "mark-tint");
    }
}
