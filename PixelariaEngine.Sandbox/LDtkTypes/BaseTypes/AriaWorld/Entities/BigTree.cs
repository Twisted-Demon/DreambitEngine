namespace PixelariaEngine.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class BigTree : ILDtkEntity
{
    public static BigTree Default() => new()
    {
        Identifier = "BigTree",
        Uid = 152,
        Size = new Vector2(160f, 160f),
        Pivot = new Vector2(0.5f, 0.925f),
        Tile = new TilesetRectangle()
        {
            X = 32,
            Y = 2304,
            W = 160,
            H = 160
        },
        SmartColor = new Color(255, 255, 255, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 32,
            Y = 2304,
            W = 160,
            H = 160
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
