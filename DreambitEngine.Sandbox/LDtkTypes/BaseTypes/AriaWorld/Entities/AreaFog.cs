namespace Dreambit.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class AreaFog : ILDtkEntity
{
    public static AreaFog Default() => new()
    {
        Identifier = "AreaFog",
        Uid = 438,
        Size = new Vector2(32f, 32f),
        Pivot = new Vector2(0f, 0f),
        Tile = new TilesetRectangle()
        {
            X = 384,
            Y = 224,
            W = 16,
            H = 16
        },
        SmartColor = new Color(159, 159, 159, 255),
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
