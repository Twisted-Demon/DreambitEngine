using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.UI;

public class UiTexture : UiElement
{
    private string _spritePath;
    private Sprite _sprite;
    
    public Color Tint { get; set; } = Color.White;

    public string SpritePath
    {
        get => _spritePath;
        set
        {
            if(_spritePath == value) return;

            _spritePath = value ?? string.Empty;
            InvalidateDependencies();
            InvalidateLayout();
        }
    }

    public override void ResolveDependencies()
    {
        _sprite = string.IsNullOrEmpty(SpritePath)
            ? null
            : Resources.LoadAsset<Sprite>(SpritePath);
    }

    public override void OnDraw()
    {
        if (_sprite is null) return;

        Graphics.SpriteBatch.Draw(_sprite.Texture, Bounds, _sprite.SourceRect, Tint);
    }

    public override void Parse(XmlNode node)
    {
        SpritePath = ParseString(node, "sprite", string.Empty);
        Tint = ParseColor(node, "tint");
    }
}
