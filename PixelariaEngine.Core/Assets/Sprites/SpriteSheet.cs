using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using PixelariaEngine.Graphics;

namespace PixelariaEngine;

public class SpriteSheet : PixelariaAsset
{
    [JsonIgnore] private static readonly Logger<SpriteSheet> Logger = new();

    [JsonProperty("columns")] public readonly int Columns = 1;

    [JsonProperty("rows")] public readonly int Rows = 1;

    [JsonProperty("texture_path")] private string _texturePath;

    private SpriteSheet(int columns, int rows, string texturePath, Texture2D texture)
    {
        Columns = columns;
        Rows = rows;
        _texturePath = texturePath;
        Texture = texture;

        SplitSprite();
    }

    private SpriteSheet(int gridSize, string texturePath, Texture2D texture)
    {
        Columns = texture.Width / gridSize;
        Rows = texture.Height / gridSize;
        _texturePath = texturePath;
        Texture = texture;

        SplitSprite();
    }


    public SpriteSheet()
    {
    }

    [JsonIgnore] public Texture2D Texture { get; private set; }

    [JsonIgnore] public string TexturePath => _texturePath;

    [JsonIgnore] public Rectangle[] Frames { get; private set; } = [];

    [JsonIgnore] public int FrameCount => Frames.Length;

    public static SpriteSheet Create(int columns, int rows, string texturePath)
    {
        var texture = Resources.Load<Texture2D>(texturePath);

        return texture == null ? null : new SpriteSheet(columns, rows, texturePath, texture);
    }

    public static SpriteSheet Create(int gridSize, string texturePath)
    {
        var texture = Resources.Load<Texture2D>(texturePath);

        return texture == null ? null : new SpriteSheet(gridSize, texturePath, texture);
    }

    public void SplitSprite()
    {
        if (Columns < 1 || Rows < 1) return;
        if (Texture == null) return;

        var totalFrames = Columns * Rows;

        Frames = new Rectangle[totalFrames];

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

                    Frames[i] = new Rectangle(x * frameWidth, y * frameHeight, frameWidth, frameHeight);
                }

                break;
            }
            case 1:
                Frames[0] = new Rectangle(0, 0, Texture.Width, Texture.Height);
                break;
            default:
                Frames = new Rectangle[1];
                Frames[0] = new Rectangle(0, 0, Texture.Width, Texture.Height);
                break;
        }
    }

    public bool TryGetFrame(int frame, out Rectangle frameRect)
    {
        try
        {
            frameRect = Frames[frame];
            return true;
        }
        catch
        {
            Logger.Warn("Frame out of bounds, unable to get frame using default source rect");
            frameRect = new Rectangle(0, 0, Texture.Width, Texture.Height);
            return false;
        }
    }

    protected override void CleanUp()
    {
        Texture = null;
    }
}