namespace Dreambit.BriGame;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class PointLight2 : ILDtkEntity
{
    public static PointLight2 Default() => new()
    {
        Identifier = "PointLight2",
        Uid = 34,
        Size = new Vector2(1f, 1f),
        Pivot = new Vector2(0.5f, 0.5f),
        Tile = new TilesetRectangle()
        {
            X = 400,
            Y = 0,
            W = 16,
            H = 16
        },
        SmartColor = new Color(234, 212, 170, 255),

        Intensity = 1f,
        Radius = 100f,
        Color = new Color(251, 235, 215, 1),
    };

    public string Identifier { get; set; }
    public System.Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }

    public Color SmartColor { get; set; }

    public float Intensity { get; set; }
    public float Radius { get; set; }
    public Color Color { get; set; }
}
#pragma warning restore
