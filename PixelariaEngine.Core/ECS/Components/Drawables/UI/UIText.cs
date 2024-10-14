using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public class UIText : UIElement
{
    public string Text { get; set; } = string.Empty;
    public float MaxWidth { get; set; } = float.MaxValue;
    public PivotType Pivot { get; set; } = PivotType.Center;

    private string _fontName = string.Empty;
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
        var position = GetScreenPos();
        
        Core.SpriteBatch.DrawMultiLineText(_spriteFont, Text, position, 
            HTextAlignment, VTextAlignment, Color.White, MaxWidth);
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