using Microsoft.Xna.Framework;

namespace Dreambit;

public class Box : Shape
{
    private Box() : base(4)
    {
    }

    public Vector2 TopLeft => Polygon.Vertices[0];
    public Vector2 TopRight => Polygon.Vertices[1];
    public Vector2 BottomRight => Polygon.Vertices[2];
    public Vector2 BottomLeft => Polygon.Vertices[3];

    public static Box CreateSquare(Vector2 center, float halfExtent)
    {
        var box = new Box();
        box.Polygon.Vertices = new[]
        {
            center + new Vector2(-halfExtent, -halfExtent),
            center + new Vector2(halfExtent, -halfExtent),
            center + new Vector2(halfExtent, halfExtent),
            center + new Vector2(-halfExtent, halfExtent)
        };

        return box;
    }

    public static Box CreateRectangle(Vector2 center, float halfWidth, float halfHeight)
    {
        var box = new Box();
        box.Polygon.Vertices = new[]
        {
            center + new Vector2(-halfWidth, -halfHeight),
            center + new Vector2(halfWidth, -halfHeight),
            center + new Vector2(halfWidth, halfHeight),
            center + new Vector2(-halfWidth, halfHeight)
        };

        return box;
    }

    public static Box CreateFromVerts(Vector2[] verts)
    {
        var box = new Box();
        box.Polygon.Vertices = new[]
        {
            verts[0],
            verts[1],
            verts[2],
            verts[3]
        };

        return box;
    }
}