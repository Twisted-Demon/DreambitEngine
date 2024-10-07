using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public class Box : Polygon
{
    public Vector2 TopLeft => Corners[0];
    public Vector2 TopRight => Corners[1];
    public Vector2 BottomLeft => Corners[2];
    public Vector2 BottomRight => Corners[3];

    private Box() : base(4)
    {
    }
    
    public static Box CreateSquare(Vector2 center, float halfExtent)
    {
        var box = new Box()
        {
            Corners =
            {
                [0] = center + new Vector2(-halfExtent, -halfExtent),
                [1] = center + new Vector2(halfExtent, -halfExtent),
                [2] = center + new Vector2(-halfExtent, halfExtent),
                [3] = center + new Vector2(halfExtent, halfExtent)
            }
        };

        return box;
    }
}