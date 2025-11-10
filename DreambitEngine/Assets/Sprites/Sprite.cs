using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Dreambit;

public class Sprite : DreambitAsset
{
    [JsonProperty("texture")]
    public Texture2D Texture { get; init; }
    
    [JsonProperty("source")]
    [JsonConverter(typeof(RectangleConverter))]
    public Rectangle SourceRect { get; init; }

    public static Sprite Create(string texturePath, int sourceX, int sourceY, int sourceWidth, int sourceHeight)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);

        if (texture is null)
            return null;

        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
        };
    }

    public static Sprite Create(Texture2D texture, int sourceX, int sourceY, int sourceWidth, int sourceHeight)
    {
        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
        };
    }

    public static Sprite Create(Texture2D texture, Rectangle sourceRect)
    {
        return new Sprite
        {
            Texture = texture,
            SourceRect = sourceRect,
        };
    }
    
    public static Sprite Create(string texturePath, Rectangle sourceRect)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);
        if (texture is null) return null;
        
        return new Sprite
        {
            Texture = texture,
            SourceRect = sourceRect,
        };
    }

    public static Sprite Create(Texture2D texture)
    {
        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(0, 0, texture.Width, texture.Height),
        };
    }

    public static Sprite Create(string texturePath)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);
        if (texture is null) return null;

        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(0, 0, texture.Width, texture.Height),
        };
    }
}