using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Newtonsoft.Json;

namespace Dreambit;

public class SpriteSheet : DreambitAsset
{
    [JsonIgnore] private static readonly Logger<SpriteSheet> Logger = new();
    private int _columns = 1;
    private int _rows = 1;
    private Sprite _sourceSprite;

    [JsonProperty("columns")]
    public int Columns
    {
        get => _columns;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Sprite sheet columns must be at least 1.");
            _columns = value;
            SplitSprite();
        }
    }

    [JsonProperty("rows")]
    public int Rows
    {
        get => _rows;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Sprite sheet rows must be at least 1.");
            _rows = value;
            SplitSprite();
        }
    }

    [JsonProperty("sprite")]
    public Sprite SourceSprite
    {
        get => _sourceSprite;
        set
        {
            _sourceSprite = value;
            SplitSprite();
        }
    }

    private SpriteSheet(int columns, int rows, Sprite sprite)
    {
        _columns = Math.Max(1, columns);
        _rows = Math.Max(1, rows);
        _sourceSprite = sprite;

        SplitSprite();
    }

    private SpriteSheet(int gridSize, Sprite sprite)
    {
        _columns = Math.Max(1, sprite.SourceRect.Width / gridSize);
        _rows = Math.Max(1, sprite.SourceRect.Height / gridSize);
        _sourceSprite = sprite;

        SplitSprite();
    }

    public SpriteSheet()
    {
    }

    [JsonIgnore] public Texture2D Texture => SourceSprite?.Texture;

    [JsonIgnore] public TextureAsset TextureAsset => SourceSprite?.TextureAsset;

    [Obsolete("Use TextureAsset. TexturePath is retained for source compatibility only.")]
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
                        TextureAsset = TextureAsset,
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
                    TextureAsset = TextureAsset,
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
                TextureAsset = TextureAsset,
                SourceRect = SourceSprite.SourceRect,
                PixelsPerUnit = SourceSprite.PixelsPerUnit
            };
            return false;
        }
    }

    protected override void CleanUp()
    {
        _sourceSprite = null;
        Frames = [];
    }
}
