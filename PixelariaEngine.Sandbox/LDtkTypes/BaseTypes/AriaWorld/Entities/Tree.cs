namespace PixelariaEngine.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Tree: ILDtkEntity
{
    public static readonly Tree Default = new()
    {
        Identifier = "Tree",
        Uid = 4,
        Size = new Vector2(64f, 96f),
        Pivot = new Vector2(0.484f, 0.875f),
        Tile = new TilesetRectangle()
        {
            X = 320,
            Y = 2096,
            W = 64,
            H = 96
        },
        SmartColor = new Color(144, 229, 138, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 64,
            Y = 2096,
            W = 64,
            H = 96
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
