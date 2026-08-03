using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
/// Draws a sprite as nine regions so its corners remain pixel-sized while its
/// edges and center stretch to fill the owning control.
/// </summary>
public sealed class NineSliceBrush : UiBrush
{
    private Sprite _sprite;

    /// <summary>Gets or sets the asset path of the sprite to slice.</summary>
    public string SpritePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the source-pixel inset for each sliced edge.</summary>
    public UiThickness SliceThickness { get; set; }

    /// <inheritdoc />
    public override Point MinimumSize
    {
        get
        {
            if (_sprite is null)
            {
                return new Point(
                    SliceThickness.Horizontal,
                    SliceThickness.Vertical);
            }

            ResolvePair(
                SliceThickness.Left,
                SliceThickness.Right,
                _sprite.SourceRect.Width,
                out var left,
                out var right);
            ResolvePair(
                SliceThickness.Top,
                SliceThickness.Bottom,
                _sprite.SourceRect.Height,
                out var top,
                out var bottom);
            return new Point(left + right, top + bottom);
        }
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        SpritePath = UiXmlParser.ParseString(node, "sprite", string.Empty);
        if (string.IsNullOrWhiteSpace(SpritePath))
        {
            throw new XmlException(
                "<NineSliceBrush> requires a non-empty sprite attribute.");
        }

        var thickness = UiXmlParser.ParseThickness(
            UiXmlParser.ParseString(node, "slice", "0"),
            "Nine-slice inset");
        SliceThickness = new UiThickness(
            ParseEdge(node, "slice-left", thickness.Left),
            ParseEdge(node, "slice-top", thickness.Top),
            ParseEdge(node, "slice-right", thickness.Right),
            ParseEdge(node, "slice-bottom", thickness.Bottom));
    }

    /// <inheritdoc />
    public override void ResolveDependencies()
    {
        _sprite = string.IsNullOrWhiteSpace(SpritePath)
            ? null
            : Resources.LoadAsset<Sprite>(SpritePath);
    }

    /// <inheritdoc />
    public override void Draw(Rectangle bounds, Color tint)
    {
        if (_sprite?.Texture is null || bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var source = _sprite.SourceRect;
        ResolvePair(
            SliceThickness.Left,
            SliceThickness.Right,
            source.Width,
            out var sourceLeft,
            out var sourceRight);
        ResolvePair(
            SliceThickness.Top,
            SliceThickness.Bottom,
            source.Height,
            out var sourceTop,
            out var sourceBottom);
        ResolvePair(
            sourceLeft,
            sourceRight,
            bounds.Width,
            out var destinationLeft,
            out var destinationRight);
        ResolvePair(
            sourceTop,
            sourceBottom,
            bounds.Height,
            out var destinationTop,
            out var destinationBottom);

        var sourceWidths = new[]
        {
            sourceLeft,
            Math.Max(0, source.Width - sourceLeft - sourceRight),
            sourceRight
        };
        var sourceHeights = new[]
        {
            sourceTop,
            Math.Max(0, source.Height - sourceTop - sourceBottom),
            sourceBottom
        };
        var destinationWidths = new[]
        {
            destinationLeft,
            Math.Max(0, bounds.Width - destinationLeft - destinationRight),
            destinationRight
        };
        var destinationHeights = new[]
        {
            destinationTop,
            Math.Max(0, bounds.Height - destinationTop - destinationBottom),
            destinationBottom
        };

        var sourceY = source.Y;
        var destinationY = bounds.Y;
        for (var row = 0; row < 3; row++)
        {
            var sourceX = source.X;
            var destinationX = bounds.X;
            for (var column = 0; column < 3; column++)
            {
                DrawRegion(
                    new Rectangle(
                        sourceX,
                        sourceY,
                        sourceWidths[column],
                        sourceHeights[row]),
                    new Rectangle(
                        destinationX,
                        destinationY,
                        destinationWidths[column],
                        destinationHeights[row]),
                    tint);
                sourceX += sourceWidths[column];
                destinationX += destinationWidths[column];
            }

            sourceY += sourceHeights[row];
            destinationY += destinationHeights[row];
        }
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
        available = Math.Max(0, available);
        first = Math.Max(0, first);
        second = Math.Max(0, second);
        var total = first + second;

        if (total <= available)
        {
            resolvedFirst = first;
            resolvedSecond = second;
            return;
        }

        if (total == 0)
        {
            resolvedFirst = 0;
            resolvedSecond = 0;
            return;
        }

        resolvedFirst = (int)MathF.Round(available * (first / (float)total));
        resolvedSecond = available - resolvedFirst;
    }

    private void DrawRegion(Rectangle source, Rectangle destination, Color tint)
    {
        if (source.Width <= 0 || source.Height <= 0 ||
            destination.Width <= 0 || destination.Height <= 0)
        {
            return;
        }

        Graphics.SpriteBatch.Draw(
            _sprite.Texture,
            destination,
            source,
            tint);
    }
}
