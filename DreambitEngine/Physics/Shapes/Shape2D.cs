using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit;

public class Shape2D
{
    protected Polygon2D Polygon2D;

    protected Shape2D(int pointsCount)
    {
        Polygon2D.Vertices = new Vector2[pointsCount];
        Polygon2D.CleanAndNormalize();
    }

    public Vector2[] GetVertices()
    {
        return Polygon2D.Vertices;
    }

    public bool Intersects(Shape2D other, out Vector2 mtvAxis, out float mtvDepth)
    {
        return Polygon2D.IntersectsGeneral(other.Polygon2D, out mtvAxis, out mtvDepth);
    }

    public bool Intersects(Shape2D other)
    {
        return Polygon2D.IntersectsGeneral(other.Polygon2D, out _, out _);
    }

    public Polygon2D TransformPolygon(Transform transform)
    {
        return Polygon2D.Transform(transform);
    }

    public Polygon2D TransformWithDesiredPos(Transform transform, Vector3 desiredPos)
    {
        return Polygon2D.TransformWithDesiredPos(transform, desiredPos);
    }

    public Polygon2D GetPolygon() => Polygon2D;
}