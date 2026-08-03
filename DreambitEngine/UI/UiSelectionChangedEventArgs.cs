using System;

namespace Dreambit.UI;

/// <summary>Contains the old and new single-selection values.</summary>
public sealed class UiSelectionChangedEventArgs : EventArgs
{
    internal UiSelectionChangedEventArgs(
        int oldIndex,
        int newIndex,
        UiElement oldItem,
        UiElement newItem)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
        OldItem = oldItem;
        NewItem = newItem;
    }

    /// <summary>Gets the previous selected index.</summary>
    public int OldIndex { get; }
    /// <summary>Gets the current selected index.</summary>
    public int NewIndex { get; }
    /// <summary>Gets the previously selected element.</summary>
    public UiElement OldItem { get; }
    /// <summary>Gets the currently selected element.</summary>
    public UiElement NewItem { get; }
}
