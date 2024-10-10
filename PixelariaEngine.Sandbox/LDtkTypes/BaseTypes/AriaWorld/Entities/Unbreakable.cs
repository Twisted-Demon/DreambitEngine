namespace PixelariaEngine.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Unbreakable : ILDtkEntity
{
    public static Unbreakable Default() => new()
    {
        Identifier = "Unbreakable",
        Uid = 170,
        Size = new Vector2(16f, 16f),
        Pivot = new Vector2(0.5f, 1f),
        Tile = new TilesetRectangle()
        {
            X = 32,
            Y = 1024,
            W = 16,
            H = 32
        },
        SmartColor = new Color(100, 99, 99, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 32,
            Y = 1024,
            W = 16,
            H = 32
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
}
#pragma warning restore
