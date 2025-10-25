using Microsoft.Xna.Framework;

namespace Dreambit;

public class PolyShape : Shape
{
    protected PolyShape(Vector2[] points) : base(points.Length)
    {
        Polygon.Vertices = points;
        Polygon.CleanAndNormalize();
    }

    public static PolyShape Create(Vector2[] points)
    {
        return new PolyShape(points);
    }
}