using System;
using System.Xml;
using FontStashSharp;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public class UiText : UiElement
{
    public SpriteFontBase Font { get; private set; }
    
    private string _fontPath = "monogram";
    private bool _multiLine = true;
    private string _text = string.Empty;
    private float _fontSize = 12f;
    private bool _autoResizeHeight = true;

    public bool AutoResizeHeight
    {
        get => _autoResizeHeight;
        set
        {
            if (_autoResizeHeight == value) return;

            _autoResizeHeight = value;
            _layoutDirty = true;
            InvalidateLayout();
        }
    }

    public Color TextColor { get; set; } = Color.White;
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;
    
    public string Text
    {
        get => _text;
        set
        {
            var next = value ?? string.Empty;
            if(_text == next) return;

            _text = next;
            _layoutDirty = true;
            InvalidateLayout();
        }
    }
    
    public bool MultiLine
    {
        get => _multiLine;
        set
        {
            if(_multiLine == value) return;
            
            _multiLine = value;
            _layoutDirty = true;
            InvalidateLayout();
        }
    }

    public float FontSize
    {
        get => _fontSize;
        set
        {
            if (Math.Abs(_fontSize - value) < float.Epsilon)
                return;
            
            _fontSize = value;

            _layoutDirty = true;
            InvalidateLayout();
            InvalidateDependencies();
        }
    }
    
    public string FontPath
    {
        get => _fontPath;
        set
        {
            var next = value ?? string.Empty;
            if (_fontPath == next) return;

            _fontPath = next;
            InvalidateDependencies();

            _layoutDirty = true;
            InvalidateLayout();
        }
    }
    
    private int _lastLayoutWidth;
    private readonly System.Collections.Generic.List<string> _lines = [];
    private readonly System.Collections.Generic.List<float> _lineWidths = [];
    private float _lineHeight;
    private float _totalHeight;
    private bool _layoutDirty = true;
    
    public override void Arrange(Rectangle parentBounds)
    {
        base.Arrange(parentBounds);

        if (Font is null)
            return;

        if (_multiLine)
        {
            EnsureLayout();
        }
        else
        {
            _lineHeight = SpriteBatchExtensions.GetLineHeight(Font);
            _totalHeight = _lineHeight;
        }

        var measuredHeight = (int)MathF.Ceiling(_totalHeight);
        if (AutoResizeHeight && Bounds.Height != measuredHeight)
        {
            Height = UiLength.Pixels(measuredHeight);
            base.Arrange(parentBounds);
        }
    }

    public override void ResolveDependencies()
    {
        Font = string.IsNullOrEmpty(_fontPath)
            ? null
            : Resources.LoadSpriteFont(_fontPath, FontSize);
    }

    private void EnsureLayout()
    {
        if (Font is null) return;
        if (!_multiLine) return;

        var width = Bounds.Width;

        if (width <= 0)
        {
            _lines.Clear();
            _lineWidths.Clear();
            _lineHeight = 0;
            _totalHeight = 0;
            return;
        }

        if (!_layoutDirty && width == _lastLayoutWidth)
            return;

        _layoutDirty = false;
        _lastLayoutWidth = width;
        
        _lines.Clear();
        _lines.AddRange(SpriteBatchExtensions.SplitTextIntoLines(Font, Text, Bounds.Width));
        
        _lineWidths.Clear();
        
        foreach (var line in _lines)
        {
            var size = Font.MeasureString(line);
            _lineWidths.Add(size.X);
        }

        _lineHeight = SpriteBatchExtensions.GetLineHeight(Font);
        _totalHeight = _lines.Count * _lineHeight;
    }

    public override void OnDraw()
    {
        base.OnDraw();

        if (Font is null)
            return;

        if (string.IsNullOrEmpty(_text))
            return;

        int xOffset = 0;
        int yOffset = Bounds.Height / 2;

        switch (HorizontalAlignment)
        {
            case HorizontalAlignment.Left:
                xOffset = 0;
                break;
            case HorizontalAlignment.Center:
                xOffset = Bounds.Width / 2;
                break;
            case HorizontalAlignment.Right:
                xOffset = Bounds.Width;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var anchorPos = new Vector2(Bounds.X + xOffset, Bounds.Y + yOffset);

        if (_multiLine)
        {
            EnsureLayout();

            if (_lines.Count == 0)
                return;

            // Vertically center block of text around anchorPos.Y
            var baseY = anchorPos.Y - _totalHeight * 0.5f;

            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                var lineWidth = _lineWidths[i];

                float lineX = anchorPos.X;
                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        // anchorPos is already at left edge
                        break;
                    case HorizontalAlignment.Center:
                        lineX -= lineWidth * 0.5f;
                        break;
                    case HorizontalAlignment.Right:
                        lineX -= lineWidth;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Center each line within its own line-height band
                float lineY = baseY + i * _lineHeight + _lineHeight * 0.5f;

                Graphics.SpriteBatch.DrawString(Font, line, new Vector2(lineX, lineY), TextColor);
            }
        }
        else
        {
            Graphics.SpriteBatch.DrawTextAligned(
                Font,
                _text,
                anchorPos,
                HorizontalAlignment,
                VerticalAlignment.Center,
                TextColor);
        }
    }
    
    public override void Parse(XmlNode node)
    {
        Text = ParseString(node, "text", "");
        FontSize = ParseFloat(node, "font-size", 12.0f);
        FontPath = ParseString(node, "font", "monogram");
        MultiLine = ParseBool(node, "multi-line", MultiLine);
        AutoResizeHeight = ParseBool(
            node,
            "auto-resize-height",
            AutoResizeHeight);
        HorizontalAlignment = ParseHAlignment(ParseString(node, "horizontal-alignment", "Center"));
        TextColor = ParseColor(node,  "text-color");
    }

    private static HorizontalAlignment ParseHAlignment(string value)
    {
        return Enum.TryParse<HorizontalAlignment>(value, true, out var alignment)
            ? alignment
            : HorizontalAlignment.Center;
    }
}
