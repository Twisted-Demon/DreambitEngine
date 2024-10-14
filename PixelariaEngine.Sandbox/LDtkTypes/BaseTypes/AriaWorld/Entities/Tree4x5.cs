namespace PixelariaEngine.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Tree4x5 : ILDtkEntity
{
    public static Tree4x5 Default() => new()
    {
        Identifier = "Tree4x5",
        Uid = 4,
        Size = new Vector2(64f, 96f),
        Pivot = new Vector2(0.415f, 0.89f),
        Tile = new TilesetRectangle()
        {
            X = 0,
            Y = 672,
            W = 128,
            H = 160
        },
        SmartColor = new Color(255, 255, 255, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 576,
            Y = 672,
            W = 128,
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
    public bool CanFade { get; set; }
}
#pragma warning restore
