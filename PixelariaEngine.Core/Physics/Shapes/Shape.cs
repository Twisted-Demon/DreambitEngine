using System.Collections.Generic;
using Microsoft.Xna.Framework;

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

    public Polygon TransformPolygon(Matrix transformationMatrix)
    {
        return Polygon.Transform(transformationMatrix);
    }
}