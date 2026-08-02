using Microsoft.Xna.Framework;
using System.Xml;

namespace Dreambit.UI;

public sealed class SpriteBrush : UiBrush
{
    private Sprite _sprite;

    public string SpritePath { get; set; }

    public SpriteBrush()
        : this(string.Empty)
    {
    }

    public SpriteBrush(string spritePath)
    {
        SpritePath = spritePath ?? string.Empty;
    }

    public override void Parse(XmlNode node)
    {
        var spritePath = UiXmlParser.ParseString(
            node,
            "sprite",
            string.Empty);
        if (string.IsNullOrWhiteSpace(spritePath))
        {
            throw new XmlException(
                "<SpriteBrush> requires a non-empty sprite attribute.");
        }

        SpritePath = spritePath;
    }

    public override Point MinimumSize => _sprite is null
        ? Point.Zero
        : _sprite.SourceRect.Size;

    public override void ResolveDependencies()
    {
        _sprite = string.IsNullOrEmpty(SpritePath)
            ? null
            : Resources.LoadAsset<Sprite>(SpritePath);
    }

    public override void Draw(Rectangle bounds, Color tint)
    {
        if (_sprite is null)
            return;

        Graphics.SpriteBatch.Draw(
            _sprite.Texture,
            bounds,
            _sprite.SourceRect,
            tint);
    }
}
