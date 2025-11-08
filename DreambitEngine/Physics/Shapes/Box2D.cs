using Microsoft.Xna.Framework;

namespace Dreambit;

public class Box2D : Shape2D
{
    private Box2D() : base(4)
    {
    }

    public Vector2 TopLeft => Polygon2D.Vertices[0];
    public Vector2 TopRight => Polygon2D.Vertices[1];
    public Vector2 BottomRight => Polygon2D.Vertices[2];
    public Vector2 BottomLeft => Polygon2D.Vertices[3];

    public static Box2D CreateSquare(Vector2 center, float halfExtent)
    {
        var box = new Box2D();
        box.Polygon2D.Vertices = new[]
        {
            center + new Vector2(-halfExtent, -halfExtent),
            center + new Vector2(halfExtent, -halfExtent),
            center + new Vector2(halfExtent, halfExtent),
            center + new Vector2(-halfExtent, halfExtent)
        };

        return box;
    }

    public static Box2D CreateRectangle(Vector2 center, float halfWidth, float halfHeight)
    {
        var box = new Box2D();
        box.Polygon2D.Vertices = new[]
        {
            center + new Vector2(-halfWidth, -halfHeight),
            center + new Vector2(halfWidth, -halfHeight),
            center + new Vector2(halfWidth, halfHeight),
            center + new Vector2(-halfWidth, halfHeight)
        };

        return box;
    }

    public static Box2D CreateFromVerts(Vector2[] verts)
    {
        var box = new Box2D();
        box.Polygon2D.Vertices = new[]
        {
            verts[0],
            verts[1],
            verts[2],
            verts[3]
        };

        return box;
    }
}