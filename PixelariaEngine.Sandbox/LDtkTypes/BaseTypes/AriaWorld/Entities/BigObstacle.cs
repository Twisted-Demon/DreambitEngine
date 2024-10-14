namespace PixelariaEngine.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class BigObstacle : ILDtkEntity
{
    public static BigObstacle Default() => new()
    {
        Identifier = "BigObstacle",
        Uid = 195,
        Size = new Vector2(32f, 32f),
        Pivot = new Vector2(0.5f, 0.8f),
        Tile = new TilesetRectangle()
        {
            X = 160,
            Y = 288,
            W = 32,
            H = 32
        },
        SmartColor = new Color(255, 255, 255, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 160,
            Y = 288,
            W = 32,
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
