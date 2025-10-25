using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit;

public class Shape
{
    protected Polygon Polygon;

    protected Shape(int pointsCount)
    {
        Polygon.Vertices = new Vector2[pointsCount];
    }

    public Vector2[] GetVertices()
    {
        return Polygon.Vertices;
    }
    
    public bool Intersects(Shape other, out Vector2 mtvAxis, out float mtvDepth)
    {
        return Polygon.IntersectsGeneral(other.Polygon, out mtvAxis, out mtvDepth);
    }

    public bool Intersects(Shape other)
    {
        return Polygon.IntersectsGeneral(other.Polygon, out _, out _);
    }

    public Polygon TransformPolygon(Transform transform)
    {
        return Polygon.Transform(transform);
    }

    public Polygon TransformWithDesiredPos(Transform transform, Vector3 desiredPos)
    {
        return Polygon.TransformWithDesiredPos(transform, desiredPos);
    }
}