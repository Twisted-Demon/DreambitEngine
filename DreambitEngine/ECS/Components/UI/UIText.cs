using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public class UIText : UIElement
{
    private Logger<UIText> _logger = new();
    
    private string _fontPath;
    

    private SpriteFontBase _spriteFont;
    public SpriteFontBase SpriteFont;
    
    public string Text { get; set; } = string.Empty;
    public float MaxWidth { get; set; } = float.MaxValue;
    public HorizontalAlignment HAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VAlignment { get; set; } = VerticalAlignment.Center;

    public string FontPath
    {
        get => _fontPath;
        set
        {
            if (_fontPath == value)
                return;
            
            _fontPath = value;
            _spriteFont = Resources.LoadSpriteFont(_fontPath, _fontSize);
        }
    }

    private float _fontSize = 12f;

    public float FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value;
            _spriteFont = Resources.LoadSpriteFont(_fontPath, _fontSize);
        }
    }

    public override void OnDrawUI()
    {
        if (_spriteFont == null)
        {
            return;
        }

        var position = GetScreenPos();
        var size = Canvas.ConvertToScreenSize(new Vector2(MaxWidth, 0));

        Core.SpriteBatch.DrawMultiLineText(_spriteFont, Text, position,
            HAlignment, VAlignment, Color.White, size.X);
    }

    public static UIText Create(Canvas canvas, string text,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center,
        string fontName = Fonts.Default, float fontSize = 12f)
    {
        var textComponent = canvas.CreateUIElement<UIText>();

        textComponent._fontSize = fontSize;
        textComponent.FontPath = fontName;
        textComponent.Text = text;
        textComponent.HAlignment = horizontalAlignment;

        return textComponent;
    }

    public override void OnDestroyed()
    {
        _spriteFont = null;
    }
}