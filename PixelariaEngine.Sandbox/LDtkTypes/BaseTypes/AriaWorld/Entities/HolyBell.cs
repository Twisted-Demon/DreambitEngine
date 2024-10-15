namespace PixelariaEngine.Sandbox;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class HolyBell : ILDtkEntity
{
    public static HolyBell Default() => new()
    {
        Identifier = "HolyBell",
        Uid = 437,
        Size = new Vector2(32f, 32f),
        Pivot = new Vector2(0.5f, 0.5f),
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
}
#pragma warning restore
