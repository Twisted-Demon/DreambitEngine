using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
///     Repeats a sprite at its native pixel size and crops the final row and column
///     to remain inside the owning control.
/// </summary>
public sealed class TiledSpriteBrush : UiBrush
{
    private Sprite _sprite;

    /// <summary>Gets or sets the asset path of the sprite used as a tile.</summary>
    public string SpritePath { get; set; } = string.Empty;

    /// <inheritdoc />
    public override Point MinimumSize => _sprite is null
        ? Point.Zero
        : _sprite.SourceRect.Size;

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        SpritePath = UiXmlParser.ParseString(node, "sprite", string.Empty);
        if (string.IsNullOrWhiteSpace(SpritePath))
            throw new XmlException(
                "<TiledSpriteBrush> requires a non-empty sprite attribute.");
    }

    /// <inheritdoc />
    public override void ResolveDependencies()
    {
        _sprite = string.IsNullOrWhiteSpace(SpritePath)
            ? null
            : Resources.LoadAsset<Sprite>(SpritePath);
    }

    /// <inheritdoc />
    public override void Draw(Rectangle bounds, Color tint)
    {
        if (_sprite?.Texture is null || bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var tile = _sprite.SourceRect;
        if (tile.Width <= 0 || tile.Height <= 0)
            return;

        for (var y = 0; y < bounds.Height; y += tile.Height)
        {
            var drawHeight = Math.Min(tile.Height, bounds.Height - y);
            for (var x = 0; x < bounds.Width; x += tile.Width)
            {
                var drawWidth = Math.Min(tile.Width, bounds.Width - x);
                Graphics.SpriteBatch.Draw(
                    _sprite.Texture,
                    new Rectangle(
                        bounds.X + x,
                        bounds.Y + y,
                        drawWidth,
                        drawHeight),
                    new Rectangle(tile.X, tile.Y, drawWidth, drawHeight),
                    tint);
            }
        }
    }
}