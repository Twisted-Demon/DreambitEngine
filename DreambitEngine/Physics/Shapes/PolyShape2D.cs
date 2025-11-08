using Microsoft.Xna.Framework;

namespace Dreambit;

public class PolyShape2D : Shape2D
{
    protected PolyShape2D(Vector2[] points) : base(points.Length)
    {
        Polygon2D.Vertices = points;
        //Polygon2D.CleanAndNormalize();
    }

    public static PolyShape2D Create(Vector2[] points)
    {
        return new PolyShape2D(points);
    }
}