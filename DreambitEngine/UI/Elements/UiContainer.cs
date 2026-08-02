using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
/// Base class for elements that own, measure, update, and draw child elements.
/// </summary>
public class UiContainer : UiElement
{
    /// <summary>Adds an element to this container and assigns its parent.</summary>
    /// <param name="child">The child to add.</param>
    public virtual void AddChild(UiElement child)
    {
        ArgumentNullException.ThrowIfNull(child);

        child.Parent = this;
        Children.Add(child);
        InvalidateLayout();
    }

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        var width = 0;
        var height = 0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            width = Math.Max(
                width,
                child.X.Resolve(availableSize.X) + child.DesiredSize.X);
            height = Math.Max(
                height,
                child.Y.Resolve(availableSize.Y) + child.DesiredSize.Y);
        }

        return new Point(width, height);
    }

    /// <inheritdoc />
    public override void OnDraw()
    {
        base.OnDraw();
        
        // sort children by ZIndex and draw
        var ordered = Children.OrderBy(c => c.ZIndex).ToList();

        foreach (var child in ordered)
            child.OnDraw();
    }
    
}
