using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public enum StackOrientation
{
    Vertical,
    Horizontal
}

public class UiStackPanel : UiContainer
{
    public StackOrientation Orientation = StackOrientation.Vertical;
    public int Spacing = 0;
    public int PaddingLeft = 0;
    public int PaddingTop = 0;
    public int PaddingRight = 0;
    public int PaddingBottom = 0;

    public override void Arrange(Rectangle parentBounds)
    {
        // A stack panel owns child placement, so arrange only this panel here.
        ArrangeSelf(parentBounds);

        int innerX = Bounds.X + PaddingLeft;
        int innerY = Bounds.Y + PaddingTop;
        int maxInnerWidth = Math.Max(
            0,
            Bounds.Width - (PaddingLeft + PaddingRight));
        int maxInnerHeight = Math.Max(
            0,
            Bounds.Height - (PaddingTop + PaddingBottom));

        if (Orientation == StackOrientation.Vertical)
        {
            int currentY = innerY;

            foreach (var child in Children)
            {
                // Auto width: fill available width
                if (!child.Width.IsPercent && child.Width.Value <= 0)
                    child.Width = UiLength.Pixels(maxInnerWidth);

                // Child spans across X, pinned to left
                child.X = UiLength.Pixels(0);
                child.Y = UiLength.Pixels(currentY - innerY);
                child.Anchor = UiAnchor.TopLeft;
                child.Origin = UiAnchor.TopLeft;

                child.Arrange(new Rectangle(innerX, innerY, maxInnerWidth, maxInnerHeight));

                currentY = child.Bounds.Bottom + Spacing;
            }
        }
        else // Horizontal
        {
            int currentX = innerX;

            foreach (var child in Children)
            {
                // Auto height: fill available height
                if (!child.Height.IsPercent && child.Height.Value <= 0)
                    child.Height = UiLength.Pixels(maxInnerHeight);

                child.X = UiLength.Pixels(currentX - innerX);
                child.Y = UiLength.Pixels(0);
                child.Anchor = UiAnchor.TopLeft;
                child.Origin = UiAnchor.TopLeft;

                child.Arrange(new Rectangle(innerX, innerY, maxInnerWidth, maxInnerHeight));

                currentX = child.Bounds.Right + Spacing;
            }
        }
    }

    public override void Parse(XmlNode node)
    {
        var orientation = ParseString(node, "orientation", "Vertical");
        Orientation = string.Equals(orientation, "Horizontal", StringComparison.OrdinalIgnoreCase) 
            ? StackOrientation.Horizontal : 
            StackOrientation.Vertical;
        
        // optional combined padding="20" or "10,5,10,5"
        var padding = ParseString(node, "padding", null);
        if (!string.IsNullOrEmpty(padding))
            ParsePadding(padding);

        Spacing = ParseInt(node, "spacing", 0);
    }
    

    private void ParsePadding(string value)
    {
        var parts = value.Split(',');

        if (parts.Length == 1)
        {
            int p = int.Parse(parts[0]);
            PaddingLeft = PaddingTop = PaddingRight = PaddingBottom = p;
        }else if (parts.Length == 4)
        {
            PaddingLeft = int.Parse(parts[0]);
            PaddingTop = int.Parse(parts[1]);
            PaddingRight = int.Parse(parts[2]);
            PaddingBottom = int.Parse(parts[3]);
        }
    }
}
