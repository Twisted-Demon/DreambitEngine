using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

public struct Polygon
{
    public Vector2[] Vertices;
    public int Length => Vertices.Length;

    public Vector2 this[int key]
    {
        get => Vertices[key];
        set => Vertices[key] = value;
    }

    public Vector2[] GetEdges()
    {
        var edges = new Vector2[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++)
        {
            var current = Vertices[i];
            var next = Vertices[(i + 1) % Vertices.Length];
            edges[i] = next - current;
        }

        return edges;
    }

    public bool IsConcave()
    {
        bool isNegative = false;
        bool isPositive = false;

        for (int i = 0; i < Vertices.Length; i++)
        {
            var p0 = Vertices[i];
            var p1 = Vertices[(i + 1) % Vertices.Length];
            var p2 = Vertices[(i + 2)%Vertices.Length];
            
            var crossProduct = CrossProduct(p0, p1, p2);

            if (crossProduct < 0) isNegative = true;
            if (crossProduct > 0) isPositive = true;

            if (isNegative && isPositive) return true; // we are concave
        }

        return false;
    }

    private float CrossProduct(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        Vector2 d1 = p1 - p0;
        Vector2 d2 = p2 - p1;
        return d1.X * d2.Y - d1.Y * d2.X;
    }
    
    public (float min, float max) ProjectOntoAxis(Vector2 axis)
    {
        var min = Vector2.Dot(Vertices[0], axis);
        var max = min;

        for (var i = 1; i < Vertices.Length; i++)
        {
            float projection = Vector2.Dot(Vertices[i], axis);
            if(projection < min)
                min=projection;
            if(projection > max)
                max = projection;
        }

        return (min, max);
    }
    
    public bool Intersects(Polygon other)
    {
        var axes = new List<Vector2>();
        
        foreach(var edge in GetEdges())
            axes.Add(new Vector2(-edge.Y, edge.X));
        
        foreach(Vector2 edge in other.GetEdges())
            axes.Add(new Vector2(-edge.X, edge.Y));

        foreach (var axis in axes)
        {
            if(axis != Vector2.Zero)
                axis.Normalize();
            
            var(minA, maxA) = ProjectOntoAxis(axis);
            var(minB, maxB) = other.ProjectOntoAxis(axis);

            if (maxA < minB || maxB < minA)
                return false;
        }

        return true;
    }

    public bool IsPointInside(Vector2 point)
    {
        var isInside = false;

        for (int i = 0, j = Length - 1; i < Length; j = i++)
        {
            var vertex1 = Vertices[i];
            var vertex2 = Vertices[j];

            // Check if the point is inside the polygon by casting a ray
            if (vertex1.Y > point.Y != vertex2.Y > point.Y && // Check if the point's Y is between the Y values of the polygon's edge
                point.X < (vertex2.X - vertex1.X) * (point.Y - vertex1.Y) / (vertex2.Y - vertex1.Y) + vertex1.X) // Check if the point is to the left of the edge
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }

    public bool RayIntersects(Vector2 rayStart, Vector2 rayEnd, out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        for (var i = 0; i < Vertices.Length; i++)
        {
            var current = Vertices[i];
            var next = Vertices[(i + 1) % Vertices.Length];

            if (LineSegmentsIntersect(rayStart, rayEnd, current, next, out intersection))
                return true;
            
        }
        return false;
    }

    public Polygon Transform(Transform transform)
    {
        var polygon = new Polygon
        {
            Vertices = new Vector2[Length]
        };

        for (var i = 0; i < Length; i++)
        {
            var point3D = new Vector3(Vertices[i], 0);
            var transformedPoint = Vector3.Transform(point3D, transform.GetTransformationMatrix());
            
            polygon.Vertices[i] = new Vector2(transformedPoint.X, transformedPoint.Y);
        }

        return polygon;
    }

    public Polygon TransformWithDesiredPos(Transform transform, Vector3 desiredPos)
    {
        var polygon = new Polygon
        {
            Vertices = new Vector2[Length]
        };

        var translationMatrix = transform.GetTransformationMatrix() * Matrix.CreateTranslation(desiredPos);

        for (var i = 0; i < Length; i++)
        {
            var point3D = new Vector3(Vertices[i], 0);
            var transformedPoint = Vector3.Transform(point3D, translationMatrix);
            
            polygon.Vertices[i] = new Vector2(transformedPoint.X, transformedPoint.Y);
        }

        return polygon;
    }

    private bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.Zero;
        
        var a1 = p2.Y - p1.Y;
        var b1 = p1.X - p2.X;
        var c1 = a1 * p1.X + b1 * p1.Y;

        var a2 = p4.Y - p3.Y;
        var b2 = p3.X - p4.X;
        var c2 = a2 * p3.X + b2 * p3.Y;

        var denominator = a1 * b2 - a2 * b1;

        if (denominator == 0)
            return false;
        
        var intersectX = (b2 * c1 - b1 * c2) / denominator;
        var intersectY = (a1 * c2 - a2 * c1) / denominator;
        
        intersection = new Vector2(intersectX, intersectY);
        
        return IsPointOnLineSegment(p1, p2, intersection) && IsPointOnLineSegment(p3, p4, intersection);
    }

    private bool IsPointOnLineSegment(Vector2 p1, Vector2 p2, Vector2 point)
    {
        return (point.X >= Math.Min(p1.X, p2.X) && point.X <= Math.Max(p1.X, p2.X)) &&
               (point.Y >= Math.Min(p1.Y, p2.Y) && point.Y <= Math.Max(p1.Y, p2.Y));
    }

    public List<Polygon> SplitPolygon(Polygon polygon)
    {
        var triangles = new List<Polygon>();

        var remainingVertices = new List<Vector2>(polygon.Vertices);

        while (remainingVertices.Count > 3)
        {
            for (var i = 0; i < remainingVertices.Count; i++)
            {
                var prev = remainingVertices[(i - 1 + remainingVertices.Count) % remainingVertices.Count];
                var current = remainingVertices[i];
                var next = remainingVertices[(i + 1) % remainingVertices.Count];

                if (IsEar(prev, current, next, remainingVertices))
                {
                    triangles.Add(new Polygon
                    {
                        Vertices = new []
                        {
                            prev, current, next
                        }
                    });
                    remainingVertices.RemoveAt(i);
                    break;
                }
            }
        }
        
        triangles.Add(new Polygon
        {
            Vertices = remainingVertices.ToArray()
        });
        return triangles;
    }
    
    private bool IsEar(Vector2 prev, Vector2 current, Vector2 next, List<Vector2> polygon)
    {
        // Check if the current triangle is convex and no other points are inside it
        if (CrossProduct(prev, current, next) >= 0)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                if (polygon[i] != prev && polygon[i] != current && polygon[i] != next)
                {
                    if (IsPointInTriangle(polygon[i], prev, current, next))
                        return false;
                }
            }
            return true;
        }
        return false;
    }
    
    private bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        // Compute the sign of the areas formed by the triangle's edges and the point
        bool hasSameSignABP = Sign(point, a, b) > 0;
        bool hasSameSignBCP = Sign(point, b, c) > 0;
        bool hasSameSignCAP = Sign(point, c, a) > 0;

        // If all the signs are the same, the point is inside the triangle
        return hasSameSignABP == hasSameSignBCP && hasSameSignBCP == hasSameSignCAP;
    }
    
    private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }
}