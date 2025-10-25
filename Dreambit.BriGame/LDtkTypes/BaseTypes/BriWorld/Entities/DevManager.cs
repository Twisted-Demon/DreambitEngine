namespace Dreambit.BriGame;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class DevManager : ILDtkEntity
{
    public static DevManager Default() => new()
    {
        Identifier = "DevManager",
        Uid = 5,
        Size = new Vector2(1f, 1f),
        Pivot = new Vector2(0.5f, 0.5f),
        Tile = new TilesetRectangle()
        {
            X = 128,
            Y = 64,
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
