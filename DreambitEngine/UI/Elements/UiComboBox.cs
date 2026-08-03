using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using FontStashSharp;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
/// A single-selection string control whose choices are displayed in a
/// popup-layer list box.
/// </summary>
public sealed class UiComboBox : UiControl
{
    private readonly List<string> _items = [];
    private UiPopup _popup;
    private UiListBox _popupList;
    private SpriteFontBase _font;
    private bool _pressed;
    private bool _dropDownWasOpenOnPress;
    private int _selectedIndex = -1;

    /// <summary>Creates a focusable popup-backed combo box.</summary>
    public UiComboBox()
    {
        IsFocusable = true;
        IsHitTestVisible = true;
        Padding = new UiThickness(7, 3, 22, 3);
    }

    /// <summary>Raised when the selected string changes.</summary>
    public event Action<UiComboBox, int, string> SelectionChanged;

    /// <summary>Gets the available string items.</summary>
    public IList<string> Items => _items;

    /// <summary>Gets or sets the selected item index.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var next = _items.Count == 0
                ? -1
                : Math.Clamp(value, -1, _items.Count - 1);
            if (_selectedIndex == next) return;
            _selectedIndex = next;
            SelectionChanged?.Invoke(this, next, SelectedItem);
        }
    }

    /// <summary>Gets the selected string, or an empty string.</summary>
    public string SelectedItem =>
        SelectedIndex >= 0 && SelectedIndex < _items.Count
            ? _items[SelectedIndex]
            : string.Empty;

    /// <summary>Gets whether the choices popup is currently open.</summary>
    public bool IsDropDownOpen => _popup?.IsOpen == true;

    /// <summary>Gets or sets the font path used by the header and generated items.</summary>
    public string FontPath { get; set; } = "monogram";
    /// <summary>Gets or sets the generated item font size.</summary>
    public float FontSize { get; set; } = 18f;
    /// <summary>Gets or sets the header text color.</summary>
    public Color TextColor { get; set; } = Color.White;
    /// <summary>Gets or sets each generated popup item's height.</summary>
    public int ItemHeight { get; set; } = 26;
    /// <summary>Gets or sets the popup background tint.</summary>
    public Color PopupTint { get; set; } = new(36, 39, 48);

    /// <inheritdoc />
    protected override bool IsPressedForVisualState => _pressed;
    /// <inheritdoc />
    protected override bool IsOpenForVisualState => IsDropDownOpen;

    /// <summary>Replaces the available choices.</summary>
    public void SetItems(IEnumerable<string> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items.Clear();
        _items.AddRange(items.Select(item => item ?? string.Empty));
        SelectedIndex = _items.Count == 0
            ? -1
            : Math.Clamp(SelectedIndex < 0 ? 0 : SelectedIndex, 0, _items.Count - 1);
        RebuildPopupItems();
        InvalidateLayout();
    }

    /// <summary>Opens the choices popup.</summary>
    public void OpenDropDown()
    {
        if (Layout is null || _items.Count == 0)
            return;

        EnsurePopup();
        _popup.Width = UiLength.Pixels(Math.Max(1, Bounds.Width));
        _popup.PlacementTarget = this;
        _popup.Open();
    }

    /// <summary>Closes the choices popup.</summary>
    public void CloseDropDown()
    {
        _popup?.Close();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(UiPointerEventArgs args)
    {
        _dropDownWasOpenOnPress = IsDropDownOpen;
        _pressed = true;
        args.CapturePointer();
        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(UiPointerEventArgs args)
    {
        var activate = _pressed && args.IsInside;
        _pressed = false;
        args.ReleasePointerCapture();
        if (activate)
        {
            if (_dropDownWasOpenOnPress || IsDropDownOpen) CloseDropDown();
            else OpenDropDown();
        }

        args.Handled = true;
    }

    /// <inheritdoc />
    protected internal override void OnPointerCaptureLost()
    {
        _pressed = false;
        _dropDownWasOpenOnPress = false;
    }

    /// <inheritdoc />
    protected override void OnActivated(UiCommandEventArgs args)
    {
        if (IsDropDownOpen) CloseDropDown();
        else OpenDropDown();
        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnCancelled(UiCommandEventArgs args)
    {
        if (!IsDropDownOpen)
            return;

        CloseDropDown();
        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnNavigationRequested(UiNavigationEventArgs args)
    {
        if (_items.Count == 0)
            return;

        if (args.Direction == UiNavigationDirection.Down)
        {
            SelectedIndex = Math.Min(_items.Count - 1, SelectedIndex + 1);
            args.Handled = true;
        }
        else if (args.Direction == UiNavigationDirection.Up)
        {
            SelectedIndex = Math.Max(0, SelectedIndex - 1);
            args.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        if (Content is not null)
            return base.MeasureContent(availableSize);

        var measured = _font?.MeasureString(
            string.IsNullOrEmpty(SelectedItem) ? "Select..." : SelectedItem) ??
            Vector2.Zero;
        return new Point(
            (int)MathF.Ceiling(measured.X) + Padding.Horizontal,
            (int)MathF.Ceiling(measured.Y) + Padding.Vertical);
    }

    /// <inheritdoc />
    public override void ResolveDependencies()
    {
        base.ResolveDependencies();
        _font = string.IsNullOrWhiteSpace(FontPath)
            ? null
            : Resources.LoadSpriteFont(FontPath, FontSize);
    }

    /// <inheritdoc />
    public override void OnDraw()
    {
        base.OnDraw();
        if (Content is not null || _font is null)
            return;

        var text = string.IsNullOrEmpty(SelectedItem) ? "Select..." : SelectedItem;
        Graphics.SpriteBatch.DrawTextAligned(
            _font,
            text,
            new Vector2(Bounds.X + Padding.Left, Bounds.Center.Y),
            HorizontalAlignment.Left,
            VerticalAlignment.Center,
            TextColor);
        Graphics.SpriteBatch.DrawTextAligned(
            _font,
            IsDropDownOpen ? "^" : "v",
            new Vector2(Bounds.Right - 8, Bounds.Center.Y),
            HorizontalAlignment.Center,
            VerticalAlignment.Center,
            TextColor);
    }

    /// <inheritdoc />
    protected override void OnAttachedToLayout(
        UiLayout previousLayout,
        UiLayout currentLayout)
    {
        if (currentLayout is null)
            _popup?.Close();
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        FontPath = UiXmlParser.ParseString(node, "font", "monogram");
        FontSize = UiXmlParser.ParseFloat(node, "font-size", 18f);
        ItemHeight = Math.Max(1, UiXmlParser.ParseInt(node, "item-height", 26));
        SetItems(UiXmlParser.ParseString(node, "items", string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        SelectedIndex = UiXmlParser.ParseInt(
            node,
            "selected-index",
            _items.Count == 0 ? -1 : 0);
        if (node.Attributes?["text-color"] is not null)
            TextColor = UiXmlParser.ParseColor(node, "text-color");
        if (node.Attributes?["popup-tint"] is not null)
            PopupTint = UiXmlParser.ParseColor(node, "popup-tint");
    }

    private void EnsurePopup()
    {
        if (_popup is not null)
            return;

        _popupList = new UiListBox
        {
            Width = UiLength.Percent(1f),
            Height = UiLength.Auto(),
            Spacing = 1,
            Background = new SolidColorBrush(),
            BackgroundTint = PopupTint
        };
        _popupList.SelectionChanged += OnPopupSelectionChanged;
        _popup = new UiPopup
        {
            Width = UiLength.Pixels(Math.Max(1, Bounds.Width)),
            Height = UiLength.Auto(),
            PlacementTarget = this,
            Placement = UiPopupPlacement.Bottom,
            StaysOpen = false,
            Background = new SolidColorBrush(),
            BackgroundTint = PopupTint,
            Padding = UiThickness.Uniform(2)
        };
        _popup.SetContent(_popupList);
        RebuildPopupItems();
    }

    private void RebuildPopupItems()
    {
        if (_popupList is null)
            return;

        _popupList.ClearItems();
        for (var index = 0; index < _items.Count; index++)
        {
            var item = new UiButton
            {
                Width = UiLength.Percent(1f),
                Height = UiLength.Pixels(ItemHeight),
                Background = new SolidColorBrush(),
                BackgroundTint = PopupTint,
                HoverTint = new Color(65, 75, 95),
                SelectedTint = new Color(55, 95, 145)
            };
            item.SetContent(new UiText
            {
                Width = UiLength.Percent(1f),
                Height = UiLength.Auto(),
                Text = _items[index],
                FontPath = FontPath,
                FontSize = FontSize,
                TextColor = TextColor,
                HorizontalAlignment = HorizontalAlignment.Left,
                MultiLine = false
            });
            _popupList.AddItem(item);
        }

        _popupList.SelectedIndex = SelectedIndex;
    }

    private void OnPopupSelectionChanged(
        object sender,
        UiSelectionChangedEventArgs args)
    {
        if (args.NewIndex < 0)
            return;

        SelectedIndex = args.NewIndex;
        CloseDropDown();
    }
}
