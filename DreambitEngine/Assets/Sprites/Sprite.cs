using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Newtonsoft.Json;

namespace Dreambit;

public class Sprite : DreambitAsset
{
    private const float MinimumPixelsPerUnit = 0.0001f;

    private float _pixelsPerUnit = 1f;
    private string _texturePath = string.Empty;

    [JsonIgnore] public Texture2D Texture { get; internal set; }

    [JsonProperty("source")] public Rectangle SourceRect { get; init; }

    [JsonProperty("pixels_per_unit")]
    public float PixelsPerUnit
    {
        get => _pixelsPerUnit;
        init
        {
            if (!float.IsFinite(value) || value < MinimumPixelsPerUnit)
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Pixels per unit must be finite and at least {MinimumPixelsPerUnit}.");

            _pixelsPerUnit = value;
        }
    }

    [JsonProperty("texture")]
    public string TexturePath
    {
        get => _texturePath;
        set
        {
            _texturePath = value ?? string.Empty;
            Texture = string.IsNullOrWhiteSpace(_texturePath)
                ? null
                : Resources.LoadAsset<Texture2D>(_texturePath);
        }
    }

    public static Sprite Create(
        string texturePath,
        int sourceX,
        int sourceY,
        int sourceWidth,
        int sourceHeight,
        float pixelsPerUnit = 1f)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);

        if (texture is null)
            return null;

        var sprite = new Sprite
        {
            Texture = texture,
            _texturePath = texturePath,
            SourceRect = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            PixelsPerUnit = pixelsPerUnit
        };

        sprite.AssetName = $"sprites/{texturePath}";
        Resources.TryRegisterAsset(sprite);
        return sprite;
    }

    public static Sprite Create(
        Texture2D texture,
        int sourceX,
        int sourceY,
        int sourceWidth,
        int sourceHeight,
        float pixelsPerUnit = 1f)
    {
        var sprite = new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            PixelsPerUnit = pixelsPerUnit
        };

        sprite.AssetName = $"sprites/{texture.Name}";
        Resources.TryRegisterAsset(sprite);
        return sprite;
    }

    public static Sprite Create(
        Texture2D texture,
        Rectangle sourceRect,
        float pixelsPerUnit = 1f)
    {
        var sprite = new Sprite
        {
            Texture = texture,
            SourceRect = sourceRect,
            PixelsPerUnit = pixelsPerUnit
        };

        sprite.AssetName = $"sprites/{texture.Name}";
        Resources.TryRegisterAsset(sprite);
        return sprite;
    }

    public static Sprite Create(
        string texturePath,
        Rectangle sourceRect,
        float pixelsPerUnit = 1f)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);
        if (texture is null) return null;

        var sprite = new Sprite
        {
            Texture = texture,
            _texturePath = texturePath,
            SourceRect = sourceRect,
            PixelsPerUnit = pixelsPerUnit
        };

        sprite.AssetName = $"sprites/{texturePath}";
        Resources.TryRegisterAsset(sprite);
        return sprite;
    }

    public static Sprite Create(
        Texture2D texture,
        float pixelsPerUnit = 1f)
    {
        return new Sprite
        {
            Texture = texture,
            SourceRect = new Rectangle(0, 0, texture.Width, texture.Height),
            PixelsPerUnit = pixelsPerUnit
        };
    }

    public static Sprite Create(
        string texturePath,
        float pixelsPerUnit = 1f)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);
        if (texture is null) return null;

        return new Sprite
        {
            Texture = texture,
            _texturePath = texturePath,
            SourceRect = new Rectangle(0, 0, texture.Width, texture.Height),
            PixelsPerUnit = pixelsPerUnit
        };
    }
}
