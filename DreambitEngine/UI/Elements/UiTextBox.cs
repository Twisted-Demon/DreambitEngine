using System;
using System.Xml;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.UI;

/// <summary>
/// A single-line editable text control with caret movement, drag selection,
/// placeholder/password display, and UI clipboard shortcuts.
/// </summary>
public sealed class UiTextBox : UiControl
{
    private string _text = string.Empty;
    private string _fontPath = "monogram";
    private float _fontSize = 18f;
    private int _caretIndex;
    private int _selectionAnchor;
    private float _caretElapsed;
    private float _scrollOffset;
    private bool _selecting;
    private int _maxLength;

    /// <summary>Creates a focusable keyboard-capturing text box.</summary>
    public UiTextBox()
    {
        IsFocusable = true;
        IsHitTestVisible = true;
        CapturesKeyboardInput = true;
        ClipToBounds = true;
        Padding = new UiThickness(6, 3, 6, 3);
    }

    /// <summary>Raised whenever the edited text changes.</summary>
    public event Action<UiTextBox, string> TextChanged;

    /// <summary>Gets the resolved font.</summary>
    public SpriteFontBase Font { get; private set; }

    /// <summary>Gets or sets the edited text.</summary>
    public string Text
    {
        get => _text;
        set
        {
            var next = value ?? string.Empty;
            if (MaxLength > 0 && next.Length > MaxLength)
                next = next[..MaxLength];
            if (_text == next) return;
            _text = next;
            _caretIndex = Math.Min(_caretIndex, _text.Length);
            _selectionAnchor = Math.Min(_selectionAnchor, _text.Length);
            InvalidateLayout();
            TextChanged?.Invoke(this, _text);
        }
    }

    /// <summary>Gets or sets the text shown while the value is empty.</summary>
    public string Placeholder { get; set; } = string.Empty;
    /// <summary>Gets or sets the text color.</summary>
    public Color TextColor { get; set; } = Color.White;
    /// <summary>Gets or sets the placeholder color.</summary>
    public Color PlaceholderColor { get; set; } = new(150, 150, 160);
    /// <summary>Gets or sets the selection highlight color.</summary>
    public Color SelectionColor { get; set; } = new(50, 105, 170, 180);
    /// <summary>Gets or sets the caret color.</summary>
    public Color CaretColor { get; set; } = Color.White;
    /// <summary>Gets or sets the font resource path.</summary>
    public string FontPath
    {
        get => _fontPath;
        set
        {
            _fontPath = value ?? string.Empty;
            InvalidateDependencies();
            InvalidateLayout();
        }
    }

    /// <summary>Gets or sets the font size.</summary>
    public float FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = Math.Max(1f, value);
            InvalidateDependencies();
            InvalidateLayout();
        }
    }

    /// <summary>Gets or sets the maximum character count, or zero for unlimited.</summary>
    public int MaxLength
    {
        get => _maxLength;
        set
        {
            var next = Math.Max(0, value);
            if (_maxLength == next)
                return;

            _maxLength = next;
            if (_maxLength > 0 && Text.Length > _maxLength)
                Text = Text[.._maxLength];
        }
    }
    /// <summary>Gets or sets the character used to conceal text, or null for normal display.</summary>
    public char? PasswordCharacter { get; set; }
    /// <summary>Gets the current caret insertion index.</summary>
    public int CaretIndex => _caretIndex;
    /// <summary>Gets the first selected character index.</summary>
    public int SelectionStart => Math.Min(_caretIndex, _selectionAnchor);
    /// <summary>Gets the number of selected characters.</summary>
    public int SelectionLength => Math.Abs(_caretIndex - _selectionAnchor);

    /// <summary>Selects a range of characters.</summary>
    public void Select(int start, int length)
    {
        start = Math.Clamp(start, 0, Text.Length);
        length = Math.Clamp(length, 0, Text.Length - start);
        _selectionAnchor = start;
        _caretIndex = start + length;
        ResetCaretBlink();
    }

    /// <summary>Selects all text.</summary>
    public void SelectAll()
    {
        Select(0, Text.Length);
    }

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        if (Font is null)
            return new Point(Padding.Horizontal, Padding.Vertical);

        var sample = string.IsNullOrEmpty(Text)
            ? string.IsNullOrEmpty(Placeholder) ? "M" : Placeholder
            : GetDisplayText();
        var measured = Font.MeasureString(sample);
        return new Point(
            (int)MathF.Ceiling(measured.X) + Padding.Horizontal,
            (int)MathF.Ceiling(
                MathF.Max(measured.Y, SpriteBatchExtensions.GetLineHeight(Font))) +
            Padding.Vertical);
    }

    /// <inheritdoc />
    public override void ResolveDependencies()
    {
        base.ResolveDependencies();
        Font = string.IsNullOrWhiteSpace(FontPath)
            ? null
            : Resources.LoadSpriteFont(FontPath, FontSize);
    }

    /// <inheritdoc />
    protected override void OnUpdate(in UiInputState input)
    {
        base.OnUpdate(input);
        if (!IsFocused)
            return;

        _caretElapsed += Time.UnscaledDeltaTime;
        foreach (var character in input.TextInput ?? [])
        {
            if (!char.IsControl(character))
                ReplaceSelection(character.ToString());
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(UiPointerEventArgs args)
    {
        _selecting = true;
        _caretIndex = GetCharacterIndex(args.Position.X);
        _selectionAnchor = _caretIndex;
        ResetCaretBlink();
        args.CapturePointer();
        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(UiPointerEventArgs args)
    {
        if (!_selecting)
            return;

        _caretIndex = GetCharacterIndex(args.Position.X);
        ResetCaretBlink();
        args.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(UiPointerEventArgs args)
    {
        _selecting = false;
        args.ReleasePointerCapture();
        args.Handled = true;
    }

    /// <inheritdoc />
    protected internal override void OnPointerCaptureLost()
    {
        _selecting = false;
    }

    /// <inheritdoc />
    protected override void OnKeyPressed(UiKeyEventArgs args)
    {
        if (args.ControlDown && HandleClipboardShortcut(args.Key))
        {
            args.Handled = true;
            return;
        }

        switch (args.Key)
        {
            case Keys.Left:
                MoveCaret(-1, args.ShiftDown);
                args.Handled = true;
                break;
            case Keys.Right:
                MoveCaret(1, args.ShiftDown);
                args.Handled = true;
                break;
            case Keys.Home:
                SetCaret(0, args.ShiftDown);
                args.Handled = true;
                break;
            case Keys.End:
                SetCaret(Text.Length, args.ShiftDown);
                args.Handled = true;
                break;
            case Keys.Back:
                Backspace();
                args.Handled = true;
                break;
            case Keys.Delete:
                Delete();
                args.Handled = true;
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnFocusChanged(bool isFocused)
    {
        ResetCaretBlink();
        if (!isFocused)
            _selecting = false;
    }

    /// <inheritdoc />
    public override void OnDraw()
    {
        base.OnDraw();
        if (Font is null)
            return;

        var displayText = GetDisplayText();
        var showingPlaceholder = displayText.Length == 0 && !IsFocused;
        if (showingPlaceholder)
            displayText = Placeholder;
        UpdateScrollOffset(displayText);
        var lineHeight = SpriteBatchExtensions.GetLineHeight(Font);
        var textY = Bounds.Center.Y - lineHeight * 0.5f;
        var textX = Bounds.X + Padding.Left - _scrollOffset;

        if (IsFocused && SelectionLength > 0)
        {
            var selectedX = MeasurePrefix(displayText, SelectionStart);
            var selectedWidth = MeasureRange(
                displayText,
                SelectionStart,
                SelectionLength);
            Graphics.SpriteBatch.DrawFilledRectangle(
                new RectangleF(
                    textX + selectedX,
                    Bounds.Y + Padding.Top,
                    selectedWidth,
                    Math.Max(1, Bounds.Height - Padding.Vertical)),
                SelectionColor);
        }

        if (!string.IsNullOrEmpty(displayText))
        {
            Graphics.SpriteBatch.DrawString(
                Font,
                displayText,
                new Vector2(textX, textY),
                showingPlaceholder ? PlaceholderColor : TextColor);
        }

        if (IsFocused && _caretElapsed % 1f < 0.55f)
        {
            var caretX = textX + MeasurePrefix(displayText, _caretIndex);
            Graphics.SpriteBatch.DrawFilledRectangle(
                new RectangleF(
                    caretX,
                    Bounds.Y + Padding.Top,
                    1,
                    Math.Max(1, Bounds.Height - Padding.Vertical)),
                CaretColor);
        }
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        Text = UiXmlParser.ParseString(node, "text", string.Empty);
        Placeholder = UiXmlParser.ParseString(node, "placeholder", string.Empty);
        FontPath = UiXmlParser.ParseString(node, "font", "monogram");
        FontSize = UiXmlParser.ParseFloat(node, "font-size", 18f);
        MaxLength = Math.Max(0, UiXmlParser.ParseInt(node, "max-length", 0));
        var password = UiXmlParser.ParseString(node, "password-character", string.Empty);
        PasswordCharacter = password.Length == 0 ? null : password[0];
        ParseColor(node, "text-color", value => TextColor = value);
        ParseColor(node, "placeholder-color", value => PlaceholderColor = value);
        ParseColor(node, "selection-color", value => SelectionColor = value);
        ParseColor(node, "caret-color", value => CaretColor = value);
        _caretIndex = Text.Length;
        _selectionAnchor = _caretIndex;
    }

    private bool HandleClipboardShortcut(Keys key)
    {
        switch (key)
        {
            case Keys.A:
                SelectAll();
                return true;
            case Keys.C:
                CopySelection();
                return true;
            case Keys.X:
                CopySelection();
                DeleteSelection();
                return true;
            case Keys.V:
                ReplaceSelection(UiClipboard.Text ?? string.Empty);
                return true;
            default:
                return false;
        }
    }

    private void CopySelection()
    {
        UiClipboard.Text = SelectionLength == 0
            ? string.Empty
            : Text.Substring(SelectionStart, SelectionLength);
    }

    private void Backspace()
    {
        if (DeleteSelection()) return;
        if (_caretIndex <= 0) return;
        var index = _caretIndex - 1;
        Text = Text.Remove(index, 1);
        _caretIndex = _selectionAnchor = index;
        ResetCaretBlink();
    }

    private void Delete()
    {
        if (DeleteSelection()) return;
        if (_caretIndex >= Text.Length) return;
        Text = Text.Remove(_caretIndex, 1);
        _selectionAnchor = _caretIndex;
        ResetCaretBlink();
    }

    private bool DeleteSelection()
    {
        if (SelectionLength == 0)
            return false;

        var start = SelectionStart;
        Text = Text.Remove(start, SelectionLength);
        _caretIndex = _selectionAnchor = start;
        ResetCaretBlink();
        return true;
    }

    private void ReplaceSelection(string value)
    {
        value ??= string.Empty;
        var start = SelectionStart;
        var candidate = Text.Remove(start, SelectionLength).Insert(start, value);
        if (MaxLength > 0 && candidate.Length > MaxLength)
            candidate = candidate[..MaxLength];
        Text = candidate;
        _caretIndex = Math.Min(start + value.Length, Text.Length);
        _selectionAnchor = _caretIndex;
        ResetCaretBlink();
    }

    private void MoveCaret(int delta, bool extendSelection)
    {
        SetCaret(Math.Clamp(_caretIndex + delta, 0, Text.Length), extendSelection);
    }

    private void SetCaret(int index, bool extendSelection)
    {
        _caretIndex = Math.Clamp(index, 0, Text.Length);
        if (!extendSelection)
            _selectionAnchor = _caretIndex;
        ResetCaretBlink();
    }

    private int GetCharacterIndex(float pointerX)
    {
        var display = GetDisplayText();
        var localX = pointerX - Bounds.X - Padding.Left + _scrollOffset;
        for (var i = 0; i < display.Length; i++)
        {
            var left = MeasurePrefix(display, i);
            var right = MeasurePrefix(display, i + 1);
            if (localX < (left + right) * 0.5f)
                return i;
        }

        return display.Length;
    }

    private void UpdateScrollOffset(string displayText)
    {
        var visibleWidth = Math.Max(0, Bounds.Width - Padding.Horizontal);
        var caretX = MeasurePrefix(displayText, _caretIndex);
        if (caretX - _scrollOffset > visibleWidth)
            _scrollOffset = caretX - visibleWidth;
        else if (caretX < _scrollOffset)
            _scrollOffset = caretX;

        var textWidth = Font?.MeasureString(displayText).X ?? 0f;
        _scrollOffset = Math.Clamp(
            _scrollOffset,
            0f,
            Math.Max(0f, textWidth - visibleWidth));
    }

    private string GetDisplayText()
    {
        return PasswordCharacter.HasValue
            ? new string(PasswordCharacter.Value, Text.Length)
            : Text;
    }

    private float MeasurePrefix(string value, int length)
    {
        if (Font is null || length <= 0)
            return 0f;
        return Font.MeasureString(value[..Math.Min(length, value.Length)]).X;
    }

    private float MeasureRange(string value, int start, int length)
    {
        if (Font is null || length <= 0 || start >= value.Length)
            return 0f;
        return Font.MeasureString(
            value.Substring(start, Math.Min(length, value.Length - start))).X;
    }

    private void ResetCaretBlink()
    {
        _caretElapsed = 0f;
    }

    private static void ParseColor(XmlNode node, string attribute, Action<Color> setter)
    {
        if (node.Attributes?[attribute] is not null)
            setter(UiXmlParser.ParseColor(node, attribute));
    }
}
