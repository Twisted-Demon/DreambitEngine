using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>Draws an inset rectangular outline using the supplied control tint.</summary>
public sealed class OutlineBrush : UiBrush
{
    /// <summary>Gets or sets the width of each outline edge in pixels.</summary>
    public UiThickness Thickness { get; set; } = UiThickness.Uniform(1);

    /// <inheritdoc />
    public override Point MinimumSize => new(
        Thickness.Horizontal,
        Thickness.Vertical);

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        var thickness = UiXmlParser.ParseThickness(
            UiXmlParser.ParseString(node, "thickness", "1"),
            "Outline thickness");
        Thickness = new UiThickness(
            ParseEdge(node, "left", thickness.Left),
            ParseEdge(node, "top", thickness.Top),
            ParseEdge(node, "right", thickness.Right),
            ParseEdge(node, "bottom", thickness.Bottom));
    }

    /// <inheritdoc />
    public override void Draw(Rectangle bounds, Color tint)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        ResolvePair(
            Thickness.Left,
            Thickness.Right,
            bounds.Width,
            out var left,
            out var right);
        ResolvePair(
            Thickness.Top,
            Thickness.Bottom,
            bounds.Height,
            out var top,
            out var bottom);

        DrawRectangle(bounds.X, bounds.Y, bounds.Width, top, tint);
        DrawRectangle(
            bounds.X,
            bounds.Bottom - bottom,
            bounds.Width,
            bottom,
            tint);
        var middleHeight = Math.Max(0, bounds.Height - top - bottom);
        DrawRectangle(bounds.X, bounds.Y + top, left, middleHeight, tint);
        DrawRectangle(
            bounds.Right - right,
            bounds.Y + top,
            right,
            middleHeight,
            tint);
    }

    private static int ParseEdge(XmlNode node, string attribute, int defaultValue)
    {
        return node.Attributes?[attribute] is null
            ? defaultValue
            : Math.Max(0, UiXmlParser.ParseInt(node, attribute, defaultValue));
    }

    private static void ResolvePair(
        int first,
        int second,
        int available,
        out int resolvedFirst,
        out int resolvedSecond)
    {
        first = Math.Max(0, first);
        second = Math.Max(0, second);
        var total = first + second;
        if (total <= available || total == 0)
        {
            resolvedFirst = first;
            resolvedSecond = second;
            return;
        }

        resolvedFirst = (int)MathF.Round(available * (first / (float)total));
        resolvedSecond = available - resolvedFirst;
    }

    private static void DrawRectangle(
        int x,
        int y,
        int width,
        int height,
        Color tint)
    {
        if (width <= 0 || height <= 0)
            return;

        Graphics.SpriteBatch.DrawFilledRectangle(
            new RectangleF(x, y, width, height),
            tint);
    }
}