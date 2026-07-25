using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Dreambit;

public class SpriteSheet : DreambitAsset
{
    [JsonIgnore] private static readonly Logger<SpriteSheet> Logger = new();

    [JsonProperty("columns")] public readonly int Columns = 1;

    [JsonProperty("rows")] public readonly int Rows = 1;

    [JsonProperty("texture_path")] private string _texturePath;
    
    [JsonProperty("pixels_per_unit")] public  readonly int PixelsPerUnit = 1;

    private SpriteSheet(int columns, int rows, string texturePath, Texture2D texture, int pixelsPerUnit)
    {
        Columns = columns;
        Rows = rows;
        _texturePath = texturePath;
        Texture = texture;
        PixelsPerUnit = pixelsPerUnit;

        SplitSprite();
    }

    private SpriteSheet(int gridSize, string texturePath, Texture2D texture, int pixelsPerUnit)
    {
        Columns = texture.Width / gridSize;
        Rows = texture.Height / gridSize;
        _texturePath = texturePath;
        Texture = texture;
        PixelsPerUnit = pixelsPerUnit;

        SplitSprite();
    }

    public SpriteSheet()
    {
    }

    [JsonIgnore] public Texture2D Texture { get; private set; }

    [JsonIgnore] public string TexturePath => _texturePath;

    [JsonIgnore] public Sprite[] Frames { get; private set; } = [];

    [JsonIgnore] public int FrameCount => Frames.Length;

    public Sprite this[int index] => Frames[index];

    public static SpriteSheet Create(int columns, int rows, string texturePath, int pixelsPerUnit = 1)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);

        return texture == null ? null : new SpriteSheet(columns, rows, texturePath, texture, pixelsPerUnit);
    }

    public static SpriteSheet Create(int gridSize, string texturePath, int pixelsPerUnit = 1)
    {
        var texture = Resources.LoadAsset<Texture2D>(texturePath);

        return texture == null ? null : new SpriteSheet(gridSize, texturePath, texture, pixelsPerUnit);
    }

    internal void LoadSpriteSheet()
    {
        Texture = Resources.LoadAsset<Texture2D>(_texturePath);
        SplitSprite();
        AssetName = _texturePath;
    }

    private void SplitSprite()
    {
        if (Columns < 1 || Rows < 1) return;
        if (Texture == null) return;

        var totalFrames = Mathf.MaxInt(1, Columns * Rows);

        Frames = new Sprite[totalFrames];

        switch (Frames.Length)
        {
            case > 1:
            {
                var frameWidth = Texture.Width / Columns;
                var frameHeight = Texture.Height / Rows;

                for (var i = 0; i < Frames.Length; i++)
                {
                    var x = i % Columns;
                    var y = i / Columns;

                    Frames[i] = new Sprite
                    {
                        PixelsPerUnit = PixelsPerUnit,
                        Texture = Texture,
                        SourceRect = new Rectangle(x * frameWidth, y * frameHeight, frameWidth, frameHeight)
                    };
                }

                break;
            }
            default:
                Frames[0] = new Sprite
                {
                    PixelsPerUnit = PixelsPerUnit,
                    Texture = Texture,
                    SourceRect = new Rectangle(0, 0, Texture.Width, Texture.Height)
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
                SourceRect = new Rectangle(0, 0, Texture.Width, Texture.Height)
            };
            return false;
        }
    }

    protected override void CleanUp()
    {
        Texture = null;
    }
}