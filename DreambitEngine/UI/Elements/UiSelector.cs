using System;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.UI;

/// <summary>
///     Base items control with one selected child and keyboard/controller
///     navigation. Selected child controls receive the selected visual state.
/// </summary>
public abstract class UiSelector : UiItemsControl
{
    private IUiBrush _background;
    private int _requestedSelectedIndex = -1;
    private int _selectedIndex = -1;

    /// <summary>Creates a focusable selector.</summary>
    protected UiSelector()
    {
        IsFocusable = true;
        IsHitTestVisible = true;
    }

    /// <summary>Gets or sets the selected child index, or -1 for no selection.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SelectIndex(value);
    }

    /// <summary>Gets the currently selected child.</summary>
    public UiElement SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < Children.Count
            ? Children[_selectedIndex]
            : null;

    /// <summary>Gets or sets the visual drawn behind all items.</summary>
    public IUiBrush Background
    {
        get => _background;
        set
        {
            _background = value;
            InvalidateDependencies();
            InvalidateLayout();
        }
    }

    /// <summary>Gets or sets the tint passed to <see cref="Background" />.</summary>
    public Color BackgroundTint { get; set; } = Color.White;

    /// <summary>Raised whenever the selected child changes.</summary>
    public event EventHandler<UiSelectionChangedEventArgs> SelectionChanged;

    /// <inheritdoc />
    public override void AddChild(UiElement child)
    {
        base.AddChild(child);
        if (child is UiButton button)
            button.Clicked += OnItemButtonClicked;

        if (_requestedSelectedIndex >= 0 &&
            _requestedSelectedIndex < Children.Count)
            SelectIndex(_requestedSelectedIndex);
    }

    /// <inheritdoc />
    public override bool RemoveChild(UiElement child)
    {
        if (child is UiButton button)
            button.Clicked -= OnItemButtonClicked;

        var removedIndex = Children.IndexOf(child);
        var removedWasSelected = removedIndex == _selectedIndex;
        if (removedWasSelected)
            SetSelectedVisual(child, false);
        if (!base.RemoveChild(child))
            return false;

        if (removedWasSelected)
        {
            var oldIndex = _selectedIndex;
            _selectedIndex = -1;
            SelectionChanged?.Invoke(
                this,
                new UiSelectionChangedEventArgs(
                    oldIndex,
                    -1,
                    child,
                    null));
        }
        else if (removedIndex >= 0 && removedIndex < _selectedIndex)
        {
            _selectedIndex--;
        }

        return true;
    }

    /// <inheritdoc />
    public override void ClearChildren()
    {
        foreach (var child in Children)
            if (child is UiButton button)
                button.Clicked -= OnItemButtonClicked;

        SetSelectedVisual(SelectedItem, false);
        _selectedIndex = -1;
        base.ClearChildren();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(UiPointerEventArgs args)
    {
        var item = FindDirectItem(args.Source);
        if (item is not null)
            SelectIndex(Children.IndexOf(item));

        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnNavigationRequested(UiNavigationEventArgs args)
    {
        var delta = args.Direction switch
        {
            UiNavigationDirection.Up => -1,
            UiNavigationDirection.Left => -1,
            UiNavigationDirection.Down => 1,
            UiNavigationDirection.Right => 1,
            _ => 0
        };
        if (delta == 0 || Children.Count == 0)
            return;

        var start = SelectedIndex < 0 ? delta > 0 ? -1 : 0 : SelectedIndex;
        SelectIndex((start + delta + Children.Count) % Children.Count);
        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyPressed(UiKeyEventArgs args)
    {
        if (args.Key == Keys.Home && Children.Count > 0)
        {
            SelectIndex(0);
            args.Handled = true;
        }
        else if (args.Key == Keys.End && Children.Count > 0)
        {
            SelectIndex(Children.Count - 1);
            args.Handled = true;
        }
    }

    /// <inheritdoc />
    public override void ResolveDependencies()
    {
        Background?.ResolveDependencies();
        base.ResolveDependencies();
    }

    /// <inheritdoc />
    public override void OnDraw()
    {
        Background?.Draw(Bounds, BackgroundTint);
        base.OnDraw();
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        _requestedSelectedIndex = UiXmlParser.ParseInt(
            node,
            "selected-index",
            -1);
        if (node.Attributes?["background-tint"] is not null)
            BackgroundTint = UiXmlParser.ParseColor(node, "background-tint");
    }

    /// <summary>Changes the selected child after clamping the requested index.</summary>
    protected void SelectIndex(int index)
    {
        var next = Children.Count == 0
            ? -1
            : Math.Clamp(index, -1, Children.Count - 1);
        if (_selectedIndex == next)
            return;

        var oldIndex = _selectedIndex;
        var oldItem = SelectedItem;
        SetSelectedVisual(oldItem, false);
        _selectedIndex = next;
        var newItem = SelectedItem;
        SetSelectedVisual(newItem, true);
        SelectionChanged?.Invoke(
            this,
            new UiSelectionChangedEventArgs(
                oldIndex,
                _selectedIndex,
                oldItem,
                newItem));
    }

    private void OnItemButtonClicked(UiButton button)
    {
        SelectIndex(Children.IndexOf(button));
    }

    private UiElement FindDirectItem(UiElement element)
    {
        for (var current = element;
             current is not null && !ReferenceEquals(current, this);
             current = current.Parent)
            if (ReferenceEquals(current.Parent, this))
                return current;

        return null;
    }

    private static void SetSelectedVisual(UiElement element, bool selected)
    {
        if (element is UiControl control)
            control.IsSelected = selected;
    }
}