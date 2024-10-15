namespace PixelariaEngine.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Prop2x1 : ILDtkEntity
{
    public static Prop2x1 Default() => new()
    {
        Identifier = "Prop2x1",
        Uid = 328,
        Size = new Vector2(64f, 32f),
        Pivot = new Vector2(0.5f, 0.88f),
        Tile = new TilesetRectangle()
        {
            X = 832,
            Y = 160,
            W = 64,
            H = 32
        },
        SmartColor = new Color(255, 255, 255, 255),

        RenderTiles = new TilesetRectangle()
        {
            X = 832,
            Y = 160,
            W = 64,
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
    public bool CanFade { get; set; }
}
#pragma warning restore
