using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Dreambit;

public class Sprite : DreambitAsset
{
    private string _texturePath;
    
    public Texture2D Texture { get; internal set; }

    [JsonProperty("source")]
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

    public static Sprite Create(string texturePath, int sourceX, int sourceY, int sourceWidth, int sourceHeight)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);

        if (texture is null)
            return null;

        var sprite = new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
        };
        
        sprite.AssetName = $"sprites/{texturePath}";
        Resources.TryRegisterAsset(sprite);
        return sprite;
    }

    public static Sprite Create(Texture2D texture, int sourceX, int sourceY, int sourceWidth, int sourceHeight)
    {
        var sprite = new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
        };
        
        sprite.AssetName = $"sprites/{texture.Name}";
        Resources.TryRegisterAsset(sprite);
        return sprite;
    }

    public static Sprite Create(Texture2D texture, Rectangle sourceRect)
    {
        var sprite = new Sprite
        {
            Texture = texture,
            SourceRect = sourceRect,
        };
        
        sprite.AssetName = $"sprites/{texture.Name}";
        Resources.TryRegisterAsset(sprite);
        return sprite;
    }

    public static Sprite Create(string texturePath, Rectangle sourceRect)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);
        if (texture is null) return null;

        var sprite = new Sprite
        {
            Texture = texture,
            SourceRect = sourceRect,
        };
        
        sprite.AssetName = $"sprites/{texturePath}";
        Resources.TryRegisterAsset(sprite);
        return sprite;
    }

    public static Sprite Create(Texture2D texture)
    {
        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(0, 0, texture.Width, texture.Height)
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