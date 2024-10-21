using Microsoft.Xna.Framework;

namespace Dreambit;

public struct Ray2D
{
    public Ray2D(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    public Vector2 Start { get; private set; }
    public Vector2 End { get; private set; }
}