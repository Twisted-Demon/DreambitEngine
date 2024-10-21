namespace Dreambit.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Preloader : ILDtkEntity
{
    public static Preloader Default() => new()
    {
        Identifier = "Preloader",
        Uid = 448,
        Size = new Vector2(32f, 32f),
        Pivot = new Vector2(0.5f, 0.5f),
        Tile = new TilesetRectangle()
        {
            X = 48,
            Y = 32,
            W = 16,
            H = 16
        },
        SmartColor = new Color(255, 255, 255, 255),

    };

    public string Identifier { get; set; }
    public System.Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }

    public Color SmartColor { get; set; }

    public string[] Textures { get; set; }
    public string[] Audios { get; set; }
    public string[] Scripts { get; set; }
}
#pragma warning restore
