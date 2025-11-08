using System.Numerics;

namespace Dreambit;

public struct AABB
{
    public Vector2 Min;
    public Vector2 Max;
    
    public bool Intersects(AABB other) =>
        !(Max.X < other.Min.X || Min.X > other.Max.X || Max.Y < other.Min.Y || Min.Y > other.Max.Y);
    public bool ContainsPoint(Vector2 p) =>
        p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y;
}

public static class PolygonExt
{
    public static AABB ComputeAABB(this Polygon2D p)
    {
        var minX = float.PositiveInfinity; var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity; var maxY = float.NegativeInfinity;
        var verts = p.Vertices; // already world-space when you call GetTransformedPolygon()
        for (int i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            if (v.X < minX) minX = v.X; if (v.Y < minY) minY = v.Y;
            if (v.X > maxX) maxX = v.X; if (v.Y > maxY) maxY = v.Y;
        }
        return new AABB { Min = new Vector2(minX, minY), Max = new Vector2(maxX, maxY) };
    }
}