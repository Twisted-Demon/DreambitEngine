namespace PixelariaEngine.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Tree3x4 : ILDtkEntity
{
    public static Tree3x4 Default() => new()
    {
        Identifier = "Tree3x4",
        Uid = 324,
        Size = new Vector2(96f, 128f),
        Pivot = new Vector2(0.5f, 0.88f),
        Tile = new TilesetRectangle()
        {
            X = 128,
            Y = 704,
            W = 96,
            H = 128
        },
        SmartColor = new Color(255, 255, 255, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 128,
            Y = 704,
            W = 96,
            H = 128
        },
    };

    public string Identifier { get; set; }
    public System.Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }

    public Color SmartColor { get; set; }

    public TilesetRectangle? RenderTiles { get; set; }
    public bool CanFade { get; set; }
}
#pragma warning restore
