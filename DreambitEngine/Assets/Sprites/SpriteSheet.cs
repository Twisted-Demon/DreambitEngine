using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Newtonsoft.Json;

namespace Dreambit;

public class SpriteSheet : DreambitAsset
{
    [JsonIgnore] private static readonly Logger<SpriteSheet> Logger = new();

    [JsonProperty("columns")] public readonly int Columns = 1;

    [JsonProperty("rows")] public readonly int Rows = 1;

    [JsonProperty("sprite")] public Sprite SourceSprite { get; set; }

    private SpriteSheet(int columns, int rows, Sprite sprite)
    {
        Columns = columns;
        Rows = rows;
        SourceSprite = sprite;

        SplitSprite();
    }

    private SpriteSheet(int gridSize, Sprite sprite)
    {
        Columns = sprite.SourceRect.Width / gridSize;
        Rows = sprite.SourceRect.Height / gridSize;
        SourceSprite = sprite;

        SplitSprite();
    }

    public SpriteSheet()
    {
    }

    [JsonIgnore] public Texture2D Texture => SourceSprite?.Texture;

    [JsonIgnore] public string TexturePath => SourceSprite?.TexturePath;

    [JsonIgnore] public Sprite[] Frames { get; private set; } = [];

    [JsonIgnore] public int FrameCount => Frames.Length;

    public Sprite this[int index] => Frames[index];

    public static SpriteSheet Create(
        int columnWidth,
        int rowHeight,
        Sprite sprite)
    {
        ArgumentNullException.ThrowIfNull(sprite);

        return new SpriteSheet(
            sprite.SourceRect.Width / columnWidth,
            sprite.SourceRect.Height / rowHeight,
            sprite);
    }

    public static SpriteSheet Create(int gridSize, Sprite sprite)
    {
        ArgumentNullException.ThrowIfNull(sprite);

        return new SpriteSheet(gridSize, sprite);
    }

    internal void LoadSpriteSheet()
    {
        SplitSprite();
    }

    private void SplitSprite()
    {
        if (Columns < 1 || Rows < 1) return;
        if (SourceSprite?.Texture == null) return;

        var totalFrames = Mathf.MaxInt(1, Columns * Rows);

        Frames = new Sprite[totalFrames];

        switch (Frames.Length)
        {
            case > 1:
            {
                var frameWidth = SourceSprite.SourceRect.Width / Columns;
                var frameHeight = SourceSprite.SourceRect.Height / Rows;

                for (var i = 0; i < Frames.Length; i++)
                {
                    var x = i % Columns;
                    var y = i / Columns;

                    Frames[i] = new Sprite
                    {
                        Texture = Texture,
                        SourceRect = new Rectangle(
                            SourceSprite.SourceRect.X + x * frameWidth,
                            SourceSprite.SourceRect.Y + y * frameHeight,
                            frameWidth,
                            frameHeight),
                        PixelsPerUnit = SourceSprite.PixelsPerUnit
                    };
                }

                break;
            }
            default:
                Frames[0] = new Sprite
                {
                    Texture = Texture,
                    SourceRect = SourceSprite.SourceRect,
                    PixelsPerUnit = SourceSprite.PixelsPerUnit
                };
                break;
        }
    }

    public bool TryGetFrame(int frame, out Sprite sprite)
    {
        try
        {
            sprite = Frames[frame];
            return true;
        }
        catch
        {
            Logger.Warn("Frame out of bounds, unable to get frame using default source rect");
            sprite = new Sprite
            {
                Texture = Texture,
                SourceRect = SourceSprite.SourceRect,
                PixelsPerUnit = SourceSprite.PixelsPerUnit
            };
            return false;
        }
    }

    protected override void CleanUp()
    {
        SourceSprite = null;
        Frames = [];
    }
}
