namespace Dreambit.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Wall : ILDtkEntity
{
    public static Wall Default() => new()
    {
        Identifier = "Wall",
        Uid = 193,
        Size = new Vector2(1f, 1f),
        Pivot = new Vector2(0.5f, 0.5f),
        Tile = new TilesetRectangle()
        {
            X = 368,
            Y = 32,
            W = 16,
            H = 16
        },
        SmartColor = new Color(234, 212, 170, 255),

    };

    public string Identifier { get; set; }
    public System.Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }

    public Color SmartColor { get; set; }

    public Point[]? Vertices { get; set; }
}
#pragma warning restore
