using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

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

    public bool Intersects(Shape other)
    {
        return Polygon.Intersects(other.Polygon);
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