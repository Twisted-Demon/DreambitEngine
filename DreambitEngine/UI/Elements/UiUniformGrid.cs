using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>Arranges visible children into equally sized cells.</summary>
public sealed class UiUniformGrid : UiContainer
{
    /// <summary>Gets or sets the requested row count, or zero to calculate it.</summary>
    public int Rows { get; set; }
    /// <summary>Gets or sets the requested column count, or zero to calculate it.</summary>
    public int Columns { get; set; }
    /// <summary>Gets or sets the horizontal gap between cells.</summary>
    public int ColumnSpacing { get; set; }
    /// <summary>Gets or sets the vertical gap between cells.</summary>
    public int RowSpacing { get; set; }
    /// <summary>Gets or sets the inner inset around the cells.</summary>
    public UiThickness Padding { get; set; }

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        ResolveDimensions(out var rows, out var columns);
        if (rows == 0 || columns == 0)
            return new Point(Padding.Horizontal, Padding.Vertical);

        var inner = new Point(
            Math.Max(0, availableSize.X - Padding.Horizontal),
            Math.Max(0, availableSize.Y - Padding.Vertical));
        var cellAvailable = new Point(
            Math.Max(0, (inner.X - ColumnSpacing * (columns - 1)) / columns),
            Math.Max(0, (inner.Y - RowSpacing * (rows - 1)) / rows));
        var maximum = Point.Zero;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Measure(cellAvailable);
            maximum.X = Math.Max(maximum.X, child.DesiredSize.X);
            maximum.Y = Math.Max(maximum.Y, child.DesiredSize.Y);
        }

        return new Point(
            maximum.X * columns + ColumnSpacing * (columns - 1) + Padding.Horizontal,
            maximum.Y * rows + RowSpacing * (rows - 1) + Padding.Vertical);
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
        ResolveDimensions(out var rows, out var columns);
        if (rows == 0 || columns == 0)
            return;

        var inner = new Rectangle(
            Bounds.X + Padding.Left,
            Bounds.Y + Padding.Top,
            Math.Max(0, Bounds.Width - Padding.Horizontal),
            Math.Max(0, Bounds.Height - Padding.Vertical));
        var cellWidth = Math.Max(
            0,
            (inner.Width - ColumnSpacing * (columns - 1)) / columns);
        var cellHeight = Math.Max(
            0,
            (inner.Height - RowSpacing * (rows - 1)) / rows);
        var index = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var row = index / columns;
            var column = index % columns;
            child.Arrange(new Rectangle(
                inner.X + column * (cellWidth + ColumnSpacing),
                inner.Y + row * (cellHeight + RowSpacing),
                cellWidth,
                cellHeight));
            index++;
        }
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        Rows = Math.Max(0, UiXmlParser.ParseInt(node, "rows", 0));
        Columns = Math.Max(0, UiXmlParser.ParseInt(node, "columns", 0));
        var spacing = Math.Max(0, UiXmlParser.ParseInt(node, "spacing", 0));
        ColumnSpacing = Math.Max(
            0,
            UiXmlParser.ParseInt(node, "column-spacing", spacing));
        RowSpacing = Math.Max(
            0,
            UiXmlParser.ParseInt(node, "row-spacing", spacing));
        Padding = UiXmlParser.ParseThickness(
            UiXmlParser.ParseString(node, "padding", "0"),
            "Uniform-grid padding");
    }

    private void ResolveDimensions(out int rows, out int columns)
    {
        var count = 0;
        foreach (var child in Children)
        {
            if (child.IsVisible) count++;
        }

        rows = Rows;
        columns = Columns;
        if (count == 0)
        {
            rows = columns = 0;
            return;
        }

        if (rows == 0 && columns == 0)
        {
            columns = (int)MathF.Ceiling(MathF.Sqrt(count));
            rows = (int)MathF.Ceiling(count / (float)columns);
        }
        else if (rows == 0)
        {
            rows = (int)MathF.Ceiling(count / (float)columns);
        }
        else if (columns == 0)
        {
            columns = (int)MathF.Ceiling(count / (float)rows);
        }
    }
}
