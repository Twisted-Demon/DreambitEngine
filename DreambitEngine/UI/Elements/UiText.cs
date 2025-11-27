using System;
using System.Xml;
using FontStashSharp;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public class UiText : UiElement
{
    public SpriteFontBase Font { get; private set; }
    
    public string Text { get; set; }
    public Color Color { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;
    
    private float _fontSize;
    public float FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value;
            IsDirty = true;
        }
    }
    
    private string _fontPath;
    public string FontPath
    {
        get => _fontPath;
        set
        {
            _fontPath = value;
            IsDirty = true;
        }
    }

    public override void ResolveDependencies()
    {
        if(!string.IsNullOrEmpty(_fontPath))
            Font = Resources.LoadSpriteFont(_fontPath, _fontSize);

        
        
        IsDirty = false;
    }

    public override void OnUpdate()
    {
        if (Font is null) return;
        
        var lineCount = SpriteBatchExtensions.SplitTextIntoLines(Font, Text, Bounds.Width).Count;
        Height = UiLength.Pixels(Font.LineHeight * lineCount);
    }

    public override void Draw()
    {
        base.Draw();
        
        if (Font is null) return;
        var windowSize = Window.ScreenSize;
        
        int xOffset = 0;
        int yOffset = Bounds.Height / 2;
        switch(HorizontalAlignment)
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

        var pos = new Vector2(Bounds.X + xOffset, Bounds.Y + yOffset);
        Graphics.SpriteBatch.DrawMultiLineText(Font, Text, pos, Color, HorizontalAlignment, maxWidth: Bounds.Width);
    }
    


    public override void Parse(XmlNode node)
    {
        FontSize = UiLoader.GetFloat(node, "font-size", 12.0f);
        FontPath = UiLoader.GetString(node, "font", "Fonts/monogram");
        Text = UiLoader.GetString(node, "text", "");
        HorizontalAlignment = ParseHAlignment(UiLoader.GetString(node, "horizontal-alignment", "Center"));
        Color = UiLoader.GetColor(node,  "color");
    }

    private static HorizontalAlignment ParseHAlignment(string value)
    {
        return Enum.TryParse<HorizontalAlignment>(value, true, out var alignment)
            ? alignment
            : HorizontalAlignment.Center;
    }
}