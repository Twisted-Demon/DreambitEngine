using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public class UiButton : UiText
{
    private string _spritePath;
    private Sprite _sprite;
    private bool _fitToTexture = false;

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
    
    public UiButton()
    {
        AutoResizeHeight = false;
        MultiLine = false;
    }
    
    public override void Arrange(Rectangle parentBounds)
    {
        if (!_fitToTexture) return;

        if (_sprite?.Texture is null) return;
        
        var tex = _sprite.Texture;
        
        Width = UiLength.Pixels(_sprite.SourceRect.Width);
        Height = UiLength.Pixels(_sprite.SourceRect.Height);
        
        base.Arrange(parentBounds);
    }

    public override void ResolveDependencies()
    {
        base.ResolveDependencies();
        
        if(!string.IsNullOrEmpty(_spritePath))
            _sprite = Resources.LoadAsset<Sprite>(_spritePath);
    }

    public override void OnDraw()
    {
        if(_sprite is null) return;
        Graphics.SpriteBatch.Draw(_sprite.Texture, Bounds, _sprite.SourceRect, TextColor);
        
        base.OnDraw();
    }

    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        
        SpritePath = ParseString(node, "sprite", "");
        _fitToTexture = ParseBool(node, "fit-to-texture", false);
    }
}