using System.Xml;
using FontStashSharp;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public class UiText : UiElement
{
    public SpriteFontBase Font { get; private set; }
    
    public string Text { get; set; }
    public Color Color { get; set; }
    
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
        if(Font is not null)
            Height = UiLength.Pixels(Font.LineHeight);
    }

    public override void Draw()
    {
        base.Draw();
        
        if (Font is null) return;
        var windowSize = Window.ScreenSize;
        var pos = new Vector2(Bounds.X, Bounds.Y);
        Graphics.SpriteBatch.DrawMultiLineText(Font, Text, pos, Color);
    }


    public override void Parse(XmlNode node)
    {
        FontSize = UiLoader.GetFloat(node, "fontSize", 12.0f);
        FontPath = UiLoader.GetString(node, "font", "Fonts/monogram");
        Text = UiLoader.GetString(node, "text", "");
        Color = Color.White;
    }
}