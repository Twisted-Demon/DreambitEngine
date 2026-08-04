using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
///     Arranges arbitrary child elements in fixed, percentage, content-sized, or
///     weighted rows and columns.
/// </summary>
public sealed class UiGrid : UiContainer
{
    /// <summary>Gets the row definitions from top to bottom.</summary>
    public IList<UiGridLength> RowDefinitions { get; } =
        new List<UiGridLength> { UiGridLength.Star() };

    /// <summary>Gets the column definitions from left to right.</summary>
    public IList<UiGridLength> ColumnDefinitions { get; } =
        new List<UiGridLength> { UiGridLength.Star() };

    /// <summary>Gets or sets the inset between the grid bounds and its tracks.</summary>
    public UiThickness Padding { get; set; }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        ParseDefinitions(
            UiXmlParser.ParseString(
                node,
                "rows",
                UiXmlParser.ParseString(node, "row-definitions", "*")),
            RowDefinitions,
            "rows");
        ParseDefinitions(
            UiXmlParser.ParseString(
                node,
                "columns",
                UiXmlParser.ParseString(node, "column-definitions", "*")),
            ColumnDefinitions,
            "columns");
        Padding = UiXmlParser.ParseThickness(
            UiXmlParser.ParseString(node, "padding", "0"),
            "Grid padding");
    }

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        var innerSize = new Point(
            Math.Max(0, availableSize.X - Padding.Horizontal),
            Math.Max(0, availableSize.Y - Padding.Vertical));
        var layout = CalculateTracks(innerSize);
        return new Point(
            layout.ColumnSizes.Sum() + Padding.Horizontal,
            layout.RowSizes.Sum() + Padding.Vertical);
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
        var innerBounds = new Rectangle(
            Bounds.X + Padding.Left,
            Bounds.Y + Padding.Top,
            Math.Max(0, Bounds.Width - Padding.Horizontal),
            Math.Max(0, Bounds.Height - Padding.Vertical));
        var layout = CalculateTracks(innerBounds.Size);
        var columnOffsets = CreateOffsets(layout.ColumnSizes);
        var rowOffsets = CreateOffsets(layout.RowSizes);

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            ResolvePlacement(
                child.GridColumn,
                child.GridColumnSpan,
                layout.ColumnSizes.Length,
                out var column,
                out var columnSpan);
            ResolvePlacement(
                child.GridRow,
                child.GridRowSpan,
                layout.RowSizes.Length,
                out var row,
                out var rowSpan);
            var cellBounds = new Rectangle(
                innerBounds.X + columnOffsets[column],
                innerBounds.Y + rowOffsets[row],
                Sum(layout.ColumnSizes, column, columnSpan),
                Sum(layout.RowSizes, row, rowSpan));
            child.Arrange(cellBounds);
        }
    }

    private GridTrackLayout CalculateTracks(Point availableSize)
    {
        EnsureDefinitions(RowDefinitions);
        EnsureDefinitions(ColumnDefinitions);

        var rowSizes = InitializeTracks(RowDefinitions, availableSize.Y);
        var columnSizes = InitializeTracks(ColumnDefinitions, availableSize.X);

        MeasureChildrenForAutoTracks(
            availableSize,
            rowSizes,
            columnSizes);
        DistributeStars(RowDefinitions, rowSizes, availableSize.Y);
        DistributeStars(ColumnDefinitions, columnSizes, availableSize.X);

        // Measure once more using the final cell constraints. This keeps
        // content-sized tracks correct when an element also spans a fixed or
        // star-sized track on the other axis.
        MeasureChildrenInFinalCells(rowSizes, columnSizes);
        GrowAutoTracks(RowDefinitions, rowSizes, false);
        GrowAutoTracks(ColumnDefinitions, columnSizes, true);
        DistributeStars(RowDefinitions, rowSizes, availableSize.Y);
        DistributeStars(ColumnDefinitions, columnSizes, availableSize.X);

        return new GridTrackLayout(rowSizes, columnSizes);
    }

    private void MeasureChildrenForAutoTracks(
        Point availableSize,
        int[] rowSizes,
        int[] columnSizes)
    {
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            ResolvePlacement(
                child.GridColumn,
                child.GridColumnSpan,
                columnSizes.Length,
                out var column,
                out var columnSpan);
            ResolvePlacement(
                child.GridRow,
                child.GridRowSpan,
                rowSizes.Length,
                out var row,
                out var rowSpan);
            child.Measure(new Point(
                GetInitialConstraint(
                    ColumnDefinitions,
                    columnSizes,
                    column,
                    columnSpan,
                    availableSize.X),
                GetInitialConstraint(
                    RowDefinitions,
                    rowSizes,
                    row,
                    rowSpan,
                    availableSize.Y)));
            GrowSpannedAutoTracks(
                ColumnDefinitions,
                columnSizes,
                column,
                columnSpan,
                child.DesiredSize.X);
            GrowSpannedAutoTracks(
                RowDefinitions,
                rowSizes,
                row,
                rowSpan,
                child.DesiredSize.Y);
        }
    }

    private void MeasureChildrenInFinalCells(
        IReadOnlyList<int> rowSizes,
        IReadOnlyList<int> columnSizes)
    {
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            ResolvePlacement(
                child.GridColumn,
                child.GridColumnSpan,
                columnSizes.Count,
                out var column,
                out var columnSpan);
            ResolvePlacement(
                child.GridRow,
                child.GridRowSpan,
                rowSizes.Count,
                out var row,
                out var rowSpan);
            child.Measure(new Point(
                Sum(columnSizes, column, columnSpan),
                Sum(rowSizes, row, rowSpan)));
        }
    }

    private void GrowAutoTracks(
        IList<UiGridLength> definitions,
        int[] sizes,
        bool horizontal)
    {
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            var start = horizontal ? child.GridColumn : child.GridRow;
            var span = horizontal ? child.GridColumnSpan : child.GridRowSpan;
            ResolvePlacement(start, span, sizes.Length, out start, out span);
            GrowSpannedAutoTracks(
                definitions,
                sizes,
                start,
                span,
                horizontal ? child.DesiredSize.X : child.DesiredSize.Y);
        }
    }

    private static int[] InitializeTracks(
        IList<UiGridLength> definitions,
        int available)
    {
        var sizes = new int[definitions.Count];
        for (var i = 0; i < definitions.Count; i++)
            sizes[i] = definitions[i].UnitType switch
            {
                UiGridUnitType.Pixel => (int)definitions[i].Value,
                UiGridUnitType.Percent =>
                    (int)(Math.Max(0, available) * definitions[i].Value),
                _ => 0
            };

        return sizes;
    }

    private static int GetInitialConstraint(
        IList<UiGridLength> definitions,
        IReadOnlyList<int> sizes,
        int start,
        int span,
        int available)
    {
        for (var i = start; i < start + span; i++)
            if (definitions[i].UnitType is UiGridUnitType.Auto or
                UiGridUnitType.Star)
                return Math.Max(0, available);

        return Sum(sizes, start, span);
    }

    private static void GrowSpannedAutoTracks(
        IList<UiGridLength> definitions,
        int[] sizes,
        int start,
        int span,
        int desiredSize)
    {
        var current = Sum(sizes, start, span);
        var deficit = desiredSize - current;
        if (deficit <= 0)
            return;

        var autoTracks = new List<int>();
        for (var i = start; i < start + span; i++)
            if (definitions[i].UnitType == UiGridUnitType.Auto)
                autoTracks.Add(i);

        if (autoTracks.Count == 0)
            return;

        var remaining = deficit;
        for (var i = 0; i < autoTracks.Count; i++)
        {
            var share = remaining / (autoTracks.Count - i);
            sizes[autoTracks[i]] += share;
            remaining -= share;
        }
    }

    private static void DistributeStars(
        IList<UiGridLength> definitions,
        int[] sizes,
        int available)
    {
        var used = 0;
        var weight = 0f;
        for (var i = 0; i < definitions.Count; i++)
            if (definitions[i].UnitType == UiGridUnitType.Star)
            {
                sizes[i] = 0;
                weight += definitions[i].Value;
            }
            else
            {
                used += sizes[i];
            }

        var remaining = Math.Max(0, available - used);
        var remainingWeight = weight;
        for (var i = 0; i < definitions.Count; i++)
        {
            if (definitions[i].UnitType != UiGridUnitType.Star)
                continue;

            var share = remainingWeight <= 0f
                ? 0
                : (int)MathF.Round(
                    remaining * definitions[i].Value / remainingWeight);
            sizes[i] = Math.Clamp(share, 0, remaining);
            remaining -= sizes[i];
            remainingWeight -= definitions[i].Value;
        }
    }

    private static int[] CreateOffsets(IReadOnlyList<int> sizes)
    {
        var offsets = new int[sizes.Count];
        for (var i = 1; i < sizes.Count; i++)
            offsets[i] = offsets[i - 1] + sizes[i - 1];

        return offsets;
    }

    private static int Sum(
        IReadOnlyList<int> values,
        int start,
        int count)
    {
        var total = 0;
        for (var i = start; i < start + count; i++)
            total += values[i];

        return total;
    }

    private static void ResolvePlacement(
        int requestedStart,
        int requestedSpan,
        int trackCount,
        out int start,
        out int span)
    {
        start = Math.Clamp(requestedStart, 0, trackCount - 1);
        span = Math.Clamp(requestedSpan, 1, trackCount - start);
    }

    private static void EnsureDefinitions(IList<UiGridLength> definitions)
    {
        if (definitions.Count == 0)
            definitions.Add(UiGridLength.Star());
    }

    private static void ParseDefinitions(
        string value,
        IList<UiGridLength> definitions,
        string attributeName)
    {
        definitions.Clear();
        foreach (var part in value.Split(','))
        {
            if (string.IsNullOrWhiteSpace(part))
                throw new XmlException(
                    $"Grid {attributeName} cannot contain an empty track.");

            definitions.Add(UiXmlParser.ParseGridLength(part));
        }

        if (definitions.Count == 0)
            throw new XmlException(
                $"Grid {attributeName} must contain at least one track.");
    }

    private readonly record struct GridTrackLayout(
        int[] RowSizes,
        int[] ColumnSizes);
}