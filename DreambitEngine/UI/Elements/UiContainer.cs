using System;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
///     Base class for elements that own, measure, update, and draw child elements.
/// </summary>
public class UiContainer : UiElement
{
    /// <summary>Adds an element to this container and assigns its parent.</summary>
    /// <param name="child">The child to add.</param>
    public virtual void AddChild(UiElement child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child.Parent is not null || Children.Contains(child))
            throw new InvalidOperationException(
                $"{child.GetType().Name} already belongs to a UI container. " +
                "Remove it from its current parent before adding it again.");

        if (child.Layout is not null &&
            !ReferenceEquals(child.Layout, Layout))
            throw new InvalidOperationException(
                $"{child.GetType().Name} is attached to a different UI layout.");

        for (var ancestor = this;
             ancestor is not null;
             ancestor = ancestor.Parent)
            if (ReferenceEquals(ancestor, child))
                throw new InvalidOperationException(
                    "A UI element cannot be added beneath one of its descendants.");

        Layout?.ValidateIdsForAttachment(child);

        child.Parent = this;
        Children.Add(child);
        child.AttachToLayout(Layout);
        InvalidateLayout();
    }

    /// <summary>Removes an owned child and detaches it from this layout.</summary>
    /// <param name="child">The child to remove.</param>
    /// <returns><see langword="true" /> when the child belonged to this container.</returns>
    public virtual bool RemoveChild(UiElement child)
    {
        if (child is null || !Children.Remove(child))
            return false;

        child.Parent = null;
        child.AttachToLayout(null);
        InvalidateLayout();
        Layout?.ValidateInteractionState();
        return true;
    }

    /// <summary>Removes and detaches every child owned by this container.</summary>
    public virtual void ClearChildren()
    {
        if (Children.Count == 0)
            return;

        foreach (var child in Children)
        {
            child.Parent = null;
            child.AttachToLayout(null);
        }

        Children.Clear();
        InvalidateLayout();
        Layout?.ValidateInteractionState();
    }

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        var width = 0;
        var height = 0;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

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
}