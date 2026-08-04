using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
///     Draws an ordered collection of brushes into the same bounds, allowing
///     backgrounds to be composed without creating a new control type.
/// </summary>
public sealed class LayeredBrush : UiBrush
{
    /// <summary>Gets the brushes in back-to-front draw order.</summary>
    public IList<IUiBrush> Brushes { get; } = new List<IUiBrush>();

    /// <inheritdoc />
    public override Point MinimumSize
    {
        get
        {
            var width = 0;
            var height = 0;
            foreach (var brush in Brushes)
            {
                if (brush is null)
                    continue;

                width = Math.Max(width, brush.MinimumSize.X);
                height = Math.Max(height, brush.MinimumSize.Y);
            }

            return new Point(width, height);
        }
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        Brushes.Clear();
        foreach (var brush in UiLoader.ParseBrushes(node))
            Brushes.Add(brush);
    }

    /// <inheritdoc />
    public override void ResolveDependencies()
    {
        foreach (var brush in Brushes)
            brush?.ResolveDependencies();
    }

    /// <inheritdoc />
    public override void Draw(Rectangle bounds, Color tint)
    {
        foreach (var brush in Brushes)
            brush?.Draw(bounds, tint);
    }
}