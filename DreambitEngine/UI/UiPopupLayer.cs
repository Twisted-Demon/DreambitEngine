using System;
using System.Collections.Generic;
using System.Linq;

namespace Dreambit.UI;

/// <summary>
/// Owns transient popup visuals separately from the normal tree so they draw
/// and hit-test above all ordinary UI content.
/// </summary>
public sealed class UiPopupLayer : UiContainer
{
    internal UiPopupLayer()
    {
        Id = "__popup-layer";
        X = UiLength.Pixels(0);
        Y = UiLength.Pixels(0);
        Width = UiLength.Percent(1f);
        Height = UiLength.Percent(1f);
        IsHitTestVisible = false;
        ZIndex = int.MaxValue;
    }

    /// <summary>Gets the currently open popups in back-to-front order.</summary>
    public IEnumerable<UiPopup> OpenPopups =>
        Children.OfType<UiPopup>().Where(popup => popup.IsOpen);

    /// <summary>Moves a popup to this layer and displays it.</summary>
    public void Open(UiPopup popup)
    {
        ArgumentNullException.ThrowIfNull(popup);
        if (!ReferenceEquals(popup.Parent, this))
        {
            popup.Parent?.RemoveChild(popup);
            AddChild(popup);
        }

        popup.SetOpen(true);
    }

    /// <summary>Hides an open popup without destroying its content.</summary>
    public void Close(UiPopup popup)
    {
        if (popup is null || !ReferenceEquals(popup.Parent, this))
            return;

        popup.SetOpen(false);
    }

    /// <summary>Closes every popup configured for outside-click dismissal.</summary>
    public void CloseDismissiblePopups()
    {
        foreach (var popup in OpenPopups.Where(popup => !popup.StaysOpen).ToList())
            Close(popup);
    }

    internal void DismissForPointerTarget(UiElement target)
    {
        foreach (var popup in OpenPopups
                     .Where(popup => !popup.StaysOpen)
                     .Reverse()
                     .ToList())
        {
            if (!IsDescendantOf(target, popup))
                Close(popup);
        }
    }

    internal void ActivateRequestedPopups(UiElement root)
    {
        var requested = Enumerate(root)
            .OfType<UiPopup>()
            .Where(popup => popup.OpenRequested)
            .ToList();
        foreach (var popup in requested)
            Open(popup);
    }

    private static bool IsDescendantOf(UiElement element, UiElement ancestor)
    {
        for (var current = element; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
        }

        return false;
    }

    private static IEnumerable<UiElement> Enumerate(UiElement root)
    {
        if (root is null)
            yield break;

        yield return root;
        foreach (var child in root.Children.ToList())
        {
            foreach (var descendant in Enumerate(child))
                yield return descendant;
        }
    }
}
