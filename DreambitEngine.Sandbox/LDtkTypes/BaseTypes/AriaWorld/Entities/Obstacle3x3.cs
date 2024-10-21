namespace Dreambit.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Obstacle3x3 : ILDtkEntity
{
    public static Obstacle3x3 Default() => new()
    {
        Identifier = "Obstacle3x3",
        Uid = 322,
        Size = new Vector2(96f, 96f),
        Pivot = new Vector2(0.5f, 1f),
        Tile = new TilesetRectangle()
        {
            X = 384,
            Y = 1472,
            W = 96,
            H = 64
        },
        SmartColor = new Color(255, 255, 255, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 384,
            Y = 1472,
            W = 96,
            H = 64
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
