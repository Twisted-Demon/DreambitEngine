using System;
using System.Collections.Generic;

namespace Dreambit.UI;

/// <summary>
///     Displays an arbitrary collection of UI elements using stack layout. Items
///     can be declared directly in XML or materialized from game data with a
///     template function.
/// </summary>
public class UiItemsControl : UiStackPanel
{
    /// <summary>Gets the currently declared or generated item elements.</summary>
    public IReadOnlyList<UiElement> Items => Children;

    /// <summary>Adds one arbitrary UI element as an item.</summary>
    /// <param name="item">The item element to append.</param>
    public void AddItem(UiElement item)
    {
        AddChild(item);
    }

    /// <summary>Removes and detaches an item element.</summary>
    /// <param name="item">The item element to remove.</param>
    /// <returns><see langword="true" /> when the item was present.</returns>
    public bool RemoveItem(UiElement item)
    {
        return RemoveChild(item);
    }

    /// <summary>Removes every item element.</summary>
    public void ClearItems()
    {
        ClearChildren();
    }

    /// <summary>
    ///     Replaces the current items by invoking a template once for each data
    ///     value. The generated item may be any <see cref="UiElement" /> subtype.
    /// </summary>
    /// <typeparam name="T">The game-data item type.</typeparam>
    /// <param name="items">The values to materialize.</param>
    /// <param name="itemTemplate">Creates one UI element for a value.</param>
    public void SetItems<T>(
        IEnumerable<T> items,
        Func<T, UiElement> itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemTemplate);

        ClearChildren();
        foreach (var item in items)
        {
            var element = itemTemplate(item);
            if (element is null)
                throw new InvalidOperationException(
                    "An items-control template cannot return null.");

            AddChild(element);
        }
    }
}