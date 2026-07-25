using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Dreambit;

public class Sprite : DreambitAsset
{
    private string _texturePath;
    
    public Texture2D Texture { get; internal set; }

    [JsonProperty("pixels_per_unit")] public int PixelsPerUnit { get; set; } = 1;

    [JsonProperty("source")]
    [JsonConverter(typeof(RectangleConverter))]
    public Rectangle SourceRect { get; init; }

    [JsonProperty("texture")]
    public string TexturePath
    {
        get => _texturePath;
        set
        {
            _texturePath = value;
            Texture = Resources.LoadAsset<Texture2D>(value);
        }
    }

    public static Sprite Create(string texturePath, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int pixelsPerUnit = 1)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);

        if (texture is null)
            return null;

        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            PixelsPerUnit = pixelsPerUnit
        };
    }

    public static Sprite Create(Texture2D texture, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int pixelsPerUnit = 1)
    {
        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            PixelsPerUnit =  pixelsPerUnit
        };
    }

    public static Sprite Create(Texture2D texture, Rectangle sourceRect, int pixelsPerUnit = 1)
    {
        return new Sprite
        {
            Texture = texture,
            SourceRect = sourceRect,
            PixelsPerUnit =  pixelsPerUnit
        };
    }

    public static Sprite Create(string texturePath, Rectangle sourceRect, int pixelsPerUnit = 1)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);
        if (texture is null) return null;

        return new Sprite
        {
            Texture = texture,
            SourceRect = sourceRect,
            PixelsPerUnit =  pixelsPerUnit
        };
    }

    public static Sprite Create(Texture2D texture, int pixelsPerUnit = 1)
    {
        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(0, 0, texture.Width, texture.Height),
            PixelsPerUnit = pixelsPerUnit
        };
    }

    public static Sprite Create(string texturePath, int pixelsPerUnit = 1)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);
        if (texture is null) return null;

        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(0, 0, texture.Width, texture.Height),
            PixelsPerUnit =   pixelsPerUnit
        };
    }
}