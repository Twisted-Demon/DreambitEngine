using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>Displays a sprite resource stretched to the element's arranged bounds.</summary>
public class UiTexture : UiElement
{
    private Sprite _sprite;
    private string _spritePath;

    /// <summary>Gets or sets the color multiplied with the rendered sprite.</summary>
    public Color Tint { get; set; } = Color.White;

    /// <summary>Gets or sets the resource path of the sprite to display.</summary>
    public string SpritePath
    {
        get => _spritePath;
        set
        {
            if (_spritePath == value) return;

            _spritePath = value ?? string.Empty;
            InvalidateDependencies();
            InvalidateLayout();
        }
    }

    /// <summary> Gets or sets the sprite to display.</summary>
    public Sprite Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            _spritePath = value is null ? string.Empty : value.AssetName;

            InvalidateLayout(); // no need to invalidate dependencies
        }
    }

    /// <inheritdoc />
    public override void ResolveDependencies()
    {
        _sprite = string.IsNullOrEmpty(SpritePath)
            ? null
            : Resources.LoadAsset<Sprite>(SpritePath);
    }

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        return _sprite is null
            ? Point.Zero
            : new Point(
                _sprite.SourceRect.Width,
                _sprite.SourceRect.Height);
    }

    /// <inheritdoc />
    public override void OnDraw()
    {
        if (_sprite is null) return;

        Graphics.SpriteBatch.Draw(_sprite.Texture, Bounds, _sprite.SourceRect, Tint);
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        SpritePath = UiXmlParser.ParseString(node, "sprite", string.Empty);
        if (node.Attributes?["tint"] is not null)
            Tint = UiXmlParser.ParseColor(node, "tint");
    }
}