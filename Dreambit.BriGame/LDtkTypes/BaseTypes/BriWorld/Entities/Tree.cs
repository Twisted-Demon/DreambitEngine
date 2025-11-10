namespace Dreambit.BriGame;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Tree : ILDtkEntity
{
    public static Tree Default() => new()
    {
        Identifier = "Tree",
        Uid = 52,
        Size = new Vector2(1f, 1f),
        Pivot = new Vector2(0.5f, 0.9f),
        Tile = new TilesetRectangle()
        {
            X = 128,
            Y = 688,
            W = 96,
            H = 144
        },
        SmartColor = new Color(115, 62, 57, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 128,
            Y = 688,
            W = 96,
            H = 144
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
