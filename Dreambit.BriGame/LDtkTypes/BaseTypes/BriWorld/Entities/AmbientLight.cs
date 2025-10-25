namespace Dreambit.BriGame;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class AmbientLight : ILDtkEntity
{
    public static AmbientLight Default() => new()
    {
        Identifier = "AmbientLight",
        Uid = 31,
        Size = new Vector2(16f, 16f),
        Pivot = new Vector2(0.5f, 0.5f),
        Tile = new TilesetRectangle()
        {
            X = 0,
            Y = 112,
            W = 16,
            H = 16
        },
        SmartColor = new Color(228, 166, 114, 255),

        Color = new Color(94, 125, 132, 1),
    };

    public string Identifier { get; set; }
    public System.Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }

    public Color SmartColor { get; set; }

    public Color Color { get; set; }
}
#pragma warning restore
