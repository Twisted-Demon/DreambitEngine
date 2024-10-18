using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public class UIText : UIElement
{
    public string Text { get; set; } = string.Empty;
    public float MaxWidth { get; set; } = int.MaxValue;
    
    private string _fontName;
    public HorizontalAlignment HTextAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VTextAlignment { get; set; } = VerticalAlignment.Center;

    private SpriteFont _spriteFont;
    public string FontName
    {
        get => _fontName;
        set
        {
            if (_fontName == value)
                return;
            _fontName = value;
            _spriteFont = Resources.LoadAsset<SpriteFont>(_fontName);
        }
    }

    public override void OnDrawUI()
    {
        if (_spriteFont == null) return;

        var position = GetScreenPos();
        var size = Canvas.ConvertToScreenSize(new Vector2(MaxWidth, 0));
            
        Core.SpriteBatch.DrawMultiLineText(_spriteFont, Text, position, 
            HTextAlignment, VTextAlignment, Color.White, size.X);
    }

    public static UIText Create(Canvas canvas, string text, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center,
        string fontName = Fonts.Default)
    {
        var labelComponent = canvas.CreateUIElement<UIText>();
        
        labelComponent.FontName = fontName;
        labelComponent.Text = text;
        labelComponent.HTextAlignment = horizontalAlignment;
        
        return labelComponent;
    }

    public override void OnDestroyed()
    {
        _spriteFont = null;
    }
}