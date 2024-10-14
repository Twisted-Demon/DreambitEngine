using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public static class IntExtensions
{
    public static Point IndexToPoint(this int i, int width, int height)
    {
        var pos = new Point();

        pos.X = i % width;
        pos.Y = i / width;

        return pos;
    }
}