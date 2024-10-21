namespace Dreambit.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Managers : ILDtkEntity
{
    public static Managers Default() => new()
    {
        Identifier = "Managers",
        Uid = 191,
        Size = new Vector2(16f, 16f),
        Pivot = new Vector2(0.5f, 0.5f),
        Tile = new TilesetRectangle()
        {
            X = 32,
            Y = 32,
            W = 16,
            H = 16
        },
        SmartColor = new Color(215, 118, 67, 255),
    };

    public string Identifier { get; set; }
    public System.Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }

    public Color SmartColor { get; set; }
}
#pragma warning restore
