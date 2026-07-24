using System;
using System.Xml;
using FontStashSharp;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public class UiText : UiElement
{
    public SpriteFontBase Font { get; private set; }
    
    private string _fontPath;
    private bool _multiLine = true;
    private string _text;
    private float _fontSize;

    public bool AutoResizeHeight { get; set; } = true;  
    public Color TextColor { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;
    
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
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
            
            InvalidateLayout();
            InvalidateDependencies();
        }
    }
    
    public string FontPath
    {
        get => _fontPath;
        set
        {
            if (_fontPath == value) return;
            
            _fontPath = value;
            InvalidateDependencies();
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
        EnsureLayout();
    }

    public override void ResolveDependencies()
    {
        if(!string.IsNullOrEmpty(_fontPath))
            Font = Resources.LoadSpriteFont(_fontPath, FontSize);
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
            Height = UiLength.Pixels(0);
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
        
        if(AutoResizeHeight)
            Height = UiLength.Pixels(_totalHeight);
    }

    protected override void OnUpdate()
    {
        if (Font is null)
            return;
        
        EnsureLayout();

        if (_multiLine) return;

        var lineHeight = SpriteBatchExtensions.GetLineHeight(Font);
        
        if(AutoResizeHeight)
            Height = UiLength.Pixels(lineHeight);
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